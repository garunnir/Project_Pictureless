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

    /// <summary>들기 슬롯 아이템 아이콘 한 변 (px).</summary>
    public const float WieldIconSize = 40f;

    /// <summary>들기 슬롯 액션 아이콘 한 변 (px).</summary>
    public const float WieldActionIconSize = 16f;

    /// <summary>착용 행 아이템 아이콘 한 변 (px).</summary>
    public const float WornIconSize = 28f;

    /// <summary>들기 슬롯 LayoutElement 높이.</summary>
    public const float WieldSlotHeight = 48f;

    /// <summary>착용 행 LayoutElement 높이.</summary>
    public const float WornRowHeight = 32f;

    /// <summary>Character 탭/슬롯 TMP 기본 크기.</summary>
    public const float UiFontSizeTab = 15f;

    /// <summary>Filter / EncTotals TMP 크기.</summary>
    public const float UiFontSizeBody = 14f;

    /// <summary>FilterLabel TMP 크기.</summary>
    public const float UiFontSizeFilter = 16f;

    /// <summary>들기 액션 코너 라벨 TMP 크기.</summary>
    public const float UiFontSizeActionIcon = 11f;

    /// <summary>HandAction 미니메뉴 행 TMP 크기.</summary>
    public const float UiFontSizeContextRow = 14f;

    public static float OffHandDpsFactor(int handSkillLevel)
    {
        float t = Mathf.Clamp01(handSkillLevel / (float)OffHandDpsFactorCapLevel);
        return Mathf.Lerp(OffHandDpsFactorMin, OffHandDpsFactorMax, t);
    }
}
