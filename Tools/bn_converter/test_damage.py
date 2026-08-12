"""BN damage object flatten + copy-from merge. Run: python test_damage.py"""

from convert import (
    deep_merge,
    export_ammo_detail,
    export_gun_detail,
    flatten_damage,
)


def _eq(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: {actual!r} != {expected!r}")


def test_flatten_int():
    _eq(flatten_damage(26), {"amount": 26, "pierce": 0, "damage_type": ""}, "int")


def test_flatten_object():
    _eq(
        flatten_damage(
            {"damage_type": "bullet", "amount": 26, "armor_penetration": 2}
        ),
        {"amount": 26, "pierce": 2, "damage_type": "bullet"},
        "object",
    )


def test_flatten_values_array():
    _eq(
        flatten_damage(
            {
                "values": [
                    {"damage_type": "bash", "amount": 10, "armor_penetration": 1},
                    {"damage_type": "cut", "amount": 4, "pierce": 3},
                ]
            }
        ),
        {"amount": 14, "pierce": 4, "damage_type": "bash"},
        "values",
    )


def test_export_ammo_object():
    ammo = export_ammo_detail(
        {
            "ammo_type": "9mm",
            "damage": {"damage_type": "bullet", "amount": 26, "armor_penetration": 2},
            "range": 14,
            "count": 50,
            "casing": "9mm_casing",
            "effects": ["COOKOFF"],
            "loudness": 90,
        },
        "AMMO",
    )
    _eq(ammo["damage"], 26, "ammo.damage")
    _eq(ammo["pierce"], 2, "ammo.pierce")
    _eq(ammo["damage_type"], "bullet", "ammo.damage_type")
    _eq(ammo["casing"], "9mm_casing", "ammo.casing")
    _eq(ammo["effects"], ["COOKOFF"], "ammo.effects")
    _eq(ammo["loudness"], 90, "ammo.loudness")


def test_export_ammo_legacy_int():
    ammo = export_ammo_detail(
        {"ammo_type": "9mm", "damage": 26, "pierce": 2},
        "AMMO",
    )
    _eq(ammo["damage"], 26, "legacy damage")
    _eq(ammo["pierce"], 2, "legacy pierce")
    _eq("damage_type" in ammo, False, "legacy no type")


def test_export_gun_object():
    gun = export_gun_detail(
        {"skill": "pistol", "ranged_damage": {"damage_type": "bullet", "amount": 5}},
        "GUN",
    )
    _eq(gun["ranged_damage"], 5, "gun.ranged_damage")


def test_copyfrom_relative_damage():
    parent = {
        "damage": {"damage_type": "bullet", "amount": 26, "armor_penetration": 2}
    }
    child = {"relative": {"damage": {"amount": 4, "armor_penetration": 1}}}
    merged = deep_merge(parent, child)
    _eq(merged["damage"]["amount"], 30, "relative amount")
    _eq(merged["damage"]["armor_penetration"], 3, "relative AP")
    _eq(merged["damage"]["damage_type"], "bullet", "relative keeps type")


def test_copyfrom_proportional_damage():
    parent = {"damage": {"damage_type": "cut", "amount": 20}}
    child = {"proportional": {"damage": {"amount": 0.5}}}
    merged = deep_merge(parent, child)
    _eq(merged["damage"]["amount"], 10, "proportional amount")
    _eq(merged["damage"]["damage_type"], "cut", "proportional keeps type")


def test_copyfrom_partial_damage_override():
    parent = {
        "damage": {"damage_type": "bullet", "amount": 26, "armor_penetration": 2}
    }
    child = {"damage": {"amount": 18}}
    merged = deep_merge(parent, child)
    _eq(merged["damage"]["amount"], 18, "override amount")
    _eq(merged["damage"]["damage_type"], "bullet", "override keeps type")
    _eq(merged["damage"]["armor_penetration"], 2, "override keeps AP")


def test_shot_fields():
    ammo = export_ammo_detail(
        {
            "ammo_type": "shot",
            "damage": {"damage_type": "bullet", "amount": 5},
            "shot_damage": {"damage_type": "bullet", "amount": 15, "armor_penetration": 1},
            "projectile_count": 9,
            "shot_spread": 30,
        },
        "AMMO",
    )
    _eq(ammo["damage"], 5, "shell damage")
    _eq(ammo["shot_damage"], 15, "pellet damage")
    _eq(ammo["projectile_count"], 9, "pellets")
    _eq(ammo["shot_spread"], 30, "spread")


if __name__ == "__main__":
    tests = [
        test_flatten_int,
        test_flatten_object,
        test_flatten_values_array,
        test_export_ammo_object,
        test_export_ammo_legacy_int,
        test_export_gun_object,
        test_copyfrom_relative_damage,
        test_copyfrom_proportional_damage,
        test_copyfrom_partial_damage_override,
        test_shot_fields,
    ]
    for fn in tests:
        fn()
        print(f"ok {fn.__name__}")
    print(f"{len(tests)} passed")
