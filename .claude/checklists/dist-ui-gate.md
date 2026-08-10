# Dist UI Gate (검증·Task용)

UI 경로를 **수정**한 작업에만 사용. 비UI 도메인 전용 작업에는 붙이지 않는다.

## Trigger paths
- `Assets/Dist/Scripts/UI/**`
- `Assets/Dist/Visual/Prefabs/UIComponents/**`
- Dist Editor UI Patch/빌더 (`Assets/Dist/Scripts/Editor/**` UI 관련)

## MUST paste into Task / verify agent prompts

```text
## Dist UI MUST (path gate)
- Read and obey: .cursor/rules/ui-font.mdc , .cursor/rules/ui-prefab-layout.mdc
- TMP: Katuri via DistUiFont or prefab-serialized font — no Liberation/unset
- Chrome layout: prefab SSOT — no runtime new+magic Rect/Layout for window chrome
- Fail either rule → task Incomplete (do not declare done)
```

## Verify agent checklist
- [ ] New/edited `TextMeshProUGUI`: font is Katuri (`DistUiFont.Apply` or prefab field)
- [ ] No Liberation Sans / null font on Dist UI labels touched this task
- [ ] Window chrome (TabBar, panels, menus meant as layout) lives on prefab; runtime only binds/pools rows
- [ ] No new permanent full-bake menu (`ui-prefab-bake.mdc`)
- [ ] If any Fail → Overall **Incomplete**

## Parent agent
UI 경로를 서브에이전트에 맡길 때 위 MUST 블록을 프롬프트에 포함. 규칙 인덱스: `.cursor/rules/00-rules-index.mdc` §Dist UI path gate.
