// ============================================================
// CharacterGridFootprintDefaults — 캐릭터 grid footprint 기본값·클램프 SSOT
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class CharacterGridFootprintDefaults
    {
        public static readonly Vector3Int Default = new Vector3Int(1, 2, 1);

        public static Vector3Int Clamp(Vector3Int footprint) =>
            new Vector3Int(
                Mathf.Max(1, footprint.x),
                Mathf.Max(1, footprint.y),
                Mathf.Max(1, footprint.z));
    }
}
