// ============================================================
// SpriteBakerBakeSceneRunner — Demo와 동일한 Enqueue + Catalog용 PNG export
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using SpriteBaker;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Thin bake host: fills <see cref="SpriteBakeRequest"/> like
/// <c>SpriteBakerDemo</c>, calls stock <see cref="SpriteAtlasBaker.Enqueue"/>.
/// Dist only adds KeepCpuReadable PNG/Sheet export for Catalog — no bake-loop forks.
/// </summary>
public sealed class SpriteBakerBakeSceneRunner : MonoBehaviour
{
    public const int DefaultFrameRate = 12;
    public const int DefaultFramePixelSize = 128;
    public const string DefaultOutputFolder = "Assets/Dist/SOData/SpriteBaker/Output/_Test";

    [Serializable]
    public sealed class ClipEntry
    {
        [Tooltip("Loose path: clip asset. Controller path: optional.")]
        public AnimationClip Clip;

        [Tooltip("Catalog animId / Controller state name when set.")]
        public string AnimIdOverride;

        public bool Loop = true;
        public bool Skip;
    }

    [Title("Source (Demo)")]
    [SerializeField] GameObject _characterPrefab;

    [Tooltip("If set, Controller path wins (Demo). Leave null for Kenney loose clips.")]
    [SerializeField] RuntimeAnimatorController _animatorController;

    [SerializeField] Avatar _avatarOverride;

    [Tooltip("Plugin SampleAnimationTargetPath. Kenney AC2: Root. Controller path ignored by baker.")]
    [SerializeField] string _sampleAnimationTargetPath = "Root";

    [SerializeField] Vector3 _captureRotationEuler;

    [Title("Rows")]
    [SerializeField] List<ClipEntry> _clips = new();

    [Title("Bake")]
    [SerializeField] int _framePixelSize = DefaultFramePixelSize;
    [SerializeField] int _frameRate = DefaultFrameRate;
    [Tooltip("0/1 = single angle (Demo).")]
    [SerializeField] int _captureYawCount;
    [SerializeField] float _capturePitchDegrees;
    [SerializeField] float _durationScale = 1f;

    [Title("Output (Dist Catalog)")]
    [SerializeField] string _outputFolder = DefaultOutputFolder;
    [SerializeField] SpriteBakerCatalog _catalog;
    [SerializeField] bool _spawnDemoPreview = true;
    [SerializeField] bool _exitPlayModeWhenDone;

    [Title("Run")]
    [SerializeField] bool _bakeOnStart = true;

    AnimatedSpriteRenderer _previewRenderer;

    void Start()
    {
        if (_bakeOnStart)
            StartCoroutine(BakeCoroutine());
    }

    [Button("Bake (Play Mode)"), EnableIf("@UnityEngine.Application.isPlaying")]
    void BakeButton() => StartCoroutine(BakeCoroutine());

