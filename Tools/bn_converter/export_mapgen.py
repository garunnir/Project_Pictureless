"""
BN house mapgen → Dist MapSaveJsonDto (OccupiedCell / floorFaces / liquidAuthoringFaces).

사용법:
    python export_mapgen.py --bn-path "Z:/Work/Project/Cataclysm-BN" --output "../../Assets/StreamingAssets/BNData/mapgen"
    python export_mapgen.py --bn-path "Z:/Work/Project/Cataclysm-BN" --output "../../Assets/StreamingAssets/BNData/mapgen" --om-terrain house_modern_1

Whitelist only. Does not dump BN rows, loot, monsters, vehicles, or nested chunks.
BN furniture has no facing — Dist identity yaw is left unset (TileSaveData has no yaw).

CC BY-SA 3.0 — derived from Cataclysm: Bright Nights.
"""

from __future__ import annotations

import argparse
import json
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any

from convert import (
    LICENSE,
    SOURCE,
    load_all_json,
    load_furniture_and_terrain,
    resolve_copy_from_entries,
)

SCHEMA_VERSION = 1
GRID_CELL_SIZE = 1.0
FLOOR_FACE_POS_Y = 1
TILE_TYPE_OCCUPIED = 0

PREFAB_FLOOR = "Floor/Floor"
PREFAB_GRASS = "Floor/GrassFloor"
PREFAB_SHALLOW = "Floor/ShallowWater"
PREFAB_DEEP = "Floor/DeepWater"
PREFAB_WALL_NE = "ThickWall/Wall_NE"
PREFAB_WALL_WN = "ThickWall/Wall_WN"
PREFAB_SLIM_NE = "SlimWall/Wall_NE"
PREFAB_SLIM_WN = "SlimWall/Wall_WN"
PREFAB_DOOR = "Door/Door"
PREFAB_STAIR = "Slope/Stair_NE"
PREFAB_BED = "Furniture/Bed"
PREFAB_WARDROBE = "Furniture/Wardrobe"
# Dist TileDefinition id (historical typo on Crate.asset).
PREFAB_CRATE = "Furniture/Create"

EMPTY_TERRAIN = frozenset({"", "t_null", "t_open_air"})
DOOR_FLAGS = frozenset({"DOOR"})
WINDOW_FLAGS = frozenset({
    "WINDOW",
    "BARRICADABLE_WINDOW",
    "BARRICADABLE_WINDOW_CURTAINS",
})
WALL_FLAGS = frozenset({"WALL"})
WATER_FLAGS = frozenset({"SWIMMABLE", "CURRENT"})
STAIR_FLAGS = frozenset({"GOES_UP", "GOES_DOWN"})
INDOOR_FLAGS = frozenset({"INDOORS"})

# Longest suffix first. `_base` is ground (house_2story_base).
# Bare `_1`/`_2` are applied only when `{base}_roof` or `{base}_basement` exists
# (2StoryModern03_1 vs house_w_1 variant ids).
LAYER_SUFFIXES: tuple[tuple[str, str], ...] = (
    ("_second_floor", "second"),
    ("_2ndfloor", "second"),
    ("_floor_2", "second"),
    ("_floor_1", "ground"),
    ("_basement", "basement"),
    ("_second", "second"),
    ("_first", "ground"),
    ("_upper", "second"),
    ("_roof", "roof"),
    ("_2f", "second"),
    ("_1f", "ground"),
    ("_base", "ground"),
)
NUMBERED_FLOOR_SUFFIXES: tuple[tuple[str, str], ...] = (
    ("_2", "second"),
    ("_1", "ground"),
)

LAYER_Y = {
    "basement": -1,
    "ground": 0,
    "second": 1,
}

