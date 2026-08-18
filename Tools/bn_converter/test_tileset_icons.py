"""first_fg / looks_like. Run: python test_tileset_icons.py"""

from export_tileset_icons import (
    collect_ids,
    first_fg,
    looks_like_target,
    map_item_sprites,
    normalize_looks_like,
    resolve_item_sprite,
)


def _eq(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: {actual!r} != {expected!r}")


def test_first_fg_int():
    _eq(first_fg(112), 112, "int")


def test_first_fg_list():
    _eq(first_fg([10, 11, 12, 13]), 10, "rot list")


def test_first_fg_weighted():
    _eq(first_fg([{"weight": 1, "sprite": 4544}, {"weight": 1, "sprite": 4545}]), 4544, "weighted")


def test_first_fg_weighted_rot():
    _eq(first_fg([{"weight": 8, "sprite": [1112, 1114, 1113, 1111]}]), 1112, "weighted rot")


def test_first_fg_empty():
    _eq(first_fg(None), None, "none")
    _eq(first_fg([]), None, "empty list")


def test_collect_ids():
    _eq(collect_ids("apple"), ["apple"], "str")
    _eq(collect_ids(["a", "b"]), ["a", "b"], "list")
    _eq(collect_ids(""), [], "empty str")


def test_normalize_looks_like():
    _eq(normalize_looks_like("apple"), "apple", "str")
    _eq(normalize_looks_like(["a", "b"]), "a", "list first")
    _eq(normalize_looks_like(""), None, "empty")


def _looks(eid, by_id, abstracts):
    return looks_like_target(eid, by_id, abstracts, {}, set())


def test_looks_like_explicit():
    by_id = {"child": {"id": "child", "looks_like": "apple"}}
    _eq(_looks("child", by_id, {}), "apple", "explicit")


def test_looks_like_copy_from_concrete():
    by_id = {
        "parent": {"id": "parent"},
        "child": {"id": "child", "copy-from": "parent"},
    }
    _eq(_looks("child", by_id, {}), "parent", "copy-from concrete")


def test_looks_like_explicit_overrides_copy_from():
    by_id = {
        "parent": {"id": "parent"},
        "child": {"id": "child", "copy-from": "parent", "looks_like": "apple"},
    }
    _eq(_looks("child", by_id, {}), "apple", "explicit wins")


def test_looks_like_abstract_inherits():
    abstracts = {"base": {"abstract": "base", "looks_like": "apple"}}
    by_id = {"child": {"id": "child", "copy-from": "base"}}
    _eq(_looks("child", by_id, abstracts), "apple", "abstract inherit")


def test_resolve_walks_looks_like():
    tile_map = {"apple": {"file": "small.png", "index": 1}}
    by_id = {
        "apple": {"id": "apple"},
        "cider": {"id": "cider", "looks_like": "apple"},
    }
    sprite = resolve_item_sprite("cider", tile_map, by_id, {}, {})
    _eq(sprite, {"file": "small.png", "index": 1}, "walk")


def test_resolve_cycle():
    tile_map = {}
    by_id = {
        "a": {"id": "a", "looks_like": "b"},
        "b": {"id": "b", "looks_like": "a"},
    }
    _eq(resolve_item_sprite("a", tile_map, by_id, {}, {}), None, "cycle")


def test_map_counts_direct_vs_looks():
    tile_map = {"apple": {"file": "small.png", "index": 1}}
    by_id = {
        "apple": {"id": "apple"},
        "cider": {"id": "cider", "looks_like": "apple"},
        "orphan": {"id": "orphan"},
    }
    mapped, direct, via_looks = map_item_sprites(
        {"apple", "cider", "orphan"}, tile_map, by_id, {}
    )
    _eq(direct, 1, "direct")
    _eq(via_looks, 1, "looks")
    _eq(set(mapped), {"apple", "cider"}, "ids")


if __name__ == "__main__":
    test_first_fg_int()
    test_first_fg_list()
    test_first_fg_weighted()
    test_first_fg_weighted_rot()
    test_first_fg_empty()
    test_collect_ids()
    test_normalize_looks_like()
    test_looks_like_explicit()
    test_looks_like_copy_from_concrete()
    test_looks_like_explicit_overrides_copy_from()
    test_looks_like_abstract_inherits()
    test_resolve_walks_looks_like()
    test_resolve_cycle()
    test_map_counts_direct_vs_looks()
    print("ok")
