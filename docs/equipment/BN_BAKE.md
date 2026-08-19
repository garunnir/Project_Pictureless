# BN bake (converter whitelist)

Canonical for `Tools/bn_converter/convert.py` → `Assets/StreamingAssets/BNData`.  
Gear runtime (Wear/Wield) stays in [`GEAR.md`](GEAR.md). Catalog locale (names / descriptions / recipe categories): [`ITEM_NAMES.md`](../inventory/ITEM_NAMES.md).

## Policy

- Dist **does not** dump Cataclysm-BN JSON. The converter is a **whitelist**: only fields with a Dist POCO + (planned) consumer.
- Three buckets: **Baked** · **Parked** (promote when Dist has a consumer) · **Won't** (decided not to use — do not sneak into `export_*`).
- BN-useful ≠ Dist-useful. Promote a Parked field when Dist grows a consumer, not because the key exists upstream.
- When promoting: `convert.py` `export_*` + `ItemData` / detail POCO + **this doc** (move the row) + rebake `BNData`. Do not leave GEAR.md as a second catalog.
- Reversing a **Won't** row needs an explicit edit in that section (reason change), not a silent whitelist add.

Command:

```text
python Tools/bn_converter/convert.py --bn-path <Cataclysm-BN> --output Assets/StreamingAssets/BNData
```

`--locale-only` keeps `items.json` / `recipes.json` and rewrites `item_names.json` (`names` / `descriptions` / `recipe_categories`).

Does **not** overwrite `GameData/items.json` demo seeds.

Item **icons** are not `ItemData` fields. MSX++ tileset bake (separate from this converter):

```text
python Tools/bn_converter/export_tileset_icons.py --bn-path <Cataclysm-BN> --items Assets/StreamingAssets/BNData/items.json --output Assets/StreamingAssets/BNData/tileset --tileset MSX++UnDeadPeopleEdition
```

Writes `BNData/tileset/item_sprites.json` + referenced PNG. Runtime: `ItemVisualPresenter` (catalog override → tileset → default). Bake follows BN `looks_like` and implicit `copy-from` (item_factory, max 10 hops) from Cataclysm-BN JSON — still not a Dist POCO. Chain miss → default icon.

## Baked ( Dist typed fields )

Item common: `id`, `name`(singular), `type`, `category`, `subcategory`, `description`, `weight`→`weight_g`, `volume`→`volume_ml`, `stack_size`/`count`→`max_stack`, `material`→`materials`, `flags`, `qualities`, `comestible_type`, `has_durability`, `repairs_like`, `repair_difficulty`, `bashing`, `cutting`, `to_hit`, `weapon_category`, `techniques`.

| Block | Fields |
|-------|--------|
| armor | covers (L/R expand), coverage, encumbrance, max_encumbrance, warmth, environmental_protection, material_thickness, power_armor, storage, pockets[{volume_ml,moves}], layer, sided |
| gun | skill, ammo, ranged_damage (flatten amount), range, dispersion, recoil, durability, clip_size, reload, **burst** (Dist: Burst Leaf 샷 수; 0이면 `WeaponActionUtil.DefaultBurstShots`) |
| ammo | ammo_type, damage (flatten amount), pierce (AP), damage_type, range, dispersion, recoil, count, shot_damage, projectile_count, shot_spread, effects, casing, loudness |
| magazine | ammo_type, capacity, default_ammo, reliability, reload_time |
| tool | max/initial charges, charges_per_use, turns_per_charge, ammo, revert_to |
| comestible | calories, quench, fun, spoils_in_minutes, charges, healthy, stim, addiction_type |
| book | intelligence, fun, chapters, read_time_minutes; item `book_skill` / required / max level |
| container | seals, watertight, preserves; root `containers[]` volume (weight ≈ ml) |
| material | bash/cut/bullet/acid/fire/chip resist, density |
| quality / skill | id, name |
| recipe | result, skills, difficulty, time_minutes, tools/components (`using` inlined), book_learn, byproducts, proficiencies, activity_level, morale, hot_result, dehydrating. Skips obsolete / never_learn / CC_BUILDING / construction_blueprint |

