// ============================================================
// UICanvasLayerHost — UICanvas의 렌더링 레이어 순서를 enum 기반 SSOT로 관리
// ============================================================

using System;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public sealed class UICanvasLayerHost : MonoBehaviour
{
    static readonly UICanvasLayer[] _allLayers = (UICanvasLayer[])Enum.GetValues(typeof(UICanvasLayer));
    static readonly (string name, UICanvasLayer layer)[] _migrationMap =
    {
        ("Grp_StaticPopUp",              UICanvasLayer.Popup),
        ("Grp_InstancePopup",            UICanvasLayer.Popup),
        ("Grp_InventoryLaunchers",       UICanvasLayer.HUD),
        ("ItemContextMenu",              UICanvasLayer.ContextMenu),
        ("InventoryScrollDragOverlay",   UICanvasLayer.Overlay),
        ("InventoryDragGhost",           UICanvasLayer.TopMost),
    };

    readonly RectTransform[] _roots = new RectTransform[_allLayers.Length];

    void Awake()
    {
        EnsureLayerRoots();
        AutoMigrateOrphanedChildren();
    }

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

#if UNITY_EDITOR
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
