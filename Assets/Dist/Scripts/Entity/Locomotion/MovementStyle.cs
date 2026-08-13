// ============================================================
// MovementStyle — NPC 이동 속도 프로파일 SO
// ============================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "MovementStyle",
    menuName = "Dist/Locomotion/Movement Style")]
public sealed class MovementStyle : ScriptableObject
{
    [SerializeField, Min(0f)] float _moveSpeed = 3f;
    [SerializeField, Min(0f)] float _stoppingDistance = 0.1f;

    public float MoveSpeed => _moveSpeed;
    public float StoppingDistance => _stoppingDistance;
}
