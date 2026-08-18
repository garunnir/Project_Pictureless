// ============================================================
// CharacterDefinition — Dist 캐릭터 생성 스펙 SO (ActorSO 필드 계약 참조)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[Serializable]
public struct CharacterAttributeBlock
{
    public int str;
    public int con;
    public int dex;
    public int intel;
    public int wis;
    public int cha;

    public static CharacterAttributeBlock Default =>
        new CharacterAttributeBlock
        {
            str = SkillGrowth.DefaultAttributeLevel,
            con = SkillGrowth.DefaultAttributeLevel,
            dex = SkillGrowth.DefaultAttributeLevel,
            intel = SkillGrowth.DefaultAttributeLevel,
            wis = SkillGrowth.DefaultAttributeLevel,
            cha = SkillGrowth.DefaultAttributeLevel
        };

    public int Get(string attributeId)
    {
        if (attributeId == AttributeIds.Str) return str;
        if (attributeId == AttributeIds.Con) return con;
        if (attributeId == AttributeIds.Dex) return dex;
        if (attributeId == AttributeIds.Int) return intel;
        if (attributeId == AttributeIds.Wis) return wis;
        if (attributeId == AttributeIds.Cha) return cha;
        return SkillGrowth.DefaultAttributeLevel;
    }
}

[Serializable]
public struct CharacterSkillOverrideEntry
{
    public string skillId;
    public int level;
    public int potential;
}

[Serializable]
public struct CharacterPartMassEntry
{
    public string partId;
    public float kg;
}

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Dist/Character/Definition")]
public sealed class CharacterDefinition : ScriptableObject
{
    [SerializeField] string _id;
    [SerializeField] string _displayName;
    [SerializeField] Sprite _portraitSprite;
    [SerializeField] Vector2 _alignment;
    [SerializeField] CharacterAttributeBlock _attributes = CharacterAttributeBlock.Default;
    [SerializeField] List<CharacterSkillOverrideEntry> _skillOverrides = new();
    [SerializeField] float _bodyMassKg;
    [SerializeField] float _bustCm;
    [SerializeField] float _waistCm;
    [SerializeField] float _hipCm;
    [SerializeField] List<CharacterPartMassEntry> _partMasses = new();
    [SerializeField] bool _prototypeSeed;
    [SerializeField] GameObject _prefab;

    public string Id => _id;
    public string DisplayNameOverride => _displayName;
    public Sprite PortraitSprite => _portraitSprite;
    public Vector2 Alignment => _alignment;
    public CharacterAttributeBlock Attributes => _attributes;
    public IReadOnlyList<CharacterSkillOverrideEntry> SkillOverrides => _skillOverrides;
    public float BodyMassKg => _bodyMassKg;
    public float BustCm => _bustCm;
    public float WaistCm => _waistCm;
    public float HipCm => _hipCm;
    public IReadOnlyList<CharacterPartMassEntry> PartMasses => _partMasses;
    public bool PrototypeSeed => _prototypeSeed;
    public GameObject Prefab => _prefab;

    public float GetPartMassKg(string partId) => LookupPartMassKg(_partMasses, partId);

    public static float LookupPartMassKg(IReadOnlyList<CharacterPartMassEntry> masses, string partId)
    {
        if (masses == null || string.IsNullOrEmpty(partId))
            return 0f;

        for (int i = 0; i < masses.Count; i++)
        {
            if (masses[i].partId == partId)
                return masses[i].kg;
        }

        return 0f;
    }

    public string ResolveDisplayName()
    {
        if (!string.IsNullOrEmpty(_displayName))
            return _displayName;

        if (string.IsNullOrEmpty(_id))
            return string.Empty;

        return Loc.Get(_id);
    }

    public DefaultCharacterSkills CreateSkills()
    {
        DefaultCharacterSkills skills = SkillCatalog.CreateSeededSkills();
        ApplyAttributes(skills);
        ApplySkillOverrides(skills);
        return skills;
    }

    public CharacterBody CreateBody()
    {
        return CharacterBody.CreateHumanDefault(_attributes.Get(AttributeIds.Str), _prototypeSeed);
    }

    void ApplyAttributes(DefaultCharacterSkills skills)
    {
        skills.SetBaseLevel(AttributeIds.Str, _attributes.str);
        skills.SetBaseLevel(AttributeIds.Con, _attributes.con);
        skills.SetBaseLevel(AttributeIds.Dex, _attributes.dex);
        skills.SetBaseLevel(AttributeIds.Int, _attributes.intel);
        skills.SetBaseLevel(AttributeIds.Wis, _attributes.wis);
        skills.SetBaseLevel(AttributeIds.Cha, _attributes.cha);
    }

    void ApplySkillOverrides(DefaultCharacterSkills skills)
    {
        for (int i = 0; i < _skillOverrides.Count; i++)
        {
            CharacterSkillOverrideEntry entry = _skillOverrides[i];
            if (string.IsNullOrEmpty(entry.skillId))
                continue;

            skills.SetBaseLevel(entry.skillId, entry.level);
            if (entry.potential > 0)
                skills.SetPotential(entry.skillId, entry.potential);
        }
    }
}
