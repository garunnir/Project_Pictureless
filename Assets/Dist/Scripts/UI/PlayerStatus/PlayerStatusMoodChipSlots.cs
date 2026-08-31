// ============================================================
// PlayerStatusMoodChipSlots — HUD 무드 칩 예약 슬롯 (수집 로직 Pending)
// ============================================================
// enum·로컬·카탈로그만 있고 Collect가 비어 있는 칩의 자리 표.
// 구현 시 각 Collect*에서 MoodEntry를 추가하고 PlayerStatusMoodEntries.Collect에서 호출한다.

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;

namespace Garunnir.Runtime.Gameplay.Data
{
    public static class PlayerStatusMoodChipSlots
    {
        public static void CollectReserved(
            ICharacterBody body,
            IPlayerVitals vitals,
            PlayerNeedsHost needs,
            PlayerEncumbranceStage encumbranceStage,
            List<MoodEntry> into)
        {
            CollectBodyEmergency(body, into);
            CollectInjuryTiers(body, into);
            CollectIllnessTiers(body, into);
            CollectRestPositive(needs, into);
            CollectStress(into);
            CollectHygiene(into);
            CollectEnvironment(into);
            CollectSocial(into);
            CollectInspiration(into);
            CollectLegacyVitals(into);
        }

        static void CollectBodyEmergency(ICharacterBody body, List<MoodEntry> into)
        {
            // Pending: BloodOxygen01 < threshold → Suffocating
            // Pending: CharacterPainHost.IsPainShocked → PainShocked
            // Pending: BodyCapacity.IsCapacityDowned → CapacityDown
            // Pending: LifeThreat01 > 0 → Dying
            // Pending: IsDefeated && Cause != StatCollapse → Defeated
        }

        static void CollectInjuryTiers(ICharacterBody body, List<MoodEntry> into)
        {
            // Pending: aggregate injury severity → SeverelyInjured (MoodIconId exists)
        }

        static void CollectIllnessTiers(ICharacterBody body, List<MoodEntry> into)
        {
            // Pending: toxin/illness tier → SeverelySick, Recovering
        }

        static void CollectRestPositive(PlayerNeedsHost needs, List<MoodEntry> into)
        {
            // Pending: SleepDisplay below tired bands → WellRested, Stable
        }

        static void CollectStress(List<MoodEntry> into)
        {
            // Pending: stress gauge or mood band → Stressed, SeverelyStressed
            // Pending: combat fear → Fear, ExtremeFear
            // Pending: anger → Angry, Furious
        }

        static void CollectHygiene(List<MoodEntry> into)
        {
            // Pending: hygiene host → Dirty, VeryDirty, NeedShower, Attractive
        }

        static void CollectEnvironment(List<MoodEntry> into)
        {
            // Pending: tile/zone/light → Dark, RestArea, SuitableEnvironment, NatureFriendly
            // Pending: GoodMeal memory chip (distinct from AteMeal thought)
        }

        static void CollectSocial(List<MoodEntry> into)
        {
            // Pending: NPC/social → Lonely, Bored, Idle, PleasantConversation
            // Pending: relationships → RelationshipImproved, Loved, MarriedEngaged, Trust, Respect
        }

        static void CollectInspiration(List<MoodEntry> into)
        {
            // Pending: craft/skill/book → Inspired, Motivated, SkillUp
        }

        static void CollectLegacyVitals(List<MoodEntry> into)
        {
            // Pending: legacy single-slot Hunger/Thirst (NEEDS.md — intentionally unused)
        }
    }
}
