// ============================================================
// ArmAnimSlotCatalog — thin SM 키 + Action 라이브러리 클립 표
// ============================================================

using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "ArmAnimSlotCatalog",
    menuName = "Dist/Combat/Arm Anim Slot Catalog")]
public sealed class ArmAnimSlotCatalog : ScriptableObject
{
    [Serializable]
    public sealed class HandClips
    {
        public AnimationClip leftBase;
        public AnimationClip rightBase;
        public AnimationClip twoHandBase;
        public AnimationClip leftFallback;
        public AnimationClip rightFallback;
    }

    [Serializable]
    public sealed class ActionLibraryEntry
    {
        public WeaponAction action;
        public HandClips hold = new HandClips();
        public HandClips aim = new HandClips();
        public HandClips attack = new HandClips();
    }

    [FormerlySerializedAs("_hold")]
    [SerializeField] HandClips _holdThin = new HandClips();
    [SerializeField] HandClips _aimThin = new HandClips();
    [SerializeField] HandClips _attackThin = new HandClips();
    [SerializeField] ActionLibraryEntry[] _actions = Array.Empty<ActionLibraryEntry>();

    /// <summary>Thin SM Hold 슬롯. 라이브러리 파지는 <see cref="ActionLibraryEntry.hold"/>.</summary>
    public HandClips HoldThin => _holdThin;
    public HandClips Hold => _holdThin;
    public HandClips AimThin => _aimThin;
    public HandClips AttackThin => _attackThin;
    public ActionLibraryEntry[] Actions => _actions;

    public void SetHoldThin(HandClips holdThin) => _holdThin = holdThin ?? new HandClips();
    public void SetHold(HandClips hold) => SetHoldThin(hold);
    public void SetAimThin(HandClips aimThin) => _aimThin = aimThin ?? new HandClips();
    public void SetAttackThin(HandClips attackThin) => _attackThin = attackThin ?? new HandClips();
    public void SetActions(ActionLibraryEntry[] actions) =>
        _actions = actions ?? Array.Empty<ActionLibraryEntry>();

    public ActionLibraryEntry FindAction(WeaponAction action)
    {
        if (_actions == null)
            return null;
        for (int i = 0; i < _actions.Length; i++)
        {
            if (_actions[i] != null && _actions[i].action == action)
                return _actions[i];
        }

        return null;
    }
}
