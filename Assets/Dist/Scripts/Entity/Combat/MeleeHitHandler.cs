// ============================================================
// MeleeHitHandler — melee_hit: 근접 사거리·명중·피해·시드
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

public sealed class MeleeHitHandler : IActionHandler
{
    public string LogicId => ActionHandlerIds.MeleeHit;

    public void Execute(CharacterAttacker attacker, in ActionHandlerContext context)
    {
        if (attacker == null)
            return;

        ItemData item = attacker.ItemFor(context.ItemId);
        WeaponResolveMode mode = WeaponResolveMode.MeleeReach;
        Vector3 origin = attacker.ResolveOrigin();

        if (attacker.GetCooldown(context.Hand) > 0f)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.Cooling, item, origin);
            return;
        }

        CharacterBodyHost targetHost = context.Target;
        if (targetHost == null || targetHost.Body == null)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.NoTarget, item, origin);
            return;
        }

        Vector3 toTarget = targetHost.transform.position - attacker.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        float range = CombatMath.RangeMeters(item, context.Action);
        if (distance > range)
        {
            attacker.EmitJudgedGate(context, mode, AttackPerformResult.OutOfRange, item, origin);
            return;
        }

        attacker.ResolveCommittedHit(context, mode, item, origin, consumeAmmo: false);
    }
}
