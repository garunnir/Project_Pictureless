// ============================================================
// SkillCatalog — skills.json 프로토타입 카탈로그 로더 + 시드 팩토리
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class SkillCatalog
    {
        const string GAME_FOLDER = "GameData";
        const string SKILLS_FILE = "skills.json";

        static Dictionary<string, SkillDef> _byId;

        public static bool IsLoaded => _byId != null;

        public static IReadOnlyDictionary<string, SkillDef> ById
        {
            get
            {
                EnsureLoaded();
                return _byId;
            }
        }

        public static SkillDef Get(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
                return null;
            return _byId.TryGetValue(id, out SkillDef def) ? def : null;
        }

        public static void Load()
        {
            _byId = new Dictionary<string, SkillDef>(StringComparer.Ordinal);

            string path = Path.Combine(
                Application.streamingAssetsPath, GAME_FOLDER, SKILLS_FILE);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SkillCatalog] {SKILLS_FILE} 없음 — 코드 기본값으로 시드: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            SkillsFileRoot root = GameDataJson.Deserialize<SkillsFileRoot>(json);
            if (root?.skills == null)
            {
                Debug.LogWarning($"[SkillCatalog] {SKILLS_FILE} 파싱 실패 또는 비어 있음");
                return;
            }

            for (int i = 0; i < root.skills.Length; i++)
            {
                SkillDef def = root.skills[i];
                if (def == null || string.IsNullOrEmpty(def.id))
                    continue;
                _byId[def.id] = def;
            }

            Debug.Log($"[SkillCatalog] skills: {_byId.Count}");
        }

        public static void Unload() => _byId = null;

        /// <summary>
        /// 카탈로그 기준으로 시드된 숙련 인스턴스를 만든다.
        /// 카탈로그에 빠진 기본 능력치는 코드 기본값으로 보강한다.
        /// </summary>
        public static DefaultCharacterSkills CreateSeededSkills()
        {
            EnsureLoaded();

            var skills = new DefaultCharacterSkills();

            foreach (KeyValuePair<string, SkillDef> pair in _byId)
            {
                SkillDef def = pair.Value;
                bool isAttribute = def.ParsedKind == SkillKind.Attribute;
                if (!isAttribute && def.initial_level <= 0)
                    continue;

                skills.SeedEntry(def.id, def.initial_level, def.initial_potential);
            }

            for (int i = 0; i < AttributeIds.All.Length; i++)
            {
                string id = AttributeIds.All[i];
                if (!_byId.ContainsKey(id))
                    skills.SeedEntry(id, SkillGrowth.DefaultAttributeLevel);
            }

            skills.Refresh();
            return skills;
        }

        static void EnsureLoaded()
        {
            if (_byId == null)
                Load();
        }
    }
}
