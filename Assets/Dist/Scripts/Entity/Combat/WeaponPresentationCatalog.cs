// ============================================================
// WeaponPresentationCatalog — itemId → category → Unarmed resolve
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponPresentationCatalog",
    menuName = "Dist/Combat/Weapon Presentation Catalog")]
public sealed class WeaponPresentationCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Binding
    {
        public string id;
        public WeaponPresentation presentation;
    }

    [SerializeField] Binding[] _byItemId = Array.Empty<Binding>();
    [SerializeField] Binding[] _byCategoryId = Array.Empty<Binding>();
    [SerializeField] WeaponPresentation _unarmed;
    [SerializeField] WeaponActionVfxDefaults _actionVfxDefaults;

    public WeaponPresentation Unarmed => _unarmed;
    public WeaponActionVfxDefaults ActionVfxDefaults => _actionVfxDefaults;

    public WeaponPresentation Resolve(string itemId, ItemData item)
    {
        if (!string.IsNullOrEmpty(itemId) &&
            TryFind(_byItemId, itemId, out WeaponPresentation byItem))
            return byItem;

        if (item?.weapon_category != null)
        {
            for (int i = 0; i < item.weapon_category.Count; i++)
            {
                string categoryId = item.weapon_category[i];
                if (string.IsNullOrEmpty(categoryId))
                    continue;
                if (TryFind(_byCategoryId, categoryId, out WeaponPresentation byCategory))
                    return byCategory;
            }
        }

        return _unarmed;
    }

    public bool TryGetByItemId(string itemId, out WeaponPresentation presentation) =>
        TryFind(_byItemId, itemId, out presentation);

    public void EnsureItemBinding(string itemId, WeaponPresentation presentation)
    {
        if (string.IsNullOrEmpty(itemId) || presentation == null)
            return;

        if (_byItemId != null)
        {
            for (int i = 0; i < _byItemId.Length; i++)
            {
                Binding binding = _byItemId[i];
                if (binding == null ||
                    !string.Equals(binding.id, itemId, StringComparison.Ordinal))
                    continue;
                binding.presentation = presentation;
                return;
            }
        }

        int len = _byItemId?.Length ?? 0;
        var next = new Binding[len + 1];
        if (_byItemId != null && len > 0)
            Array.Copy(_byItemId, next, len);
        next[len] = new Binding { id = itemId, presentation = presentation };
        _byItemId = next;
    }

    public void UnlinkItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || _byItemId == null || _byItemId.Length == 0)
            return;

        int keep = 0;
        for (int i = 0; i < _byItemId.Length; i++)
        {
            Binding binding = _byItemId[i];
            if (binding == null ||
                string.Equals(binding.id, itemId, StringComparison.Ordinal))
                continue;
            _byItemId[keep++] = binding;
        }

        if (keep == _byItemId.Length)
            return;

        Array.Resize(ref _byItemId, keep);
    }

    static bool TryFind(Binding[] bindings, string id, out WeaponPresentation presentation)
    {
        presentation = null;
        if (bindings == null || string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < bindings.Length; i++)
        {
            Binding binding = bindings[i];
            if (binding == null ||
                binding.presentation == null ||
                !string.Equals(binding.id, id, StringComparison.Ordinal))
                continue;
            presentation = binding.presentation;
            return true;
        }

        return false;
    }
}
