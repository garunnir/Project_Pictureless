// ============================================================
// CharacterActionCancelConsumer — possessed Host.CancelAll ESC 어댑터
// ============================================================

using UnityEngine;

public sealed class CharacterActionCancelConsumer : MonoBehaviour, IUiCancelConsumer
{
    CharacterActionHost _host;
    CharacterMotor _motor;

    public int CancelPriority => UiCancelPriority.CharacterAction;

    void Awake()
    {
        TryGetComponent(out _host);
        TryGetComponent(out _motor);
    }

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable() => UiCancelRouter.Unregister(this);

    public bool TryHandleCancel()
    {
        if (_host == null)
            return false;
        if (_motor != null && !_motor.IsPossessed)
            return false;
        if (!_host.HasCancellableWork)
            return false;

        _host.CancelAll();
        return true;
    }
}
