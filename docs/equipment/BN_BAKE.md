# BN bake (converter whitelist)

Canonical for `Tools/bn_converter/convert.py` → `Assets/StreamingAssets/BNData`.  
Gear runtime (Wear/Wield) stays in [`GEAR.md`](GEAR.md). Catalog locale (names / descriptions / recipe categories / qualities): [`ITEM_NAMES.md`](../inventory/ITEM_NAMES.md).

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

`--locale-only` keeps existing `items.json` / `recipes.json` and rewrites `item_names.json` (`names` / `descriptions` / `recipe_categories` / `qualities`).

Does **not** overwrite `GameData/items.json` demo seeds.

Item **icons** are not `ItemData` fields. MSX++ tileset bake (separate from this converter):

```text
python Tools/bn_converter/export_tileset_icons.py --bn-path <Cataclysm-BN> --items Assets/StreamingAssets/BNData/items.json --output Assets/StreamingAssets/BNData/tileset --tileset MSX++UnDeadPeopleEdition
```

Writes `BNData/tileset/item_sprites.json` + referenced PNG. Runtime: `ItemVisualPresenter` (catalog override → tileset → default). Bake follows BN `looks_like` and implicit `copy-from` (item_factory, max 10 hops) from Cataclysm-BN JSON — still not a Dist POCO. Chain miss → default icon.

## Baked ( Dist typed fields )

Item common: `id`, `name`(singular), `type`, `category`, `subcategory`, `description`, `weight`→`weight_g`, `volume`→`volume_ml`, `stack_size`/`count`→`max_stack`, `material`→`materials`, `flags`, `qualities`, `comestible_type`, `has_durability`, `repairs_like`, `repair_difficulty`, `bashing`, `cutting`, `to_hit`, `weapon_category`, `techniques`, consume `use_action` (`heal` / `consume_drug` / `ANTIBIOTIC` / `WEAK_ANTIBIOTIC` / `STRONG_ANTIBIOTIC`: `type`; heal keeps BN power keys `limb_power` / `bandages_power` / `head_power` / `torso_power` / `amount` / `bleed`; consume_drug `effect_id`/`duration`). Dist JSON `type` is lowercase (`antibiotic`, `weak_antibiotic`, `strong_antibiotic`). **BN 키 이름 유지** — 여러 BN 키를 Dist 한 키로 접거나 개명 금지 (`heal_amount` 금지). BN에 없는 키는 출력에 넣지 않음(`bleed` 포함). 중첩 객체는 **같은 키**에서 스칼라로만 unwrap.

| Block | Fields |
|-------|--------|
| armor | covers (L/R expand), coverage, encumbrance, max_encumbrance, warmth, environmental_protection, material_thickness, power_armor, storage, pockets[{volume_ml,moves}], layer, sided |
| gun | skill (**Catalog By Skill Id** 모션 폴백), ammo, ranged_damage (flatten amount), range, dispersion, recoil, **sight_dispersion**, **aim_speed**, **handling**, durability, clip_size, reload, **burst** (Dist: Burst Leaf 샷 수; 0이면 `WeaponActionUtil.DefaultBurstShots`), **magazines** `[{ammo_type, magazines[]}]` (장착 허용 탄창 id) |
| ammo | ammo_type, damage (flatten amount), pierce (AP), damage_type, range, dispersion, recoil, count, shot_damage, projectile_count, shot_spread, effects, casing, loudness |
| magazine | ammo_type, capacity, default_ammo, reliability, reload_time |
| tool | max/initial charges, charges_per_use, turns_per_charge, ammo, revert_to |
| comestible | calories, quench, fun, spoils_in_minutes, charges, healthy, stim, addiction_type, **addiction_potential**, **vitamins** `{id: amount}` |
| book | intelligence, fun, chapters, read_time_minutes; item `book_skill` / required / max level |
| container | seals, watertight, preserves; root `containers[]` volume (weight ≈ ml) |
| material | bash/cut/bullet/acid/fire/chip resist, density |
| quality / skill | id, name |
| recipe | result, skills, difficulty, time_minutes, tools/components (`using` inlined), book_learn, byproducts, proficiencies, activity_level, morale, hot_result, dehydrating. Skips obsolete / never_learn / CC_BUILDING / construction_blueprint |
| seed | fruit, plant_name, grow_minutes, seeds, fruit_div, byproducts, required_terrain_flag (when `seed_data` present). Int `grow` = season-days × minutes-per-day — not recipe moves/100 |
| terrain / furniture | id, name, farming flags (`PLANTABLE`, `PLOWABLE`, `PLANT`, `GROWTH_SEED`, `GROWTH_SEEDLING`, `GROWTH_MATURE`, `GROWTH_HARVEST`). Furniture `plant_data`: transform, base, growth_multiplier, harvest_multiplier. Tree: `data/json/furniture_and_terrain` (copy-from resolved). Output: `terrain_furniture.json` |

