# Project Pictureless

**[프로젝트 위키 (Notion)](https://platinum-snowstorm-e11.notion.site/2a4f453fbe768022a0aad18946279d37?v=2a4f453fbe7680e4ac3c000c97ff3a01)**

Unity 6 기반 **아이소메트릭 타일맵** 개인 프로젝트.  
확장 가능한 맵·가시성·캐릭터 상호작용을 직접 구현하며, **몰입감 있는 스토리·관계·시각 표현**을 목표로 한다.

---

## 프로젝트 소개

| 항목 | 내용 |
|------|------|
| 엔진 | Unity **6000.3.10f1** (Unity 6) |
| 렌더 파이프라인 | **URP** (Universal Render Pipeline) |
| 주요 씬 | `Assets/Dist/Scenes/IsoLand.unity` |
| 커스텀 코드 | `Assets/Dist/` (`DistScript` 어셈블리) |

유니티·C# 학습, 포트폴리오, 개인 흥미를 겸한 프로젝트다.  
규모를 키운 이유는 **최적화 경험**과 **확장 가능한 세계**를 만들기 위해서다.

---

## 설계 철학 — 몰입

게임에서 중요하게 보는 것은 **몰입**이다. 아래 세 가지를 특히 의식한다.

| 영감 | 추구하는 요소 |
|------|----------------|
| 헤븐 번즈 레드 | 공들인 선택지의 스토리텔링 |
| 림월드 | 구체적이고 밀접한 캐릭터 간 상호작용 |
| 좀보이드 | 직관적이고 합리적인 시각 표현 |

큰 틀부터 잡고 세부 디테일을 채워 나가는 방식으로 진행한다.

---

## 기술 스택

- **렌더링**: URP, SRP Batcher 호환 셰이더 프로퍼티 패턴 (`ShaderController`)
- **입력**: Unity Input System
- **카메라**: Cinemachine 3
- **에셋 로딩**: Addressables
- **비동기**: UniTask
- **대화**: Pixel Crushers Dialogue System
- **에디터·디버그**: Odin Inspector, Ingame Debug Console
- **기타**: DOTween, LeanPool, UniRx

---

## 요구 사항

- [Unity 6000.3.10f1](https://unity.com/releases/editor/whats-new/6000.3.10) (또는 호환되는 Unity 6.3.x)
- Windows / macOS / Linux 중 Unity가 지원하는 플랫폼

---

## 시작하기

1. 저장소를 클론한다.
   ```bash
   git clone <repository-url>
   cd Project_Pictureless
   ```
2. Unity Hub에서 **Unity 6000.3.10f1**로 프로젝트를 연다.
3. 첫 실행 시 패키지·Library 복원이 끝날 때까지 기다린다.
4. 메인 씬을 연다: `Assets/Dist/Scenes/IsoLand.unity`
5. Play로 실행한다.

> `Library/`, `Temp/`, `Logs/` 등은 `.gitignore` 대상이다. 클론 직후에는 Unity가 자동 생성한다.

### IDE

- **Visual Studio** / **Rider** / **Cursor** (`com.boxqkrtm.ide.cursor` 패키지 포함)
- 솔루션: `Project_Pictureless.sln` (Unity가 재생성할 수 있음)

---

## 프로젝트 구조

```
Project_Pictureless/
├── Assets/
│   ├── Dist/                    # 프로젝트 고유 에셋·스크립트 (작업 핵심)
│   │   ├── Scenes/              # 게임·테스트 씬
│   │   ├── Scripts/             # C# 게임 로직
│   │   ├── Visual/              # 셰이더, 프리팹, 뷰 리소스
│   │   ├── Resources/           # 런타임 리소스
│   │   ├── SOData/              # ScriptableObject 데이터
│   │   └── DialogueAssets/      # 대화 시스템 에셋
│   ├── Settings/                # URP, Rendering Layer, 아웃라인 설정
│   └── Plugins/                 # 서드파티 (Dialogue System, DOTween 등)
├── Packages/manifest.json       # UPM 의존성
├── ProjectSettings/
├── .cursor/rules/               # 에이전트·렌더링·맵 스트리밍 규약
└── CLAUDE.md                    # AI 에이전트 작업 규칙
```

### `Assets/Dist/Scripts/` 주요 모듈

| 폴더 | 역할 |
|------|------|
| `Map/` | 타일맵 로드·저장·가시성·충돌·청크 스트리밍 |
| `Entity/`, `Player/` | 캐릭터 상태, 이동, 오클루전 브로드캐스트 |
| `Interactions/` | 문·상호작용 |
| `UI/` | MVC 기반 UI (맵, 스테이터스, 선택지 팝업) |
| `BattleSystem/` | 반자동 전투 (진행 중) |
| `Camera/` | 카메라·시점 |
| `StateMachine/` | 캐릭터 상태 머신 |
| `Debug/` | 런타임·에디터 디버그 토글 (`Config.DebugMode`) |

---

## 핵심 시스템

### 타일맵 (Map)

**MVC + Pipeline + Observer** 패턴으로 구성된다.

- **Coordinator**: `TileMapManager` — 로드·저장·뷰 바인딩 조율
- **Model**: `TileMapModel` — 런타임 타일 상태, BFS 오클루전
- **View**: `TileMapVisualizer` / `TileView` — GameObject 생성·갱신
- **스트리밍**: `TileMapChunkStreamer` — 카메라 footprint 기준 청크 로드/언로드
- **저장**: JSON (`MapSaveJsonDto`) — 프로젝트 루트 `map01.json` 등

### 가시성·오클루전

타일·캐릭터 가시성은 **독립된 3개 축**으로 동작한다.

1. **층 가시성** — 실내/야외, buildingId·cellY 기준 renderer 토글
2. **근접 시선 블렌드** — 카메라↔플레이어 밴드, 셰이더 `_CharacterOcclusion`
3. **시선 차단 건물** — 야외 차단 건물 1층 바닥 윤곽

### 플레이어 이동·충돌

- `PlayerMovement` — 캡슐 기반 kinematic 이동, 관성·달리기
- `MapCollisionServices` / `MapTopologyDepenetration` — 그리드 토폴로지 충돌·끼임 탈출

### 렌더링 (URP)

- `MaterialPropertyBlock` 사용 금지 — SRP Batcher 호환을 위해 머티리얼 인스턴스 프로퍼티 직접 수정
- 오브젝트별 후처리(선택·하이라이트) — **RenderingLayer + RendererFeature**

---

## 씬 목록 (`Assets/Dist/Scenes/`)

| 씬 | 용도 |
|----|------|
| `IsoLand.unity` | 메인 아이소메트릭 월드 |
| `firstScene.unity` | 타이틀·모드 선택 UI |
| `TileWorldView.unity` | 타일맵 뷰 테스트 |
| `BattleSystemTest.unity` | 전투 시스템 테스트 |
| `TestConversation.unity` | 대화 시스템 테스트 |
| `UINSaveTest.unity` | UI·저장 테스트 |
| `Permission.unity` | 권한(네이티브 갤러리 등) 테스트 |

---

## 문서

상세 설계는 `Assets/Dist/Scripts/` 아래 마크다운을 참고한다.

| 문서 | 내용 |
|------|------|
| [Map/SYSTEM.md](Assets/Dist/Scripts/Map/SYSTEM.md) | 맵 시스템 전체 개요 |
| [Map/Components/COMPONENTS.md](Assets/Dist/Scripts/Map/Components/COMPONENTS.md) | MonoBehaviour 진입점 |
| [Map/Internal/DATA.md](Assets/Dist/Scripts/Map/Internal/DATA.md) | 타일 내부 데이터 구조 |
| [Map/TileMap/TILEMAP.md](Assets/Dist/Scripts/Map/TileMap/TILEMAP.md) | 타일맵 핵심 로직 |
| [Map/TileMap/TILEMAP_VISIBILITY.md](Assets/Dist/Scripts/Map/TileMap/TILEMAP_VISIBILITY.md) | 가시성·오클루전 |
| [UI/UI_Scripts.md](Assets/Dist/Scripts/UI/UI_Scripts.md) | UI MVC 구조 |

에이전트·렌더링 규약: `.cursor/rules/` (`urp-rendering.mdc`, `tile-chunk-streaming.mdc` 등)

---

## 개발 메모

- **디버그 플래그**: `Assets/Dist/Scripts/Config.cs` — `DebugLogController`가 런타임에 갱신
- **맵 JSON**: `TileMapManager` / `MapFileLoader`의 `fileName`으로 지정 (`usePersistentPath`로 영구 저장 경로 사용 가능)
- **컴포넌트 주석**: 새 Unity 컴포넌트 상단에 `// [ComponentName] — 한 줄 요약` 헤더 사용 (`CLAUDE.md` 참고)

---

## 라이선스

개인 학습·포트폴리오 프로젝트. 서드파티 에셋·플러그인은 각 패키지 라이선스를 따른다.
