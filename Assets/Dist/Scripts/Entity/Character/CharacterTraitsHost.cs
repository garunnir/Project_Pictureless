// ============================================================
// CharacterTraitsHost — 상시 패시브 특성 보유 (SkillsHost와 분리)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterTraitsHost : MonoBehaviour
{
    [SerializeField] bool _useGameplayDataTraits;

    DefaultCharacterTraits _ownedTraits;
    ICharacterTraits _traits;

    public ICharacterTraits Traits
    {
        get
        {
            EnsureTraits();
            return _traits;
        }
    }

    public bool UseGameplayDataTraits => _useGameplayDataTraits;

    public void ConfigureUseGameplayDataTraits(bool useGameplayDataTraits)
    {
        _useGameplayDataTraits = useGameplayDataTraits;
    }

    void Awake() => EnsureTraits();

    void EnsureTraits()
    {
        if (_traits != null)
            return;

        if (_useGameplayDataTraits)
        {
            _traits = GameplayData.Traits;
            return;
        }

        _ownedTraits = new DefaultCharacterTraits();
        _traits = _ownedTraits;
    }

    public void BindTraits(DefaultCharacterTraits traits)
    {
        if (traits == null)
            return;

        if (_useGameplayDataTraits)
        {
            GameplayData.Traits = traits;
            _traits = GameplayData.Traits;
            return;
        }

        _ownedTraits = traits;
        _traits = traits;
    }
}
