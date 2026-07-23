// ============================================================
// TimeUIBridge — TimeViewModel 씬 수명주기 + WorldClock 바인드
// ============================================================

using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class TimeUIBridge : MonoBehaviour
{
    TimeViewModel _viewModel;

    public TimeViewModel ViewModel
    {
        get
        {
            EnsureInitialized();
            return _viewModel;
        }
    }

    void Awake() => EnsureInitialized();

    void Start()
    {
        EnsureInitialized();
        RebindClockIfNeeded();
    }

    void OnDestroy()
    {
        _viewModel?.Unbind();
        _viewModel = null;
    }

    void EnsureInitialized()
    {
        if (_viewModel != null)
            return;

        _viewModel = new TimeViewModel();
        _viewModel.Bind(ResolveClock());
    }

    void RebindClockIfNeeded()
    {
        if (_viewModel == null)
            return;

        WorldClock clock = ResolveClock();
        if (clock == null)
            return;

        // Awake 시 Instance가 아직 없으면 Start에서 다시 바인드.
        _viewModel.Bind(clock);
    }

    static WorldClock ResolveClock()
    {
        WorldClock clock = WorldClock.Instance;
        if (clock == null)
            clock = FindAnyObjectByType<WorldClock>();
        return clock;
    }

    public static bool TryResolve(out TimeViewModel viewModel)
    {
        viewModel = null;
        TimeUIBridge bridge = FindAnyObjectByType<TimeUIBridge>();
        if (bridge == null)
            return false;

        viewModel = bridge.ViewModel;
        return viewModel != null;
    }
}
