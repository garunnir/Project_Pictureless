// ============================================================
// FishWorkClipCatalog — 낚시 Work 모션 클립·대기시간 SSOT
// ============================================================

using IsoTilemap;
using UnityEngine;

[CreateAssetMenu(fileName = "FishWorkClipCatalog", menuName = "Dist/Fishing/Work Clip Catalog")]
public sealed class FishWorkClipCatalog : ScriptableObject
{
    public const string DefaultAssetPath =
        "Assets/Dist/SOData/Gameplay/Fishing/FishWorkClipCatalog.asset";

    [SerializeField] AnimationClip _cast;
    [SerializeField] AnimationClip _deployTrap;
    [SerializeField] AnimationClip _collectTrap;
    [SerializeField, Min(0f)] float _castDurationSeconds = MapFishConsts.CastWorkDurationSeconds;
    [SerializeField, Min(0f)] float _deployTrapDurationSeconds = MapFishConsts.DeployTrapWorkDurationSeconds;
    [SerializeField, Min(0f)] float _collectTrapDurationSeconds = MapFishConsts.CollectTrapWorkDurationSeconds;

    /// <summary>MapGameplayBootstrap이 주입. Resources 폴백 없음.</summary>
    public static FishWorkClipCatalog Runtime { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void DomainReset() => Runtime = null;

    public static void BindRuntime(FishWorkClipCatalog catalog) => Runtime = catalog;

    public AnimationClip Resolve(FishCellActionKind kind)
    {
        switch (kind)
        {
            case FishCellActionKind.Cast:
                return _cast;
            case FishCellActionKind.DeployTrap:
                return _deployTrap;
            case FishCellActionKind.CollectTrap:
                return _collectTrap;
            default:
                return null;
        }
    }

    public float ResolveDuration(FishCellActionKind kind)
    {
        switch (kind)
        {
            case FishCellActionKind.Cast:
                return Mathf.Max(0f, _castDurationSeconds);
            case FishCellActionKind.DeployTrap:
                return Mathf.Max(0f, _deployTrapDurationSeconds);
            case FishCellActionKind.CollectTrap:
                return Mathf.Max(0f, _collectTrapDurationSeconds);
            default:
                return 0f;
        }
    }
}