# Dist already has a TileDefinition. Everything else keeps the BN id
# so Furniture/BN stubs can be filled later (visual fallback is the crate prefab).
FURNITURE_DIST_OWNED: tuple[tuple[str, str], ...] = (
    ("f_makeshift_bed", PREFAB_BED),
    ("f_straw_bed", PREFAB_BED),
    ("f_floor_mattress", PREFAB_BED),
    ("f_bed", PREFAB_BED),
    ("f_wardrobe", PREFAB_WARDROBE),
    ("f_crate", PREFAB_CRATE),
    ("f_cardboard", PREFAB_CRATE),
)

OUTDOOR_FLOOR_PREFIXES: tuple[str, ...] = (
    "t_grass",
    "t_dirt",
    "t_sand",
    "t_pavement",
    "t_sidewalk",
    "t_concrete",
    "t_region_groundcover",
    "t_region_soil",
)

DEEP_WATER_MARKERS: tuple[str, ...] = (
    "t_water_dp",
    "t_water_pool",
    "deep",
)


def pick_map_id(spec: Any) -> str | None:
    """Deterministic BN id from a mapgen/palette symbol value. Highest weight; tie → name."""
    if spec is None:
        return None
    if isinstance(spec, str):
        key = spec.strip()
        if key in ("", "t_null", "f_null"):
            return None
        return key
    if isinstance(spec, list):
        best_id: str | None = None
        best_w = -1
        for item in spec:
            cid: str | None = None
            weight = 1
            if isinstance(item, str):
                cid = item
            elif isinstance(item, list) and item:
                if isinstance(item[0], str):
                    cid = item[0]
                if len(item) > 1 and isinstance(item[1], (int, float)):
                    weight = int(item[1])
            if not cid:
                continue
            if weight > best_w or (weight == best_w and (best_id is None or cid < best_id)):
                best_id = cid
                best_w = weight
        return pick_map_id(best_id)
    if isinstance(spec, dict):
        if "chunks" in spec:
            return None
        best_id = None
        best_w = -1
        for key, val in spec.items():
            if not isinstance(key, str) or key.startswith("//"):
                continue
            weight = int(val) if isinstance(val, (int, float)) else 1
            if weight > best_w or (weight == best_w and (best_id is None or key < best_id)):
                best_id = key
                best_w = weight
        return pick_map_id(best_id)
    return None


def _flags(entry: dict | None) -> set[str]:
    if not entry:
        return set()
    raw = entry.get("flags", [])
    if isinstance(raw, str):
        raw = [raw]
    if not isinstance(raw, list):
        return set()
    return {str(flag) for flag in raw if flag}


def _move_cost(entry: dict | None) -> int:
    if not entry:
        return 0
    val = entry.get("move_cost", 0)
    try:
        return int(val)
    except (TypeError, ValueError):
        return 0


def _starts_with_any(value: str, prefixes: tuple[str, ...]) -> bool:
    return any(value.startswith(prefix) for prefix in prefixes)


def classify_terrain(ter_id: str | None, catalog: dict[str, dict]) -> tuple[str, str | None]:
    """Return (kind, prefab_or_none). kind: empty|wall|window|door|floor|water|stairs|skip."""
    if not ter_id or ter_id in EMPTY_TERRAIN or ter_id.startswith("t_open_air"):
        return "empty", None

    entry = catalog.get(ter_id)
    flags = _flags(entry)
    cost = _move_cost(entry)

    if flags & WATER_FLAGS or ter_id.startswith("t_water"):
        deep = any(marker in ter_id for marker in DEEP_WATER_MARKERS)
        return "water", PREFAB_DEEP if deep else PREFAB_SHALLOW

    if flags & STAIR_FLAGS or "stairs" in ter_id:
        return "stairs", PREFAB_STAIR

    if flags & DOOR_FLAGS or ter_id.startswith("t_door"):
        return "door", PREFAB_DOOR

    glass_wall = bool(flags & WALL_FLAGS) and "TRANSPARENT" in flags
    if (
        flags & WINDOW_FLAGS
        or ter_id.startswith("t_window")
        or ter_id.startswith("t_curtains")
        or glass_wall
    ):
        return "window", PREFAB_SLIM_NE

    if flags & WALL_FLAGS or ter_id.startswith("t_wall"):
        return "wall", PREFAB_WALL_NE

    if cost > 0 or "FLAT" in flags or ter_id.startswith("t_floor") or "roof" in ter_id:
        outdoor = _starts_with_any(ter_id, OUTDOOR_FLOOR_PREFIXES)
        indoor = (
            bool(flags & INDOOR_FLAGS)
            or ter_id.startswith("t_floor")
            or ter_id.startswith("t_linoleum")
            or ter_id.startswith("t_thconc")
            or ter_id.startswith("t_carpet")
            or "roof" in ter_id
        )
        if indoor:
            prefab = PREFAB_FLOOR
        elif outdoor:
            prefab = PREFAB_GRASS
        else:
            prefab = PREFAB_FLOOR
        return "floor", prefab

    return "skip", None


