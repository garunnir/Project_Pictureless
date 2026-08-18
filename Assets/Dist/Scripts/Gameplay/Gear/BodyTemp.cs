// ============================================================
// BodyTemp — 부위별 체온 저장·틱 SSOT (core=chest, BN-style heat flow)
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// Per-thermal-part body temperature. Core getters (BodyTempC / Feeling / TargetTempC) are chest.
/// Target = ambient + WarmthForPart × DegreesPerWarmth − wetness cool.
/// Heat flow: arms←chest, hands←arms, legs←chest, feet←legs.
/// Ambient °C from WeatherExposure; BaseAmbientTempC = Clear fallback. dt = World.
/// </summary>
public sealed class BodyTemp
{
    /// <summary>편안 체온 (°C).</summary>
    public const float ComfortBodyTempC = 37f;

    /// <summary>코어(가슴) 체온 하한 (°C).</summary>
    public const float BodyTempMinC = 27f;

    /// <summary>코어(가슴) 체온 상한 (°C).</summary>
    public const float BodyTempMaxC = 43f;

    /// <summary>말단 체온 하한 (°C). 코어 min보다 낮아 frostbite가 도달 가능하다.</summary>
    public const float ExtremityTempMinC = 12f;

    /// <summary>말단 체온 상한 (°C).</summary>
    public const float ExtremityTempMaxC = 48f;

    /// <summary>Clear 날씨 기본 환경 온도 (°C). WeatherExposure.ClearAmbientTempC와 동일.</summary>
    public const float BaseAmbientTempC = 18f;

    /// <summary>WarmthForPart 1포인트당 목표 온도 상승 (°C). Formula name: DegreesPerWarmth.</summary>
    public const float DegreesPerWarmthPoint = 0.5f;

    /// <summary>Target: ambient + WarmthForPart * DegreesPerWarmth − wetness cool.</summary>
    public const float DegreesPerWarmth = DegreesPerWarmthPoint;

    /// <summary>Wetness01=1일 때 목표 온도 감소 (°C). 전투/습윤 연동 아님.</summary>
    public const float WetnessCoolDegreesC = 2f;

    /// <summary>현재→목표 수렴 속도 (World초당 비율).</summary>
    public const float ConvergencePerSecond = 0.08f;

    /// <summary>팔 온도가 가슴 쪽으로 섞이는 비율 (World초당).</summary>
    public const float HeatFlowArmFromChestPerSecond = 0.12f;

    /// <summary>손 온도가 팔 쪽으로 섞이는 비율 (World초당).</summary>
    public const float HeatFlowHandFromArmPerSecond = 0.18f;

    /// <summary>다리 온도가 가슴 쪽으로 섞이는 비율 (World초당).</summary>
    public const float HeatFlowLegFromChestPerSecond = 0.12f;

    /// <summary>발 온도가 다리 쪽으로 섞이는 비율 (World초당).</summary>
    public const float HeatFlowFootFromLegPerSecond = 0.18f;

    /// <summary>편안 밴드 하한 (°C) — Comfort ± ComfortBandHalfWidth.</summary>
    public const float ComfortBandHalfWidthC = 1f;

    /// <summary>코어가 이 온도 이하면 Mood Hypothermia (Cold 밴드보다 엄격).</summary>
    public const float HypothermiaBodyTempC = 32f;

    readonly float[] _tempC;
    readonly float[] _targetC;
    readonly int[] _lastWarmth;
    readonly bool[] _present;

    public event Action Changed;

    public BodyTemp()
    {
        int n = BodyPartIds.ThermalParts.Length;
        _tempC = new float[n];
        _targetC = new float[n];
        _lastWarmth = new int[n];
        _present = new bool[n];
        Reset();
    }

    /// <summary>코어(가슴) 체온 (°C). 가슴이 없으면 Comfort.</summary>
    public float BodyTempC => GetCoreOrComfort(_tempC);

    /// <summary>직전 Tick의 코어 목표 체온 (°C).</summary>
    public float TargetTempC => GetCoreOrComfort(_targetC);

    /// <summary>직전 Tick의 가슴 WarmthForPart.</summary>
    public int LastTotalWarmth
    {
        get
        {
            int chest = ChestIndex();
            return chest >= 0 ? _lastWarmth[chest] : 0;
        }
    }

    public void Reset(float bodyTempC = ComfortBodyTempC)
    {
        float core = Mathf.Clamp(bodyTempC, BodyTempMinC, BodyTempMaxC);
        float extremity = Mathf.Clamp(bodyTempC, ExtremityTempMinC, ExtremityTempMaxC);
        for (int i = 0; i < _tempC.Length; i++)
        {
            bool corePart = IsCoreIndex(i);
            float seeded = corePart ? core : extremity;
            _tempC[i] = seeded;
            _targetC[i] = seeded;
            _lastWarmth[i] = 0;
            _present[i] = false;
        }
    }

