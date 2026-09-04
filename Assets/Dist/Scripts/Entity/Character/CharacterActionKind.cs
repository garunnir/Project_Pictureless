// ============================================================
// CharacterActionKind — 행위자 행동 큐 잡 종류
// ============================================================

public enum CharacterActionKind
{
    None = 0,
    Gear = 1,
    Inventory = 2,
    Combat = 3,
    Craft = 4,

    /// <summary>
    /// 그리드/월드 셀 스크립트 행동 슬롯 (도착·농사·낚시·건설·vault).
    /// TileMap 시스템과 무관 — 행동큐 종류 태그만.
    /// </summary>
    Cell = 5
}
