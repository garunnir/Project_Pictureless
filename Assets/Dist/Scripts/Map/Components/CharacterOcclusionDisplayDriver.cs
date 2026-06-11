// ============================================================
// CharacterOcclusionDisplayDriver — engaged 타일 occlusion display 중앙 보간
// ============================================================
using IsoTilemap;
using UnityEngine;

/// <summary>
/// entry store의 character occlusion <b>target</b>을 읽어
/// <see cref="TileViewPresentationApplier"/>가 display를 프레임마다 부드럽게 반영합니다.
/// BFS·근접 시선 target 갱신 이후(LateUpdate 50)에 실행됩니다.
/// </summary>
[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public sealed class CharacterOcclusionDisplayDriver : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float _occlusionSmoothSpeed = 6f;

    TileViewPresentationApplier _applier;

    public void Init(TileViewPresentationApplier applier) => _applier = applier;

    public void Shutdown()
    {
        _applier?.ResetCharacterOcclusionDisplay();
        _applier = null;
    }

    void LateUpdate()
    {
        if (_applier == null)
            return;

        _applier.TickCharacterOcclusionDisplay(_occlusionSmoothSpeed, Time.deltaTime);
    }

    void OnDestroy() => Shutdown();
}
