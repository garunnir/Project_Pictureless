// ============================================================
// CraftingWindowLauncher — HUD 제작 창 토글 버튼
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public sealed class CraftingWindowLauncher : MonoBehaviour
{
    [Required, SerializeField] UICraftingController _controller;
    [SerializeField] Button _button;
    [SerializeField] Image _iconImage;
    [SerializeField] Color _closedColor = new(1f, 1f, 1f, 1f);
    [SerializeField] Color _openColor = new(1f, 1f, 1f, 0.55f);

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

    public void Bind(UICraftingController controller) => _controller = controller;

    public void SetOpen(bool open)
    {
        if (_iconImage != null)
            _iconImage.color = open ? _openColor : _closedColor;
    }

    void OnClicked()
    {
        if (_controller != null)
            _controller.Toggle();
    }
}
