// ============================================================
// HandActionBindingPersistence — itemId→액션 맵 디스크 영속 (세이브 전 SSOT)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

/// <summary>
/// 전체 세이브 시스템이 없을 때 persistentDataPath JSON으로 HandActionBinding을 유지.
/// 세이브 파이프라인 도입 시 이 파일을 세이브 슬롯으로 이전.
/// </summary>
public static class HandActionBindingPersistence
{
    const string FileName = "hand_action_bindings.json";
    const int NoneSentinel = -1;

    [Serializable]
    sealed class EntryDto
    {
        public string id;
        public int action;
    }

    [Serializable]
    sealed class FileDto
    {
        public List<EntryDto> entries = new();
    }

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static void LoadInto(HandActionBinding binding)
    {
        if (binding == null)
            return;

        string path = FilePath;
        if (!File.Exists(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            FileDto dto = JsonUtility.FromJson<FileDto>(json);
            if (dto?.entries == null)
                return;

            for (int i = 0; i < dto.entries.Count; i++)
            {
                EntryDto e = dto.entries[i];
                if (e == null || string.IsNullOrEmpty(e.id))
                    continue;

                if (e.action == NoneSentinel)
                    binding.Set(e.id, null);
                else if (Enum.IsDefined(typeof(WeaponAction), e.action))
                    binding.Set(e.id, (WeaponAction)e.action);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HandActionBindingPersistence] Load failed: {ex.Message}");
        }
    }

    public static void SaveFrom(HandActionBinding binding)
    {
        if (binding == null)
            return;

        var dto = new FileDto();
        binding.ForEach((id, action) =>
        {
            dto.entries.Add(new EntryDto
            {
                id = id,
                action = action.HasValue ? (int)action.Value : NoneSentinel
            });
        });

        try
        {
            string json = JsonUtility.ToJson(dto, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HandActionBindingPersistence] Save failed: {ex.Message}");
        }
    }
}
