// ============================================================
// MapBloodHitSink — 자상·절단 히트 시 Impact 콘 혈흔 spray
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
        if (!outcome.DidHit)
            return;

        MapBloodHost host = MapBloodHost.Runtime;
        if (host == null)
            return;

        if (outcome.DidSeverPart)
        {
            int severCount = Mathf.Clamp(
                MapBloodConsts.SeverSprayMinCount + outcome.Damage / 3,
                MapBloodConsts.SeverSprayMinCount,
                MapBloodConsts.SeverSprayMaxCount);
            host.Spray(
                outcome.ImpactPoint,
                outcome.Direction,
                severCount,
                MapBloodConsts.SeverSprayConeHalfRad,
                MapBloodConsts.SeverSprayMinDist,
                MapBloodConsts.SeverSprayMaxDist,
                MapBloodConsts.SeverSprayGroundBias,
                MapBloodConsts.SeverSprayScale,
                MapBloodConsts.SeverSprayAlpha);
            return;
        }

        if (!outcome.LeftCutWound)
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
