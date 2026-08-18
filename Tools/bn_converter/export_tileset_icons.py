"""
Cataclysm-BN Ultica tileset → Dist item icon index.

Usage:
    python export_tileset_icons.py --bn-path "Z:/Work/Project/Cataclysm-BN" ^
        --items "../../Assets/StreamingAssets/BNData/items.json" ^
        --output "../../Assets/StreamingAssets/BNData/tileset"
"""

from __future__ import annotations

import argparse
import json
import shutil
import struct
import sys
from pathlib import Path
from typing import Any

from convert import load_all_json

LICENSE = "CC BY-SA 3.0 — derived from Cataclysm: Bright Nights / UltimateCataclysm"
SOURCE = "https://github.com/cataclysmbnteam/Cataclysm-BN"
DEFAULT_TILESET = "UltimateCataclysm"
PIXELS_PER_UNIT = 100
LOOKS_LIKE_JUMPS = 10
ITEM_TYPES = {
    "GENERIC", "COMESTIBLE", "ARMOR", "TOOL", "GUN", "AMMO",
    "BOOK", "MAGAZINE", "GUNMOD", "TOOL_ARMOR", "BIONIC_ITEM",
    "PET_ARMOR", "ENGINE", "CONTAINER", "TOOLMOD", "WHEEL",
    "MIGRATION", "BATTERY",
}


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as fh:
        signature = fh.read(8)
        if signature != b"\x89PNG\r\n\x1a\n":
            raise ValueError(f"not a PNG: {path}")
        length = struct.unpack(">I", fh.read(4))[0]
        chunk = fh.read(4)
        if chunk != b"IHDR" or length < 8:
            raise ValueError(f"missing IHDR: {path}")
        width, height = struct.unpack(">II", fh.read(8))
        return width, height


def first_fg(fg: Any) -> int | None:
    """First foreground sprite index (ignore animation / rotation extras)."""
    if fg is None:
        return None
    if isinstance(fg, bool):
        return None
    if isinstance(fg, int):
        return fg
    if isinstance(fg, list):
        if not fg:
            return None
        return first_fg(fg[0])
    if isinstance(fg, dict):
        if "sprite" in fg:
            return first_fg(fg["sprite"])
        return None
    return None


def collect_ids(raw_id: Any) -> list[str]:
    if isinstance(raw_id, str) and raw_id:
        return [raw_id]
    if isinstance(raw_id, list):
        return [item for item in raw_id if isinstance(item, str) and item]
    return []


def parse_tileset_txt(tileset_dir: Path) -> str:
    txt = tileset_dir / "tileset.txt"
    json_name = "tile_config.json"
    if not txt.is_file():
        return json_name
    for line in txt.read_text(encoding="utf-8", errors="replace").splitlines():
        stripped = line.strip()
        if stripped.startswith("JSON:"):
            json_name = stripped.split(":", 1)[1].strip()
            break
    return json_name


def load_item_ids(items_path: Path) -> set[str]:
    with items_path.open("r", encoding="utf-8") as fh:
        data = json.load(fh)
    items = data.get("items") if isinstance(data, dict) else None
    if not isinstance(items, list):
        raise ValueError(f"no items[] in {items_path}")
    ids: set[str] = set()
    for item in items:
        if isinstance(item, dict):
            item_id = item.get("id")
            if isinstance(item_id, str) and item_id:
                ids.add(item_id)
    return ids


class Sheet:
    __slots__ = (
        "file_name",
        "path",
        "offset",
        "count",
        "cols",
        "sprite_width",
        "sprite_height",
        "png_width",
        "png_height",
    )

    def __init__(
        self,
        file_name: str,
        path: Path,
        offset: int,
        count: int,
        cols: int,
        sprite_width: int,
        sprite_height: int,
        png_width: int,
        png_height: int,
    ) -> None:
        self.file_name = file_name
        self.path = path
        self.offset = offset
        self.count = count
        self.cols = cols
        self.sprite_width = sprite_width
        self.sprite_height = sprite_height
        self.png_width = png_width
        self.png_height = png_height

    def contains(self, global_index: int) -> bool:
        return self.offset <= global_index < self.offset + self.count

    def local_index(self, global_index: int) -> int:
        return global_index - self.offset


