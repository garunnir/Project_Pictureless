// ============================================================
// PlayerInputDirectionAnim — facing 방향을 SpriteSwap 또는 Animator 파라미터로 반영
// ============================================================
using UnityEngine;

/// <summary>
/// Maps player facing direction to an animation.
/// Two modes supported:
/// - SpriteSwap: assign eight directional sprite sequences (8-way) and it will cycle frames.
/// - Animator: set parameters on an Animator (floats DirX/DirY and optional bool) so a Mecanim controller can drive animations.
/// </summary>
[RequireComponent(typeof(CharacterState))]
public class PlayerInputDirectionAnim : MonoBehaviour
{
    public enum Mode { SpriteSwap, Animator }

    [Header("General")]
    public Mode mode = Mode.SpriteSwap;
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

    void UpdateAnimator(Vector2 input, bool moving)
    {
        if (animator == null)
            return;

        if (moving)
        {
            if (_hasDirX)
                animator.SetFloat(_hashDirX, input.x);
            if (_hasDirY)
                animator.SetFloat(_hashDirY, input.y);
        }

        if (_hasMoving)
            animator.SetBool(_hashMoving, moving);
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

        animTimer += Time.deltaTime;
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
