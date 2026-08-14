// ============================================================
// WeaponAnimClipSpeeds — Override에 할당한 클립의 재생 배속 (thin 슬롯 속도 아님)
// ============================================================

using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// AnimatorOverrideController 서브에셋. 키는 thin 슬롯이 아니라 덮어쓴 <see cref="AnimationClip"/>.
/// 표에 없는 클립은 <see cref="DefaultSpeed"/>.
/// </summary>
[InfoBox(
    "Override에 할당한 클립의 재생 배속입니다. thin 슬롯(Hold/Aim/Attack) 속도가 아닙니다.\n" +
    "없는 클립은 1. Catalog 폴백(Override 없음)도 1.")]
public sealed class WeaponAnimClipSpeeds : ScriptableObject
{
    public const float DefaultSpeed = 1f;
    public const string ParamRight = "ArmSpeedR";
    public const string ParamLeft = "ArmSpeedL";
    public const string ParamTwoHand = "ArmSpeed2H";
    public const string ParamImpact = "ImpactSpeed";

    const float SpeedEpsilon = 0.0001f;

    [Serializable]
    public sealed class Entry
    {
        public AnimationClip clip;
        [Min(0f)] public float speed = DefaultSpeed;
    }

    [SerializeField] Entry[] _entries = Array.Empty<Entry>();

    public float GetSpeed(AnimationClip clip)
    {
        if (clip == null || _entries == null)
            return DefaultSpeed;
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i] == null || !ReferenceEquals(_entries[i].clip, clip))
                continue;
            return Mathf.Max(0f, _entries[i].speed);
        }

        return DefaultSpeed;
    }

    public void SetSpeed(AnimationClip clip, float speed)
    {
        if (clip == null)
            return;

        speed = Mathf.Max(0f, speed);
        int index = IndexOf(clip);
        if (Mathf.Abs(speed - DefaultSpeed) <= SpeedEpsilon)
        {
            if (index >= 0)
                RemoveAt(index);
            return;
        }

        if (index >= 0)
        {
            _entries[index].speed = speed;
            return;
        }

        int length = _entries != null ? _entries.Length : 0;
        var next = new Entry[length + 1];
        if (length > 0)
            Array.Copy(_entries, next, length);
        next[length] = new Entry { clip = clip, speed = speed };
        _entries = next;
    }

    public void RetainOnly(AnimationClip[] keep)
    {
        if (_entries == null || _entries.Length == 0)
            return;

        int write = 0;
        for (int i = 0; i < _entries.Length; i++)
        {
            Entry entry = _entries[i];
            if (entry == null || entry.clip == null || !Contains(keep, entry.clip))
                continue;
            if (write != i)
                _entries[write] = entry;
            write++;
        }

        if (write == _entries.Length)
            return;

        Array.Resize(ref _entries, write);
    }

    int IndexOf(AnimationClip clip)
    {
        if (_entries == null)
            return -1;
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i] != null && ReferenceEquals(_entries[i].clip, clip))
                return i;
        }

        return -1;
    }

    void RemoveAt(int index)
    {
        int length = _entries.Length;
        if (length <= 1)
        {
            _entries = Array.Empty<Entry>();
            return;
        }

        var next = new Entry[length - 1];
        if (index > 0)
            Array.Copy(_entries, 0, next, 0, index);
        if (index < length - 1)
            Array.Copy(_entries, index + 1, next, index, length - 1 - index);
        _entries = next;
    }

    static bool Contains(AnimationClip[] keep, AnimationClip clip)
    {
        if (keep == null)
            return false;
        for (int i = 0; i < keep.Length; i++)
        {
            if (ReferenceEquals(keep[i], clip))
                return true;
        }

        return false;
    }
}
