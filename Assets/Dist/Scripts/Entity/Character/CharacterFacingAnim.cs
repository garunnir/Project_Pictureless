// ============================================================
// CharacterFacingAnim — CharacterState facing을 8방향 SpriteSwap으로 반영 (시간 채널 SSOT)
// ============================================================
using UnityEngine;

/// <summary>
/// Drives eight-directional sprite facing from <see cref="CharacterState.GetFacingDir"/>.
/// For 3D Mecanim use <see cref="CharacterLocomotionAnim"/> instead.
/// Frame time advances via <see cref="TimeScaleService"/> channel.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class CharacterFacingAnim : MonoBehaviour
{
    [Tooltip("애니 진행에 사용할 시간 채널. 플레이어=Player, NPC·환경=World.")]
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Player;
    [Tooltip("Minimum squared magnitude of input to consider 'moving'")]
    public float moveThreshold = 0.01f;
    [Tooltip("방향 판정 기준 회전 오프셋 (도). 카메라/스프라이트 정렬 보정용")]
    public float angleOffset = 0f;

    public SpriteRenderer spriteRenderer;
    [Tooltip("Frames per second when animating sprite frames")]
    public float fps = 8f;

    [System.Serializable]
    public class DirectionFrames { public string name; public Sprite[] frames; }

    [Tooltip("Order: 0 = East (0°), 1 = NorthEast (45°), 2 = North (90°), 3 = NorthWest (135°), 4 = West (180°), 5 = SouthWest (225°), 6 = South (270°), 7 = SouthEast (315°)")]
    public DirectionFrames[] directionFrames = new DirectionFrames[8];

    int currentDirection;
    float animTimer;
    CharacterState _characterState;

    void Awake()
    {
        _characterState = GetComponentInParent<CharacterState>();
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();
    }

    void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        Vector3 dir3 = Quaternion.Euler(0f, angleOffset, 0f) * _characterState.GetFacingDir();
        Vector2 dir = new Vector2(dir3.x, dir3.z);
        bool moving = dir.sqrMagnitude > moveThreshold;
        UpdateSpriteSwap(dir, moving);
    }

    /// <summary>
    /// Sprint 입력 브릿지. 스프라이트 런 프레임 세트가 생기면 여기서 반영한다.
    /// </summary>
    public void SetRunning(bool _) { }

    void UpdateSpriteSwap(Vector2 input, bool moving)
    {
        if (spriteRenderer == null)
            return;
        if (directionFrames == null || directionFrames.Length != 8)
            return;

        int dir = moving ? AngleTo8Dir(input) : currentDirection;

        if (dir != currentDirection)
        {
            currentDirection = dir;
            animTimer = 0f;
        }

        Sprite[] frames = directionFrames[currentDirection] != null
            ? directionFrames[currentDirection].frames
            : null;
        if (frames == null || frames.Length == 0)
            return;

        if (!moving)
        {
            spriteRenderer.sprite = frames[0];
            return;
        }

        animTimer += TimeScaleService.Delta(_timeChannel);
        int frameIdx = Mathf.FloorToInt(animTimer * fps) % frames.Length;
        if (frameIdx < 0)
            frameIdx = 0;
        spriteRenderer.sprite = frames[frameIdx];
    }

    int AngleTo8Dir(Vector2 v)
    {
        if (v.sqrMagnitude < 1e-6f)
            return currentDirection;
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;
        return Mathf.RoundToInt(angle / 45f) % 8;
    }
}
