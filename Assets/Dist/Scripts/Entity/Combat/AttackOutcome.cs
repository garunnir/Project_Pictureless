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
    /// <summary>완화 후 HP. 0이어도 히트일 수 있음.</summary>
    public readonly int Damage;

    /// <summary>완화 전 HP 합. p = Damage/RawDamage.</summary>
    public readonly int RawDamage;

    /// <summary>들어온 충격량 J_in (밀침). 데미지 아님.</summary>
    public readonly float ImpulseJin;

    /// <summary>공격이 출발한 월드 지점 (근접 휘두름 기준점 / 원거리 총구).</summary>
    public readonly Vector3 OriginPoint;

    /// <summary>타격이 닿은 월드 지점. 빗나감·차폐 시에도 채워진다.</summary>
    public readonly Vector3 ImpactPoint;

    /// <summary>Origin에서 Impact를 향하는 정규화 방향.</summary>
    public readonly Vector3 Direction;

    /// <summary>Hit 특성 키 (bash/cut/bullet). Action 시그널·Reaction은 비움.</summary>
    public readonly string HitTag;

    public readonly WeaponAttack Attack;

    /// <summary>무기 축 접촉 0=손/자루 ~ 1=끝. 근접 히트박스만. 치명타 Pending.</summary>
    public readonly float WeaponReach01;

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
        WeaponAttack attack = null,
        float weaponReach01 = 0f,
        int rawDamage = 0,
        float impulseJin = 0f)
    {
        Action = action;
        Hand = hand;
        ResolveMode = resolveMode;
        Result = result;
        Target = target;
        AimedPartId = aimedPartId;
        Damage = damage;
        RawDamage = rawDamage;
        ImpulseJin = Mathf.Max(0f, impulseJin);
        OriginPoint = originPoint;
        ImpactPoint = impactPoint;
        HitTag = hitTag ?? string.Empty;
        Attack = attack;
        WeaponReach01 = Mathf.Clamp01(weaponReach01);

        Vector3 offset = impactPoint - originPoint;
        Direction = offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector3.forward;
    }
}
