# Item display names (localization)

Canonical for BN/custom **item display names** and how they tie to Data Definitions + fonts.

## Decision

- **Data Definitions** (`Tools/Data Definitions`) is the hub for serialized editable game data. Prefer editing here (or entering from here); do not scatter parallel edit UIs.
- **Display names are locale-only.** Consumers must go through `UITextPresenter.GetItemName` / `ItemNameTable`. Do **not** use `ItemData.name` for display, edit, or fallback.
- Lookup key is **item `id`**, slots **`en` / `ko` / `ja`**.
- Editing **Name** in Definitions writes `GameData/item_names.json` for the **active** language — same as editing locale data. It does **not** change `items.json` `name`.
- If Definitions is the better place to edit a setting, **do not add a separate SO** just for that UX. Exception: one `LocalizationBundle` SO holds TMP font asset references and active language. Language/font change is **rare** — edit the bundle in Inspector (Definitions toolbar `Loc Bundle` pings it). Do **not** put language/font fields inline on the Definitions hub.
- **Fonts + language are one bundle** (`LocalizationBundle`). `DistUiFont` reads the active language’s font from that bundle.
- Cataclysm-BN `lang/po/*.po` is **convert import only**. Runtime never looks up by English msgid.
- Converter field whitelist / not-baked catalog: [`docs/equipment/BN_BAKE.md`](../equipment/BN_BAKE.md).

## Data

| Path | Role |
|------|------|
| `Assets/StreamingAssets/BNData/item_names.json` | BN bake: id → `{ en, ko?, ja? }` |
| `Assets/StreamingAssets/GameData/item_names.json` | Project overlay (Definitions Name edits + custom seeds) |
| `Assets/Dist/Resources/Localization/LocalizationBundle.asset` | Active language + per-lang TMP fonts |

Regenerate BN names:

```text
python Tools/bn_converter/convert.py --bn-path <Cataclysm-BN> --output Assets/StreamingAssets/BNData
```

## Lookup

1. Optional Dist force: `Loc` key `Item.{id}` (`UI_ko`)
2. `item_names[id][activeLang]` (GameData overlay wins over BNData)
3. If active ≠ `en` and miss → `en`
4. Else `[Missing: ItemName {id}]` — **never** `ItemData.name`

Default active language: **`ko`**.

## Caution

- Do not display via `ItemData.name` / `stack.Item.name`.
- Do not mass-edit `item_names.json` by hand when Definitions/convert can do it.
- Do not use English msgid as runtime key; do not copy `ko.po`/`ja.po` into StreamingAssets.
- Do not merge BN item names into `UI_ko.asset`.
- Do not use Liberation Sans or unset TMP fonts; set fonts on `LocalizationBundle` (ko/en default Galmuri7). JA unset falls back to ko with a warning (possible tofu).
- Do not keep a second SSOT for language (EditorPrefs-only, etc.) parallel to the bundle.
- Do not put Korean (or other) display strings into custom `items.json` `name` — use locale slots.
- Description / other string locales are follow-ups; keep the same **id + language code** pattern.

## Related

- Hub window: `Assets/Dist/Scripts/Editor/BN/GameDataEditorWindow.cs`
- Runtime: `UITextPresenter`, `ItemNameTable`, `LocalizationBundle`, `DistUiFont`
- UI chrome keys remain `UI_ko` / `Loc` (separate from item display names)
