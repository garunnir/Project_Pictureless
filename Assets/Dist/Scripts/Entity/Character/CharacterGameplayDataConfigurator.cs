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

        if (instance.TryGetComponent(out CharacterBodyHost bodyHost))
            bodyHost.ConfigureUseGameplayDataBody(true);

        if (instance.TryGetComponent(out CharacterSkillsHost skillsHost))
            skillsHost.ConfigureUseGameplayDataSkills(true);

        if (instance.TryGetComponent(out CharacterTraitsHost traitsHost))
            traitsHost.ConfigureUseGameplayDataTraits(true);
    }
}
