// ============================================================
// MapBloodHitSink — 히트 시 Impact+Direction 콘 혈흔 spray
// ============================================================

using IsoTilemap;
using UnityEngine;

public static class MapBloodHitSink
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        CharacterAttacker.AnyAttackJudged -= OnAnyAttackJudged;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        CharacterAttacker.AnyAttackJudged -= OnAnyAttackJudged;
        CharacterAttacker.AnyAttackJudged += OnAnyAttackJudged;
    }

    static void OnAnyAttackJudged(AttackOutcome outcome)
    {
        if (!outcome.DidHit || outcome.Damage <= 0)
            return;

        MapBloodHost host = MapBloodHost.Runtime;
        if (host == null)
            return;

        int count = Mathf.Clamp(
            MapBloodConsts.HitSprayMinCount + outcome.Damage / 4,
            MapBloodConsts.HitSprayMinCount,
            MapBloodConsts.HitSprayMaxCount);

        host.Spray(
            outcome.ImpactPoint,
            outcome.Direction,
            count,
            MapBloodConsts.HitSprayConeHalfRad,
            MapBloodConsts.HitSprayMinDist,
            MapBloodConsts.HitSprayMaxDist,
            MapBloodConsts.HitSprayGroundBias,
            MapBloodConsts.DefaultScale,
            MapBloodConsts.DefaultAlpha);
    }
}
