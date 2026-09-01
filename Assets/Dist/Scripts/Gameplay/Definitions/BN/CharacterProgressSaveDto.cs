// ============================================================
// CharacterProgressSaveDto — 플레이어 성장·상태 맵 JSON 직렬화 DTO
// ============================================================

using System;
using System.Collections.Generic;

namespace Garunnir.Runtime.Gameplay.Data
{
    [Serializable]
    public sealed class CharacterSkillEntrySaveDto
    {
        public string skillId;
        public int baseLevel;
        public int potential;
        public int experience;
    }

    [Serializable]
    public sealed class CharacterSkillsSaveDto
    {
        public CharacterSkillEntrySaveDto[] entries;
    }

    [Serializable]
    public sealed class VitalEntrySaveDto
    {
        public string vitalKey;
        public int current;
        public int max;
    }

    [Serializable]
    public sealed class PlayerVitalsSaveDto
    {
        public VitalEntrySaveDto[] entries;
    }

    [Serializable]
    public sealed class ProficiencyPracticeSaveDto
    {
        public string proficiencyId;
        public int xp;
    }

    [Serializable]
    public sealed class CharacterProficienciesSaveDto
    {
        public string[] knownIds;
        public ProficiencyPracticeSaveDto[] practice;
    }

    [Serializable]
    public sealed class CharacterRecipeMemorySaveDto
    {
        public string[] knownRecipeIds;
    }

    [Serializable]
    public sealed class CharacterTraitsSaveDto
    {
        public string[] traitIds;
    }

    [Serializable]
    public sealed class BodyTempPartSaveDto
    {
        public string partId;
        public float tempC;
    }

    [Serializable]
    public sealed class BodyTempSaveDto
    {
        public BodyTempPartSaveDto[] parts;
    }

    [Serializable]
    public sealed class PlayerProgressSaveDto
    {
        public float worldX;
        public float worldY;
        public float worldZ;
        public float facingX;
        public float facingZ;

        public CharacterBodyDto body;
        public BodyTempSaveDto bodyTemp;
        public CharacterSkillsSaveDto skills;
        public PlayerVitalsSaveDto vitals;
        public CharacterProficienciesSaveDto proficiencies;
        public CharacterRecipeMemorySaveDto recipeMemory;
        public CharacterTraitsSaveDto traits;

        /// <summary><see cref="InventoryGearSaveDto"/> JsonUtility JSON (Dist.Inventory).</summary>
        public string inventoryJson;
    }

    public static class CharacterProgressSaveMapper
    {
        public static CharacterSkillsSaveDto ToDto(DefaultCharacterSkills skills)
        {
            if (skills == null)
                return null;

            IReadOnlyList<CharacterSkillEntrySaveDto> exported = skills.ExportProgressEntries();
            var dto = new CharacterSkillsSaveDto
            {
                entries = new CharacterSkillEntrySaveDto[exported.Count]
            };
            for (int i = 0; i < exported.Count; i++)
                dto.entries[i] = exported[i];
            return dto;
        }

        public static void ApplyDto(DefaultCharacterSkills skills, CharacterSkillsSaveDto dto)
        {
            if (skills == null)
                return;

            skills.ImportProgressEntries(dto?.entries);
        }

        public static PlayerVitalsSaveDto ToDto(DefaultPlayerVitals vitals)
        {
            if (vitals == null)
                return null;

            var entries = new VitalEntrySaveDto[VitalKeys.All.Length];
            for (int i = 0; i < VitalKeys.All.Length; i++)
            {
                string key = VitalKeys.All[i];
                entries[i] = new VitalEntrySaveDto
                {
                    vitalKey = key,
                    current = vitals.GetCurrent(key),
                    max = vitals.GetMax(key)
                };
            }

            return new PlayerVitalsSaveDto { entries = entries };
        }

        public static void ApplyDto(DefaultPlayerVitals vitals, PlayerVitalsSaveDto dto)
        {
            if (vitals == null || dto?.entries == null)
                return;

            for (int i = 0; i < dto.entries.Length; i++)
            {
                VitalEntrySaveDto entry = dto.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.vitalKey))
                    continue;

                vitals.SetMax(entry.vitalKey, entry.max);
                vitals.SetCurrent(entry.vitalKey, entry.current);
            }
        }

        public static CharacterProficienciesSaveDto ToDto(DefaultCharacterProficiencies proficiencies)
        {
            if (proficiencies == null)
                return null;

            IReadOnlyCollection<string> known = proficiencies.GetKnownIds();
            IReadOnlyList<ProficiencyPracticeSaveDto> practice = proficiencies.ExportPracticeEntries();

            var knownArray = new string[known.Count];
            int w = 0;
            foreach (string id in known)
                knownArray[w++] = id;

            var practiceArray = new ProficiencyPracticeSaveDto[practice.Count];
            for (int i = 0; i < practice.Count; i++)
                practiceArray[i] = practice[i];

            return new CharacterProficienciesSaveDto
            {
                knownIds = knownArray,
                practice = practiceArray
            };
        }

        public static void ApplyDto(DefaultCharacterProficiencies proficiencies, CharacterProficienciesSaveDto dto)
        {
            if (proficiencies == null)
                return;

            proficiencies.ImportFromSave(dto);
        }

        public static CharacterRecipeMemorySaveDto ToDto(DefaultCharacterRecipeMemory memory)
        {
            if (memory == null)
                return null;

            IReadOnlyCollection<string> known = memory.GetKnownIds();
            var ids = new string[known.Count];
            int w = 0;
            foreach (string id in known)
                ids[w++] = id;

            return new CharacterRecipeMemorySaveDto { knownRecipeIds = ids };
        }

        public static void ApplyDto(DefaultCharacterRecipeMemory memory, CharacterRecipeMemorySaveDto dto)
        {
            if (memory == null)
                return;

            memory.ImportFromSave(dto?.knownRecipeIds);
        }

        public static CharacterTraitsSaveDto ToDto(DefaultCharacterTraits traits)
        {
            if (traits == null)
                return null;

            IReadOnlyCollection<string> known = traits.GetKnownIds();
            var ids = new string[known.Count];
            int w = 0;
            foreach (string id in known)
                ids[w++] = id;

            return new CharacterTraitsSaveDto { traitIds = ids };
        }

        public static void ApplyDto(DefaultCharacterTraits traits, CharacterTraitsSaveDto dto)
        {
            if (traits == null)
                return;

            traits.ImportFromSave(dto?.traitIds);
        }
    }
}
