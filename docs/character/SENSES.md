# Character Senses (Sight + Hearing)

**SSOT:** `CharacterDefinition.Senses` (`CharacterSenseBlock`)  
**시력:** `CharacterVision` — XZ 부채꼴 (`IsWithinConeXZ`)  
**청력:** `CharacterHearing` + `CharacterHearingEvaluator` — 3D 구형 (시야 API **미사용**)  
**채널:** `CharacterSenseContactResolver` — **Vision > Hearing**

## CharacterSenseBlock

| 필드 | 기본 SSOT | 런타임 |
|------|-----------|--------|
| `sightDetectMeters` | `CharacterVisionDefaults.DetectRadius` (10) | `CharacterVision.EffectiveDetectRadius` × HelmetVision |
| `sightLoseMeters` | `CharacterVisionDefaults.LoseRadius` (14) | `CharacterVision.EffectiveLoseRadius` × HelmetVision |
| `hearingRadiusMeters` | `CharacterHearingDefaults.BaseRadius` (8) | `CharacterHearing.EffectiveHearingRadius` |

`spotAngleDegrees`는 SenseBlock 밖 — Definition 필드 그대로 `CharacterVision`에 실시간 반영.

## Vision vs Hearing

| | 시력 | 청력 |
|---|------|------|
| 공간 | XZ 부채꼴 | 3D 구 |
| 거리 | `sqrt(dx²+dz²)` | `Vector3.Distance` |
| 방향 | forward + spotAngle | 없음 |
| 이동 | 불필요 | `CurrentSpeed ≥ MovementSpeedThreshold` |
| occlusion | SightFade 3D LOS (표현) | grid 벽 segment + \|ΔgridY\| 층 감쇠 |

## CharacterSenseContactResolver

한 틱·한 대상:

```text
Resolve(visionActive, hearingActive) → Vision 우선
```

| 채널 | NPC steer | Attack | Alert | 플레이어 핑 |
|------|-----------|--------|-------|------------|
| Vision | target transform | O | O | 없음 |
| Hearing | 적 grid 셀 | X | X (바로 Chase) | grid alpha (페이드 숨김) |
| None | — | — | ClearTarget | 없음 |

NPC: `NpcManager.RefreshTarget` + 단일 `Chase` + `ResolveSteerGoal`.  
플레이어: `CharacterHearingPingDriver` — `ShowsHearingPing` + `DisplayVisibility ≤ HiddenThreshold`.

## 청각 핑 (플레이어)

- **Driver:** `CharacterHearingPingDriver` (`IMapHearingPingDriver`)
- **Overlay:** `MapHearingPingOverlay` / `MapHearingPingRenderer` / `MapHearingPingHost`
- **튜닝:** `CharacterHearingPingSettings` (HiddenThreshold, MaxAlpha, Y offset, fade)
- 적 **메시는 SightFade** — 핑은 **바닥 quad** (셀 중심, audibility × MaxAlpha)
- 같은 셀 다수 적 → **max(audibility)** 1 quad

## 맵 바인딩

- `CharacterHearing.BindMapCollision(MapTopologyLineCast)` — `MapGameplayBootstrap` (Start + 스폰 증분)
- 청각 핑 — `TileMapManager`가 `IMapHearingPingDriver.Init` + `MapHearingPingHost` Ensure

## 디버그

`Tools/Character Runtime Debug` → Combat 탭 → **Senses** (Definition base / effective radii). 스킬 목록에 넣지 않음.

## See also

[`DEFINITION.md`](DEFINITION.md) · [`SIGHT_FADE.md`](SIGHT_FADE.md)