def map_furniture_prefab(furn_id: str | None) -> str | None:
    """Dist-owned prefabId, or the BN id itself. f_null → None."""
    if not furn_id or furn_id == "f_null":
        return None
    for prefix, prefab in FURNITURE_DIST_OWNED:
        if furn_id.startswith(prefix):
            return prefab
    return furn_id


def is_bn_furniture_stub(furn_id: str | None) -> bool:
    prefab = map_furniture_prefab(furn_id)
    return bool(prefab and prefab == furn_id)


def collect_stub_ids_from_furniture_map(furniture_by_sym: dict[str, Any]) -> set[str]:
    ids: set[str] = set()
    for spec in furniture_by_sym.values():
        furn_id = pick_map_id(spec)
        if is_bn_furniture_stub(furn_id):
            ids.add(furn_id)
    return ids


def parse_layer_om(om_id: str, known_oms: set[str] | None = None) -> tuple[str, str]:
    for suffix, kind in LAYER_SUFFIXES:
        if om_id.endswith(suffix) and len(om_id) > len(suffix):
            return om_id[: -len(suffix)], kind
    if known_oms:
        for suffix, kind in NUMBERED_FLOOR_SUFFIXES:
            if not (om_id.endswith(suffix) and len(om_id) > len(suffix)):
                continue
            base = om_id[: -len(suffix)]
            if f"{base}_roof" in known_oms or f"{base}_basement" in known_oms:
                return base, kind
    return om_id, "ground"


def layer_walkable_y(kinds: set[str], kind: str) -> int:
    if kind == "roof":
        return 2 if "second" in kinds else 1
    return LAYER_Y[kind]


def _occupied_tile(x: int, y: int, z: int, prefab_id: str) -> dict[str, Any]:
    return {
        "x": x,
        "y": y,
        "z": z,
        "sizeX": 1,
        "sizeY": 1,
        "sizeZ": 1,
        "prefabId": prefab_id,
        "tileType": TILE_TYPE_OCCUPIED,
        "face": 0,
        "seedItemId": "",
        "plantedWorldMinute": 0,
        "fertilized": False,
        "lastFruitHarvestWorldMinute": 0,
        "fishTrapBaitId": "",
        "fishTrapBaitRemaining": 0,
        "fishTrapDeployedMinute": 0,
        "fishTrapAccumulatedFish": 0,
    }


def _face(x: int, y: int, z: int, prefab_id: str) -> dict[str, Any]:
    return {
        "x": x,
        "y": y,
        "z": z,
        "face": FLOOR_FACE_POS_Y,
        "prefabId": prefab_id,
        "simulateFlow": False,
    }


def _axis_prefab(x: int, z: int, is_member, ne: str, wn: str) -> str:
    ns = is_member(x, z + 1) or is_member(x, z - 1)
    ew = is_member(x + 1, z) or is_member(x - 1, z)
    if ns and not ew:
        return wn
    return ne