**Silent-zero rule:** if a BN value is an object (`damage`, `ranged_damage`, `shot_damage`), flatten — never `_int_or_zero` on the object. Same trap for future `damage_modifier` (gunmod). Dist combat reads `ammo.damage` + `gun.ranged_damage` and `ammo.damage_type` when a round is chambered (`ItemInstance.ChamberAmmoId`). `GameData/items.json` must not reuse BN item ids (`9mm`, …) or it overlays RefData.

## Won't bake (decision)

Do not add these to Dist POCOs. Dist sprites / `item_names.json` / recipe filters replace them.

| BN | Why not |
|----|---------|
| `ascii_picture` | ASCII art for curses; Dist does not render it |
| `symbol` | Curses map glyph |
| `color` | Curses glyph color — not Unity sprite tint |
| `looks_like` | Tileset inheritance. Converter already drops it on copy-from merge. Icons: `export_tileset_icons.py` walks BN source `looks_like`/`copy-from` at bake time. Do not promote into Dist POCOs |
| `str_pl` / other plural name keys | Display SSOT is `item_names.json` (`ITEM_NAMES.md`). Converter keeps singular `name` only as bake `en` source |
| remaining unknown keys as `ItemData` fields or an extra JSON bag | Catalog-in-this-doc, not a dump. Revisit only by replacing this row |
| recipe `obsolete` / `never_learn` / `CC_BUILDING` / `construction_blueprint` | Converter **skips** these recipes (not Dist content) |

Parked is everything in the next section. Weather JSON / visor FOV stay Parked (Dist has stand-ins, not a never).

## Parked — promote when Dist has a consumer

### Item common (display / economy / physics)

| BN | Notes |
|----|--------|
| `price` / `price_postapoc` | Shop / barter |
| `phase` | solid / liquid / gas |
| `longest_side` / `rigid` / `integral_volume` | Pocket fit / rigidity |
| `snippet_category` / `conditional_names` | Flavor text variants |

### Gates / actions

| BN | Notes |
|----|--------|
| `min_str` / `min_dex` / `min_int` / `min_per` | Lift currently uses weight formula only |
| `min_skills` | Skill gate |
| `use_action` / `drop_action` / `tick_action` / `countdown_*` | Tool-action runner (GEAR milestone) |
| `explosion` / `explode_in_fire` / `emits` | Explosion / field |
| `seed_data` / `brewable` / `milling` / `fuel` | Farming / brewing / fuel |
| `relic_data` | Artifacts |
| wear/wield **move cost** field | Dist: `GearActionDuration` proxy |

### Gun (beyond current `GunDetailData`)

`loudness`, `handling`, **`modes`**, `valid_mod_locations`, `built_in_mods`, `default_mods`, `magazines`, `magazine_well`, `ups_charges`, `ammo_effects`, `sight_dispersion`, `aim_speed`, `barrel_length` / `barrel_volume`, `reload_noise`, `blackpowder_tolerance`, `min_cycle_recoil`.

**`modes` (Parked) vs Dist Leaf (interim):**

| BN / Dist | Status |
|-----------|--------|
| `gun.burst` | **Baked** — Burst Leaf `ShotsPerPerform` (= burst, else default 3) |
| `gun.clip_size` | **Baked** — Auto Leaf 클릭 볼리 상한(`AutoClickVolleyMax`와 min) |
| `gun.modes` JSON | **Parked** — 아직 컨버터 미반입. Dist는 Presentation Leaf(Semi/Burst/Auto) + UI Family `Trigger`로 대체 |
| Leaf Ensure | Presentation: `Ensure Ranged Leaf Entries`. Catalog 폴백: `Ensure Arm Anim Pipeline` — **Semi/Burst/Auto 행 필수** |
| Semi / Burst / Auto 시전 | `SpawnProjectileHandler` 볼리. Catalog는 Leaf마다 폴백 행 ([`LOCOMOTION.md`](../locomotion/LOCOMOTION.md)) |
| Auto 홀드 연사 | **Pending** — 현재는 클릭당 볼리 |
| `modes` bake | Promote when Dist maps BN mode ids → Leaf mask without manual Ensure |

