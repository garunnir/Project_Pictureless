"""
Cataclysm-BN tileset → Dist plant growth-stage sprite index.

Maps BN furniture ids (f_plant_seed … f_plant_harvest) to atlas entries.
Does not replace item icons (export_tileset_icons.py).

Usage:
    python export_plant_sprites.py --bn-path "Z:/Work/Project/Cataclysm-BN" ^
        --output "../../Assets/StreamingAssets/BNData/tileset"
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path
from typing import Any

from export_tileset_icons import (
    DEFAULT_TILESET,
    LICENSE,
    PIXELS_PER_UNIT,
    SOURCE,
    collect_tile_sprites,
    load_sheets,
    parse_tileset_txt,
)

# Dist PlantGrowthStage ↔ BN furniture (generic field crop). Withered has no BN id.
STAGE_FURNITURE: dict[str, str] = {
    "Seed": "f_plant_seed",
    "Seedling": "f_plant_seedling",
    "Mature": "f_plant_mature",
    "Harvestable": "f_plant_harvest",
}

VERIFY_EXTRA = (
    "f_planter_seed",
    "f_planter_seedling",
    "f_planter_mature",
    "f_planter_harvest",
)


def write_output(
    out_dir: Path,
    tileset_name: str,
    files_meta: dict[str, dict[str, int]],
    stages: dict[str, dict[str, Any]],
    furniture: dict[str, dict[str, Any]],
    sheets_by_name: dict[str, Path],
) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)

    used_files = {entry["file"] for entry in furniture.values()}
    copied = 0
    for file_name in used_files:
        src = sheets_by_name.get(file_name)
        if src is None or not src.is_file():
            print(f"  [WARN] missing PNG for plant sheet: {file_name}", file=sys.stderr)
            continue
        dest = out_dir / file_name
        if not dest.is_file() or dest.stat().st_mtime < src.stat().st_mtime:
            shutil.copy2(src, dest)
        copied += 1

    payload = {
        "_license": LICENSE,
        "_source": SOURCE,
        "tileset": tileset_name,
        "ppu": PIXELS_PER_UNIT,
        "files": {name: files_meta[name] for name in sorted(used_files) if name in files_meta},
        "stages": dict(sorted(stages.items())),
        "furniture": dict(sorted(furniture.items())),
    }
    index_path = out_dir / "plant_sprites.json"
    with index_path.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, ensure_ascii=False, indent=2)

    print(f"[output] {index_path}")
    print(f"[output] plant atlas file(s) ensured: {copied}")


def main() -> int:
    parser = argparse.ArgumentParser(description="BN tileset → plant stage sprite index")
    parser.add_argument("--bn-path", required=True, help="Cataclysm-BN 프로젝트 루트")
    parser.add_argument("--output", required=True, help="출력 디렉토리 (BNData/tileset)")
    parser.add_argument("--tileset", default=DEFAULT_TILESET, help="gfx 폴더 이름")
    args = parser.parse_args()

    bn_path = Path(args.bn_path)
    out_dir = Path(args.output)
    tileset_dir = bn_path / "gfx" / args.tileset

    if not tileset_dir.is_dir():
        print(f"[ERROR] tileset folder not found: {tileset_dir}", file=sys.stderr)
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

    sheets = load_sheets(tileset_dir, tiles_new, default_w, default_h)
    if not sheets:
        print("[ERROR] no tileset PNG sheets loaded", file=sys.stderr)
        return 1

    tile_map = collect_tile_sprites(tiles_new, sheets)
    sheets_by_name = {sheet.file_name: sheet.path for sheet in sheets}
    files_meta = {
        sheet.file_name: {
            "sprite_width": sheet.sprite_width,
            "sprite_height": sheet.sprite_height,
        }
        for sheet in sheets
    }

    furniture: dict[str, dict[str, Any]] = {}
    stages: dict[str, dict[str, Any]] = {}
    for stage, furn_id in STAGE_FURNITURE.items():
        entry = tile_map.get(furn_id)
        if entry is None:
            print(f"  [MISS] {furn_id} ({stage})", file=sys.stderr)
            continue
        furniture[furn_id] = entry
        stages[stage] = entry
        print(f"  [OK] {furn_id} → {entry['file']}#{entry['index']}")

    for furn_id in VERIFY_EXTRA:
        entry = tile_map.get(furn_id)
        if entry is None:
            print(f"  [verify miss] {furn_id}")
        else:
            print(f"  [verify ok] {furn_id} → {entry['file']}#{entry['index']}")

    print("── Coverage ──")
    print(f"  required stages: {len(STAGE_FURNITURE)}")
    print(f"  mapped stages:   {len(stages)}")

    if len(stages) < 3:
        print("[ERROR] mapped plant stages < 3", file=sys.stderr)
        return 1

    write_output(out_dir, args.tileset, files_meta, stages, furniture, sheets_by_name)
    return 0


if __name__ == "__main__":
    sys.exit(main())
