// ============================================================
// GearConstants — Wear/Wield 힘·무게·Strain·보조손 배율 SSOT
// ============================================================

using UnityEngine;

/// <summary>BN식 기어 상수. 문서: docs/equipment/GEAR.md</summary>
public static class GearConstants
{
    /// <summary>필요 힘 = Ceil(weight_g / GramsPerStr). 양손은 × TwoHandWeightFactor.</summary>
    public const int GramsPerStr = 500;

    /// <summary>양손 들기 시 유효 가반 무게 배율 (RequiredStr 분모).</summary>
    public const int TwoHandWeightFactor = 2;

    /// <summary>하드 통과 직후, strength−RequiredStr 가 이 미만이면 LiftStrain.</summary>
    public const int SoftMargin = 3;

    /// <summary>LiftStrain 활성 시 이동 속도 배율 (호버만 표시, UI 배지 없음).</summary>
    public const float LiftStrainMoveFactor = 0.9f;

    /// <summary>보조손 숙련 0일 때 OffHand DPS 배율.</summary>
    public const float OffHandDpsFactorMin = 0.7f;

    /// <summary>보조손 숙련 Cap 이상일 때 OffHand DPS 배율.</summary>
    public const float OffHandDpsFactorMax = 1f;

    /// <summary>OffHandDpsFactor가 Max에 도달하는 hand_l/hand_r 레벨.</summary>
    public const int OffHandDpsFactorCapLevel = 10;

    /// <summary>ArmorDetailData.layer 비어 있을 때 기본 (BN NORMAL).</summary>
    public const string DefaultArmorLayer = "NORMAL";

    /// <summary>sided=true 아이템이 같은 부위+레이어에 허용되는 최대 개수 (좌/우).</summary>
    public const int MaxSidedPerLayer = 2;

    public static float OffHandDpsFactor(int handSkillLevel)
    {
        float t = Mathf.Clamp01(handSkillLevel / (float)OffHandDpsFactorCapLevel);
        return Mathf.Lerp(OffHandDpsFactorMin, OffHandDpsFactorMax, t);
    }
}
