// ============================================================
// CharacterBodyDto — CharacterBody 트리 직렬화 DTO (저장 UI 아님)
// ============================================================

using System;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    [Serializable]
    public sealed class CharacterBodyDto
    {
        public CharacterBodyPartDto[] parts;
    }

    [Serializable]
    public sealed class CharacterBodyPartDto
    {
        public string partId;
        public string parent;
        public BodyPartKind kind;
        public bool hasCondition;
        public int conditionCur;
        public int conditionMax;
        public BodyPartEffectDto[] effects;
    }

    [Serializable]
    public sealed class BodyPartEffectDto
    {
        public string effectId;
        public int intensity;
        public float remainingSeconds;
    }

    /// <summary>에디터·MCP 검증용. 세이브 슬롯/파일 경로 없음.</summary>
    public static class CharacterBodyDtoRoundTrip
    {
        public static string Execute()
        {
            CharacterBody src = CharacterBody.CreateHumanDefault(8, prototypeSeed: false);
            src.RemovePart(BodyPartIds.UpperArmR);
            src.RemovePart(BodyPartIds.HandL);
            if (!BodyPartRestoreService.TryAttachProsthetic(src, BodyPartIds.HandL))
                return "FAIL: prosthetic HandL attach";

            src.SetCondition(BodyPartIds.Chest, CharacterBody.BaseCondition, src.GetConditionMax(BodyPartIds.Chest));
            src.AddEffect(
                BodyPartIds.HandL,
                new BodyPartEffect(BodyPartEffectIds.Bleed, 1, BodyIllness.PrototypeBleedSeconds));
            src.AddEffect(BodyPartIds.FingerIndexL, new BodyPartEffect(BodyPartEffectIds.Fracture, 1, -1f));

            CharacterBodyDto dto = src.ToDto();
            string json = JsonUtility.ToJson(dto);
            CharacterBodyDto parsed = JsonUtility.FromJson<CharacterBodyDto>(json);

            bool changed = false;
            CharacterBody loaded = CharacterBody.CreateHumanDefault(8, prototypeSeed: true);
            loaded.Changed += () => changed = true;
            loaded.FromDto(parsed);

            if (!changed)
                return "FAIL: FromDto did not raise Changed";
            if (loaded.Has(BodyPartIds.UpperArmR) || loaded.Has(BodyPartIds.HandR))
                return "FAIL: severed right arm still present";
            if (!loaded.TryGet(BodyPartIds.UpperArmL, out BodyPartNode upperL) ||
                upperL.Kind != BodyPartKind.Organic)
                return "FAIL: organic UpperArmL not preserved";
            if (!loaded.TryGet(BodyPartIds.HandL, out BodyPartNode handL) ||
                handL.Kind != BodyPartKind.Prosthetic)
                return "FAIL: prosthetic HandL kind not preserved";
            if (loaded.GetConditionCur(BodyPartIds.Chest) != CharacterBody.BaseCondition)
                return "FAIL: chest condition not restored";
            if (!HasEffect(handL, BodyPartEffectIds.Bleed, BodyIllness.PrototypeBleedSeconds))
                return "FAIL: HandL bleed not restored";
            if (!loaded.Has(BodyPartIds.Brain) || !loaded.Has(BodyPartIds.Heart))
                return "FAIL: vital organs missing after FromDto";
            if (loaded.Blood01 < 0.999f)
                return "FAIL: Blood01 not reset on FromDto";
            if (!loaded.TryGet(BodyPartIds.FingerIndexL, out BodyPartNode finger) ||
                !HasEffect(finger, BodyPartEffectIds.Fracture, -1f))
                return "FAIL: FingerIndexL fracture not restored";
            if (loaded.Has(BodyPartIds.FingerThumbR))
                return "FAIL: missing right fingers still present";

            return "PASS";
        }

        static bool HasEffect(BodyPartNode node, string effectId, float remainingSeconds)
        {
            var effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                BodyPartEffect e = effects[i];
                if (e.EffectId != effectId)
                    continue;
                return Mathf.Approximately(e.RemainingSeconds, remainingSeconds);
            }

            return false;
        }
    }
}
