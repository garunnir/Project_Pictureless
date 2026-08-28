// ============================================================
// TestRunner — 액체 흐름 수동 검증 하네스 (임시 — 검증 후 빈 스텁으로 원복)
// ============================================================
// 맵에 물을 넣는 게임 내 경로가 아직 없어(MapLiquidMlBridge.Pour 호출부 0개)
// FlowSolver가 한 번도 실행되지 않는다. 이 하네스가 Pour + 분 틱 + 지형/열 탐침을 제공한다.
// 검증 대상: MapLiquidFlowSolver StableBelowMl 오버플로 수정 + 1)↔3) 무한진동 차단.
//
// 씬 배치 불필요 — AfterSceneLoad에 자동 스폰된다. Play 중 Hierarchy에서 "[TestRunner]"를
// 선택하면 Inspector로 셀·ml을 재컴파일 없이 바꿀 수 있다.

using IsoTilemap;
using UnityEngine;

public class TestRunner : MonoBehaviour
{
    const string AutoSpawnName = "[TestRunner]";

    [SerializeField] TileMapManager _manager;

    [Header("Liquid — 붓기")]
    [Tooltip("물을 부을 셀. map01의 SHALLOW_WATER는 (-1,0,-2)·(-2,0,-1), DEEP_WATER는 (-1,0,-1)·(0,0,-2).")]
    [SerializeField] Vector3Int _pourCell = new Vector3Int(-1, 1, -2);

    [Tooltip("한 번에 부을 ml. 셀 정원은 1,000,000.")]
    [SerializeField] int _pourMl = 700_000;

    [Header("Liquid — 틱")]
    [Tooltip("틱 키 1회당 진행할 월드 분. 각 분마다 FlowSolver.ProcessDirty가 1회 돈다.")]
    [SerializeField] int _tickMinutes = 5;

    [Header("Liquid — 탐침 범위")]
    [Tooltip("_pourCell 기준 위로 몇 셀까지 찍을지.")]
    [SerializeField] int _probeAbove = 2;

    [Tooltip("_pourCell 기준 아래로 몇 셀까지 찍을지.")]
    [SerializeField] int _probeBelow = 3;

    [Header("Keys")]
    [SerializeField] KeyCode _pourKey = KeyCode.F9;
    [SerializeField] KeyCode _tickKey = KeyCode.F10;
    [SerializeField] KeyCode _probeKey = KeyCode.F11;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        // 씬에 수동 배치한 인스턴스가 있으면 중복 생성하지 않는다.
        if (FindAnyObjectByType<TestRunner>() != null)
            return;

        new GameObject(AutoSpawnName).AddComponent<TestRunner>();
    }
#endif

    void Start()
    {
        Debug.Log(
            $"[TestRunner] {_pourKey}=붓기 / {_tickKey}={_tickMinutes}분 진행 / {_probeKey}=지형·열 탐침. "
            + $"먼저 {_probeKey}로 occ=1 · floor=0 인 셀을 찾아 _pourCell에 넣을 것.");
    }

    void Update()
    {
        if (Input.GetKeyDown(_pourKey))
            PourOnce();

        if (Input.GetKeyDown(_tickKey))
            TickMinutes();

        if (Input.GetKeyDown(_probeKey))
            Probe("probe");
    }

    void PourOnce()
    {
        MapLiquidHost host = MapLiquidHost.Runtime;
        if (host == null)
        {
            Debug.LogError("[TestRunner] MapLiquidHost.Runtime null — 맵 로드가 끝난 Play 중에 눌러야 한다.");
            return;
        }

        int poured = MapLiquidMlBridge.Pour(host, _pourCell, MapLiquidConsts.WaterTypeId, _pourMl);
        if (poured <= 0)
        {
            Debug.LogError(
                $"[TestRunner] Pour 거부 {_pourCell} — 맵에 정의되지 않은 셀(occ=0). "
                + $"{_probeKey} 탐침에서 occ=1인 셀을 골라 다시 시도할 것.");
            return;
        }

        Debug.Log($"[TestRunner] Pour {poured} ml → {_pourCell}");
        Probe("after-pour");
    }

    void TickMinutes()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock == null)
        {
            Debug.LogError("[TestRunner] WorldClock.Instance null.");
            return;
        }

        // SetTime이 MinuteChanged를 동기로 발생시켜 MapLiquidHost.OnWorldMinuteChanged → ProcessDirty를 돌린다.
        // SetTime은 minuteOfDay를 wrap이 아니라 clamp하므로, 하루 마지막 분에서는 값이 그대로 남아
        // MinuteChanged가 발생하지 않는다. 분이 안 늘었으면 다음 날 0분으로 넘겨 틱을 잇는다.
        for (int i = 0; i < _tickMinutes; i++)
        {
            int before = clock.MinuteOfDay;
            clock.SetTime(clock.DayIndex, before + 1);
            if (clock.MinuteOfDay == before)
                clock.SetTime(clock.DayIndex + 1, 0);

            Probe($"tick{i + 1}");
        }
    }

    /// <summary>
    /// 열의 ml + 지형 게이트를 함께 찍는다 — 셀 선택 실패(occ=0)와 낙하 불가(floor=1)를 로그 한 번으로 가린다.
    /// </summary>
    void Probe(string label)
    {
        TileMapCacheHub hub = TileMapCacheHub.Runtime;

        for (int dy = _probeAbove; dy >= -_probeBelow; dy--)
        {
            var cell = new Vector3Int(_pourCell.x, _pourCell.y + dy, _pourCell.z);
            int ml = MapLiquidQuery.GetEffectiveMl(cell);

            // 인자 순서 주의: CellHasOccupancy(x, z, y) / CellHasFloor(x, cellY, z) — FlowSolver와 동일.
            string terrain = hub == null
                ? "hub=null"
                : $"occ={(hub.CellHasOccupancy(cell.x, cell.z, cell.y) ? 1 : 0)} "
                  + $"floor={(hub.CellHasFloor(cell.x, cell.y, cell.z) ? 1 : 0)}";

            Debug.Log(
                $"[TestRunner] {label} {cell} ml={ml} fill={MapLiquidQuery.Fill01(cell):F4} {terrain}");
        }

        // 낚시는 국소 fill이 아니라 아래로 누적한 수심을 본다 — 임계 미달이면 컬럼 ml로 원인이 보인다.
        int columnMl = MapLiquidQuery.ColumnMlDownward(_pourCell);
        Debug.Log(
            $"[TestRunner] {label} column {_pourCell} columnMl={columnMl}"
            + $" / need={MapFishConsts.FishableColumnMl}"
            + $" fishable={MapFishService.CellHasFishableWater(_pourCell)}");
    }
}
