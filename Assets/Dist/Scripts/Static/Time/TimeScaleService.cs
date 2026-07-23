// ============================================================
// TimeScaleService — 채널별 배율·모디파이어 스택·GetDelta SSOT
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class TimeScaleService : MonoBehaviour
{
    public static TimeScaleService Instance { get; private set; }

    struct Modifier
    {
        public string Key;
        public TimeScaleChannel Channel;
        public float Scale;
    }

    readonly List<Modifier> _modifiers = new(8);
    float _worldTime;
    float _playerTime;

    public event Action Changed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TimeScaleService] Duplicate ignored.", this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public float GetScale(TimeScaleChannel channel)
    {
        if (channel == TimeScaleChannel.Realtime)
            return 1f;

        float scale = 1f;
        for (int i = 0; i < _modifiers.Count; i++)
        {
            Modifier mod = _modifiers[i];
            if (mod.Channel != channel)
                continue;
            scale *= mod.Scale;
        }

        return Mathf.Max(0f, scale);
    }

    public float GetDelta(TimeScaleChannel channel) =>
        Time.unscaledDeltaTime * GetScale(channel);

    public float GetFixedDelta(TimeScaleChannel channel)
    {
        // FixedUpdate 이동/중력은 레거시 Time.fixedDeltaTime과 동일 베이스를 쓴다.
        // fixedUnscaledDeltaTime은 에디터 포커스 복귀 등에서 catch-up 스파이크가 나면
        // 논리 Floor 스냅을 한 프레임에 뚫고 낙하할 수 있다.
        float scale = GetScale(channel);
        float dt = Time.fixedDeltaTime * scale;
        float maxStep = Time.maximumDeltaTime;
        if (maxStep > 0f && dt > maxStep)
            dt = maxStep;
        return dt;
    }

    public static float FixedDelta(TimeScaleChannel channel)
    {
        TimeScaleService svc = Instance;
        if (svc != null)
            return svc.GetFixedDelta(channel);

        float dt = Time.fixedDeltaTime;
        float maxStep = Time.maximumDeltaTime;
        if (maxStep > 0f && dt > maxStep)
            dt = maxStep;
        return dt;
    }

    /// <summary>
    /// 채널 누적 시각. Realtime = <see cref="Time.unscaledTime"/>.
    /// World/Player는 Update에서 delta 누적 (스로틀·히스테리시스용).
    /// </summary>
    public float GetTime(TimeScaleChannel channel)
    {
        switch (channel)
        {
            case TimeScaleChannel.World:
                return _worldTime;
            case TimeScaleChannel.Player:
                return _playerTime;
            default:
                return Time.unscaledTime;
        }
    }

    /// <summary>Instance 없을 때 unscaled fallback (부트/에디터).</summary>
    public static float Delta(TimeScaleChannel channel)
    {
        TimeScaleService svc = Instance;
        return svc != null ? svc.GetDelta(channel) : Time.unscaledDeltaTime;
    }

    public static float TimeNow(TimeScaleChannel channel)
    {
        TimeScaleService svc = Instance;
        return svc != null ? svc.GetTime(channel) : Time.unscaledTime;
    }

    void Update()
    {
        _worldTime += GetDelta(TimeScaleChannel.World);
        _playerTime += GetDelta(TimeScaleChannel.Player);
    }

    /// <summary>
    /// 채널에 배율 모디파이어를 푸시한다. 같은 키로 여러 채널에 Push 가능.
    /// </summary>
    public void Push(string key, TimeScaleChannel channel, float scale)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[TimeScaleService] Push ignored: empty key.", this);
            return;
        }

        if (channel == TimeScaleChannel.Realtime)
        {
            Debug.LogWarning(
                "[TimeScaleService] Push ignored: Realtime channel is always 1.",
                this);
            return;
        }

        _modifiers.Add(new Modifier
        {
            Key = key,
            Channel = channel,
            Scale = Mathf.Max(0f, scale),
        });
        Changed?.Invoke();
    }

    /// <summary>해당 키의 모디파이어를 전부 제거한다.</summary>
    public bool Pop(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        int removed = 0;
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Key != key)
                continue;
            _modifiers.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
            Changed?.Invoke();
        return removed > 0;
    }

    public bool HasModifier(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Key == key)
                return true;
        }

        return false;
    }

    public void ClearAllModifiers()
    {
        if (_modifiers.Count == 0)
            return;
        _modifiers.Clear();
        Changed?.Invoke();
    }
}
