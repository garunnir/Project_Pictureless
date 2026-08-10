// ============================================================
// UICanvasLayerHost — UICanvas의 렌더링 레이어 순서를 enum 기반 SSOT로 관리
// ============================================================

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class UICanvasLayerHost : MonoBehaviour
{
    static readonly UICanvasLayer[] _allLayers = (UICanvasLayer[])Enum.GetValues(typeof(UICanvasLayer));

    // Scene-resident groups only. Ephemeral UI (ghost/overlay/context) is prefab-spawned at runtime.
    static readonly (string name, UICanvasLayer layer)[] _migrationMap =
    {
        ("Grp_StaticPopUp",        UICanvasLayer.Popup),
        ("Grp_InstancePopup",      UICanvasLayer.Popup),
        ("Grp_InventoryLaunchers", UICanvasLayer.HUD),
    };

    readonly RectTransform[] _roots = new RectTransform[_allLayers.Length];

    [ShowInInspector, ReadOnly, PropertyOrder(-10)]
    [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false, DraggableItems = false)]
    [LabelText("Render Order (bottom → top)")]
    List<string> LayerOrderPreview => BuildPreview();

    void Awake()
    {
        EnsureLayerRoots();
        AutoMigrateOrphanedChildren();
        EnsureWindowActivate();
    }

    void EnsureWindowActivate()
    {
        if (TryGetComponent(out UIOverlayWindowActivate _))
            return;

        // Scene Canvas host — not a UIComponents prefab instance.
        gameObject.AddComponent<UIOverlayWindowActivate>();
    }

    void OnEnable()
    {
        UIContextMenuHost.TryResolveParent = ResolveContextMenuParent;
    }

    void OnDisable()
    {
        if (UIContextMenuHost.TryResolveParent == ResolveContextMenuParent)
            UIContextMenuHost.TryResolveParent = null;
    }

    Transform ResolveContextMenuParent() => GetLayerRoot(UICanvasLayer.ContextMenu);

    public Transform GetLayerRoot(UICanvasLayer layer)
    {
        int idx = (int)layer;
        if (idx < 0 || idx >= _roots.Length)
            return transform;

        if (_roots[idx] == null)
            EnsureLayerRoots();

        return _roots[idx] != null ? _roots[idx] : transform;
    }

    void EnsureLayerRoots()
    {
        for (int i = 0; i < _allLayers.Length; i++)
        {
            UICanvasLayer layer = _allLayers[i];
            string rootName = $"Layer_{layer}";
            RectTransform existing = FindChildByName(rootName);

            if (existing != null)
            {
                _roots[i] = existing;
            }
            else
            {
                var go = new GameObject(rootName, typeof(RectTransform));
                RectTransform rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                StretchFill(rt);
                _roots[i] = rt;
            }
        }

        for (int i = 0; i < _roots.Length; i++)
            _roots[i].SetSiblingIndex(i);
    }

    void AutoMigrateOrphanedChildren()
    {
        for (int m = 0; m < _migrationMap.Length; m++)
        {
            (string childName, UICanvasLayer targetLayer) = _migrationMap[m];
            ReparentDirectChild(childName, targetLayer);
        }
    }

    void ReparentDirectChild(string childName, UICanvasLayer targetLayer)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != childName)
                continue;

            Transform layerRoot = GetLayerRoot(targetLayer);
            if (child == layerRoot || child.parent == layerRoot)
                return;

            child.SetParent(layerRoot, false);
            return;
        }
    }

    RectTransform FindChildByName(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == childName)
                return child as RectTransform;
        }

        return null;
    }

    static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    List<string> BuildPreview()
    {
        var lines = new List<string>(_allLayers.Length);
        for (int i = 0; i < _allLayers.Length; i++)
        {
            UICanvasLayer layer = _allLayers[i];
            string rootName = $"Layer_{layer}";
            RectTransform root = FindChildByName(rootName);
            if (root != null)
                lines.Add($"{(int)layer}  {layer,-12} {rootName} ✓ ({root.childCount} children)");
            else
                lines.Add($"{(int)layer}  {layer,-12} {rootName} ✗ (missing)");
        }

        return lines;
    }

#if UNITY_EDITOR
    [Button("Setup Layer Hierarchy"), PropertyOrder(-5)]
    [ContextMenu("Setup Layer Hierarchy")]
    public void EditorSetupLayerHierarchy()
    {
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Setup UI Canvas Layers");

        EnsureLayerRoots();
        AutoMigrateOrphanedChildren();

        for (int i = 0; i < _roots.Length; i++)
            _roots[i].SetSiblingIndex(i);

        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[UICanvasLayerHost] Layer hierarchy setup complete.", this);
    }
#endif
}
