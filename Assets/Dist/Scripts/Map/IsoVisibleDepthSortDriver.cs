// ============================================================
// IsoVisibleDepthSortDriver — 가시 투명 sortOrder 재빌드 (Play 전용)
// ============================================================

using UnityEngine;

namespace IsoTilemap
{
    /// <summary>
    /// Play 중 <see cref="IsoVisibleDepthSortRegistry"/> dirty 시 LateUpdate에 sortOrder를 재부여한다.
    /// Game 카메라는 <see cref="TransparencySortMode.Default"/>를 유지한다 (CustomAxis와 이중 정렬 방지).
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

            ResetMainCameraTransparencySort();
            EnsureDriver();
        }

        static void EnsureDriver()
        {
            if (FindAnyObjectByType<IsoVisibleDepthSortDriver>() != null)
                return;

            var go = new GameObject(nameof(IsoVisibleDepthSortDriver))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.AddComponent<IsoVisibleDepthSortDriver>();
        }

        public static void ResetMainCameraTransparencySort()
        {
            Camera main = Camera.main;
            if (main == null)
                return;

            main.transparencySortMode = TransparencySortMode.Default;
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
