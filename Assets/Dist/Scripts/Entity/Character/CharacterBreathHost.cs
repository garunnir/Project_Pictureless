// ============================================================
// CharacterBreathHost — Oxygen01·DIVE_TANK 차지·익사 다운
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
[DefaultExecutionOrder(6)]
public sealed class CharacterBreathHost : MonoBehaviour
{
    CharacterBodyHost _bodyHost;
    CharacterPainHost _pain;
    ItemStack _activeTank;
    float _oxygen01 = 1f;
    float _tankChargeAccum;
    bool _asphyxiaDowned;

    public float Oxygen01 => _oxygen01;
    public bool IsAsphyxiaDowned => _asphyxiaDowned;
    public bool IsDiveTankActive => _activeTank?.Instance != null
        && _activeTank.Instance.ToolCharges > 0
        && DiveTankService.IsDiveTankItem(_activeTank.Item);

    public event Action Changed;

    void Awake()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        TryGetComponent(out _pain);
    }

    public void TickSwim(float dt, MapSwimImmersion immersion)
    {
        if (dt <= 0f)
            return;

        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body == null || body.IsDeadState)
        {
            _oxygen01 = 1f;
            SetAsphyxia(false);
            return;
        }

        bool diving = immersion.Mode == MapSwimMode.Dive;
        if (diving && IsDiveTankActive)
        {
            _oxygen01 = 1f;
            TickTankCharges(dt);
        }
        else if (diving)
        {
            _oxygen01 = Mathf.Max(0f, _oxygen01 - MapSwimConsts.BreathHoldDrainPerSecond * dt);
        }
        else
        {
            _oxygen01 = Mathf.Min(1f, _oxygen01 + MapSwimConsts.OxygenRecoverPerSecond * dt);
        }

        if (_oxygen01 <= MapSwimConsts.OxygenAsphyxiaThreshold && diving)
            SetAsphyxia(true);
        else if (_oxygen01 >= MapSwimConsts.OxygenRecoverWakeThreshold)
            SetAsphyxia(false);

        Changed?.Invoke();
        _pain?.Refresh();
    }

    public bool TryToggleDiveTank(ItemStack stack)
    {
        if (!DiveTankService.IsDiveTankItem(stack?.Item))
            return false;

        if (_activeTank == stack)
        {
            _activeTank = null;
            Changed?.Invoke();
            return true;
        }

        if (stack.Instance == null || stack.Instance.ToolCharges <= 0)
            return false;

        _activeTank = stack;
        Changed?.Invoke();
        return true;
    }

    public void ClearActiveDiveTank()
    {
        if (_activeTank == null)
            return;
        _activeTank = null;
        Changed?.Invoke();
    }

    public bool IsActiveTank(ItemStack stack) =>
        stack != null && _activeTank == stack;

    void TickTankCharges(float dt)
    {
        if (_activeTank?.Instance == null)
            return;

        _tankChargeAccum += dt;
        while (_tankChargeAccum >= MapSwimConsts.DiveTankChargeIntervalSeconds)
        {
            _tankChargeAccum -= MapSwimConsts.DiveTankChargeIntervalSeconds;
            if (!_activeTank.Instance.TryConsumeToolCharges(MapSwimConsts.DiveTankChargePerInterval))
            {
                _activeTank = null;
                break;
            }
        }
    }

    void SetAsphyxia(bool downed)
    {
        if (_asphyxiaDowned == downed)
            return;
        _asphyxiaDowned = downed;
    }
}
