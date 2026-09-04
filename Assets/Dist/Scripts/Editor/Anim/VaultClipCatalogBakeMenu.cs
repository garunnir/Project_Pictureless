#if UNITY_EDITOR
// ============================================================
// VaultClipCatalogBakeMenu — Vault 클립 루트 → progress 커브 bake
// ============================================================

using UnityEditor;
using UnityEngine;

static class VaultClipCatalogBakeMenu
{
    [MenuItem("Dist/MCP/Bake Vault Hybrid Progress Curves")]
    static void BakeVaultHybridProgressCurves()
    {
        VaultClipCatalog catalog = AssetDatabase.LoadAssetAtPath<VaultClipCatalog>(
            VaultClipCatalog.DefaultAssetPath);
        if (catalog == null)
        {
            Debug.LogError(
                "[VaultClipCatalogBakeMenu] Missing catalog at " + VaultClipCatalog.DefaultAssetPath);
            return;
        }

        SerializedObject so = new SerializedObject(catalog);
        int baked = 0;
        baked += BakePair(so, "_lowCross", "_lowCrossProgress");
        baked += BakePair(so, "_lowMantle", "_lowMantleProgress");
        baked += BakePair(so, "_highCross", "_highCrossProgress");
        baked += BakeHighMantleAxisPair(so);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VaultClipCatalogBakeMenu] Baked {baked} progress curve(s).");
    }

    static int BakePair(SerializedObject so, string clipProperty, string curveProperty)
    {
        var clipProp = so.FindProperty(clipProperty);
        var curveProp = so.FindProperty(curveProperty);
        if (clipProp == null || curveProp == null)
            return 0;

        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        AnimationCurve baked = AnimationRootProgressBake.TryBakeProgressCurve(clip);
        if (baked == null)
        {
            Debug.LogWarning(
                $"[VaultClipCatalogBakeMenu] No root motion to bake for '{clip?.name ?? clipProperty}' — runtime uses linear progress.");
            return 0;
        }

        curveProp.animationCurveValue = baked;
        return 1;
    }

    static int BakeHighMantleAxisPair(SerializedObject so)
    {
        var clipProp = so.FindProperty("_highMantle");
        var yProp = so.FindProperty("_highMantleProgress");
        var xzProp = so.FindProperty("_highMantleXzProgress");
        if (clipProp == null || yProp == null || xzProp == null)
            return 0;

        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        if (!AnimationRootProgressBake.TryBakeAxisProgressCurves(clip, out AnimationCurve yCurve, out AnimationCurve xzCurve))
        {
            Debug.LogWarning(
                $"[VaultClipCatalogBakeMenu] No root motion to bake for High Mantle '{clip?.name ?? "_highMantle"}' — runtime uses linear progress.");
            return 0;
        }

        int baked = 0;
        if (yCurve != null)
        {
            yProp.animationCurveValue = yCurve;
            baked++;
        }

        if (xzCurve != null)
        {
            xzProp.animationCurveValue = xzCurve;
            baked++;
        }

        return baked;
    }
}
#endif
