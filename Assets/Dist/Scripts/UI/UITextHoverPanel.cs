// ============================================================
// UITextHoverPanel — 공용 텍스트 호버 본문 (셸 + TMP)
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UITextHoverPanel : MonoBehaviour
{
    public static readonly UIHoverStyle DefaultStyle = new(new Vector2(16f, -16f), followMouse: false);

    [SerializeField] UIHoverPanelShell _shell;
    [SerializeField] RectTransform _rect;
    [SerializeField] TMP_Text _bodyText;

    Canvas _rootCanvas;

    public void Wire(TMP_Text bodyText)
    {
        _bodyText = bodyText;
        _shell = GetComponent<UIHoverPanelShell>();
        _rect = transform as RectTransform;
    }

    public void Initialize(Canvas rootCanvas)
    {
        _rootCanvas = rootCanvas;
        EnsureHoverLayout();
        EnsureShell();
        UIHoverCanvasLayer.EnsureParent(transform, rootCanvas);
        if (_shell != null)
            _shell.Initialize(rootCanvas);
        Hide();
    }

    public void Hide()
    {
        if (_shell != null)
            _shell.Hide();
        else
            gameObject.SetActive(false);
    }

    public void ShowNearAnchor(string body, RectTransform anchor, UIHoverStyle style)
    {
        if (string.IsNullOrEmpty(body) || anchor == null)
        {
            Hide();
            return;
        }

        EnsureShell();
        if (_shell == null)
            return;

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        UIHoverCanvasLayer.EnsureParent(transform, _rootCanvas);
        UIHoverCanvasLayer.BringToFront(transform);

        if (_bodyText != null)
            _bodyText.text = body;

        RebuildLayout();
        _shell.ShowNearAnchor(anchor, style);
    }

    void EnsureShell()
    {
        if (_shell != null)
            return;

        _shell = GetComponent<UIHoverPanelShell>();
        if (_shell == null)
        {
            Debug.LogError(
                "[UITextHoverPanel] UIHoverPanelShell missing. Run Dist/MCP/Inventory/Setup Canvas Overlays In Open Scene.",
                this);
        }
    }

    void EnsureHoverLayout()
    {
        if (_rect == null)
            _rect = transform as RectTransform;
        if (_rect == null)
            return;

        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0f, 1f);
    }

    void RebuildLayout()
    {
        if (_rect == null)
            _rect = transform as RectTransform;
        if (_rect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        Canvas.ForceUpdateCanvases();
    }
}