def load_sheets(tileset_dir: Path, tiles_new: list[dict], default_w: int, default_h: int) -> list[Sheet]:
    sheets: list[Sheet] = []
    offset = 0
    for part in tiles_new:
        if not isinstance(part, dict):
            continue
        file_name = part.get("file")
        if not isinstance(file_name, str) or not file_name:
            continue
        png_path = tileset_dir / file_name
        if not png_path.is_file():
            print(f"  [WARN] missing PNG, skip sheet: {png_path}", file=sys.stderr)
            continue
        sprite_w = int(part.get("sprite_width") or default_w)
        sprite_h = int(part.get("sprite_height") or default_h)
        if sprite_w <= 0 or sprite_h <= 0:
            print(f"  [WARN] invalid sprite size on {file_name}", file=sys.stderr)
            continue
        png_w, png_h = png_size(png_path)
        cols = png_w // sprite_w
        rows = png_h // sprite_h
        if cols <= 0 or rows <= 0:
            print(f"  [WARN] PNG smaller than sprite on {file_name}", file=sys.stderr)
            continue
        count = cols * rows
        sheets.append(
            Sheet(
                file_name=file_name,
                path=png_path,
                offset=offset,
                count=count,
                cols=cols,
                sprite_width=sprite_w,
                sprite_height=sprite_h,
                png_width=png_w,
                png_height=png_h,
            )
        )
        offset += count
    return sheets


def find_sheet(sheets: list[Sheet], global_index: int) -> Sheet | None:
    for sheet in sheets:
        if sheet.contains(global_index):
            return sheet
    return None


def collect_tile_sprites(
    tiles_new: list[dict],
    sheets: list[Sheet],
) -> dict[str, dict[str, Any]]:
    """First matching tile id wins (dedicated item sheets come before filler)."""
    mapped: dict[str, dict[str, Any]] = {}
    for part in tiles_new:
        if not isinstance(part, dict):
            continue
        tiles = part.get("tiles")
        if not isinstance(tiles, list):
            continue
        for tile in tiles:
            if not isinstance(tile, dict):
                continue
            fg_index = first_fg(tile.get("fg"))
            if fg_index is None or fg_index < 0:
                continue
            sheet = find_sheet(sheets, fg_index)
            if sheet is None:
                continue
            local = sheet.local_index(fg_index)
            entry = {"file": sheet.file_name, "index": local}
            for tile_id in collect_ids(tile.get("id")):
                if tile_id not in mapped:
                    mapped[tile_id] = entry
    return mapped


def normalize_looks_like(raw: Any) -> str | None:
    if isinstance(raw, str) and raw:
        return raw
    if isinstance(raw, list):
        for item in raw:
            if isinstance(item, str) and item:
                return item
    return None


def index_bn_item_entries(bn_path: Path) -> tuple[dict[str, dict], dict[str, dict]]:
    """Raw BN item JSON (looks_like / copy-from 유지). Dist items.json 에는 없음."""
    by_id: dict[str, dict] = {}
    abstracts: dict[str, dict] = {}
    items_dir = bn_path / "data" / "json" / "items"
    obsoletion_dir = bn_path / "data" / "json" / "obsoletion"
    raw = load_all_json(items_dir)
    if obsoletion_dir.is_dir():
        raw.extend(load_all_json(obsoletion_dir))

    for entry in raw:
        if not isinstance(entry, dict) or entry.get("type") not in ITEM_TYPES:
            continue
        eid = entry.get("id") or entry.get("abstract")
        if isinstance(eid, list):
            eid = eid[0] if eid and isinstance(eid[0], str) else None
        if not isinstance(eid, str) or not eid:
            continue
        if "abstract" in entry:
            abstracts[eid] = entry
        else:
            by_id[eid] = entry
    return by_id, abstracts


def looks_like_target(
    eid: str,
    by_id: dict[str, dict],
    abstracts: dict[str, dict],
    cache: dict[str, str | None],
    stack: set[str],
) -> str | None:
    """BN item_factory: explicit looks_like, else copy-from (abstract inherits)."""
    if eid in cache:
        return cache[eid]
    if eid in stack:
        return None

    entry = by_id.get(eid) or abstracts.get(eid)
    if entry is None:
        cache[eid] = None
        return None

    stack.add(eid)
    explicit = normalize_looks_like(entry.get("looks_like"))
    if explicit:
        cache[eid] = explicit
        stack.discard(eid)
        return explicit

    parent = entry.get("copy-from")
    if not isinstance(parent, str) or not parent or parent == eid:
        cache[eid] = None
        stack.discard(eid)
        return None

    if parent in by_id:
        result: str | None = parent
    else:
        inherited = looks_like_target(parent, by_id, abstracts, cache, stack)
        result = inherited if inherited else parent

    cache[eid] = result
    stack.discard(eid)
    return result


def resolve_item_sprite(
    item_id: str,
    tile_map: dict[str, dict[str, Any]],
    by_id: dict[str, dict],
    abstracts: dict[str, dict],
    looks_cache: dict[str, str | None],
    jumps: int = LOOKS_LIKE_JUMPS,
) -> dict[str, Any] | None:
    current = item_id
    seen: set[str] = set()
    remaining = jumps
    while current and remaining > 0:
        if current in seen:
            return None
        seen.add(current)
        sprite = tile_map.get(current)
        if sprite is not None:
            return sprite
        current = looks_like_target(current, by_id, abstracts, looks_cache, set())
        remaining -= 1
    return None


