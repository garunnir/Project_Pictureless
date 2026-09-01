// ============================================================
// CharacterGameplayDataConfigurator — possessed 스폰 전 GameplayData alias 플래그 SSOT
// ============================================================

using UnityEngine;

public static class CharacterGameplayDataConfigurator
{
    public static void ConfigureAsGameplayDataOwner(GameObject instance)
    {
        if (instance == null)
            return;

        if (instance.TryGetBodyComponent(out CharacterBodyHost bodyHost))
            bodyHost.ConfigureUseGameplayDataBody(true);

        if (instance.TryGetBodyComponent(out CharacterSkillsHost skillsHost))
            skillsHost.ConfigureUseGameplayDataSkills(true);

        if (instance.TryGetBodyComponent(out CharacterTraitsHost traitsHost))
            traitsHost.ConfigureUseGameplayDataTraits(true);
    }
}
