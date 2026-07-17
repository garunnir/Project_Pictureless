// ============================================================
// PlayerSkillDebugCommands — 플레이어 스킬 런타임 디버그 명령
// ============================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using IngameDebugConsole;
using UnityEngine.Scripting;

public static class PlayerSkillDebugCommands
{
    public const string GetCommand = "player.skill.get";
    public const string SetCommand = "player.skill.set";
    public const string PracticeCommand = "player.skill.practice";

    [ConsoleMethod(GetCommand, "Returns a player skill level", "skillId"), Preserve]
    public static string GetLevel(string skillId)
    {
        skillId = skillId?.Trim();
        if (string.IsNullOrEmpty(skillId))
            return "ERROR: skillId is required";

        int level = GameplayData.Stats.GetSkillLevel(skillId);
        return $"{skillId} Lv.{level}";
    }

    [ConsoleMethod(SetCommand, "Sets a player skill level", "skillId", "level"), Preserve]
    public static string SetLevel(string skillId, int level)
    {
        skillId = skillId?.Trim();
        if (string.IsNullOrEmpty(skillId))
            return "ERROR: skillId is required";
        if (level < 0)
            return "ERROR: level must be 0 or greater";

        GameplayData.Stats.SetSkillLevel(skillId, level);
        return GetLevel(skillId);
    }

    [ConsoleMethod(PracticeCommand, "Adds practice XP to a player skill", "skillId", "xp"), Preserve]
    public static string AddPractice(string skillId, int xp)
    {
        skillId = skillId?.Trim();
        if (string.IsNullOrEmpty(skillId))
            return "ERROR: skillId is required";
        if (xp <= 0)
            return "ERROR: xp must be greater than 0";

        GameplayData.Stats.AddPractice(skillId, xp);
        return GetLevel(skillId);
    }
}
#endif
