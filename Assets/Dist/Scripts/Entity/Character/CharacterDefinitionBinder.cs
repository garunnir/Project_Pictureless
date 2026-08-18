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

        _appearanceHost.ApplyFromDefinition(definition);

        DefaultCharacterSkills skills = definition.CreateSkills();
        CharacterBody body = definition.CreateBody();

        if (definition.Kind == CharacterKind.Player)
        {
            GameplayData.Stats = new DefaultPlayerStats(skills);
            GameplayData.Body = body;
            return;
        }

        if (_bodyHost == null || _skillsHost == null)
        {
            Debug.LogError(
                $"[CharacterDefinitionBinder] Npc definition '{definition.name}' requires CharacterBodyHost and CharacterSkillsHost on '{name}'.",
                this);
            return;
        }

        _bodyHost.BindBody(body);
        _skillsHost.BindSkills(skills);
    }

    void EnsureHosts()
    {
        _bodyHost = GetComponent<CharacterBodyHost>();
        _skillsHost = GetComponent<CharacterSkillsHost>();
        _appearanceHost ??= GetComponent<CharacterAppearanceHost>();
        if (_appearanceHost == null)
            _appearanceHost = gameObject.AddComponent<CharacterAppearanceHost>();
    }
}