def map_item_sprites(
    item_ids: set[str],
    tile_map: dict[str, dict[str, Any]],
    by_id: dict[str, dict],
    abstracts: dict[str, dict],
) -> tuple[dict[str, dict[str, Any]], int, int]:
    mapped: dict[str, dict[str, Any]] = {}
    looks_cache: dict[str, str | None] = {}
    direct = 0
    via_looks = 0
    for item_id in item_ids:
        sprite = resolve_item_sprite(item_id, tile_map, by_id, abstracts, looks_cache)
        if sprite is None:
            continue
        mapped[item_id] = sprite
        if item_id in tile_map:
            direct += 1
        else:
            via_looks += 1
    return mapped, direct, via_looks


def write_output(
    out_dir: Path,
    tileset_name: str,
    sheets: list[Sheet],
    mapped: dict[str, dict[str, Any]],
    tileset_dir: Path,
) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    used_files = {entry["file"] for entry in mapped.values()}
    files_meta = {}
    copied_bytes = 0
    for sheet in sheets:
        if sheet.file_name not in used_files:
            continue
        files_meta[sheet.file_name] = {
            "sprite_width": sheet.sprite_width,
            "sprite_height": sheet.sprite_height,
        }
        dest = out_dir / sheet.file_name
        shutil.copy2(sheet.path, dest)
        copied_bytes += dest.stat().st_size

    for existing in out_dir.glob("*.png"):
        if existing.name not in used_files:
            existing.unlink()

    payload = {
        "_license": LICENSE,
        "_source": SOURCE,
        "tileset": tileset_name,
        "ppu": PIXELS_PER_UNIT,
        "files": files_meta,
        "items": dict(sorted(mapped.items())),
    }
    index_path = out_dir / "item_sprites.json"
    with index_path.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, ensure_ascii=False, indent=2)

    credits_src = tileset_dir / "tileset.txt"
    if credits_src.is_file():
        shutil.copy2(credits_src, out_dir / "TILESET.txt")

    print(f"[output] {index_path}")
    print(f"[output] copied {len(files_meta)} PNG(s), {copied_bytes} bytes")


def main() -> int:
    parser = argparse.ArgumentParser(description="BN Ultica tileset → item icon index")
    parser.add_argument("--bn-path", required=True, help="Cataclysm-BN 프로젝트 루트")
    parser.add_argument("--items", required=True, help="BNData items.json")
    parser.add_argument("--output", required=True, help="출력 디렉토리 (BNData/tileset)")
    parser.add_argument("--tileset", default=DEFAULT_TILESET, help="gfx 폴더 이름")
    args = parser.parse_args()

    bn_path = Path(args.bn_path)
    items_path = Path(args.items)
    out_dir = Path(args.output)
    tileset_dir = bn_path / "gfx" / args.tileset

    if not tileset_dir.is_dir():
        print(f"[ERROR] tileset folder not found: {tileset_dir}", file=sys.stderr)
        return 1
    if not items_path.is_file():
        print(f"[ERROR] items.json not found: {items_path}", file=sys.stderr)
        return 1

    json_name = parse_tileset_txt(tileset_dir)
    config_path = tileset_dir / json_name
    if not config_path.is_file():
        print(f"[ERROR] tile_config not found: {config_path}", file=sys.stderr)
        return 1

    with config_path.open("r", encoding="utf-8") as fh:
        config = json.load(fh)

    tile_info = config.get("tile_info") or []
    default_w = 32
    default_h = 32
    if tile_info and isinstance(tile_info[0], dict):
        default_w = int(tile_info[0].get("width") or default_w)
        default_h = int(tile_info[0].get("height") or default_h)

    tiles_new = config.get("tiles-new")
    if not isinstance(tiles_new, list):
        print("[ERROR] tiles-new missing", file=sys.stderr)
        return 1

    item_ids = load_item_ids(items_path)
    sheets = load_sheets(tileset_dir, tiles_new, default_w, default_h)
    if not sheets:
        print("[ERROR] no tileset PNG sheets loaded", file=sys.stderr)
        return 1

    tile_map = collect_tile_sprites(tiles_new, sheets)
    by_id, abstracts = index_bn_item_entries(bn_path)
    mapped, direct, via_looks = map_item_sprites(item_ids, tile_map, by_id, abstracts)
    write_output(out_dir, args.tileset, sheets, mapped, tileset_dir)

    missing = len(item_ids) - len(mapped)
    print("── Coverage ──")
    print(f"  BN items:       {len(item_ids)}")
    print(f"  mapped direct:  {direct}")
    print(f"  mapped looks:   {via_looks}")
    print(f"  mapped total:   {len(mapped)}")
    print(f"  missing:        {missing}")
    if not mapped:
        print("[ERROR] mapped count is 0", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
