using System;
using UnityEngine;

namespace SpriteBaker
{
    /// <summary>
    /// Plays back a baked sprite atlas as an animated quad in the scene.
    /// Drop on a GameObject, call <see cref="Bind"/> with the cache key, then
    /// drive animation state from your gameplay code via
    /// <see cref="SetRow"/> + <see cref="SetFacing"/>.
    ///
    /// When <see cref="ExternalClock"/> is true, built-in <c>Update</c> skips
    /// advancement — callers drive time via <see cref="Tick"/>.
    /// </summary>
    public class AnimatedSpriteRenderer : MonoBehaviour
    {
        private BakedSpriteAtlas atlas;
        private bool hasAtlas;
        private int boundKey;

        private int currentRow;
        private int currentFrame;
        private float animTimer;
        private bool facingRight = true;
        private bool rowComplete;
        private bool externalClock;

        private int   yawIndex;
        private int   yawCount = 1;
        private float pendingYawDegrees;
        private bool  hasPendingYaw;

        private Mesh quadMesh;
        private MeshRenderer meshRenderer;
        private Vector2[] uvBuffer = new Vector2[4];
        private float frameU, frameV;

        public bool ExternalClock
        {
            get => externalClock;
            set => externalClock = value;
        }

        public bool IsRowComplete => rowComplete;
        public int CurrentRow => currentRow;
        public event Action RowCompleted;

        public void Bind(int atlasKey)
        {
            boundKey = atlasKey;
            EnsureQuadMesh();
            hasAtlas = false;
            rowComplete = false;
            if (meshRenderer != null) meshRenderer.enabled = false;
            TryBindAtlas();
        }

        public void SetRow(int row)
        {
            currentRow = row;
            currentFrame = 0;
            animTimer = 0f;
            rowComplete = false;

            if (!hasAtlas) return;
            if (row < 0 || row >= atlas.Rows.Length) return;
            if (atlas.Rows[row].FrameCount <= 0) return;
            UpdateUVs();
        }

        public void SetFacing(bool right) => facingRight = right;

        public void SetYaw(float degrees)
        {
            if (!hasAtlas)
            {
                pendingYawDegrees = degrees;
                hasPendingYaw = true;
                return;
            }
            int idx = ComputeYawIndex(degrees);
            if (idx == yawIndex) return;
            yawIndex = idx;
            UpdateUVs();
        }

        public void SetBillboardYaw(float degrees)
        {
            transform.rotation = Quaternion.Euler(0f, degrees + 180f, 0f);
        }

        public void Tick(float deltaTime)
        {
            if (!hasAtlas)
            {
                TryBindAtlas();
                if (!hasAtlas) return;
            }

            if (atlas.Rows == null || currentRow < 0 || currentRow >= atlas.Rows.Length)
                return;

            var info = atlas.Rows[currentRow];
            if (info.FrameCount <= 0)
                return;

            if (info.FrameCount > 1 && deltaTime > 0f && !rowComplete)
            {
                animTimer += deltaTime;
                while (animTimer >= info.FrameDuration && info.FrameDuration > 0f)
                {
                    animTimer -= info.FrameDuration;
                    if (info.Loop)
                    {
                        currentFrame = (currentFrame + 1) % info.FrameCount;
                    }
                    else if (currentFrame < info.FrameCount - 1)
                    {
                        currentFrame++;
                    }
                    else
                    {
                        MarkRowComplete();
                        break;
                    }
                }
            }
            else if (info.FrameCount <= 1)
            {
                currentFrame = 0;
                if (!info.Loop && !rowComplete)
                    MarkRowComplete();
            }

            UpdateUVs();
        }

        private void MarkRowComplete()
        {
            if (rowComplete) return;
            rowComplete = true;
            RowCompleted?.Invoke();
        }

        private int ComputeYawIndex(float degrees)
        {
            if (yawCount <= 1) return 0;
            float wrapped = Mathf.Repeat(degrees, 360f);
            float bin = 360f / yawCount;
            int idx = Mathf.RoundToInt(wrapped / bin);
            if (idx >= yawCount) idx -= yawCount;
            if (idx < 0) idx += yawCount;
            return idx;
        }

        private void Update()
        {
            if (externalClock)
            {
                if (!hasAtlas)
                    TryBindAtlas();
                return;
            }

            Tick(Time.deltaTime);
        }

        private void TryBindAtlas()
        {
            if (!SpriteAtlasCache.TryGet(boundKey, out atlas)) return;

            float hw = atlas.QuadWidth * 0.5f;
            float hh = atlas.QuadHeight;
            quadMesh.vertices = new[]
            {
                new Vector3(-hw, 0, 0),
                new Vector3( hw, 0, 0),
                new Vector3( hw, hh, 0),
                new Vector3(-hw, hh, 0),
            };
            quadMesh.RecalculateBounds();

            meshRenderer.sharedMaterial = atlas.SharedMaterial;
            meshRenderer.enabled = true;

            yawCount = Mathf.Max(1, atlas.YawCount);
            int textureRows = Mathf.Max(1, atlas.Rows.Length) * yawCount;

            frameU = 1f / Mathf.Max(1, atlas.AtlasCols);
            frameV = 1f / textureRows;

            hasAtlas = true;

            if (hasPendingYaw)
            {
                yawIndex = ComputeYawIndex(pendingYawDegrees);
                hasPendingYaw = false;
            }

            UpdateUVs();
        }

        private void EnsureQuadMesh()
        {
            if (quadMesh != null) return;
            quadMesh = new Mesh { name = "SpriteQuad" };
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0,  0), new Vector3(0.5f, 0,  0),
                new Vector3(0.5f,  1,  0), new Vector3(-0.5f, 1, 0),
            };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.normals = new[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            quadMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quadMesh.RecalculateBounds();

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = quadMesh;

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
        }

        private void UpdateUVs()
        {
            float u0 = currentFrame * frameU;
            float u1 = u0 + frameU;
            int textureRow = currentRow * yawCount + yawIndex;
            float v0 = textureRow * frameV;
            float v1 = v0 + frameV;

            if (!facingRight)
            {
                float tmp = u0; u0 = u1; u1 = tmp;
            }

            uvBuffer[0] = new Vector2(u0, v0);
            uvBuffer[1] = new Vector2(u1, v0);
            uvBuffer[2] = new Vector2(u1, v1);
            uvBuffer[3] = new Vector2(u0, v1);
            quadMesh.uv = uvBuffer;
        }

        private void OnDestroy()
        {
            if (quadMesh != null) Destroy(quadMesh);
        }
    }
}
