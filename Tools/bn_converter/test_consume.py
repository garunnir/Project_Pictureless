"""BN consume field export. Run: python test_consume.py"""

from convert import (
    export_comestible_detail,
    export_consume_use_action,
    export_item_game_detail,
    flatten_vitamins,
)


def _eq(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: {actual!r} != {expected!r}")


def test_flatten_vitamins_pairs():
    _eq(
        flatten_vitamins([["vitC", 12], ["calcium", 4]]),
        {"vitC": 12, "calcium": 4},
        "pairs",
    )


def test_flatten_vitamins_object():
    _eq(
        flatten_vitamins({"iron": 2, "calcium": {"amount": 8}}),
        {"iron": 2, "calcium": 8},
        "object",
    )


def test_export_comestible_consume_fields():
    comestible = export_comestible_detail(
        {
            "calories": 80,
            "quench": 2,
            "fun": 1,
            "spoils_in": "1 d",
            "addiction_type": "caffeine",
            "addiction_potential": 5,
            "vitamins": [["vitC", 6]],
        },
        "COMESTIBLE",
    )
    _eq(comestible["calories"], 80, "calories")
    _eq(comestible["quench"], 2, "quench")
    _eq(comestible["fun"], 1, "fun")
    _eq(comestible["spoils_in_minutes"], 1440.0, "spoils")
    _eq(comestible["addiction_type"], "caffeine", "addiction_type")
    _eq(comestible["addiction_potential"], 5, "addiction_potential")
    _eq(comestible["vitamins"], {"vitC": 6}, "vitamins")


def test_export_heal_keeps_limb_power_key():
    action = export_consume_use_action(
        {
            "use_action": {
                "type": "heal",
                "limb_power": {"amount": 7},
                "move_cost": 300,
            }
        }
    )
    _eq(action, {"type": "heal", "limb_power": 7}, "heal limb_power object")
    _eq("heal_amount" in action, False, "no Dist heal_amount")


def test_export_heal_keeps_limb_power_range():
    action = export_consume_use_action(
        {"use_action": {"type": "heal", "limb_power": [3, 8]}}
    )
    _eq(action["type"], "heal", "heal type")
    _eq(action["limb_power"], 3, "heal range first")
    _eq("heal_amount" in action, False, "no Dist heal_amount")


def test_export_heal_keeps_all_power_keys():
    action = export_consume_use_action(
        {
            "use_action": {
                "type": "heal",
                "limb_power": 2,
                "bandages_power": 4,
                "head_power": 1,
            }
        }
    )
    _eq(
        action,
        {
            "type": "heal",
            "limb_power": 2,
            "bandages_power": 4,
            "head_power": 1,
        },
        "heal keeps BN keys",
    )


def test_export_heal_keeps_bleed_key():
    action = export_consume_use_action(
        {"use_action": {"type": "heal", "bleed": 0.9, "limb_power": 0}}
    )
    _eq(action["type"], "heal", "heal type")
    _eq(action["bleed"], 1, "bleed rounded")
    _eq(action["limb_power"], 0, "limb_power zero kept")
    _eq("bandages_power" in action, False, "no invented bandages_power")


def test_export_heal_omits_absent_bleed():
    action = export_consume_use_action(
        {"use_action": {"type": "heal", "bandages_power": 4}}
    )
    _eq("bleed" in action, False, "absent bleed omitted")
    _eq(action["bandages_power"], 4, "bandages_power")


def test_export_consume_drug_flattens_effect():
    action = export_consume_use_action(
        {
            "use_action": {
                "type": "consume_drug",
                "activation_message": "You swallow it.",
                "effects": [{"id": "pkill1", "duration": 720}],
            }
        }
    )
    _eq(
        action,
        {"type": "consume_drug", "effect_id": "pkill1", "duration": 720},
        "consume_drug",
    )


def test_export_antibiotic_string():
    _eq(
        export_consume_use_action({"use_action": "ANTIBIOTIC"}),
        {"type": "antibiotic"},
        "ANTIBIOTIC string",
    )


def test_export_weak_antibiotic_string():
    _eq(
        export_consume_use_action({"use_action": "WEAK_ANTIBIOTIC"}),
        {"type": "weak_antibiotic"},
        "WEAK_ANTIBIOTIC string",
    )


def test_export_strong_antibiotic_object():
    _eq(
        export_consume_use_action({"use_action": {"type": "STRONG_ANTIBIOTIC"}}),
        {"type": "strong_antibiotic"},
        "STRONG_ANTIBIOTIC object",
    )


def test_export_ignores_non_consume_use_action():
    _eq(
        export_consume_use_action({"use_action": {"type": "place_monster"}}),
        None,
        "non-consume",
    )


def test_item_detail_exports_consume_use_action_only():
    detail = export_item_game_detail(
        {
            "type": "COMESTIBLE",
            "use_action": {"type": "heal", "limb_power": 4},
            "drop_action": {"type": "explode"},
            "tick_action": {"type": "transform"},
            "countdown_action": {"type": "explosion"},
            "countdown_interval": "1 d",
            "countdown_destroy": True,
        },
        "COMESTIBLE",
    )
    _eq(detail["use_action"], {"type": "heal", "limb_power": 4}, "use_action")
    for key in (
        "drop_action",
        "tick_action",
        "countdown_action",
        "countdown_interval",
        "countdown_destroy",
    ):
        _eq(key in detail, False, key)


if __name__ == "__main__":
    tests = [
        test_flatten_vitamins_pairs,
        test_flatten_vitamins_object,
        test_export_comestible_consume_fields,
        test_export_heal_keeps_limb_power_key,
        test_export_heal_keeps_limb_power_range,
        test_export_heal_keeps_all_power_keys,
        test_export_heal_keeps_bleed_key,
        test_export_heal_omits_absent_bleed,
        test_export_consume_drug_flattens_effect,
        test_export_antibiotic_string,
        test_export_weak_antibiotic_string,
        test_export_strong_antibiotic_object,
        test_export_ignores_non_consume_use_action,
        test_item_detail_exports_consume_use_action_only,
    ]
    for fn in tests:
        fn()
        print(f"ok {fn.__name__}")
    print(f"{len(tests)} passed")
