// ============================================================
// UIWindowChromeBar — 창 헤더 접기(헤더만) / 끄기(전체 숨김)
// ============================================================

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIWindowChromeBar : MonoBehaviour
{
    [SerializeField] RectTransform _window;
    [SerializeField] Button _foldButton;
    [SerializeField] Button _closeButton;
    [SerializeField] TMP_Text _foldLabel;
    [SerializeField] TMP_Text _foldedTitle;
    [SerializeField] bool _enableFold = true;
    [SerializeField] bool _enableClose = true;

    readonly List<GameObject> _hidden = new(8);
    Action _onClose;
    Vector2 _expandedSize;
    Vector2 _expandedAnchoredPos;
    bool _folded;
    bool _hasExpandedSize;
    bool _proximityWasEnabled;
    bool _bound;

    public bool IsFolded => _folded;

    void Awake()
    {
        EnsureWindow();
        ApplyButtonVisibility();
        BindButtons(true);
        ApplyFoldedTitle();
        ApplyFoldLabel();
    }

    void OnDestroy() => BindButtons(false);

    void OnDisable()
    {
        if (_folded)
            SetFolded(false);
    }

    public void Initialize(RectTransform window)
    {
        if (window != null)
            _window = window;
        EnsureWindow();
    }

    public void BindClose(Action onClose) => _onClose = onClose;

    public static void BindCloseOnWindow(Component host, Action onClose)
    {
        if (host == null)
            return;

        UIWindowChromeBar bar = host.GetComponentInChildren<UIWindowChromeBar>(true);
        if (bar == null)
        {
            Debug.LogError(
                "[UIWindowChromeBar] missing. Run Dist/MCP/WindowChrome/Patch Fold Close Buttons.",
                host);
            return;
        }

        bar.BindClose(onClose);
    }

    public void SetFoldedTitle(string text)
    {
        if (_foldedTitle != null)
            _foldedTitle.text = text;
    }

    public void SetFolded(bool folded)
    {
        EnsureWindow();
        if (_window == null || _folded == folded)
            return;

        if (folded)
            Fold();
        else
            Unfold();
    }

    void BindButtons(bool bind)
    {
        if (bind == _bound)
            return;

        if (_foldButton != null)
        {
            if (bind)
                _foldButton.onClick.AddListener(OnFoldClicked);
            else
                _foldButton.onClick.RemoveListener(OnFoldClicked);
        }

        if (_closeButton != null)
        {
            if (bind)
                _closeButton.onClick.AddListener(OnCloseClicked);
            else
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        _bound = bind;
    }

    void ApplyButtonVisibility()
    {
        if (_foldButton != null)
            _foldButton.gameObject.SetActive(_enableFold);
        if (_closeButton != null)
            _closeButton.gameObject.SetActive(_enableClose);
    }

    void OnFoldClicked() => SetFolded(!_folded);

    void OnCloseClicked() => _onClose?.Invoke();

    void Fold()
    {
        CacheExpandedSize();
        HideFoldTargets();
        SetWindowHeightKeepEdge(ResolveFoldedHeight());
        SetResizeActive(false);
        ForceHeaderVisible();
        _folded = true;
        ApplyFoldedTitle();
        ApplyFoldLabel();
    }

    void Unfold()
    {
        RestoreFoldTargets();
        RestoreExpandedSize();
        SetResizeActive(true);
        RestoreHeaderProximity();
        _folded = false;
        ApplyFoldedTitle();
        ApplyFoldLabel();
    }

    void CacheExpandedSize()
    {
        if (_window == null)
            return;

        _expandedSize = _window.sizeDelta;
        _expandedAnchoredPos = _window.anchoredPosition;
        _hasExpandedSize = true;
    }

    void RestoreExpandedSize()
    {
        if (_window == null || !_hasExpandedSize)
            return;

        _window.sizeDelta = _expandedSize;
        _window.anchoredPosition = _expandedAnchoredPos;
        _hasExpandedSize = false;
    }

    void SetWindowHeightKeepEdge(float newHeight)
    {
        if (_window == null)
            return;

        float oldHeight = _window.rect.height;
        float dy = newHeight - oldHeight;
        Vector2 size = _window.sizeDelta;
        size.y += dy;
        _window.sizeDelta = size;

        float keep = _window.pivot.y >= 0.5f ? 1f : 0f;
        _window.anchoredPosition += new Vector2(0f, -dy * (keep - _window.pivot.y));
    }

    float ResolveFoldedHeight()
    {
        var header = transform as RectTransform;
        if (header == null)
            return UIWindowChromeLayout.FoldedHeaderHeight;

        bool stretchedY = !Mathf.Approximately(header.anchorMin.y, header.anchorMax.y);
        if (stretchedY)
            return UIWindowChromeLayout.FoldedHeaderHeight;

        float height = header.rect.height;
        if (height < 8f)
            return UIWindowChromeLayout.FoldedHeaderHeight;

        return height;
    }

    void HideFoldTargets()
    {
        RestoreFoldTargets();
        Transform header = transform;
        Transform window = _window != null ? _window : header.parent;
        if (window == null)
            return;

        HideUnprotectedChildren(window, header);
        if (header.parent != null && header.parent != window)
            HideUnprotectedChildren(header.parent, header);
    }

    void HideUnprotectedChildren(Transform parent, Transform header)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null || IsProtected(child, header))
                continue;

            GameObject go = child.gameObject;
            if (!go.activeSelf)
                continue;

            go.SetActive(false);
            _hidden.Add(go);
        }
    }

    static bool IsProtected(Transform child, Transform header)
    {
        if (child == header)
            return true;

        string name = child.name;
        if (name.StartsWith("Area_ResizeHandle_", StringComparison.Ordinal))
            return true;
        if (child.GetComponent<UIWindowResizeHandler>() != null)
            return true;

        Transform t = header;
        while (t != null)
        {
            if (t == child)
                return true;
            t = t.parent;
        }

        return false;
    }

    void RestoreFoldTargets()
    {
        for (int i = 0; i < _hidden.Count; i++)
        {
            GameObject go = _hidden[i];
            if (go != null)
                go.SetActive(true);
        }

        _hidden.Clear();
    }

    void SetResizeActive(bool active)
    {
        if (_window == null)
            return;

        UIWindowResizeHandles handles = _window.GetComponent<UIWindowResizeHandles>();
        handles?.SetHandlesActive(active);

        UIWindowResizeProximity proximity = _window.GetComponent<UIWindowResizeProximity>();
        proximity?.SetResizeHandlesActive(active);
    }

    void ForceHeaderVisible()
    {
        UIWindowDragHandler drag = GetComponent<UIWindowDragHandler>();
        if (drag == null)
            return;

        _proximityWasEnabled = drag.IsProximityRevealEnabled;
        drag.SetProximityRevealEnabled(false);
        drag.SetVisualActive(true);
    }

    void RestoreHeaderProximity()
    {
        UIWindowDragHandler drag = GetComponent<UIWindowDragHandler>();
        if (drag == null)
            return;

        drag.SetProximityRevealEnabled(_proximityWasEnabled);
        if (!_proximityWasEnabled)
            drag.SetVisualActive(true);
    }

    void ApplyFoldedTitle()
    {
        if (_foldedTitle != null)
            _foldedTitle.gameObject.SetActive(_folded);
    }

    void ApplyFoldLabel()
    {
        if (_foldLabel == null)
            return;

        _foldLabel.text = _folded
            ? UIWindowChromeLayout.FoldCollapsedLabel
            : UIWindowChromeLayout.FoldExpandedLabel;
    }

    void EnsureWindow()
    {
        if (_window != null)
            return;

        UIOverlayWindow overlay = GetComponentInParent<UIOverlayWindow>();
        if (overlay != null)
        {
            _window = overlay.transform as RectTransform;
            return;
        }

        UIWindowResizeHandles handles = GetComponentInParent<UIWindowResizeHandles>();
        if (handles != null)
            _window = handles.transform as RectTransform;
    }
}
