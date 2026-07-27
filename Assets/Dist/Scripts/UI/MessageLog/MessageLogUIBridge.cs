// ============================================================
// MessageLogUIBridge — MessageLogViewModel 씬 수명
// ============================================================

using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class MessageLogUIBridge : MonoBehaviour
{
    MessageLogViewModel _viewModel;

    public MessageLogViewModel ViewModel
    {
        get
        {
            EnsureInitialized();
            return _viewModel;
        }
    }

    void Awake() => EnsureInitialized();

    void OnDestroy()
    {
        _viewModel?.Unbind();
        _viewModel = null;
    }

    void EnsureInitialized()
    {
        if (_viewModel != null)
            return;

        _viewModel = new MessageLogViewModel();
        _viewModel.Bind();
    }

    public static bool TryResolve(out MessageLogViewModel viewModel)
    {
        viewModel = null;
        MessageLogUIBridge bridge = FindAnyObjectByType<MessageLogUIBridge>();
        if (bridge == null)
            return false;

        viewModel = bridge.ViewModel;
        return viewModel != null;
    }
}
