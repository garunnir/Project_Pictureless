// ============================================================
// CharacterFootprintHost — CharacterDefinition grid footprint 런타임 보관
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterFootprintHost : MonoBehaviour
{
    [SerializeField] Vector3Int _gridFootprint = CharacterGridFootprintDefaults.Default;

    public Vector3Int GridFootprint => CharacterGridFootprintDefaults.Clamp(_gridFootprint);

    public void ApplyFromDefinition(CharacterDefinition definition)
    {
        _gridFootprint = definition != null
            ? definition.GridFootprint
            : CharacterGridFootprintDefaults.Default;
    }

    void OnValidate()
    {
        _gridFootprint = CharacterGridFootprintDefaults.Clamp(_gridFootprint);
#if UNITY_EDITOR
        ValidateFootprintHeightAgainstCapsule();
#endif
    }

#if UNITY_EDITOR
    const float FootprintHeightEpsilonMeters = 0.05f;

    void ValidateFootprintHeightAgainstCapsule()
    {
        if (!CharacterBodyResolve.TryGetInBody(this, out CapsuleCollider capsule))
            return;

        float cellSize = ResolveEditorCellSize();
        float expectedHeight = _gridFootprint.y * cellSize;
        float capsuleHeight = capsule.height;
        float tolerance = Mathf.Max(FootprintHeightEpsilonMeters, expectedHeight * 0.1f);
        if (Mathf.Abs(expectedHeight - capsuleHeight) <= tolerance)
            return;

        Debug.LogWarning(
            $"[{nameof(CharacterFootprintHost)}] '{name}' grid footprint Y={_gridFootprint.y} " +
            $"× cellSize={cellSize:0.###} → {expectedHeight:0.###} m, " +
            $"but CapsuleCollider.height={capsuleHeight:0.###} m.",
            this);
    }

    static float ResolveEditorCellSize()
    {
        TileMapManager manager = Object.FindFirstObjectByType<TileMapManager>();
        if (manager != null && manager.WorldGrid != null && manager.WorldGrid.CellSize > 0f)
            return manager.WorldGrid.CellSize;
        return 1f;
    }
#endif
}
