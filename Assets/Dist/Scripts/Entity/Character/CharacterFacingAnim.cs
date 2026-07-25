// ============================================================
// CharacterFacingAnim — CharacterState의 facing 방향을 SpriteSwap/Animator로 반영 (시간 채널 SSOT)
// ============================================================
using UnityEngine;

/// <summary>
/// Drives a character facing animation from <see cref="CharacterState.GetFacingDir"/>.
/// Two modes supported:
/// - SpriteSwap: assign eight directional sprite sequences (8-way) and it will cycle frames.
/// - Animator: set parameters on an Animator (floats DirX/DirY and optional bool) so a Mecanim controller can drive animations.
/// Animation time advances via <see cref="TimeScaleService"/> channel, never Unity wall-clock.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class CharacterFacingAnim : MonoBehaviour
{
    public enum Mode { SpriteSwap, Animator }

    [Header("General")]
    public Mode mode = Mode.SpriteSwap;
    [Tooltip("애니 진행에 사용할 시간 채널. 플레이어=Player, NPC·환경=World.")]
    [SerializeField] private TimeScaleChannel _timeChannel = TimeScaleChannel.Player;
    [Tooltip("Minimum squared magnitude of input to consider 'moving'")]
    public float moveThreshold = 0.01f;
    [Tooltip("방향 판정 기준 회전 오프셋 (도). 카메라/스프라이트 정렬 보정용")]
    public float angleOffset = 0f;

    [Header("Animator Mode")]
    public Animator animator;
    public string paramDirX = "DirX";
    public string paramDirY = "DirY";
    [Tooltip("비우면 bool 파라미터를 쓰지 않는다. Player.controller는 Moving이 없고 IsRun만 있다.")]
    public string paramMoving = "";

    [Header("SpriteSwap Mode (8 directions)")]
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
    bool _animatorManualControl;

    int _hashDirX;
    int _hashDirY;
    int _hashMoving;
    bool _hasDirX;
    bool _hasDirY;
    bool _hasMoving;

    void Awake()
    {
        _characterState = GetComponentInParent<CharacterState>();
        if (_characterState == null)
            _characterState = GetComponent<CharacterState>();

        CacheAnimatorParameters();
        TakeAnimatorManualControl();
    }

    void Reset()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnValidate()
    {
        if (animator != null)
            CacheAnimatorParameters();
    }

    void Update()
    {
        Vector3 dir3 = Quaternion.Euler(0f, angleOffset, 0f) * _characterState.GetFacingDir();
        Vector2 dir = new Vector2(dir3.x, dir3.z);
        bool moving = dir.sqrMagnitude > moveThreshold;

        if (mode == Mode.Animator)
            UpdateAnimator(dir, moving);
        else
            UpdateSpriteSwap(dir, moving);
    }

    void CacheAnimatorParameters()
    {
        _hasDirX = false;
        _hasDirY = false;
        _hasMoving = false;

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        _hashDirX = Animator.StringToHash(paramDirX);
        _hashDirY = Animator.StringToHash(paramDirY);
        _hashMoving = string.IsNullOrEmpty(paramMoving)
            ? 0
            : Animator.StringToHash(paramMoving);

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            int nameHash = parameters[i].nameHash;
            if (!string.IsNullOrEmpty(paramDirX) && nameHash == _hashDirX)
                _hasDirX = true;
            if (!string.IsNullOrEmpty(paramDirY) && nameHash == _hashDirY)
                _hasDirY = true;
            if (!string.IsNullOrEmpty(paramMoving) && nameHash == _hashMoving)
                _hasMoving = true;
        }
    }

    // Animator를 수동 갱신으로 전환한다. Unity 자동 틱(wall-clock)을 끄고
    // Update에서 채널 delta로만 진행시켜 배속·정지 SSOT를 지킨다.
    void TakeAnimatorManualControl()
    {
        if (mode != Mode.Animator || animator == null)
            return;

        animator.enabled = false;
        _animatorManualControl = true;
    }

    void UpdateAnimator(Vector2 input, bool moving)
    {
        if (animator == null)
            return;

        if (!_animatorManualControl)
            TakeAnimatorManualControl();

        if (moving)
        {
            if (_hasDirX)
                animator.SetFloat(_hashDirX, input.x);
            if (_hasDirY)
                animator.SetFloat(_hashDirY, input.y);
        }

        if (_hasMoving)
            animator.SetBool(_hashMoving, moving);

        animator.Update(TimeScaleService.Delta(_timeChannel));
    }

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
