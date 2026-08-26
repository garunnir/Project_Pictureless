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

    public const string ResourcesAssetPath =
        "Assets/Dist/Resources/Fishing/FishWorkClipCatalog.asset";

    /// <summary>Player build Resources.Load SSOT (확장자 제외).</summary>
    public const string ResourcesLoadName = "Fishing/FishWorkClipCatalog";

    [SerializeField] AnimationClip _cast;
    [SerializeField] AnimationClip _deployTrap;
    [SerializeField] AnimationClip _collectTrap;
    [SerializeField, Min(0f)] float _castDurationSeconds = MapFishConsts.CastWorkDurationSeconds;
    [SerializeField, Min(0f)] float _deployTrapDurationSeconds = MapFishConsts.DeployTrapWorkDurationSeconds;
    [SerializeField, Min(0f)] float _collectTrapDurationSeconds = MapFishConsts.CollectTrapWorkDurationSeconds;

    static FishWorkClipCatalog _runtime;

    public static FishWorkClipCatalog Runtime
    {
        get => _runtime;
        set => _runtime = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void DomainReset() => _runtime = null;

    public static FishWorkClipCatalog ResolveRuntimeCatalog() =>
        _runtime != null ? _runtime : _runtime = Resources.Load<FishWorkClipCatalog>(ResourcesLoadName);

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