def convert_rows(
    rows: list[str],
    terrain_by_sym: dict[str, Any],
    furniture_by_sym: dict[str, Any],
    terrain_catalog: dict[str, dict],
    fill_ter: str | None,
    walkable_y: int,
) -> dict[str, Any]:
    """Convert one BN rows grid into Dist lists. z = BN north-up row flipped."""
    tiles: list[dict[str, Any]] = []
    floor_faces: list[dict[str, Any]] = []
    liquids: list[dict[str, Any]] = []
    skipped_terrain: dict[str, int] = defaultdict(int)
    skipped_furniture: dict[str, int] = defaultdict(int)

    if not rows:
        return {
            "tiles": tiles,
            "floorFaces": floor_faces,
            "liquidAuthoringFaces": liquids,
            "skippedTerrain": dict(skipped_terrain),
            "skippedFurniture": dict(skipped_furniture),
        }

    height = len(rows)
    width = max(len(row) for row in rows)
    cells: dict[tuple[int, int], dict[str, Any]] = {}

    for row_i, row in enumerate(rows):
        bn_y = height - 1 - row_i
        for col, symbol in enumerate(row):
            ter_spec = terrain_by_sym.get(symbol)
            ter_id = pick_map_id(ter_spec)
            if ter_id is None:
                ter_id = fill_ter
            furn_id = pick_map_id(furniture_by_sym.get(symbol))
            kind, prefab = classify_terrain(ter_id, terrain_catalog)
            cells[(col, bn_y)] = {
                "kind": kind,
                "ter_id": ter_id,
                "prefab": prefab,
                "furn_id": furn_id,
            }

    def is_kind(x: int, z: int, kinds: set[str]) -> bool:
        cell = cells.get((x, z))
        return bool(cell and cell["kind"] in kinds)

    wall_kinds = {"wall"}
    window_kinds = {"window"}

    for (x, z), cell in cells.items():
        kind = cell["kind"]
        ter_id = cell["ter_id"]
        if kind == "empty":
            continue
        if kind == "skip":
            if ter_id:
                skipped_terrain[ter_id] += 1
            continue

        if kind == "water":
            liquids.append(_face(x, walkable_y - 1, z, cell["prefab"] or PREFAB_SHALLOW))
            continue

        if kind == "floor":
            floor_faces.append(_face(x, walkable_y - 1, z, cell["prefab"] or PREFAB_FLOOR))
        elif kind == "door":
            floor_faces.append(_face(x, walkable_y - 1, z, PREFAB_FLOOR))
            tiles.append(_occupied_tile(x, walkable_y, z, PREFAB_DOOR))
        elif kind == "stairs":
            floor_faces.append(_face(x, walkable_y - 1, z, PREFAB_FLOOR))
            tiles.append(_occupied_tile(x, walkable_y, z, PREFAB_STAIR))
        elif kind == "wall":
            prefab = _axis_prefab(
                x, z,
                lambda cx, cz, _k=wall_kinds: is_kind(cx, cz, _k),
                PREFAB_WALL_NE,
                PREFAB_WALL_WN,
            )
            tiles.append(_occupied_tile(x, walkable_y, z, prefab))
        elif kind == "window":
            floor_faces.append(_face(x, walkable_y - 1, z, PREFAB_FLOOR))
            prefab = _axis_prefab(
                x, z,
                lambda cx, cz, _k=window_kinds: is_kind(cx, cz, _k) or is_kind(cx, cz, wall_kinds),
                PREFAB_SLIM_NE,
                PREFAB_SLIM_WN,
            )
            tiles.append(_occupied_tile(x, walkable_y, z, prefab))

        furn_id = cell["furn_id"]
        if not furn_id:
            continue
        furn_prefab = map_furniture_prefab(furn_id)
        if not furn_prefab:
            skipped_furniture[furn_id] += 1
            continue
        if kind in {"wall", "empty", "skip", "water"}:
            skipped_furniture[furn_id] += 1
            continue
        tiles.append(_occupied_tile(x, walkable_y, z, furn_prefab))

    return {
        "tiles": tiles,
        "floorFaces": floor_faces,
        "liquidAuthoringFaces": liquids,
        "skippedTerrain": dict(skipped_terrain),
        "skippedFurniture": dict(skipped_furniture),
        "width": width,
        "height": height,
    }


