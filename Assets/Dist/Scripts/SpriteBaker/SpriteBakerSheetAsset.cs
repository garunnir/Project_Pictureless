// ============================================================
// SpriteBakerSheetAsset — 에디터 사전베이크 Output 시트 1장 (클립당 1시트)
// ============================================================

using SpriteBaker;
using UnityEngine;

/// <summary>
/// One pre-baked sprite sheet (typically one animId / clip). Runtime
/// <see cref="SpriteBakerCatalog"/> registers these into
/// <see cref="SpriteAtlasCache"/>.
/// </summary>
[CreateAssetMenu(fileName = "SpriteBakerSheet", menuName = "Dist/SpriteBaker/Sheet")]
public sealed class SpriteBakerSheetAsset : ScriptableObject
{
    [SerializeField] string _animId;
    [SerializeField] Texture2D _atlas;
    [SerializeField] Material _sharedMaterial;
    [SerializeField] int _framePixelSize = 128;
    [SerializeField] int _atlasCols = 1;
    [SerializeField] float _quadWidth = 1f;
    [SerializeField] float _quadHeight = 1f;
    [SerializeField] int _yawCount = 8;
    [SerializeField] int _frameCount = 1;
    [SerializeField] float _frameDuration = 1f / 12f;
    [SerializeField] bool _loop = true;
    [SerializeField] int _cacheKey;

    public string AnimId => _animId;
    public int CacheKey => _cacheKey;
    public Texture2D Atlas => _atlas;
    public Material SharedMaterial => _sharedMaterial;
    public bool Loop => _loop;

    public BakedSpriteAtlas ToBakedAtlas()
    {
        return new BakedSpriteAtlas
        {
            Atlas = _atlas,
            SharedMaterial = _sharedMaterial,
            FramePixelSize = _framePixelSize,
            AtlasCols = _atlasCols,
            QuadWidth = _quadWidth,
            QuadHeight = _quadHeight,
            YawCount = Mathf.Max(1, _yawCount),
            Rows = new[]
            {
                new AnimRowInfo
                {
                    FrameCount = Mathf.Max(1, _frameCount),
                    FrameDuration = _frameDuration > 0f ? _frameDuration : (1f / 12f),
                    Loop = _loop,
                },
            },
        };
    }

#if UNITY_EDITOR
    public void EditorApplyBakeResult(
        string animId,
        Texture2D atlas,
        Material material,
        BakedSpriteAtlas baked,
        bool loop,
        int cacheKey)
    {
        _animId = animId;
        _atlas = atlas;
        _sharedMaterial = material;
        _framePixelSize = baked.FramePixelSize;
        _atlasCols = baked.AtlasCols;
        _quadWidth = baked.QuadWidth;
        _quadHeight = baked.QuadHeight;
        _yawCount = baked.YawCount;
        _loop = loop;
        _cacheKey = cacheKey;

        if (baked.Rows != null && baked.Rows.Length > 0)
        {
            _frameCount = baked.Rows[0].FrameCount;
            _frameDuration = baked.Rows[0].FrameDuration;
            _loop = baked.Rows[0].Loop;
        }
    }
#endif
}
