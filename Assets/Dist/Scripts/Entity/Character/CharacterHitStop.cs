// ============================================================
// CharacterHitStop — 이 캐릭터만 애니·이동·공격을 잠시 멈춤 (전역 TimeScale 아님)
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
public sealed class CharacterHitStop : MonoBehaviour
{
    public const string DefaultSettingsPath = CombatHitStopSettings.DefaultAssetPath;

    [SerializeField] CombatHitStopSettings _settings;

    CharacterAttacker _attacker;
    CharacterBodyHost _bodyHost;
    float _remaining;

    public bool IsFrozen => _remaining > 0f;

    /// <summary>시뮬 배율. 경직 중 0, 아니면 1. Update 할당 없음.</summary>
    public float SimScale => _remaining > 0f ? 0f : 1f;

    public static CharacterHitStop Find(Component origin) =>
        CharacterBodyResolve.GetInBody<CharacterHitStop>(origin);

    void Awake()
    {
        TryGetComponent(out _attacker);
        TryGetComponent(out _bodyHost);
#if UNITY_EDITOR
        if (_settings == null)
        {
            _settings = UnityEditor.AssetDatabase.LoadAssetAtPath<CombatHitStopSettings>(
                DefaultSettingsPath);
        }
#endif
    }

    void OnEnable()
    {
        if (_attacker != null)
            _attacker.AttackJudged += OnAttackerJudged;
        CharacterAttacker.AnyAttackJudged += OnAnyAttackJudged;
    }

    void OnDisable()
    {
        if (_attacker != null)
            _attacker.AttackJudged -= OnAttackerJudged;
        CharacterAttacker.AnyAttackJudged -= OnAnyAttackJudged;
        _remaining = 0f;
    }

    void LateUpdate()
    {
        // 할당 없음. Realtime이라 Pause·배속 HUD와 지속시간이 분리됨.
        if (_remaining <= 0f)
            return;
        _remaining -= TimeScaleService.Delta(TimeScaleChannel.Realtime);
        if (_remaining < 0f)
            _remaining = 0f;
    }

    public void Apply(float seconds)
    {
        if (seconds <= 0f)
            return;
        if (seconds > _remaining)
            _remaining = seconds;
    }

    void OnAttackerJudged(AttackOutcome outcome) =>
        ApplyResolved(outcome);

    void OnAnyAttackJudged(AttackOutcome outcome)
    {
        if (outcome.Target != _bodyHost)
            return;
        ApplyResolved(outcome);
    }

    void ApplyResolved(in AttackOutcome outcome)
    {
        if (_settings == null)
            return;
        Apply(_settings.ResolveDuration(outcome));
    }
}