Chamber finds magazines by nested `magazine` block (not `gun.magazines` list). Combat already uses baked ammo `damage` / `damage_type` / `pierce` / `range` / `dispersion`.

### Gunmod (`GUNMOD`)

Type is loaded; **no gunmod block**. Only entries with `gun_data` get a `gun` (underbarrel).

`location`, `mod_targets`, `damage_modifier` (damage object — flatten like ammo), `dispersion_modifier`, `recoil_modifier`, `range_modifier`, `ammo_modifier`, `loudness_modifier`, `sight_dispersion`, `aim_speed`, `handling_modifier`, install time, `magazine_adaptor`.

Needs a Dist install/slot consumer before bake.

### Ammo (beyond current `AmmoDetailData`)

`drop`, `critical_multiplier`, `dont_recover_one_in`, `show_stats`, special cookoff / fuel-ammo blobs.

### Armor (beyond current `ArmorDetailData`)

Per-part coverage/encumbrance objects; `environmental_protection_with_filter`; visor/FOV JSON (Dist: `HelmetVision` stand-in); `pocket_data` keys other than volume + moves (watertight, item restrictions, max length). BN tree currently has ~0 `pocket_data`.

### Comestible / book / magazine / tool / container

| Type | Not baked |
|------|-----------|
| COMESTIBLE | `vitamins`, `addiction_potential`, `parasites`, `cooks_like`, `freeze_point`, `rot_spawn`, `smoking_result`, `monotony_penalty` |
| BOOK | martial art, recipes/proficiencies taught by the book |
| MAGAZINE | `linkage` |
| TOOL | `charged_qualities` (use_action: see gates) |
| CONTAINER | `airtight`, `unseals_into` (max_weight is ml≈g approximation) |

### Material / skill / quality / recipe extras

| Domain | Not baked |
|--------|-----------|
| material | `burn_data`, `fuel_data`, `dmg_adj`, `conductive`, `edible`, vitamins, `sheet_thickness`, elec/wind resist, salvage/repair links |
| skill / quality | description, tags, usages |
| recipe | `flags`, `batch_time_factors`, `decomp_learn`, `contained` |

### Types loaded as generic (almost no dedicated bake)

`BIONIC_ITEM`, `ENGINE`, `WHEEL`, `PET_ARMOR`, `TOOLMOD`, `BATTERY`, `MIGRATION`.

### Trees `convert.py` does not load

Monsters, mutations, vehicle parts, terrain/furniture/traps, mapgen/overmap, weather JSON (Dist: `WeatherKind` stand-in), NPC/missions, spells, harvest.

## Rebake log

| Run | Scope | Result |
|-----|-------|--------|
| Full `convert.py` (prior) | items+materials+qualities+skills+recipes/uncraft | 5591 items, 862 armor; layer≈423, sided≈48, storage≈194, pockets=0 |
| 2026-08-12 flatten rebake | same trees; damage object → amount/pierce/`damage_type` | 5591 items. ammo.damage nonzero 289/312; `damage_type` 312 (bullet 250, stab 41, heat 11, bash 10); pierce 203; gun.ranged_damage nonzero 180/185. Sample `9mm` damage 34 bullet. `GameData` demo `9mm`/`mag_9mm` removed so they do not overlay BN |
| Combat ammo consume | `ChamberAmmoId` + `CombatMath` 탄+총 양, `damage_type` Hit | Dist runtime |

When a row moves Parked → Baked, or a Won't reason is reversed, add a rebake line here.
