// ============================================================
// ContainerIconCatalog — containerDefId → Sprite (가상·월드 컨테이너 UI 아이콘 SSOT)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ContainerIconCatalog", menuName = "Dist/Container Icon Catalog")]
public sealed class ContainerIconCatalog : ScriptableObject
{
    public const string ResourcesLoadName = "ContainerIconCatalog";
    public const string DefaultAssetPath = "Assets/Dist/Resources/ContainerIconCatalog.asset";

    [Serializable]
    public sealed class Entry
    {
        public string ContainerId;
        public Sprite Icon;
    }

    [SerializeField] Sprite _defaultIcon;
    [SerializeField] List<Entry> _entries = new();

    Dictionary<string, Sprite> _map;

    public Sprite DefaultIcon => _defaultIcon;
    public IReadOnlyList<Entry> Entries => _entries;

    public static ContainerIconCatalog Active
    {
        get
        {
            if (_active == null)
                _active = LoadCatalog();
            return _active;
        }
    }

    static ContainerIconCatalog _active;

    void OnEnable() => RebuildCache();

    public void RebuildCache()
    {
        _map = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.ContainerId) || entry.Icon == null)
                continue;

            _map[entry.ContainerId] = entry.Icon;
        }
    }

    public Sprite GetAssignedIcon(string containerDefId)
    {
        if (string.IsNullOrEmpty(containerDefId))
            return null;

        if (_map == null)
            RebuildCache();

        return _map.TryGetValue(containerDefId, out Sprite icon) ? icon : null;
    }

    public Sprite Resolve(string containerDefId)
    {
        Sprite assigned = GetAssignedIcon(containerDefId);
        return assigned != null ? assigned : _defaultIcon;
    }

    public static void BindCatalog(ContainerIconCatalog catalog)
    {
        _active = catalog;
        catalog?.RebuildCache();
    }

    public static void InvalidateCache() => _active = null;

    static ContainerIconCatalog LoadCatalog()
    {
        ContainerIconCatalog fromResources = Resources.Load<ContainerIconCatalog>(ResourcesLoadName);
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<ContainerIconCatalog>(DefaultAssetPath);
#else
        return null;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => InvalidateCache();
}