**Silent-zero rule:** if a BN value is an object (`damage`, `ranged_damage`, `shot_damage`, heal `limb_power` / consume_drug `effects`), unwrap the number onto **that same BN key** — never `_int_or_zero` on the object, never rename the key. Same trap for future `damage_modifier` (gunmod). Dist combat reads `ammo.damage` + `gun.ranged_damage` and `ammo.damage_type` when a round is chambered (`ItemInstance.ChamberAmmoId`). `GameData/items.json` must not reuse BN item ids (`9mm`, …) or it overlays RefData.

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
| terrain/furniture `symbol` / `color` / `looks_like` / `bash` / `deconstruct` | Curses glyph, tileset inheritance, smash/deconstruct dump. Farming whitelist only — do not sneak into `export_terrain_entry` / `export_furniture_entry` |

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
| `drop_action` / `tick_action` / `countdown_action` / `countdown_interval` / `countdown_destroy` | Tool-action runner (GEAR milestone). Not consume. |
| `explosion` / `explode_in_fire` / `emits` | Explosion / field |
| `brewable` / `milling` / `fuel` | Brewing / milling / fuel |
| `relic_data` | Artifacts |
| wear/wield **move cost** field | Dist: `GearActionDuration` proxy |

#### `use_action` tags (BN `Item_factory::init`)

BN `use_action` is either a **string iuse** (`"ANTIBIOTIC"`) or an **actor object** (`{ "type": "heal", ... }`). Dist does not dump unknown types into `ItemData`.

**Baked consume** (converter `CONSUME_USE_ACTION_TYPES`; Dist `UseActionData.type` lowercase):

| BN | Dist `type` | Consumer |
|----|-------------|----------|
| `heal` | `heal` | `BodyHealApply` (지정 `partId`). 즉시 HP는 `limb_power` / `head_power` / `torso_power` (`amount` 폴백). `bandages_power` → `bandaged`(영구). `bleed`만(붕대 없음) → 지혈. Dist는 붕대 아이템의 `bleed`를 무시. Do not emit `heal_amount` |
| `consume_drug` | `consume_drug` | `ConsumeService` drug + effect |
| `ANTIBIOTIC` | `antibiotic` | immunity race 2× (`BodyIllness`) |
| `WEAK_ANTIBIOTIC` | `weak_antibiotic` | immunity race 1.5× |
| `STRONG_ANTIBIOTIC` | `strong_antibiotic` | immunity race 3× |

**Parked string iuse** (`add_iuse` — consume가 아니거나 Dist 소비처 없음):