    IEnumerator BakeCoroutine()
    {
        if (_characterPrefab == null)
        {
            Debug.LogError("[SpriteBakerBakeSceneRunner] Assign Character Prefab (project asset).", this);
            yield break;
        }

        bool useController = _animatorController != null;
        var active = new List<(ClipEntry entry, string clipName, string animId)>();
        var looseClips = new List<AnimationClip>();

        if (_clips != null)
        {
            for (int i = 0; i < _clips.Count; i++)
            {
                ClipEntry entry = _clips[i];
                if (entry == null || entry.Skip)
                    continue;
                if (!useController && entry.Clip == null)
                    continue;

                string clipName = useController
                    ? (string.IsNullOrEmpty(entry.AnimIdOverride)
                        ? (entry.Clip != null ? entry.Clip.name : null)
                        : entry.AnimIdOverride)
                    : entry.Clip.name;

                if (string.IsNullOrEmpty(clipName))
                    continue;

                string animId = string.IsNullOrEmpty(entry.AnimIdOverride)
                    ? DeriveAnimId(clipName)
                    : entry.AnimIdOverride;

                active.Add((entry, clipName, animId));
                if (!useController)
                    looseClips.Add(entry.Clip);
            }
        }

        if (active.Count == 0)
        {
            Debug.LogError(
                "[SpriteBakerBakeSceneRunner] Need AnimatorController or Loose Clips (Demo).",
                this);
            yield break;
        }

        var rows = new SpriteAnimRow[active.Count];
        for (int i = 0; i < active.Count; i++)
        {
            rows[i] = new SpriteAnimRow
            {
                Row = i,
                ClipName = active[i].clipName,
                Loop = active[i].entry.Loop,
            };
        }

        int framePx = _framePixelSize > 0 ? _framePixelSize : DefaultFramePixelSize;
        int frameRate = _frameRate > 0 ? _frameRate : DefaultFrameRate;
        float scale = _durationScale > 0f ? _durationScale : 1f;
        int key = ComputeCacheKey(active, useController, framePx, frameRate);

        SpriteAtlasCache.Evict(key);

#if UNITY_EDITOR
        EnsureFolder(_outputFolder);
#endif

        // Same shape as SpriteBakerDemo — plus KeepCpuReadable / optional yaw for Dist Catalog.
        SpriteAtlasBaker.Instance.Enqueue(new SpriteBakeRequest
        {
            Key = key,
            Prefab = _characterPrefab,
            AnimatorController = _animatorController,
            AvatarOverride = _avatarOverride,
            Clips = useController ? null : looseClips.ToArray(),
            SampleAnimationTargetPath = useController ? null : _sampleAnimationTargetPath,
            CaptureRotation = Quaternion.Euler(_captureRotationEuler),
            FramePixelSize = framePx,
            FrameRate = frameRate,
            FrameDurationScale = scale,
            CaptureYawCount = _captureYawCount,
            CapturePitch = _capturePitchDegrees,
            Rows = rows,
            Lighting = CaptureLighting.Default,
            KeepCpuReadable = true,
            BackgroundColor = Color.clear,
        });

        while (SpriteAtlasBaker.Instance.IsPending(key) || !SpriteAtlasCache.IsReady(key))
            yield return null;

        if (!SpriteAtlasCache.TryGet(key, out BakedSpriteAtlas baked) || baked.Atlas == null)
        {
            Debug.LogError("[SpriteBakerBakeSceneRunner] Bake failed (cache empty).", this);
            yield break;
        }

        if (_spawnDemoPreview)
            SpawnDemoPreview(key);

        int exported = 0;
#if UNITY_EDITOR
        exported = ExportPerAnimSheets(active, key, baked);
#endif

        Debug.Log(
            $"[SpriteBakerBakeSceneRunner] Done mode={(useController ? "Controller" : "Loose")} " +
            $"rows={active.Count} exported={exported} sample='{_sampleAnimationTargetPath}'",
            this);

#if UNITY_EDITOR
        if (_exitPlayModeWhenDone && Application.isPlaying)
            EditorApplication.isPlaying = false;
#endif
    }

    void SpawnDemoPreview(int bakeKey)
    {
        if (_previewRenderer != null)
            return;
        var go = new GameObject("SpritePlayback");
        go.transform.position = transform.position;
        _previewRenderer = go.AddComponent<AnimatedSpriteRenderer>();
        _previewRenderer.Bind(bakeKey);
        _previewRenderer.SetRow(0);
    }

#if UNITY_EDITOR
    int ExportPerAnimSheets(
        List<(ClipEntry entry, string clipName, string animId)> active,
        int sharedKey,
        BakedSpriteAtlas baked)
    {
        if (baked.Atlas == null || !baked.Atlas.isReadable)
        {
            Debug.LogError("[SpriteBakerBakeSceneRunner] Atlas not readable for export.", this);
            return 0;
        }

        int px = baked.FramePixelSize > 0 ? baked.FramePixelSize : DefaultFramePixelSize;
        int yaw = Mathf.Max(1, baked.YawCount);
        int cols = Mathf.Max(1, baked.AtlasCols);
        int exported = 0;

        for (int r = 0; r < active.Count; r++)
        {
            Texture2D sheetTex = CropAnimBlock(baked.Atlas, r, yaw, px, cols);
            if (sheetTex == null)
                continue;

            int cacheKey = unchecked(sharedKey * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(active[r].animId));
            var slice = baked;
            slice.Atlas = sheetTex;
            slice.YawCount = yaw;
            slice.AtlasCols = cols;
            slice.Rows = baked.Rows != null && r < baked.Rows.Length
                ? new[] { baked.Rows[r] }
                : new[]
                {
                    new AnimRowInfo
                    {
                        FrameCount = cols,
                        FrameDuration = 1f / frameRateSafe(),
                        Loop = active[r].entry.Loop,
                    },
                };
            slice.Rows[0].Loop = active[r].entry.Loop;

            if (ExportSheet(active[r].animId, active[r].entry.Loop, cacheKey, slice))
                exported++;
            Destroy(sheetTex);
        }

        return exported;
    }

