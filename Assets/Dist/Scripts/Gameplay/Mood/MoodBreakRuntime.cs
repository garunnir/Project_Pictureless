// ============================================================
// MoodBreakRuntime — Wander 조향 (본체 AI MB 없음, NpcSteer 재사용)
// ============================================================
// Hot path Tick: 할당 없음. Random/Steer만.

using UnityEngine;

public sealed class MoodBreakRuntime
{
    CharacterMotor _motor;
    CharacterPainHost _pain;
    MoodSettings _settings;
    Vector3 _destination;
    bool _hasDestination;
    int _remainingMinutes;
    MoodBreakKind _kind;

    public MoodBreakKind Kind => _kind;
    public bool IsActive => _kind != MoodBreakKind.None;
    public int RemainingMinutes => _remainingMinutes;

    public void Bind(CharacterMotor motor, CharacterPainHost pain, MoodSettings settings)
    {
        _motor = motor;
        _pain = pain;
        _settings = settings;
    }

    public void BeginWander(int durationMinutes)
    {
        _kind = MoodBreakKind.Wander;
        _remainingMinutes = durationMinutes < 1 ? 1 : durationMinutes;
        _hasDestination = false;
        PickDestination();
    }

    public bool TickMinute()
    {
        if (!IsActive)
            return false;

        _remainingMinutes--;
        if (_remainingMinutes > 0)
            return false;

        End();
        return true;
    }

    public void Tick(float dt)
    {
        if (!IsActive || _motor == null || dt <= 0f)
            return;

        if (_pain != null && _pain.IsPainShocked)
        {
            NpcSteer.Stop(_motor);
            return;
        }

        if (!_hasDestination)
            PickDestination();

        if (NpcSteer.TryArriveOrSteer(
                _motor,
                _motor.transform.position,
                _destination,
                _settings != null
                    ? _settings.WanderStoppingDistance
                    : MoodSettings.DefaultWanderStoppingDistance))
        {
            PickDestination();
        }
    }

    public void End()
    {
        if (!IsActive)
            return;

        NpcSteer.Stop(_motor);
        _kind = MoodBreakKind.None;
        _remainingMinutes = 0;
        _hasDestination = false;
    }

    void PickDestination()
    {
        if (_motor == null)
        {
            _hasDestination = false;
            return;
        }

        float radius = _settings != null
            ? _settings.WanderRadius
            : MoodSettings.DefaultWanderRadius;
        Vector2 circle = Random.insideUnitCircle * radius;
        Vector3 origin = _motor.transform.position;
        _destination = new Vector3(origin.x + circle.x, origin.y, origin.z + circle.y);
        _hasDestination = true;
    }
}
