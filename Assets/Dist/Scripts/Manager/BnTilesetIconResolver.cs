// ============================================================
// BnTilesetIconResolver — BN 타일셋(MSX++)에서 itemId → Sprite (lazy)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class BnTilesetIconResolver
{
    public const string RefDataFolder = "BNData";
    public const string TilesetFolder = "tileset";
    public const string IndexFileName = "item_sprites.json";
    public const int DefaultPixelsPerUnit = 100;

    static readonly Vector2 Pivot = new Vector2(0.5f, 0.5f);

    static BnTilesetIndexFile _index;
    static bool _loadAttempted;
    static readonly Dictionary<string, Texture2D> Atlases = new(StringComparer.Ordinal);
    static readonly Dictionary<string, Sprite> Sprites = new(StringComparer.Ordinal);

    public static bool Contains(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        BnTilesetIndexFile index = LoadIndex();
        return index?.items != null && index.items.ContainsKey(itemId);
    }

    public static bool TryGet(string itemId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (Sprites.TryGetValue(itemId, out sprite) && sprite != null)
            return true;

        BnTilesetIndexFile index = LoadIndex();
        if (index?.items == null ||
            !index.items.TryGetValue(itemId, out BnTilesetItemSpec spec) ||
            spec == null ||
            string.IsNullOrEmpty(spec.file))
            return false;

        sprite = CreateSprite(index, spec);
        if (sprite == null)
            return false;

        Sprites[itemId] = sprite;
        return true;
    }

    public static void Invalidate()
    {
        foreach (KeyValuePair<string, Sprite> pair in Sprites)
            DestroyUnityObject(pair.Value);
        foreach (KeyValuePair<string, Texture2D> pair in Atlases)
            DestroyUnityObject(pair.Value);

        Sprites.Clear();
        Atlases.Clear();
        _index = null;
        _loadAttempted = false;
    }

    static BnTilesetIndexFile LoadIndex()
    {
        if (_loadAttempted)
            return _index;

        _loadAttempted = true;
        string path = Path.Combine(
            Application.streamingAssetsPath,
            RefDataFolder,
            TilesetFolder,
            IndexFileName);

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            _index = JsonConvert.DeserializeObject<BnTilesetIndexFile>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BnTilesetIconResolver] Failed to load '{path}': {ex.Message}");
            _index = null;
        }

        return _index;
    }

    static Sprite CreateSprite(BnTilesetIndexFile index, BnTilesetItemSpec spec)
    {
        Texture2D atlas = LoadAtlas(spec.file);
        if (atlas == null)
            return null;

        if (index.files == null ||
            !index.files.TryGetValue(spec.file, out BnTilesetFileSpec fileSpec) ||
            fileSpec == null)
            return null;

        int spriteW = fileSpec.sprite_width;
        int spriteH = fileSpec.sprite_height;
        if (spriteW <= 0 || spriteH <= 0)
            return null;

        int cols = atlas.width / spriteW;
        int rows = atlas.height / spriteH;
        if (cols <= 0 || rows <= 0)
            return null;

        int local = spec.index;
        if (local < 0 || local >= cols * rows)
            return null;

        int col = local % cols;
        int row = local / cols;
        int srcX = col * spriteW;
        int srcY = row * spriteH;
        int unityY = atlas.height - srcY - spriteH;
        if (srcX < 0 || unityY < 0 || srcX + spriteW > atlas.width || unityY + spriteH > atlas.height)
            return null;

        int ppu = index.ppu > 0 ? index.ppu : DefaultPixelsPerUnit;
        var rect = new Rect(srcX, unityY, spriteW, spriteH);
        Sprite sprite = Sprite.Create(atlas, rect, Pivot, ppu, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static Texture2D LoadAtlas(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (Atlases.TryGetValue(fileName, out Texture2D cached) && cached != null)
            return cached;

        string path = Path.Combine(
            Application.streamingAssetsPath,
            RefDataFolder,
            TilesetFolder,
            fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"[BnTilesetIconResolver] Atlas missing: {path}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
            name = fileName
        };
        if (!tex.LoadImage(bytes, false))
        {
            DestroyUnityObject(tex);
            Debug.LogError($"[BnTilesetIconResolver] LoadImage failed: {path}");
            return null;
        }

        Atlases[fileName] = tex;
        return tex;
    }

    static void DestroyUnityObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(obj);
            return;
        }
#endif
        UnityEngine.Object.Destroy(obj);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Invalidate();

    sealed class BnTilesetIndexFile
    {
        public int ppu = DefaultPixelsPerUnit;
        public Dictionary<string, BnTilesetFileSpec> files;
        public Dictionary<string, BnTilesetItemSpec> items;
    }

    sealed class BnTilesetFileSpec
    {
        public int sprite_width;
        public int sprite_height;
    }

    sealed class BnTilesetItemSpec
    {
        public string file;
        public int index;
    }
}