    /// <summary>
    /// Per present thermal part: Target = ambient + warmth×DegreesPerWarmth − wetness×WetnessCool.
    /// Then heat flow arms←chest, hands←arms, legs←chest, feet←legs.
    /// Missing parts are skipped. warmthIn/presentIn length = ThermalParts.
    /// </summary>
    public void Tick(
        float deltaSeconds,
        float wetness01,
        float ambientTempC,
        int[] warmthIn,
        bool[] presentIn)
    {
        if (deltaSeconds <= 0f || warmthIn == null || presentIn == null)
            return;
        if (warmthIn.Length < _tempC.Length || presentIn.Length < _tempC.Length)
            return;

        float wet = Mathf.Clamp01(wetness01);
        for (int i = 0; i < _tempC.Length; i++)
        {
            bool present = presentIn[i];
            _present[i] = present;
            if (!present)
            {
                _lastWarmth[i] = 0;
                continue;
            }

            int warmth = Mathf.Max(0, warmthIn[i]);
            _lastWarmth[i] = warmth;
            float minC = MinCForIndex(i);
            float maxC = MaxCForIndex(i);
            _targetC[i] = ComputeTargetTempC(warmth, wet, ambientTempC, minC, maxC);

            float next = _tempC[i] + (_targetC[i] - _tempC[i]) * ConvergencePerSecond * deltaSeconds;
            _tempC[i] = Mathf.Clamp(next, minC, maxC);
        }

        ApplyHeatFlow(deltaSeconds);
    }

    public static float ComputeTargetTempC(
        int totalWarmth,
        float wetness01,
        float ambientTempC = BaseAmbientTempC)
    {
        return ComputeTargetTempC(
            totalWarmth,
            wetness01,
            ambientTempC,
            BodyTempMinC,
            BodyTempMaxC);
    }

    public static float ComputeTargetTempC(
        int warmth,
        float wetness01,
        float ambientTempC,
        float minC,
        float maxC)
    {
        float w = Mathf.Max(0, warmth);
        float wet = Mathf.Clamp01(wetness01);
        float target = ambientTempC
                       + w * DegreesPerWarmth
                       - wet * WetnessCoolDegreesC;
        return Mathf.Clamp(target, minC, maxC);
    }

    public bool TryGetPartTempC(string partId, out float tempC)
    {
        tempC = ComfortBodyTempC;
        int i = ResolveThermalIndex(partId);
        if (i < 0 || !_present[i])
            return false;
        tempC = _tempC[i];
        return true;
    }

    public bool TryGetPartTargetTempC(string partId, out float targetC)
    {
        targetC = ComfortBodyTempC;
        int i = ResolveThermalIndex(partId);
        if (i < 0 || !_present[i])
            return false;
        targetC = _targetC[i];
        return true;
    }

    public bool TryGetPartWarmth(string partId, out int warmth)
    {
        warmth = 0;
        int i = ResolveThermalIndex(partId);
        if (i < 0 || !_present[i])
            return false;
        warmth = _lastWarmth[i];
        return true;
    }

    public bool IsPartTracked(string partId)
    {
        int i = ResolveThermalIndex(partId);
        return i >= 0 && _present[i];
    }

    public BodyTempDto ToDto()
    {
        int count = 0;
        for (int i = 0; i < _present.Length; i++)
        {
            if (_present[i])
                count++;
        }

        var parts = new BodyTempPartDto[count];
        int w = 0;
        for (int i = 0; i < _present.Length; i++)
        {
            if (!_present[i])
                continue;

            parts[w++] = new BodyTempPartDto
            {
                partId = BodyPartIds.ThermalParts[i],
                tempC = _tempC[i]
            };
        }

        return new BodyTempDto { parts = parts };
    }

