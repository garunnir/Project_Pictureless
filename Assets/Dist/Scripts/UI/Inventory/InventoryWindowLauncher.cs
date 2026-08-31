// ============================================================
// InventoryWindowLauncher — 인벤/루팅 창 아이콘 토글 + open 시각
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
    [SerializeField] Image _iconImage;
    [SerializeField] Color _closedColor = new(1f, 1f, 1f, 1f);
    [SerializeField] Color _openColor = new(1f, 1f, 1f, 0.55f);

    bool _isOpen;

    void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();

        if (_iconImage == null && _button != null)
            _iconImage = _button.targetGraphic as Image;

        if (_button != null)
            _button.onClick.AddListener(OnClicked);

        SetOpen(false);
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

        if (_iconImage == null && _button != null)
            _iconImage = _button.targetGraphic as Image;
    }

    public void Bind(UIInventoryController controller) => _controller = controller;

    public void SetOpen(bool open)
    {
        _isOpen = open;
        if (_iconImage != null)
            _iconImage.color = open ? _openColor : _closedColor;
    }

    public bool IsOpen => _isOpen;

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
