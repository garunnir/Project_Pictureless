// ============================================================
// HandActionBindingPersistence — DEPRECATED. itemId 영속 중단.
// ============================================================

using UnityEngine;

/// <summary>itemId 맵은 select SSOT가 아님. Load/Save는 no-op.</summary>
public static class HandActionBindingPersistence
{
    public static string FilePath =>
        System.IO.Path.Combine(Application.persistentDataPath, "hand_action_bindings.json");

    public static void LoadInto(HandActionBinding binding)
    {
    }

    public static void SaveFrom(HandActionBinding binding)
    {
    }
}
