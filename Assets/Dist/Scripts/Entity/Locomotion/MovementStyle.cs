// ============================================================
// MovementStyle — NPC 이동 속도 프로파일 SO
// ============================================================

using UnityEngine;

public enum MovementStyleSpeedSource
{
    /// <summary>고정 m/s (<see cref="MovementStyle.MoveSpeed"/>).</summary>
    Fixed = 0,
    /// <summary>본체 <see cref="CharacterDefinition.WalkSpeedMeters"/> (Motor 현재 걷기 속도).</summary>
    CharacterWalk = 1,
}

[CreateAssetMenu(
    fileName = "MovementStyle",
    menuName = "Dist/Locomotion/Movement Style")]
public sealed class MovementStyle : ScriptableObject
{
    [SerializeField] MovementStyleSpeedSource _speedSource = MovementStyleSpeedSource.Fixed;
    [SerializeField, Min(0f)] float _moveSpeed = CharacterLocomotionDefaults.DefaultWalkSpeedMeters;
    [SerializeField, Min(0f)] float _stoppingDistance = 0.1f;

    public MovementStyleSpeedSource SpeedSource => _speedSource;
    public bool UsesCharacterWalkSpeed => _speedSource == MovementStyleSpeedSource.CharacterWalk;
    public float MoveSpeed => _moveSpeed;
    public float StoppingDistance => _stoppingDistance;
}