`ACIDBOMB_ACT`, `ADRENALINE_INJECTOR`, `ALCOHOL`, `ALCOHOL_STRONG`, `ALCOHOL_WEAK`, `AMPUTATE`, `ANTIASTHMATIC`, `ANTICONVULSANT`, `ANTIFUNGAL`, `ANTIPARASITIC`, `ARROW_FLAMABLE`, `ARTIFACT`, `AUTOCLAVE`, `BELL`, `BLECH`, `BLECH_BECAUSE_UNCLEAN`, `BOLTCUTTERS`, `C4`, `C4_BREACHING`, `TOW_ATTACH`, `CABLE_ATTACH`, `CAMERA`, `CAN_GOO`, `COIN_FLIP`, `DIRECTIONAL_HOLOGRAM`, `CAPTURE_MONSTER_ACT`, `CAPTURE_MONSTER_VEH`, `RPGDIE`, `BURROW`, `CHOP_TREE`, `CHOP_LOGS`, `CLEAR_RUBBLE`, `CONTACTS`, `CROWBAR`, `DATURA`, `DIG`, `DIVE_TANK`, `DIRECTIONAL_ANTENNA`, `DISASSEMBLE`, `CRAFT`, `DOG_WHISTLE`, `DOLLCHAT`, `EHANDCUFFS`, `EINKTABLETPC`, `EXTINGUISHER`, `EYEDROPS`, `FILL_PIT`, `FIRECRACKER`, `FIRECRACKER_ACT`, `FIRECRACKER_PACK`, `FIRECRACKER_PACK_ACT`, `FISH_ROD`, `FISH_TRAP`, `FLUMED`, `FLUSLEEP`, `FOODPERSON`, `FUNGICIDE`, `GASMASK`, `GEIGER`, `DEBUG_GRENADE`, `DEBUG_GRENADE_ACT`, `GRENADE_INC_ACT`, `GUN_CLEAN`, `GUN_REPAIR`, `GUNMOD_ATTACH`, `TOOLMOD_ATTACH`, `HACKSAW`, `HAIRKIT`, `HAMMER`, `HONEYCOMB`, `INHALER`, `JACKHAMMER`, `JET_INJECTOR`, `LADDER`, `LUMBER`, `MAGIC_8_BALL`, `PLAY_GAME`, `MAKEMOUND`, `DIG_CHANNEL`, `MARLOSS`, `MARLOSS_GEL`, `MARLOSS_SEED`, `MA_MANUAL`, `MEDITATE`, `METH`, `MININUKE`, `MOLOTOV_LIT`, `MOP`, `MP3_ON`, `MYCUS`, `NOISE_EMITTER_OFF`, `NOISE_EMITTER_ON`, `NOTE_BIONICS`, `OXYGEN_BOTTLE`, `OXYTORCH`, `PACK_CBM`, `PACK_ITEM`, `PETFOOD`, `PHEROMONE`, `PICK_LOCK`, `PICKAXE`, `PLANTBLECH`, `POISON`, `PORTABLE_GAME`, `PORTAL`, `PROZAC`, `PURIFIER`, `PURIFY_IV`, `PURIFY_SMART`, `RADGLOVE`, `RADIOCAR`, `RADIOCARON`, `RADIOCONTROL`, `RADIO_MOD`, `RADIO_OFF`, `RADIO_ON`, `REMOTEVEH`, `REMOVE_ALL_MODS`, `REPORT_GRID_CHARGE`, `REPORT_GRID_CONNECTIONS`, `MODIFY_GRID_CONNECTIONS`, `REPORT_FLUID_GRID_CONNECTIONS`, `MODIFY_FLUID_GRID_CONNECTIONS`, `ROBOTCONTROL`, `SEED`, `SEWAGE`, `SHAVEKIT`, `SIPHON`, `SLEEP`, `SOLARPACK`, `SOLARPACK_OFF`, `SPRAY_CAN`, `STIMPACK`, `TAZER`, `TAZER2`, `TELEPORT`, `THORAZINE`, `THROWABLE_EXTINGUISHER_ACT`, `TOWEL`, `TOGGLE_HEATS_FOOD`, `TOGGLE_UPS_CHARGING`, `UNFOLD_GENERIC`, `UNPACK_ITEM`, `VACCINE`, `CALL_OF_TINDALOS`, `BLOOD_DRAW`, `MIND_SPLICER`, `VIBE`, `VORTEX`, `WATER_PURIFIER`, `WEATHER_TOOL`, `XANAX`, `BULLET_VIBE_ON`, `HOTPLATE`, `HEAT_FOOD`, `HEATPACK`

**Parked iuse_actor `type`** (`add_actor` — JSON object `type`; `heal`/`consume_drug`는 위 Baked):

`ammobelt`, `bandolier`, `cauterize`, `delayed_transform`, `set_transform`, `set_transformed`, `enzlave`, `explosion`, `firestarter`, `fireweapon_off`, `fireweapon_on`, `holster`, `inscribe`, `transform`, `unpack`, `countdown`, `manualnoise`, `musical_instrument`, `deploy_furn`, `place_monster`, `change_scent`, `cloning_syringe`, `dna_editor`, `place_npc`, `reveal_map`, `unfold_vehicle`, `place_trap`, `emit`, `saw_barrel`, `saw_stock`, `install_bionic`, `detach_gunmods`, `mutagen`, `mutagen_iv`, `deploy_tent`, `learn_spell`, `cast_spell`, `weigh_self`, `gps_device`, `sew_advanced`, `multicooker`, `hand_crank`, `sex_toy`, `train_skill`, `music_player`, `prospect_pick`, `reveal_contents`, `flowerpot_plant`, `flowerpot_collect`, `dimension_travel`, `pocket_dimension`, `portal_link`, `paint_stuff`, `paint_stuff_config`