def empty_map_dto() -> dict[str, Any]:
    return {
        "schemaVersion": SCHEMA_VERSION,
        "gridCellSize": GRID_CELL_SIZE,
        "hasMapBounds": False,
        "mapBoundsMinX": 0,
        "mapBoundsMaxX": 0,
        "mapBoundsMinZ": 0,
        "mapBoundsMaxZ": 0,
        "mapBoundsMinY": 0,
        "tiles": [],
        "wallEdges": [],
        "floorFaces": [],
        "bloodStamps": [],
        "liquidAuthoringFaces": [],
        "liquidCells": [],
        "hasLiquidSnapshot": False,
        "hasLiquidTemperature": False,
        "plantCells": [],
        "hasClockSnapshot": False,
        "dayIndex": 0,
        "minuteOfDay": 0,
    }


def apply_bounds(dto: dict[str, Any]) -> None:
    xs: list[int] = []
    zs: list[int] = []
    ys: list[int] = []
    for row in dto["tiles"]:
        xs.append(row["x"])
        zs.append(row["z"])
        ys.append(row["y"])
    for row in dto["floorFaces"] + dto["liquidAuthoringFaces"]:
        xs.append(row["x"])
        zs.append(row["z"])
        ys.append(row["y"])
    if not xs:
        dto["hasMapBounds"] = False
        return
    dto["hasMapBounds"] = True
    dto["mapBoundsMinX"] = min(xs)
    dto["mapBoundsMaxX"] = max(xs)
    dto["mapBoundsMinZ"] = min(zs)
    dto["mapBoundsMaxZ"] = max(zs)
    dto["mapBoundsMinY"] = min(ys)


def _om_ids(raw: Any) -> list[str]:
    found: list[str] = []

    def walk(node: Any) -> None:
        if isinstance(node, str) and node:
            found.append(node)
        elif isinstance(node, list):
            for item in node:
                walk(item)

    walk(raw)
    return found


def load_palettes(bn_path: Path) -> dict[str, dict]:
    src = bn_path / "data" / "json" / "mapgen_palettes"
    raw = load_all_json(src) if src.exists() else []
    return resolve_copy_from_entries(raw, {"palette"})


def resolve_palette_maps(
    palette_ids: list[str],
    palettes: dict[str, dict],
    stack: set[str] | None = None,
) -> tuple[dict[str, Any], dict[str, Any]]:
    stack = stack or set()
    terrain: dict[str, Any] = {}
    furniture: dict[str, Any] = {}
    for pid in palette_ids:
        if not isinstance(pid, str) or pid in stack:
            continue
        pal = palettes.get(pid)
        if not pal:
            continue
        nested = pal.get("palettes") or []
        if isinstance(nested, str):
            nested = [nested]
        if isinstance(nested, list):
            pt, pf = resolve_palette_maps(
                [p for p in nested if isinstance(p, str)],
                palettes,
                stack | {pid},
            )
            terrain.update(pt)
            furniture.update(pf)
        terrain.update(pal.get("terrain") or {})
        furniture.update(pal.get("furniture") or {})
        toilets = pal.get("toilets") or {}
        if isinstance(toilets, dict):
            for symbol in toilets:
                furniture.setdefault(symbol, "f_toilet")
    return terrain, furniture


def symbol_maps_for_object(obj: dict, palettes: dict[str, dict]) -> tuple[dict[str, Any], dict[str, Any]]:
    raw_ids = obj.get("palettes") or []
    if isinstance(raw_ids, str):
        raw_ids = [raw_ids]
    palette_ids = [pid for pid in raw_ids if isinstance(pid, str)]
    terrain, furniture = resolve_palette_maps(palette_ids, palettes)
    terrain.update(obj.get("terrain") or {})
    furniture.update(obj.get("furniture") or {})
    toilets = obj.get("toilets") or {}
    if isinstance(toilets, dict):
        for symbol in toilets:
            furniture.setdefault(symbol, "f_toilet")
    return terrain, furniture


