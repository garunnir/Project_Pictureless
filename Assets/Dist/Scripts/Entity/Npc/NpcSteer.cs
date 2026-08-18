// ============================================================
// NpcSteer — 목표점까지 CharacterMotor 조향 (IsArrived/stoppingDistance 계약)
// ============================================================

using UnityEngine;

public static class NpcSteer
{
    public static bool TryArriveOrSteer(
        CharacterMotor motor,
        Vector3 origin,
        Vector3 destination,
        float stoppingDistance)
    {
        if (motor == null)
            return true;

        Vector3 offset = destination - origin;
        offset.y = 0f;
        float stop = Mathf.Max(0f, stoppingDistance);
        if (offset.sqrMagnitude <= stop * stop)
        {
            Stop(motor);
            return true;
        }

        motor.SetDesiredWorldDir(offset.normalized);
        motor.SetTravelLimit(offset.magnitude - stop);
        return false;
    }

    public static void Stop(CharacterMotor motor)
    {
        if (motor == null)
            return;

        motor.SetDesiredWorldDir(Vector3.zero);
        motor.ClearTravelLimit();
    }
}
