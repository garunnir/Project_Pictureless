// ============================================================
// IsoVisibleDepthSortDriver — 가시 투명 sortOrder 재빌드 (Play 전용)
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// Play 중 <see cref="IsoVisibleDepthSortRegistry"/> dirty 시 LateUpdate에 sortOrder를 재부여한다.
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class IsoVisibleDepthSortDriver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInstance()
        {
            if (!Application.isPlaying)
                return;

            if (FindAnyObjectByType<IsoVisibleDepthSortDriver>() != null)
                return;

            var go = new GameObject(nameof(IsoVisibleDepthSortDriver))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.AddComponent<IsoVisibleDepthSortDriver>();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            IsoVisibleDepthSortRegistry.RebuildIfDirty();
        }

        void OnDestroy()
        {
            if (Application.isPlaying)
                IsoVisibleDepthSortRegistry.Clear();
        }
    }
}