def load_house_mapgen(bn_path: Path) -> dict[str, dict]:
    """Best-weight json mapgen per om_terrain from data/json/mapgen/house."""
    house_dir = bn_path / "data" / "json" / "mapgen" / "house"
    if not house_dir.is_dir():
        return {}
    best: dict[str, tuple[int, dict]] = {}
    for entry in load_all_json(house_dir):
        if not isinstance(entry, dict):
            continue
        if entry.get("type") != "mapgen" or entry.get("method") != "json":
            continue
        obj = entry.get("object")
        if not isinstance(obj, dict) or not obj.get("rows"):
            continue
        try:
            weight = int(entry.get("weight", 100))
        except (TypeError, ValueError):
            weight = 100
        for om_id in _om_ids(entry.get("om_terrain")):
            prev = best.get(om_id)
            if prev is None or weight > prev[0]:
                best[om_id] = (weight, entry)
    return {om_id: item[1] for om_id, item in best.items()}


def group_layers(om_entries: dict[str, dict]) -> dict[str, dict[str, str]]:
    """group_id → { kind → om_id }."""
    known = set(om_entries)
    groups: dict[str, dict[str, str]] = defaultdict(dict)
    for om_id in om_entries:
        group_id, kind = parse_layer_om(om_id, known)
        groups[group_id][kind] = om_id
    return dict(groups)


def convert_group(
    group_id: str,
    layers: dict[str, str],
    om_entries: dict[str, dict],
    palettes: dict[str, dict],
    terrain_catalog: dict[str, dict],
) -> tuple[dict[str, Any], dict[str, Any]]:
    dto = empty_map_dto()
    skipped_terrain: dict[str, int] = defaultdict(int)
    skipped_furniture: dict[str, int] = defaultdict(int)
    stub_ids: set[str] = set()
    kinds = set(layers)
    used_om: list[str] = []

    for kind, om_id in sorted(layers.items(), key=lambda item: layer_walkable_y(kinds, item[0])):
        entry = om_entries[om_id]
        obj = entry["object"]
        rows = obj.get("rows") or []
        if not isinstance(rows, list) or not all(isinstance(row, str) for row in rows):
            continue
        terrain_by_sym, furniture_by_sym = symbol_maps_for_object(obj, palettes)
        stub_ids.update(collect_stub_ids_from_furniture_map(furniture_by_sym))
        fill_ter = obj.get("fill_ter") if isinstance(obj.get("fill_ter"), str) else None
        walkable_y = layer_walkable_y(kinds, kind)
        converted = convert_rows(
            rows,
            terrain_by_sym,
            furniture_by_sym,
            terrain_catalog,
            fill_ter,
            walkable_y,
        )
        dto["tiles"].extend(converted["tiles"])
        dto["floorFaces"].extend(converted["floorFaces"])
        dto["liquidAuthoringFaces"].extend(converted["liquidAuthoringFaces"])
        for key, count in converted["skippedTerrain"].items():
            skipped_terrain[key] += count
        for key, count in converted["skippedFurniture"].items():
            skipped_furniture[key] += count
        used_om.append(om_id)

    apply_bounds(dto)
    meta = {
        "id": group_id,
        "om_terrain": used_om,
        "tiles": len(dto["tiles"]),
        "floorFaces": len(dto["floorFaces"]),
        "liquidAuthoringFaces": len(dto["liquidAuthoringFaces"]),
        "skippedTerrain": dict(sorted(skipped_terrain.items())),
        "skippedFurniture": dict(sorted(skipped_furniture.items())),
        "furnitureStubIds": sorted(stub_ids),
    }
    return dto, meta


def _safe_filename(name: str) -> str:
    out = []
    for ch in name:
        if ch.isalnum() or ch in "-_":
            out.append(ch)
        else:
            out.append("_")
    return "".join(out) or "house"


