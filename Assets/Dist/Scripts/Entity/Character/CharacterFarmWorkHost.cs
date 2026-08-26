// ============================================================
// CharacterFarmWorkHost — 농사 Work 타이머·클립 (대기초 SSOT)
// ============================================================

using System;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterFarmWorkHost : MonoBehaviour
{
    public const string WorkLayerName = "Work Layer";

    [SerializeField] FarmWorkClipCatalog _clips;

    Animator _animator;
    int _workLayerIndex = -1;
    float _elapsed;
    float _duration;
    Action _onComplete;
    bool _running;

    public bool IsBusy => _running;
    public float Progress01 =>
        _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _workLayerIndex = _animator.GetLayerIndex(WorkLayerName);
    }

    void Update()
    {
        if (!_running || _duration <= 0f)
            return;

        float dt = TimeScaleService.Delta(
            TryGetComponent(out CharacterMotor motor) && motor.IsPossessed
                ? TimeScaleChannel.Player
                : TimeScaleChannel.World);
        CharacterActionHost action = GetComponent<CharacterActionHost>();
        if (action != null)
            dt *= action.ActionTickScale;

        _elapsed += dt;
        if (_elapsed < _duration)
            return;

        Finish();
    }

    public bool TryBegin(FarmCellActionKind kind, Action onComplete)
    {
        if (onComplete == null || _running)
            return false;

        AnimationClip clip = _clips != null ? _clips.Resolve(kind) : null;
        float configured = ResolveConfiguredDuration(kind);
        float clipLen = clip != null ? clip.length : 0f;
        float duration = Mathf.Max(clipLen, configured);

        if (clip != null && _animator != null && _workLayerIndex >= 0)
            _animator.Play(clip.name, _workLayerIndex, 0f);

        _onComplete = onComplete;
        _elapsed = 0f;
        _duration = Mathf.Max(0f, duration);
        _running = true;

        if (_duration <= 0f)
            Finish();

        return true;
    }

    public void Cancel()
    {
        if (!_running)
            return;

        _running = false;
        _onComplete = null;
        _elapsed = 0f;
        _duration = 0f;
    }

    float ResolveConfiguredDuration(FarmCellActionKind kind)
    {
        if (_clips != null)
            return _clips.ResolveDuration(kind);

        switch (kind)
        {
            case FarmCellActionKind.Plant:
                return MapPlantConsts.PlantWorkDurationSeconds;
            case FarmCellActionKind.Till:
                return MapPlantConsts.TillWorkDurationSeconds;
            case FarmCellActionKind.Chop:
                return MapPlantConsts.ChopWorkDurationSeconds;
            default:
                return 0f;
        }
    }

    void Finish()
    {
        Action complete = _onComplete;
        _running = false;
        _onComplete = null;
        _elapsed = 0f;
        _duration = 0f;
        complete?.Invoke();
    }
}
