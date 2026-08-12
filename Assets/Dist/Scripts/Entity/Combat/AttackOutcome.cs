// ============================================================
// AttackOutcome — 시전된 공격 1회의 판정 결과 스냅샷 (연출 소비용)
// ============================================================

using UnityEngine;

public readonly struct AttackOutcome
{
    public readonly WeaponAction Action;
    public readonly WieldHand Hand;
    public readonly WeaponResolveMode ResolveMode;
    public readonly AttackPerformResult Result;
    public readonly CharacterBodyHost Target;
    public readonly string AimedPartId;
    public readonly int Damage;

    /// <summary>공격이 출발한 월드 지점 (근접 휘두름 기준점 / 원거리 총구).</summary>
    public readonly Vector3 OriginPoint;

    /// <summary>타격이 닿은 월드 지점. 빗나감·차폐 시에도 채워진다.</summary>
    public readonly Vector3 ImpactPoint;

    /// <summary>Origin에서 Impact를 향하는 정규화 방향.</summary>
    public readonly Vector3 Direction;

    /// <summary>Hit 특성 키 (bash/cut/bullet). Action 시그널·Reaction은 비움.</summary>
    public readonly string HitTag;

    public readonly WeaponAttack Attack;

    public bool DidHit => Result == AttackPerformResult.Performed;

    public AttackOutcome(
        WeaponAction action,
        WieldHand hand,
        WeaponResolveMode resolveMode,
        AttackPerformResult result,
        CharacterBodyHost target,
        string aimedPartId,
        int damage,
        Vector3 originPoint,
        Vector3 impactPoint,
        string hitTag = null,
        WeaponAttack attack = null)
    {
        Action = action;
        Hand = hand;
        ResolveMode = resolveMode;
        Result = result;
        Target = target;
        AimedPartId = aimedPartId;
        Damage = damage;
        OriginPoint = originPoint;
        ImpactPoint = impactPoint;
        HitTag = hitTag ?? string.Empty;
        Attack = attack;

        Vector3 offset = impactPoint - originPoint;
        Direction = offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector3.forward;
    }
}
