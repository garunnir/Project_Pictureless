// ============================================================
// CharacterDefinition — Dist 캐릭터 생성 스펙 SO (ActorSO 필드 계약 참조)
// ============================================================

using System;
using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public struct CharacterAttributeBlock
{
    [LabelText("STR")] public int str;
    [LabelText("CON")] public int con;
    [LabelText("DEX")] public int dex;
    [LabelText("INT")] public int intel;
    [LabelText("WIS")] public int wis;
    [LabelText("CHA")] public int cha;

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
public struct CharacterSenseBlock
{
    [LabelText("Sight Detect (m)"), Min(0f)] public float sightDetectMeters;
    [LabelText("Sight Lose (m)"), Min(0f)] public float sightLoseMeters;
    [LabelText("Hearing Radius (m)"), Min(0f)] public float hearingRadiusMeters;

    public static CharacterSenseBlock Default => new CharacterSenseBlock
    {
        sightDetectMeters = CharacterVisionDefaults.DetectRadius,
        sightLoseMeters = CharacterVisionDefaults.LoseRadius,
        hearingRadiusMeters = CharacterHearingDefaults.BaseRadius,
    };
}

[Serializable]
public struct CharacterSkillOverrideEntry
{
    [LabelText("Skill ID")] public string skillId;
    public int level;
    public int potential;
}

[Serializable]
public struct CharacterPartMassEntry
{
    [LabelText("Part ID")] public string partId;
    [LabelText("Mass (kg)")] public float kg;
}

[Serializable]
public struct CharacterWieldLoadoutEntry
{
    [LabelText("Item ID")] public string itemId;
    public WieldHand hand;
}

[Serializable]
public struct CharacterBodyItemSeed
{
    [LabelText("Item ID")] public string itemId;
    [Min(1)] public int count;
}

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Dist/Character/Definition")]
public sealed class CharacterDefinition : ScriptableObject
{
    [FoldoutGroup("Identity", Order = 0)]
    [LabelText("ID (Loc key)")]
    [SerializeField] string _id;

    [FoldoutGroup("Identity")]
    [LabelText("Display Name")]
    [SerializeField] string _displayName;

    [FoldoutGroup("Identity")]
    [LabelText("Portrait")]
    [SerializeField] Sprite _portraitSprite;

    [FoldoutGroup("Faction", Order = 10)]
    [SerializeField] CharacterFaction _faction;

    [HideInInspector]
    [SerializeField] Vector2 _alignment;

    [FoldoutGroup("Stats", Order = 20)]
    [LabelText("Attributes")]
    [SerializeField] CharacterAttributeBlock _attributes = CharacterAttributeBlock.Default;

    [FoldoutGroup("Stats")]
    [LabelText("Skill Overrides")]
    [SerializeField] List<CharacterSkillOverrideEntry> _skillOverrides = new();

    [FoldoutGroup("Stats")]
    [SerializeField] List<string> _traits = new();

    [FoldoutGroup("Body", Order = 30)]
    [LabelText("Body Mass (kg)")]
    [SerializeField] float _bodyMassKg;

    [FoldoutGroup("Body")]
    [LabelText("Bust (cm)")]
    [SerializeField] float _bustCm;

    [FoldoutGroup("Body")]
    [LabelText("Waist (cm)")]
    [SerializeField] float _waistCm;

    [FoldoutGroup("Body")]
    [LabelText("Hip (cm)")]
    [SerializeField] float _hipCm;

    [FoldoutGroup("Body")]
    [LabelText("Part Masses")]
    [SerializeField] List<CharacterPartMassEntry> _partMasses = new();

    [FoldoutGroup("Body")]
    [LabelText("Prototype Seed")]
    [SerializeField] bool _prototypeSeed;

    [FoldoutGroup("Body")]
    [LabelText("Grid Footprint (cells)")]
    [Tooltip("X/Z = 바닥 점유 폭·깊이, Y = 수직 점유 높이. 기본 (1,2,1) = 휴머노이드 1×2×1.")]
    [SerializeField] Vector3Int _gridFootprint = CharacterGridFootprintDefaults.Default;

    [FoldoutGroup("Senses & Locomotion", Order = 40)]
    [LabelText("Spot Angle (°)")]
    [SerializeField, Range(CharacterVisionDefaults.SpotAngleMinDegrees, CharacterVisionDefaults.SpotAngleMaxDegrees)]
    [Tooltip("시야 부채꼴 전체 각(도). Spot Light/프리팹이 아니라 이 SO가 SSOT. 예: 180≈전방 반원, 360≈전방위.")]
    float _spotAngleDegrees = CharacterVisionDefaults.SpotAngleDegrees;

    [FoldoutGroup("Senses & Locomotion")]
    [LabelText("Senses")]
    [SerializeField] CharacterSenseBlock _senses = CharacterSenseBlock.Default;

    [FoldoutGroup("Senses & Locomotion")]
    [LabelText("Walk Speed (m/s)")]
    [SerializeField, Min(0f)]
    [Tooltip("걷기 속도(m/s). possessed 달리기·관성 상한도 같은 비율로 스케일. 0이면 프리팹·시스템 기본값 유지.")]
    float _walkSpeedMeters;

    [FoldoutGroup("Spawn", Order = 50)]
    [SerializeField] GameObject _prefab;

    [FoldoutGroup("Spawn")]
    [LabelText("Wear Items")]
    [SerializeField] List<string> _wearItemIds = new();

    [FoldoutGroup("Spawn")]
    [LabelText("Wield Loadout")]
    [SerializeField] List<CharacterWieldLoadoutEntry> _wieldLoadout = new();

    [FoldoutGroup("Spawn")]
    [LabelText("Body Item Seeds")]
    [SerializeField] List<CharacterBodyItemSeed> _bodyItemSeeds = new();

    public string Id => _id;
    public string DisplayNameOverride => _displayName;
    public Sprite PortraitSprite => _portraitSprite;
    public CharacterFaction Faction => _faction;
    public Vector2 Alignment => _alignment;
    public CharacterAttributeBlock Attributes => _attributes;
    public IReadOnlyList<CharacterSkillOverrideEntry> SkillOverrides => _skillOverrides;
    public IReadOnlyList<string> Traits => _traits;
    public float BodyMassKg => _bodyMassKg;
    public float BustCm => _bustCm;
    public float WaistCm => _waistCm;
    public float HipCm => _hipCm;
    public IReadOnlyList<CharacterPartMassEntry> PartMasses => _partMasses;
    public bool PrototypeSeed => _prototypeSeed;
    public GameObject Prefab => _prefab;
    public IReadOnlyList<string> WearItemIds => _wearItemIds;
    public IReadOnlyList<CharacterWieldLoadoutEntry> WieldLoadout => _wieldLoadout;
    public IReadOnlyList<CharacterBodyItemSeed> BodyItemSeeds => _bodyItemSeeds;
    public float SpotAngleDegrees => _spotAngleDegrees;
    public CharacterSenseBlock Senses => _senses;
    public float WalkSpeedMeters => _walkSpeedMeters;
    public Vector3Int GridFootprint => CharacterGridFootprintDefaults.Clamp(_gridFootprint);

    public static float ResolveWalkSpeedMeters(
        CharacterDefinition definition,
        float prefabFallbackMeters) =>
        definition != null && definition._walkSpeedMeters > 0f
            ? definition._walkSpeedMeters
            : Mathf.Max(0f, prefabFallbackMeters);

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

    public DefaultCharacterTraits CreateTraits()
    {
        var traits = new DefaultCharacterTraits();
        ApplyTraits(traits);
        return traits;
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

    void ApplyTraits(DefaultCharacterTraits traits)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            string id = _traits[i];
            if (string.IsNullOrEmpty(id))
                continue;
            traits.Grant(id);
        }
    }
}
