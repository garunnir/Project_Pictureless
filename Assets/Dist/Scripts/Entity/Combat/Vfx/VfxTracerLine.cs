// ============================================================
// VfxTracerLine — 발사 지점과 타격 지점을 잇는 궤적 라인
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class VfxTracerLine : MonoBehaviour
{
    LineRenderer _line;

    void Awake() => EnsureLine();

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        EnsureLine();
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }

    void EnsureLine()
    {
        if (_line == null)
            _line = GetComponent<LineRenderer>();

        _line.useWorldSpace = true;
        _line.positionCount = 2;
    }
}
