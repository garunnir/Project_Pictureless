"""BN house mapgen → Dist MapSaveJsonDto. Run: python test_mapgen.py"""

from export_mapgen import (
    PREFAB_BED,
    PREFAB_CRATE,
    PREFAB_DOOR,
    PREFAB_FLOOR,
    PREFAB_GRASS,
    PREFAB_SHALLOW,
    PREFAB_SLIM_NE,
    PREFAB_STAIR,
    PREFAB_WALL_NE,
    PREFAB_WALL_WN,
    PREFAB_WARDROBE,
    classify_terrain,
    convert_rows,
    empty_map_dto,
    apply_bounds,
    map_furniture_prefab,
    parse_layer_om,
    pick_map_id,
    layer_walkable_y,
)


def _eq(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: {actual!r} != {expected!r}")


def test_pick_map_id_weighted():
    _eq(pick_map_id("t_floor"), "t_floor", "string")
    _eq(
        pick_map_id([["t_door_c", 5], ["t_door_o", 5], ["t_door_locked_interior", 1]]),
        "t_door_c",
        "tie name",
    )
    _eq(
        pick_map_id([["t_window_open", 1], ["t_window_domestic", 10]]),
        "t_window_domestic",
        "max weight",
    )
    _eq(pick_map_id({"t_floor": 3, "t_carpet": 1}), "t_floor", "dict weight")
    _eq(pick_map_id({"chunks": [["null", 60]]}), None, "nested chunks skipped")
    _eq(pick_map_id("t_null"), None, "t_null")
    _eq(pick_map_id("f_null"), None, "f_null")


def test_layer_suffix():
    _eq(parse_layer_om("house_10"), ("house_10", "ground"), "ground")
    _eq(parse_layer_om("house_10_roof"), ("house_10", "roof"), "roof")
    _eq(parse_layer_om("house_10_basement"), ("house_10", "basement"), "basement")
    _eq(parse_layer_om("house_2story_base"), ("house_2story", "ground"), "base")
    _eq(parse_layer_om("house_2story_second"), ("house_2story", "second"), "second")
    _eq(parse_layer_om("2storyModern01_first"), ("2storyModern01", "ground"), "first")
    _eq(
        parse_layer_om("garden_house_1_floor_2"),
        ("garden_house_1", "second"),
        "floor_2",
    )
    known = {"bungalow01_1", "bungalow01_roof", "house_w_1", "house_w_1_roof"}
    _eq(parse_layer_om("bungalow01_1", known), ("bungalow01", "ground"), "numbered 1")
    _eq(parse_layer_om("house_w_1", known), ("house_w_1", "ground"), "variant keeps _1")
    _eq(layer_walkable_y({"ground", "roof"}, "roof"), 1, "roof y without second")
    _eq(layer_walkable_y({"ground", "second", "roof"}, "roof"), 2, "roof y with second")
    _eq(layer_walkable_y({"basement"}, "basement"), -1, "basement y")


def test_classify_and_furniture():
    catalog = {
        "t_floor": {"flags": ["INDOORS", "FLAT"], "move_cost": 2},
        "t_wall_w": {"flags": ["WALL"], "move_cost": 0},
        "t_window_domestic": {"flags": ["BARRICADABLE_WINDOW", "TRANSPARENT"], "move_cost": 0},
        "t_wall_glass": {"flags": ["WALL", "TRANSPARENT"], "move_cost": 0},
        "t_door_c": {"flags": ["DOOR"], "move_cost": 0},
        "t_wood_stairs_up": {"flags": ["GOES_UP"], "move_cost": 0},
        "t_water_sh": {"flags": ["SWIMMABLE"], "move_cost": 4},
        "t_region_groundcover_urban": {"flags": ["TRANSPARENT"], "move_cost": 2},
        "t_open_air": {"flags": ["TRANSPARENT"], "move_cost": 2},
    }
    _eq(classify_terrain("t_open_air", catalog)[0], "empty", "open air")
    _eq(classify_terrain("t_wall_w", catalog), ("wall", PREFAB_WALL_NE), "wall")
    _eq(classify_terrain("t_window_domestic", catalog)[0], "window", "window flag")
    _eq(classify_terrain("t_wall_glass", catalog)[0], "window", "glass wall")
    _eq(classify_terrain("t_door_c", catalog), ("door", PREFAB_DOOR), "door")
    _eq(classify_terrain("t_wood_stairs_up", catalog), ("stairs", PREFAB_STAIR), "stairs")
    _eq(classify_terrain("t_water_sh", catalog), ("water", PREFAB_SHALLOW), "water")
    _eq(classify_terrain("t_floor", catalog), ("floor", PREFAB_FLOOR), "indoor floor")
    _eq(
        classify_terrain("t_region_groundcover_urban", catalog),
        ("floor", PREFAB_GRASS),
        "urban groundcover",
    )
    _eq(classify_terrain("t_region_shrub", catalog)[0], "skip", "unmapped shrub")
    _eq(map_furniture_prefab("f_bed"), PREFAB_BED, "bed")
    _eq(map_furniture_prefab("f_wardrobe"), PREFAB_WARDROBE, "wardrobe")
    _eq(map_furniture_prefab("f_sofa"), "f_sofa", "keep BN id")
    _eq(map_furniture_prefab("f_dresser"), "f_dresser", "dresser not wardrobe")
    _eq(map_furniture_prefab("f_crate_c"), PREFAB_CRATE, "actual crate")
    _eq(map_furniture_prefab("f_indoor_plant"), "f_indoor_plant", "plant keeps id")
    _eq(map_furniture_prefab("f_null"), None, "null furniture")


def test_convert_tiny_house():
    catalog = {
        "t_floor": {"flags": ["INDOORS", "FLAT"], "move_cost": 2},
        "t_wall_w": {"flags": ["WALL"], "move_cost": 0},
        "t_door_c": {"flags": ["DOOR"], "move_cost": 0},
    }
    # BN row 0 is north. 3x3: walls around, door south, bed inside.
    rows = [
        "###",
        "#@#",
        "#+#",
    ]
    terrain = {"#": "t_wall_w", "+": "t_door_c", "@": "t_floor"}
    furniture = {"@": "f_bed"}
    out = convert_rows(rows, terrain, furniture, catalog, "t_floor", 0)
    wall_prefabs = [t["prefabId"] for t in out["tiles"] if t["prefabId"].startswith("ThickWall/")]
    _eq(len(wall_prefabs), 7, "seven wall cells")
    _eq(any(t["prefabId"] == PREFAB_DOOR for t in out["tiles"]), True, "door")
    beds = [t for t in out["tiles"] if t["prefabId"] == PREFAB_BED]
    _eq(len(beds), 1, "one bed")
    _eq((beds[0]["x"], beds[0]["y"], beds[0]["z"]), (1, 0, 1), "bed cell north-up flip")
    floors = {(f["x"], f["y"], f["z"]) for f in out["floorFaces"]}
    _eq((1, -1, 1) in floors, True, "floor under bed CellBelow")
    _eq((1, -1, 0) in floors, True, "floor under door")
    _eq(any(f["prefabId"] == PREFAB_FLOOR for f in out["floorFaces"]), True, "indoor floor prefab")
    _eq(out["skippedFurniture"], {}, "no skipped furniture")


def test_wall_axis_and_skip_plant():
    catalog = {
        "t_floor": {"flags": ["INDOORS", "FLAT"], "move_cost": 2},
        "t_wall_w": {"flags": ["WALL"], "move_cost": 0},
    }
    rows = [
        ".#.",
        ".#.",
        ".#.",
    ]
    terrain = {"#": "t_wall_w", ".": "t_floor"}
    furniture = {".": "f_indoor_plant"}
    out = convert_rows(rows, terrain, furniture, catalog, "t_floor", 0)
    walls = [t for t in out["tiles"] if t["prefabId"].startswith("ThickWall/")]
    _eq({t["prefabId"] for t in walls}, {PREFAB_WALL_WN}, "NS run → Wall_WN")
    plants = [t for t in out["tiles"] if t["prefabId"] == "f_indoor_plant"]
    _eq(len(plants), 6, "plants keep BN id on floor cells")


def test_water_and_window():
    catalog = {
        "t_floor": {"flags": ["INDOORS", "FLAT"], "move_cost": 2},
        "t_window_domestic": {"flags": ["BARRICADABLE_WINDOW"], "move_cost": 0},
        "t_water_sh": {"flags": ["SWIMMABLE"], "move_cost": 4},
    }
    rows = ["o~"]
    terrain = {"o": "t_window_domestic", "~": "t_water_sh"}
    out = convert_rows(rows, terrain, {}, catalog, "t_floor", 0)
    _eq(any(t["prefabId"] == PREFAB_SLIM_NE for t in out["tiles"]), True, "window slim")
    _eq(len(out["liquidAuthoringFaces"]), 1, "water is liquid face")
    _eq(out["liquidAuthoringFaces"][0]["prefabId"], PREFAB_SHALLOW, "shallow")
    _eq(out["liquidAuthoringFaces"][0]["y"], -1, "water CellBelow")


def test_bounds_and_dto_shape():
    dto = empty_map_dto()
    dto["tiles"].append({
        "x": 2, "y": 0, "z": 4, "sizeX": 1, "sizeY": 1, "sizeZ": 1,
        "prefabId": PREFAB_DOOR, "tileType": 0, "face": 0,
        "seedItemId": "", "plantedWorldMinute": 0, "fertilized": False,
        "lastFruitHarvestWorldMinute": 0, "fishTrapBaitId": "",
        "fishTrapBaitRemaining": 0, "fishTrapDeployedMinute": 0,
        "fishTrapAccumulatedFish": 0,
    })
    apply_bounds(dto)
    _eq(dto["hasMapBounds"], True, "bounds on")
    _eq(dto["mapBoundsMinX"], 2, "min x")
    _eq(dto["mapBoundsMaxZ"], 4, "max z")
    _eq(dto["schemaVersion"], 1, "schema")
    _eq(dto["wallEdges"], [], "no edge walls")


def main():
    test_pick_map_id_weighted()
    test_layer_suffix()
    test_classify_and_furniture()
    test_convert_tiny_house()
    test_wall_axis_and_skip_plant()
    test_water_and_window()
    test_bounds_and_dto_shape()
    print("ok")


if __name__ == "__main__":
    main()
