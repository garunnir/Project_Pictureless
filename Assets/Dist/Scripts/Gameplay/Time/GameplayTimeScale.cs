// ============================================================
// GameplayTimeScale — gameplay_speed 키 소유 정책 (TimeScaleService 얇은 래퍼)
// ============================================================

using System;
using UnityEngine;

public sealed class GameplayTimeScale : MonoBehaviour
{
    public enum Mode
    {
        Pause = 0,
        Normal = 1,
        Double = 2,
        Smart = 3,
    }

    public const float DoubleScale = 2f;
    public const float SmartScale = 10f;

    static GameplayTimeScale _instance;

    Mode _mode = Mode.Normal;
    bool _hadWorkWhileSmart;

    public static GameplayTimeScale Instance => _instance;

    public Mode CurrentMode => _mode;

    public event Action Changed;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[GameplayTimeScale] Duplicate ignored.", this);
            return;
        }

        _instance = this;
    }

    void OnEnable()
    {
        ReservedWorkHub.Changed += OnReservedWorkChanged;
        ApplyMode(_mode);
    }

    void OnDisable()
    {
        ReservedWorkHub.Changed -= OnReservedWorkChanged;
        ClearGameplaySpeedModifier();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void SetMode(Mode mode)
    {
        if (_mode == mode && mode != Mode.Smart)
            return;

        _mode = mode;
        _hadWorkWhileSmart = mode == Mode.Smart && ReservedWorkHub.HasAnyActiveWork;
        ApplyMode(_mode);
        Changed?.Invoke();
    }

    void OnReservedWorkChanged()
    {
        if (_mode != Mode.Smart)
            return;

        bool hasWork = ReservedWorkHub.HasAnyActiveWork;
        if (hasWork)
            _hadWorkWhileSmart = true;

        if (!hasWork && _hadWorkWhileSmart)
        {
            _hadWorkWhileSmart = false;
            _mode = Mode.Normal;
            ApplyMode(Mode.Normal);
            Changed?.Invoke();
            return;
        }

        ApplyMode(Mode.Smart);
        Changed?.Invoke();
    }

    void ApplyMode(Mode mode)
    {
        ClearGameplaySpeedModifier();

        switch (mode)
        {
            case Mode.Pause:
                PushGameplaySpeed(0f);
                break;
            case Mode.Normal:
                break;
            case Mode.Double:
                PushGameplaySpeed(DoubleScale);
                break;
            case Mode.Smart:
                if (ReservedWorkHub.HasAnyActiveWork)
                {
                    _hadWorkWhileSmart = true;
                    PushGameplaySpeed(SmartScale);
                }
                break;
        }
    }

    void PushGameplaySpeed(float scale)
    {
        TimeScaleService svc = TimeScaleService.Instance;
        if (svc == null)
            return;

        svc.Push(GameplayTimeScaleKeys.GameplaySpeed, TimeScaleChannel.World, scale);
        svc.Push(GameplayTimeScaleKeys.GameplaySpeed, TimeScaleChannel.Player, scale);
    }

    void ClearGameplaySpeedModifier()
    {
        TimeScaleService svc = TimeScaleService.Instance;
        if (svc == null)
            return;

        svc.Pop(GameplayTimeScaleKeys.GameplaySpeed);
    }
}
