// ============================================================
// InventoryWindowLauncher — 인벤/루팅 창 아이콘 토글 버튼
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryWindowLauncher : MonoBehaviour
{
    public enum LauncherTarget
    {
        Primary,
        Loot
    }

    [SerializeField] LauncherTarget _target = LauncherTarget.Primary;
    [Required, SerializeField] UIInventoryController _controller;
    [SerializeField] Button _button;

    void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
            _button.onClick.AddListener(OnClicked);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClicked);
    }

    void OnValidate()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    public void Bind(UIInventoryController controller) => _controller = controller;

    void OnClicked()
    {
        if (_controller == null)
            return;

        switch (_target)
        {
            case LauncherTarget.Primary:
                _controller.TogglePrimaryWindow();
                break;
            case LauncherTarget.Loot:
                _controller.ToggleLootWindow();
                break;
        }
    }
}
