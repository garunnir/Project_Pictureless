// ============================================================
// ArmAnimSlotCatalog — thin SM 키 + Action 라이브러리 클립 표
// ============================================================

using System;
using UnityEngine;

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
        public HandClips aim = new HandClips();
        public HandClips attack = new HandClips();
    }

    [SerializeField] HandClips _hold = new HandClips();
    [SerializeField] HandClips _aimThin = new HandClips();
    [SerializeField] HandClips _attackThin = new HandClips();
    [SerializeField] ActionLibraryEntry[] _actions = Array.Empty<ActionLibraryEntry>();

    public HandClips Hold => _hold;
    public HandClips AimThin => _aimThin;
    public HandClips AttackThin => _attackThin;
    public ActionLibraryEntry[] Actions => _actions;

    public void SetHold(HandClips hold) => _hold = hold ?? new HandClips();
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
