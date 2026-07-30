// ============================================================
// SpriteBakerBakeSceneRunner — 베이크 전용 씬에서 Enqueue로 아틀라스 추출·Output 저장
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
/// Play Mode bake host: uses stock <see cref="SpriteAtlasBaker.Enqueue"/> (frame yields),
/// then writes Output PNG / material / <see cref="SpriteBakerSheetAsset"/>.
/// Configure on the bake scene — no CaptureRecipe SO.
/// </summary>
public sealed class SpriteBakerBakeSceneRunner : MonoBehaviour
{
    public const int DefaultYawCount = 8;
    public const float DefaultIsoPitchDegrees = 35.264f;
    public const int DefaultFrameRate = 12;
    public const int DefaultFramePixelSize = 128;
    public const string DefaultOutputFolder = "Assets/Dist/SOData/SpriteBaker/Output/_Test";

    [Serializable]
    public sealed class ClipEntry
    {
        public AnimationClip Clip;
        public string AnimIdOverride;
        public bool Loop = true;
        [Tooltip("≤0 = use runner default durationScale")]
        public float DurationScale;
        public bool Skip;
    }

    [Title("Source")]
    [SerializeField] GameObject _characterPrefab;
    [Tooltip("Armature child under Character Prefab (e.g. Root). Empty = prefab root.")]
    [SerializeField] string _sampleAnimationTargetPath = "Root";
    [SerializeField] Vector3 _captureRotationEuler;

    [Title("Clips")]
    [SerializeField] List<ClipEntry> _clips = new();

    [Title("Bake")]
    [SerializeField] int _framePixelSize = DefaultFramePixelSize;
    [SerializeField] int _frameRate = DefaultFrameRate;
    [SerializeField] int _captureYawCount = DefaultYawCount;
    [SerializeField] float _capturePitchDegrees = DefaultIsoPitchDegrees;
    [SerializeField] float _durationScale = 1f;

    [Title("Output")]
    [SerializeField] string _outputFolder = DefaultOutputFolder;
    [SerializeField] SpriteBakerCatalog _catalog;
    [SerializeField] bool _exitPlayModeWhenDone = true;

    [Title("Run")]
    [SerializeField] bool _bakeOnStart = true;

    void Start()
    {
        if (_bakeOnStart)
            StartCoroutine(BakeAllCoroutine());
    }

    [Button("Bake All (Play Mode)"), EnableIf("@UnityEngine.Application.isPlaying")]
    void BakeAllButton()
    {
        StartCoroutine(BakeAllCoroutine());
    }

