// ============================================================
// CharacterSkillsHost — skills + Defeat + traits host (pairs with BodyHost)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
public sealed class CharacterSkillsHost : MonoBehaviour
{
    [SerializeField] bool _useGameplayDataSkills;

    CharacterBodyHost _bodyHost;
    DefaultCharacterSkills _ownedSkills;
    ICharacterSkills _skills;
    BodySkillModifierAggregator _bodyAggregator;
    DefaultCharacterDefeat _ownedDefeat;
    ICharacterDefeat _defeat;
    DefaultCharacterRecipeMemory _ownedRecipeMemory;
    ICharacterRecipeMemory _recipeMemory;
    DefaultCharacterTraits _ownedTraits;
    ICharacterTraits _traits;

    public ICharacterSkills Skills
    {
        get
        {
            EnsureSkills();
            return _skills;
        }
    }

    public ICharacterDefeat Defeat
    {
        get
        {
            EnsureDefeat();
            return _defeat;
        }
    }

    public ICharacterRecipeMemory RecipeMemory
    {
        get
        {
            EnsureRecipeMemory();
            return _recipeMemory;
        }
    }

    public ICharacterTraits Traits
    {
        get
        {
            EnsureTraits();
            return _traits;
        }
    }

    public bool UseGameplayDataSkills => _useGameplayDataSkills;

    void Awake()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        EnsureSkills();
        BindBodyToSkills();
        EnsureDefeat();
        EnsureRecipeMemory();
        EnsureTraits();
    }

    void OnEnable()
    {
        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body != null)
            body.Changed += OnBodyChanged;
    }

    void OnDisable()
    {
        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body != null)
            body.Changed -= OnBodyChanged;
    }

    void OnDestroy()
    {
        if (_bodyAggregator != null && _skills != null)
            _skills.RemoveModifierSource(_bodyAggregator);
        _ownedDefeat?.Dispose();
    }

    void EnsureSkills()
    {
        if (_skills != null)
            return;

        if (_useGameplayDataSkills)
        {
            _skills = GameplayData.CharacterSkills;
            return;
        }

        _ownedSkills = SkillCatalog.CreateSeededSkills();
        _skills = _ownedSkills;
    }

    void EnsureRecipeMemory()
    {
        if (_recipeMemory != null)
            return;

        if (_useGameplayDataSkills)
        {
            _recipeMemory = GameplayData.RecipeMemory;
            return;
        }

        _ownedRecipeMemory = new DefaultCharacterRecipeMemory();
        _recipeMemory = _ownedRecipeMemory;
    }

    void EnsureTraits()
    {
        if (_traits != null)
            return;

        if (_useGameplayDataSkills)
        {
            _traits = GameplayData.Traits;
            return;
        }

        _ownedTraits = new DefaultCharacterTraits();
        _traits = _ownedTraits;
    }

    /// <summary>Definition Apply — replace owned skills instance.</summary>
    public void BindSkills(DefaultCharacterSkills skills)
    {
        _bodyHost ??= GetComponent<CharacterBodyHost>();

        if (_bodyAggregator != null && _skills != null)
        {
            _skills.RemoveModifierSource(_bodyAggregator);
            _bodyAggregator = null;
        }

        _ownedDefeat?.Dispose();
        _ownedDefeat = null;
        _defeat = null;

        _ownedSkills = skills;
        _skills = skills;

        BindBodyToSkills();
        EnsureDefeat();
    }

    public void BindTraits(DefaultCharacterTraits traits)
    {
        if (traits == null)
            return;

        if (_useGameplayDataSkills)
        {
            GameplayData.Traits = traits;
            _traits = GameplayData.Traits;
            return;
        }

        _ownedTraits = traits;
        _traits = traits;
    }

    void BindBodyToSkills()
    {
        if (_skills == null || _bodyHost == null)
            return;

        ICharacterBody body = _bodyHost.Body;
        if (body == null)
            return;

        if (_bodyAggregator != null)
        {
            _skills.RemoveModifierSource(_bodyAggregator);
            _bodyAggregator = null;
        }

        _bodyAggregator = new BodySkillModifierAggregator(body, _skills);
        _skills.AddModifierSource(_bodyAggregator);
        _skills.Refresh();
    }

    void EnsureDefeat()
    {
        if (_defeat != null)
            return;

        if (_useGameplayDataSkills)
        {
            _defeat = GameplayData.Defeat;
            return;
        }

        _ownedDefeat = new DefaultCharacterDefeat(_bodyHost != null ? _bodyHost.Body : null, Skills);
        _defeat = _ownedDefeat;
    }

    void OnBodyChanged()
    {
        _skills?.Refresh();
    }
}
