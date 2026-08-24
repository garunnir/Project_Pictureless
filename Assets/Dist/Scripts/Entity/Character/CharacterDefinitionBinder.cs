// ============================================================
// CharacterDefinitionBinder — CharacterDefinition → 호스트/GameplayData Apply
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DefaultExecutionOrder(-80)]
[DisallowMultipleComponent]
public sealed class CharacterDefinitionBinder : MonoBehaviour
{
    [SerializeField] CharacterDefinition _definition;

    CharacterBodyHost _bodyHost;
    CharacterSkillsHost _skillsHost;
    CharacterAppearanceHost _appearanceHost;
    CharacterFactionHost _factionHost;

    public CharacterDefinition Definition => _definition;

    void Awake()
    {
        if (_definition == null)
            return;

        Apply(_definition);
    }

    public void Apply(CharacterDefinition definition)
    {
        if (definition == null)
            return;

        _definition = definition;
        EnsureHosts();
        if (_appearanceHost != null)
            _appearanceHost.ApplyFromDefinition(definition);
        if (_factionHost != null)
            _factionHost.ApplyFromDefinition(definition);

        DefaultCharacterSkills skills = definition.CreateSkills();
        CharacterBody body = definition.CreateBody();

        if (UsesGameplayData())
        {
            GameplayData.Stats = new DefaultPlayerStats(skills);
            GameplayData.Body = body;
            return;
        }

        if (_bodyHost == null || _skillsHost == null)
        {
            Debug.LogError(
                $"[CharacterDefinitionBinder] '{name}' needs CharacterBodyHost and CharacterSkillsHost, or UseGameplayData on those hosts.",
                this);
            return;
        }

        _bodyHost.BindBody(body);
        _skillsHost.BindSkills(skills);
    }

    bool UsesGameplayData()
    {
        if (_bodyHost != null && _bodyHost.UseGameplayDataBody)
            return true;
        if (_skillsHost != null && _skillsHost.UseGameplayDataSkills)
            return true;
        return _bodyHost == null && _skillsHost == null;
    }

    void EnsureHosts()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        _skillsHost = GetComponent<CharacterSkillsHost>();
        _appearanceHost ??= GetComponent<CharacterAppearanceHost>();
        _factionHost ??= GetComponent<CharacterFactionHost>();
        if (_appearanceHost == null)
        {
            Debug.LogError(
                $"[CharacterDefinitionBinder] '{name}' needs CharacterAppearanceHost on the prefab.",
                this);
        }
        if (_factionHost == null)
        {
            Debug.LogError(
                $"[CharacterDefinitionBinder] '{name}' needs CharacterFactionHost on the prefab.",
                this);
        }
    }
}
