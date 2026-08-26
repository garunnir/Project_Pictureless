// ============================================================
// FarmWorkClipCatalog — 농사 Work 모션 클립·대기시간 SSOT
// ============================================================

using IsoTilemap;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmWorkClipCatalog", menuName = "Dist/Farming/Work Clip Catalog")]
public sealed class FarmWorkClipCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Farming/FarmWorkClipCatalog.asset";

    [SerializeField] AnimationClip _plant;
    [SerializeField] AnimationClip _till;
    [SerializeField] AnimationClip _harvest;
    [SerializeField, Min(0f)] float _plantDurationSeconds = MapPlantConsts.PlantWorkDurationSeconds;
    [SerializeField, Min(0f)] float _tillDurationSeconds = MapPlantConsts.TillWorkDurationSeconds;

    public AnimationClip Resolve(FarmCellActionKind kind)
    {
        switch (kind)
        {
            case FarmCellActionKind.Plant:
                return _plant;
            case FarmCellActionKind.Till:
                return _till;
            case FarmCellActionKind.Harvest:
                return _harvest;
            default:
                return null;
        }
    }

    public float ResolveDuration(FarmCellActionKind kind)
    {
        switch (kind)
        {
            case FarmCellActionKind.Plant:
                return Mathf.Max(0f, _plantDurationSeconds);
            case FarmCellActionKind.Till:
                return Mathf.Max(0f, _tillDurationSeconds);
            default:
                return 0f;
        }
    }
}
