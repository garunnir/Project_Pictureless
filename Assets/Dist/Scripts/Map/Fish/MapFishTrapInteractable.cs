// ============================================================
// MapFishTrapInteractable — 통발 오버레이 셀·컨텍스트 타겟
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    [DisallowMultipleComponent]
    public sealed class MapFishTrapInteractable : MonoBehaviour
    {
        public Vector3Int Cell { get; private set; }

        public void BindCell(Vector3Int cell) => Cell = cell;
    }
}
