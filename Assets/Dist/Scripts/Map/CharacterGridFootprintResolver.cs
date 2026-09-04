// ============================================================
// CharacterGridFootprintResolver — CapsuleCollider → grid footprint 해석
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    public static class CharacterGridFootprintResolver
    {
        public static Vector3Int Resolve(
            CapsuleCollider capsule,
            float cellSize,
            Vector3Int baseFootprint)
        {
            baseFootprint = CharacterGridFootprintDefaults.Clamp(baseFootprint);
            Vector3Int derived = DeriveFromCapsule(capsule, cellSize);
            if (derived.x <= 0)
                return baseFootprint;

            return new Vector3Int(
                Mathf.Max(baseFootprint.x, derived.x),
                Mathf.Max(baseFootprint.y, derived.y),
                Mathf.Max(baseFootprint.z, derived.z));
        }

        public static Vector3Int DeriveFromCapsule(CapsuleCollider capsule, float cellSize)
        {
            if (capsule == null || cellSize <= 0f)
                return Vector3Int.zero;

            Transform capsuleTransform = capsule.transform;
            Vector3 lossyScale = capsuleTransform.lossyScale;
            float horizontalScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
            float verticalScale = Mathf.Abs(lossyScale.y);

            int cellsXz = Mathf.Max(
                1,
                Mathf.CeilToInt(capsule.radius * 2f * horizontalScale / cellSize));
            int cellsY = Mathf.Max(
                1,
                Mathf.CeilToInt(capsule.height * verticalScale / cellSize));

            return new Vector3Int(cellsXz, cellsY, cellsXz);
        }
    }
}