출처: Cataclysm-BN `src/item_factory.cpp` `Item_factory::init`. 태그 하나를 Dist에서 쓰게 되면 이 목록에서 빼고 Baked 표로 옮긴다.

### Gun (beyond current `GunDetailData`)

`loudness`, **`modes`**, `valid_mod_locations`, `built_in_mods`, `default_mods`, `magazine_well`, `ups_charges`, `ammo_effects`, `barrel_length` / `barrel_volume`, `reload_noise`, `blackpowder_tolerance`, `min_cycle_recoil`.

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

`gun.magazines` is **Baked** — Dist 장착/교체가 허용 탄창 id를 본다. Combat feed is `ItemStack.LoadedMagazine` + `SupplyRounds` (not Nested). Ammo `damage` / `damage_type` / `pierce` / `range` / `dispersion` still from chambered round.

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
| COMESTIBLE | `parasites`, `cooks_like`, `freeze_point`, `rot_spawn`, `smoking_result`, `monotony_penalty` |
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

Monsters, mutations, vehicle parts, traps, mapgen/overmap, weather JSON (Dist: `WeatherKind` stand-in), NPC/missions, spells, harvest.

`furniture_and_terrain` is **opened** (farming whitelist only — see Baked).

## Rebake log

| Run | Scope | Result |
|-----|-------|--------|
| Full `convert.py` (prior) | items+materials+qualities+skills+recipes/uncraft | 5591 items, 862 armor; layer≈423, sided≈48, storage≈194, pockets=0 |
| 2026-08-12 flatten rebake | same trees; damage object → amount/pierce/`damage_type` | 5591 items. ammo.damage nonzero 289/312; `damage_type` 312 (bullet 250, stab 41, heat 11, bash 10); pierce 203; gun.ranged_damage nonzero 180/185. Sample `9mm` damage 34 bullet. `GameData` demo `9mm`/`mag_9mm` removed so they do not overlay BN |
| Combat ammo consume | `ChamberAmmoId` + `CombatMath` 탄+총 양, `damage_type` Hit | Dist runtime |

| 2026-08-19 gun aim fields | `sight_dispersion`, `aim_speed`, `handling` whitelist | convert.py 승격. Dist: RMB `aim01` 조임 + handling 킥 배율. BNData rebake는 BN 트리 있을 때 |
| 2026-08-22 consume fields | `vitamins`, `addiction_potential`, consume `use_action` heal/consume_drug | convert.py 승격. GameData `consumable_egg` comestible seed. BNData rebake는 BN 트리 있을 때 |
| 2026-08-24 seed + terrain/furniture farming | `seed_data` whitelist; `furniture_and_terrain` farming flags + furniture `plant_data` | convert.py 승격. 64 seed items; `terrain_furniture.json` 665 terrain / 339 furniture. brewable/milling/fuel Parked |
| 2026-08-24 antibiotic use_action | `ANTIBIOTIC` / `WEAK_ANTIBIOTIC` / `STRONG_ANTIBIOTIC` | convert.py 승격. Dist: 면역 레이스 배율. BNData 3 items에 `use_action.type` 기입. 전체 트리 rebake는 BN 경로 있을 때 |
| 2026-08-24 heal key names | `limb_power` / `bandages_power` / `head_power` / `torso_power` / `amount` | convert.py가 `heal_amount`로 접지 않음. Dist `UseActionData` 동명 필드. 2026-08-24 full rebake: `heal_amount` 0건. `bandages_power` 6 (`bandages*` 4 + cotton_ball + medical_gauze), `limb_power` 1 (`rag`=0) |
| 2026-08-25 heal `bleed` | `bleed` whitelist passthrough (absent → omit) | Dist: `bandages_power`면 붕대(JSON `bleed` 무시). `bleed`만이면 지혈. BNData rebake는 BN 트리 있을 때 |