    int frameRateSafe() => _frameRate > 0 ? _frameRate : DefaultFrameRate;

    static Texture2D CropAnimBlock(Texture2D src, int animRow, int yawCount, int px, int cols)
    {
        int blockH = yawCount * px;
        int width = cols * px;
        int srcY = animRow * blockH;
        if (srcY + blockH > src.height || width > src.width)
            return null;

        Color[] block = src.GetPixels(0, srcY, width, blockH);
        var tex = new Texture2D(width, blockH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels(block);
        tex.Apply(false, false);
        return tex;
    }

    bool ExportSheet(string animId, bool loop, int cacheKey, BakedSpriteAtlas baked)
    {
        string safe = SanitizeFileName(animId);
        string texPath = $"{_outputFolder}/{safe}_Atlas.png";
        string matPath = $"{_outputFolder}/{safe}_Mat.mat";
        string sheetPath = $"{_outputFolder}/{safe}_Sheet.asset";

        byte[] png = baked.Atlas.EncodeToPNG();
        if (png == null || png.Length == 0)
            return false;

        File.WriteAllBytes(ToAbsolute(texPath), png);
        AssetDatabase.ImportAsset(texPath);
        ConfigureAtlasImporter(texPath);

        Texture2D texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (texAsset == null)
            return false;

        Material matAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matAsset == null)
        {
            AssetDatabase.CreateAsset(SpriteAtlasBaker.CreatePlaybackMaterial(texAsset), matPath);
            matAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        }
        else
        {
            if (matAsset.HasProperty("_BaseMap"))
                matAsset.SetTexture("_BaseMap", texAsset);
            matAsset.mainTexture = texAsset;
            EditorUtility.SetDirty(matAsset);
        }

        SpriteBakerSheetAsset sheet = AssetDatabase.LoadAssetAtPath<SpriteBakerSheetAsset>(sheetPath);
        if (sheet == null)
        {
            sheet = ScriptableObject.CreateInstance<SpriteBakerSheetAsset>();
            AssetDatabase.CreateAsset(sheet, sheetPath);
        }

        var meta = baked;
        meta.Atlas = texAsset;
        meta.SharedMaterial = matAsset;
        sheet.EditorApplyBakeResult(animId, texAsset, matAsset, meta, loop, cacheKey);
        EditorUtility.SetDirty(sheet);

        if (_catalog != null)
        {
            _catalog.EditorUpsert(animId, sheet);
            EditorUtility.SetDirty(_catalog);
        }

        AssetDatabase.SaveAssets();
        return true;
    }

    void ConfigureAtlasImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;
        string[] parts = assetFolder.Replace('\\', '/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string ToAbsolute(string assetPath)
    {
        string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.GetFullPath(Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Clip";
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            for (int j = 0; j < invalid.Length; j++)
            {
                if (chars[i] == invalid[j] || chars[i] == '|' || chars[i] == ' ')
                {
                    chars[i] = '_';
                    break;
                }
            }
        }
        return new string(chars);
    }
#endif

    static string DeriveAnimId(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return "Clip";
        int pipe = clipName.LastIndexOf('|');
        return pipe >= 0 && pipe < clipName.Length - 1
            ? clipName.Substring(pipe + 1).Trim()
            : clipName;
    }

    int ComputeCacheKey(
        List<(ClipEntry entry, string clipName, string animId)> active,
        bool useController,
        int framePx,
        int frameRate)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (_characterPrefab != null ? _characterPrefab.name.GetHashCode() : 0);
            hash = hash * 31 + (useController && _animatorController != null
                ? _animatorController.GetInstanceID()
                : 0);
            for (int i = 0; i < active.Count; i++)
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(active[i].animId);
            hash = hash * 31 + framePx;
            hash = hash * 31 + frameRate;
            hash = hash * 31 + _captureYawCount;
            return hash;
        }
    }
}