    IEnumerator BakeAllCoroutine()
    {
        if (_characterPrefab == null)
        {
            Debug.LogError("[SpriteBakerBakeSceneRunner] Character Prefab missing.", this);
            yield break;
        }

        if (_clips == null || _clips.Count == 0)
        {
            Debug.LogError("[SpriteBakerBakeSceneRunner] Assign clips on the runner.", this);
            yield break;
        }

#if UNITY_EDITOR
        EnsureFolder(_outputFolder);
#endif

        int baked = 0;
        for (int i = 0; i < _clips.Count; i++)
        {
            ClipEntry entry = _clips[i];
            if (entry == null || entry.Clip == null || entry.Skip)
                continue;

            string animId = string.IsNullOrEmpty(entry.AnimIdOverride)
                ? DeriveAnimId(entry.Clip.name)
                : entry.AnimIdOverride;

            float scale = entry.DurationScale > 0f ? entry.DurationScale : _durationScale;
            int key = ComputeCacheKey(animId, scale);

            SpriteAtlasCache.Evict(key);

            var rows = new[]
            {
                new SpriteAnimRow
                {
                    Row = 0,
                    ClipName = entry.Clip.name,
                    Loop = entry.Loop,
                    SingleFrame = false,
                },
            };

            SpriteAtlasBaker.Instance.Enqueue(new SpriteBakeRequest
            {
                Key = key,
                Prefab = _characterPrefab,
                Clips = new[] { entry.Clip },
                SampleAnimationTargetPath = _sampleAnimationTargetPath,
                CaptureRotation = Quaternion.Euler(_captureRotationEuler),
                FramePixelSize = _framePixelSize > 0 ? _framePixelSize : DefaultFramePixelSize,
                FrameRate = _frameRate > 0 ? _frameRate : DefaultFrameRate,
                FrameDurationScale = scale > 0f ? scale : 1f,
                CaptureYawCount = _captureYawCount > 0 ? _captureYawCount : DefaultYawCount,
                CapturePitch = _capturePitchDegrees,
                Rows = rows,
                Lighting = CaptureLighting.Default,
                KeepCpuReadable = true,
                SkipCacheStore = false,
                BackgroundColor = Color.clear,
            });

            while (SpriteAtlasBaker.Instance.IsPending(key) || !SpriteAtlasCache.IsReady(key))
                yield return null;

            if (!SpriteAtlasCache.TryGet(key, out BakedSpriteAtlas bakedAtlas) ||
                bakedAtlas.Atlas == null)
            {
                Debug.LogError($"[SpriteBakerBakeSceneRunner] Bake failed for '{animId}'.", this);
                continue;
            }

#if UNITY_EDITOR
            if (ExportSheet(animId, entry.Loop, key, bakedAtlas))
                baked++;
#else
            Debug.LogWarning(
                "[SpriteBakerBakeSceneRunner] Export requires Editor. Atlas left in runtime cache.",
                this);
            baked++;
#endif
            SpriteAtlasCache.Evict(key);
        }

        Debug.Log($"[SpriteBakerBakeSceneRunner] Done. Exported {baked} sheet(s) → {_outputFolder}", this);

#if UNITY_EDITOR
        if (_exitPlayModeWhenDone && Application.isPlaying)
            EditorApplication.isPlaying = false;
#endif
    }

    static string DeriveAnimId(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return "Clip";
        int pipe = clipName.LastIndexOf('|');
        if (pipe >= 0 && pipe < clipName.Length - 1)
            return clipName.Substring(pipe + 1).Trim();
        return clipName;
    }

    int ComputeCacheKey(string animId, float durationScale)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (_characterPrefab != null ? _characterPrefab.name.GetHashCode() : 0);
            hash = hash * 31 + (animId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(animId) : 0);
            hash = hash * 31 + (_framePixelSize > 0 ? _framePixelSize : DefaultFramePixelSize);
            hash = hash * 31 + (_frameRate > 0 ? _frameRate : DefaultFrameRate);
            hash = hash * 31 + (_captureYawCount > 0 ? _captureYawCount : DefaultYawCount);
            hash = hash * 31 + (int)(_capturePitchDegrees * 1000f);
            hash = hash * 31 + (int)(durationScale * 1000f);
            return hash;
        }
    }

#if UNITY_EDITOR
    bool ExportSheet(string animId, bool loop, int cacheKey, BakedSpriteAtlas baked)
    {
        string safeName = SanitizeFileName(animId);
        string texPath = $"{_outputFolder}/{safeName}_Atlas.png";
        string matPath = $"{_outputFolder}/{safeName}_Mat.mat";
        string sheetPath = $"{_outputFolder}/{safeName}_Sheet.asset";

        byte[] png = baked.Atlas.EncodeToPNG();
        if (png == null || png.Length == 0)
        {
            Debug.LogError($"[SpriteBakerBakeSceneRunner] EncodeToPNG failed for '{animId}'.", this);
            return false;
        }

        File.WriteAllBytes(ToAbsolute(texPath), png);
        AssetDatabase.ImportAsset(texPath);
        ConfigureAtlasImporter(texPath);

        Texture2D texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (texAsset == null)
            return false;

        Material matAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matAsset == null)
        {
            Material runtimeMat = SpriteAtlasBaker.CreatePlaybackMaterial(texAsset);
            AssetDatabase.CreateAsset(runtimeMat, matPath);
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

        BakedSpriteAtlas meta = baked;
        meta.Atlas = texAsset;
        meta.SharedMaterial = matAsset;
        if (meta.Rows != null && meta.Rows.Length > 0)
            meta.Rows[0].Loop = loop;

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
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
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
}