    public void FromDto(BodyTempDto dto)
    {
        Reset();
        BodyTempPartDto[] parts = dto != null ? dto.parts : null;
        if (parts != null)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                BodyTempPartDto part = parts[i];
                if (part == null)
                    continue;

                int index = BodyPartIds.IndexOfThermalPart(part.partId);
                if (index < 0)
                    continue;

                float minC = MinCForIndex(index);
                float maxC = MaxCForIndex(index);
                float tempC = Mathf.Clamp(part.tempC, minC, maxC);
                _tempC[index] = tempC;
                _targetC[index] = tempC;
                _present[index] = true;
            }
        }

        Changed?.Invoke();
    }

    public static string ExecuteDtoRoundTripVerify()
    {
        var src = new BodyTemp();
        src.FromDto(new BodyTempDto
        {
            parts = new[]
            {
                new BodyTempPartDto { partId = BodyPartIds.Chest, tempC = 36.4f },
                new BodyTempPartDto { partId = BodyPartIds.HandL, tempC = 22f }
            }
        });

        string json = JsonUtility.ToJson(src.ToDto());
        BodyTempDto parsed = JsonUtility.FromJson<BodyTempDto>(json);
        if (parsed == null || parsed.parts == null)
            return "FAIL: BodyTempDto json parse";
        if (parsed.GetType().GetField("wetness") != null ||
            parsed.GetType().GetField("wetness01") != null)
            return "FAIL: wetness duplicated on BodyTempDto";

        var loaded = new BodyTemp();
        bool changed = false;
        loaded.Changed += () => changed = true;
        loaded.FromDto(parsed);

        if (!changed)
            return "FAIL: BodyTemp.FromDto did not raise Changed";
        if (!loaded.TryGetPartTempC(BodyPartIds.Chest, out float chest) ||
            Mathf.Abs(chest - 36.4f) > 0.05f)
            return "FAIL: chest °C not restored";
        if (!loaded.TryGetPartTempC(BodyPartIds.HandL, out float hand) ||
            Mathf.Abs(hand - 22f) > 0.05f)
            return "FAIL: hand_l °C not restored";
        if (loaded.IsPartTracked(BodyPartIds.Head) || loaded.IsPartTracked(BodyPartIds.HandR))
            return "FAIL: omitted thermal parts still present";

        return "PASS";
    }

    public BodyTempFeeling FeelingForPart(string partId)
    {
        return TryGetPartTempC(partId, out float tempC)
            ? ClassifyFeeling(tempC)
            : BodyTempFeeling.Comfortable;
    }

    /// <summary>표시용 소수 1자리 코어 체온.</summary>
    public float BodyTempDisplayC => Mathf.Round(BodyTempC * 10f) * 0.1f;

    /// <summary>표시용 소수 1자리 코어 목표.</summary>
    public float TargetTempDisplayC => Mathf.Round(TargetTempC * 10f) * 0.1f;

    /// <summary>0.1°C 단위 정수 (Changed 스로틀용).</summary>
    public int BodyTempTenths => Mathf.RoundToInt(BodyTempC * 10f);

    public BodyTempFeeling Feeling => ClassifyFeeling(BodyTempC);

    public static BodyTempFeeling ClassifyFeeling(float bodyTempC)
    {
        float comfortLo = ComfortBodyTempC - ComfortBandHalfWidthC;
        float comfortHi = ComfortBodyTempC + ComfortBandHalfWidthC;
        if (bodyTempC < comfortLo - ComfortBandHalfWidthC * 2f)
            return BodyTempFeeling.Cold;
        if (bodyTempC < comfortLo)
            return BodyTempFeeling.Cool;
        if (bodyTempC <= comfortHi)
            return BodyTempFeeling.Comfortable;
        if (bodyTempC <= comfortHi + ComfortBandHalfWidthC * 2f)
            return BodyTempFeeling.Warm;
        return BodyTempFeeling.Hot;
    }

    static int ResolveThermalIndex(string partId)
    {
        int direct = BodyPartIds.IndexOfThermalPart(partId);
        if (direct >= 0)
            return direct;
        string thermal = BodyPartIds.GetThermalPart(partId);
        return BodyPartIds.IndexOfThermalPart(thermal);
    }

    void ApplyHeatFlow(float deltaSeconds)
    {
        FlowTowardParent(
            BodyPartIds.UpperArmL, BodyPartIds.Chest, HeatFlowArmFromChestPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.UpperArmR, BodyPartIds.Chest, HeatFlowArmFromChestPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.HandL, BodyPartIds.UpperArmL, HeatFlowHandFromArmPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.HandR, BodyPartIds.UpperArmR, HeatFlowHandFromArmPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.ThighL, BodyPartIds.Chest, HeatFlowLegFromChestPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.ThighR, BodyPartIds.Chest, HeatFlowLegFromChestPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.FootL, BodyPartIds.ThighL, HeatFlowFootFromLegPerSecond, deltaSeconds);
        FlowTowardParent(
            BodyPartIds.FootR, BodyPartIds.ThighR, HeatFlowFootFromLegPerSecond, deltaSeconds);
    }

    void FlowTowardParent(string childId, string parentId, float ratePerSecond, float deltaSeconds)
    {
        int child = BodyPartIds.IndexOfThermalPart(childId);
        int parent = BodyPartIds.IndexOfThermalPart(parentId);
        if (child < 0 || parent < 0)
            return;
        if (!_present[child] || !_present[parent])
            return;

        float next = _tempC[child]
                     + (_tempC[parent] - _tempC[child]) * ratePerSecond * deltaSeconds;
        _tempC[child] = Mathf.Clamp(next, MinCForIndex(child), MaxCForIndex(child));
    }

    float GetCoreOrComfort(float[] values)
    {
        int chest = ChestIndex();
        if (chest < 0 || !_present[chest])
            return ComfortBodyTempC;
        return values[chest];
    }

    static int ChestIndex() => BodyPartIds.IndexOfThermalPart(BodyPartIds.Chest);

    static bool IsCoreIndex(int index)
    {
        int chest = ChestIndex();
        return chest >= 0 && index == chest;
    }

    static float MinCForIndex(int index) =>
        IsCoreIndex(index) ? BodyTempMinC : ExtremityTempMinC;

    static float MaxCForIndex(int index) =>
        IsCoreIndex(index) ? BodyTempMaxC : ExtremityTempMaxC;
}

public enum BodyTempFeeling
{
    Cold = 0,
    Cool = 1,
    Comfortable = 2,
    Warm = 3,
    Hot = 4
}

[Serializable]
public sealed class BodyTempDto
{
    public BodyTempPartDto[] parts;
}

[Serializable]
public sealed class BodyTempPartDto
{
    public string partId;
    public float tempC;
}
