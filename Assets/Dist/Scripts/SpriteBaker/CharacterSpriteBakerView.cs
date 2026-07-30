// ============================================================
// CharacterSpriteBakerView — Catalog Register + ASR Tick (TimeScale) 어댑터
// ============================================================

using Sirenix.OdinInspector;
using SpriteBaker;
using UnityEngine;

/// <summary>
/// Dist adapter: registers catalog sheets, binds
/// <see cref="AnimatedSpriteRenderer"/>, drives yaw + TimeScale
/// <see cref="AnimatedSpriteRenderer.Tick"/>. One-shot rows return to Idle;
/// re-entry while a one-shot plays is ignored.
/// </summary>
[RequireComponent(typeof(AnimatedSpriteRenderer))]
public sealed class CharacterSpriteBakerView : MonoBehaviour
{
    [Required, SerializeField] SpriteBakerCatalog _catalog;
    [Required, SerializeField] AnimatedSpriteRenderer _renderer;

    [Tooltip("애니 진행 시간 채널. 플레이어=Player, NPC=World.")]
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.Player;

    [SerializeField] string _idleAnimId = SpriteBakerCatalog.IdleAnimId;
    [SerializeField] bool _billboardToCamera = true;

    string _currentAnimId;
    bool _playingOneShot;
    bool _registered;
    Camera _camera;

    public string CurrentAnimId => _currentAnimId;
    public bool IsPlayingOneShot => _playingOneShot;

    void Reset()
    {
        _renderer = GetComponent<AnimatedSpriteRenderer>();
    }

    void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<AnimatedSpriteRenderer>();

        if (_renderer == null)
        {
            Debug.LogError("[CharacterSpriteBakerView] AnimatedSpriteRenderer missing on prefab.", this);
            enabled = false;
            return;
        }

        _renderer.ExternalClock = true;
        _renderer.RowCompleted += OnRowCompleted;
    }

    void OnDestroy()
    {
        if (_renderer != null)
            _renderer.RowCompleted -= OnRowCompleted;
    }

    void Start()
    {
        EnsureRegistered();
        if (string.IsNullOrEmpty(_currentAnimId))
            Play(_idleAnimId, force: true);
    }

    void Update()
    {
        if (_renderer == null)
            return;

        float dt = TimeScaleService.Delta(_timeChannel);
        _renderer.Tick(dt);

        if (_billboardToCamera)
            UpdateYawAndBillboard();
    }

    /// <summary>
    /// Play a catalog anim. One-shot in progress ignores new requests unless
    /// <paramref name="force"/>. Looping Idle/Run replace freely.
    /// </summary>
    public void Play(string animId, bool force = false)
    {
        if (string.IsNullOrEmpty(animId))
            return;

        if (_playingOneShot && !force)
            return;

        EnsureRegistered();

        if (!_catalog.TryGetSheet(animId, out SpriteBakerSheetAsset sheet) || sheet == null)
        {
            Debug.LogWarning(
                $"[CharacterSpriteBakerView] Anim '{animId}' not in catalog '{_catalog}'.",
                this);
            return;
        }

        _currentAnimId = animId;
        _playingOneShot = !sheet.Loop;
        _renderer.Bind(sheet.CacheKey);
        _renderer.SetRow(0);
    }

    public void SetFacing(bool right)
    {
        if (_renderer != null)
            _renderer.SetFacing(right);
    }

    void EnsureRegistered()
    {
        if (_registered || _catalog == null)
            return;

        _catalog.RegisterAll();
        _registered = true;
    }

    void OnRowCompleted()
    {
        if (!_playingOneShot)
            return;

        _playingOneShot = false;
        if (!string.Equals(_currentAnimId, _idleAnimId, System.StringComparison.OrdinalIgnoreCase))
            Play(_idleAnimId, force: true);
    }

    void UpdateYawAndBillboard()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            return;

        Vector3 toCam = _camera.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f)
            return;

        float yaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
        _renderer.SetYaw(yaw);
        _renderer.SetBillboardYaw(yaw);
    }
}