def export_house_mapgen(
    bn_path: Path,
    out_dir: Path,
    om_terrain: str | None = None,
    limit: int = 0,
) -> dict[str, Any]:
    house_dir = bn_path / "data" / "json" / "mapgen" / "house"
    if not house_dir.is_dir():
        print(f"[ERROR] house mapgen not found: {house_dir}", file=sys.stderr)
        sys.exit(1)

    print(f"[mapgen] palettes + terrain from {bn_path}")
    palettes = load_palettes(bn_path)
    terrain_catalog, _furniture_catalog = load_furniture_and_terrain(bn_path)
    om_entries = load_house_mapgen(bn_path)
    groups = group_layers(om_entries)

    if om_terrain:
        if om_terrain in groups:
            groups = {om_terrain: groups[om_terrain]}
        elif om_terrain in om_entries:
            group_id, kind = parse_layer_om(om_terrain)
            groups = {group_id: {kind: om_terrain}}
        else:
            print(f"[ERROR] om_terrain not found in house mapgen: {om_terrain}", file=sys.stderr)
            sys.exit(1)

    names = sorted(groups)
    if limit > 0:
        names = names[:limit]

    houses_dir = out_dir / "houses"
    if houses_dir.is_dir():
        for old in houses_dir.glob("*.json"):
            old.unlink()
    houses_dir.mkdir(parents=True, exist_ok=True)
    index_maps: list[dict[str, Any]] = []
    all_stub_ids: set[str] = set()

    for group_id in names:
        dto, meta = convert_group(
            group_id,
            groups[group_id],
            om_entries,
            palettes,
            terrain_catalog,
        )
        filename = f"{_safe_filename(group_id)}.json"
        rel = f"houses/{filename}"
        path = out_dir / rel
        with path.open("w", encoding="utf-8") as fh:
            json.dump(dto, fh, ensure_ascii=False, indent=2)
        meta["file"] = rel
        all_stub_ids.update(meta.get("furnitureStubIds") or [])
        index_maps.append(meta)
        print(
            f"[mapgen] {group_id}  tiles={meta['tiles']}  floors={meta['floorFaces']}  "
            f"skip_ter={len(meta['skippedTerrain'])}  skip_furn={len(meta['skippedFurniture'])}"
        )

    index = {
        "_license": LICENSE,
        "_source": SOURCE,
        "maps": index_maps,
    }
    index_path = out_dir / "index.json"
    with index_path.open("w", encoding="utf-8") as fh:
        json.dump(index, fh, ensure_ascii=False, indent=2)
    stub_ids = sorted(all_stub_ids)
    stubs_path = out_dir / "furniture_ids.json"
    with stubs_path.open("w", encoding="utf-8") as fh:
        json.dump({
            "_license": LICENSE,
            "_source": SOURCE,
            "ids": stub_ids,
        }, fh, ensure_ascii=False, indent=2)
    print(f"[output] {index_path}  ({len(index_maps)} houses)")
    print(f"[output] {stubs_path}  ({len(stub_ids)} BN furniture stub ids)")
    return index


def main() -> None:
    parser = argparse.ArgumentParser(description="BN house mapgen → Dist MapSaveJsonDto")
    parser.add_argument("--bn-path", required=True, help="Cataclysm-BN project root")
    parser.add_argument("--output", required=True, help="Output directory (writes houses/ + index.json)")
    parser.add_argument("--om-terrain", default=None, help="Single house group or om_terrain id")
    parser.add_argument("--limit", type=int, default=0, help="Bake at most N house groups (0 = all)")
    args = parser.parse_args()

    bn_path = Path(args.bn_path)
    if not (bn_path / "data" / "json").is_dir():
        print(f"[ERROR] BN data path not found: {bn_path / 'data' / 'json'}", file=sys.stderr)
        sys.exit(1)
    export_house_mapgen(bn_path, Path(args.output), om_terrain=args.om_terrain, limit=args.limit)


if __name__ == "__main__":
    main()
