// ============================================================
// UIPlayerStatusDetailPanel — Status 부위 호버 (특이사항만)
// ============================================================

using System.Collections.Generic;
using System.Text;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIPlayerStatusDetailPanel : MonoBehaviour
{
    static readonly UIHoverStyle PartHoverStyle = new(new Vector2(16f, -16f), followMouse: false);

    [SerializeField] UIHoverPanelShell _shell;
    [SerializeField] RectTransform _rect;
    [SerializeField] TMP_Text _bodyText;

    readonly StringBuilder _builder = new(256);
    readonly List<string> _lostBuffer = new(8);

    Canvas _rootCanvas;

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

    public void ShowForPart(ICharacterBody body, string mainPartId, RectTransform anchor)
    {
        if (body == null || string.IsNullOrEmpty(mainPartId) || anchor == null)
        {
            Hide();
            return;
        }

        if (!PlayerStatusBodyPartNoteworthy.HasUnder(body, mainPartId))
        {
            Hide();
            return;
        }

        EnsureShell();
        if (_shell == null)
            return;

        Canvas canvas = _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();
        UITextHoverService.HideOn(canvas);

        PrepareHoverShow();
        if (!BindPart(body, mainPartId))
        {
            Hide();
            return;
        }

        RebuildLayout();
        _shell.ShowNearAnchor(anchor, PartHoverStyle);
    }

    /// <summary>Deprecated for text hover — use UITextHoverService. Kept only if legacy callers remain.</summary>
    public void ShowText(string body, RectTransform anchor)
    {
        Canvas canvas = _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();
        if (!UITextHoverService.TryShowNearAnchor(canvas, body, anchor))
        {
            // Fallback: local shell (body-part panel) so hover is not silent.
            if (string.IsNullOrEmpty(body) || anchor == null)
            {
                Hide();
                return;
            }

            EnsureShell();
            if (_shell == null)
                return;

            PrepareHoverShow();
            if (_bodyText != null)
                _bodyText.text = body;
            RebuildLayout();
            _shell.ShowNearAnchor(anchor, PartHoverStyle);
        }
        else
        {
            Hide();
        }
    }

    void PrepareHoverShow()
    {
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        UIHoverCanvasLayer.EnsureParent(transform, _rootCanvas);
        UIHoverCanvasLayer.BringToFront(transform);
    }

    bool BindPart(ICharacterBody body, string mainPartId)
    {
        _builder.Clear();
        if (!body.TryGet(mainPartId, out BodyPartNode node))
        {
            AppendLost(mainPartId);
            return ApplyBodyText();
        }

        WalkNoteworthy(body, node);
        return ApplyBodyText();
    }

    void WalkNoteworthy(ICharacterBody body, BodyPartNode node)
    {
        if (PlayerStatusBodyPartNoteworthy.IsSelf(node))
            AppendPresent(node);

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            WalkNoteworthy(body, children[i]);

        _lostBuffer.Clear();
        PlayerStatusBodyPartNoteworthy.CollectMissingExpectedChildren(
            body,
            node.PartId,
            _lostBuffer);
        for (int i = 0; i < _lostBuffer.Count; i++)
            AppendLost(_lostBuffer[i]);
    }

    void AppendPresent(BodyPartNode node)
    {
        if (_builder.Length > 0)
            _builder.Append('\n');

        _builder.Append(PlayerStatusLabels.GetPartName(node.PartId));
        if (node.HasCondition && node.ConditionCur < node.ConditionMax)
        {
            _builder.Append("  ");
            _builder.Append(
                PlayerStatusLabels.FormatCondition(
                    node.ConditionCur,
                    node.ConditionMax));
        }

        if (node.Kind == BodyPartKind.Prosthetic)
        {
            _builder.Append('\n');
            _builder.Append(PlayerStatusLabels.Prosthetic);
        }

        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            BodyPartEffect effect = effects[i];
            _builder.Append('\n');
            _builder.Append("- ");
            _builder.Append(PlayerStatusLabels.GetEffectName(effect.EffectId));
            if (effect.EffectId == BodyPartEffectIds.Bandaged)
            {
                float dirty01 = BodyHealApply.BandageDirty01(node);
                if (dirty01 > 0f)
                {
                    _builder.Append(' ');
                    _builder.Append(PlayerStatusLabels.FormatBandageDirty(dirty01));
                }
            }

            if (effect.Intensity > 1)
            {
                _builder.Append(" x");
                _builder.Append(effect.Intensity);
            }
        }
    }

    void AppendLost(string partId)
    {
        if (_builder.Length > 0)
            _builder.Append('\n');

        _builder.Append(PlayerStatusLabels.GetPartName(partId));
        _builder.Append('\n');
        _builder.Append(PlayerStatusLabels.Lost);
    }

    bool ApplyBodyText()
    {
        if (_builder.Length == 0)
            return false;

        if (_bodyText != null)
            _bodyText.text = _builder.ToString();
        return true;
    }

    public void Wire(TMP_Text bodyText)
    {
        _bodyText = bodyText;
        _shell = GetComponent<UIHoverPanelShell>();
    }

    void EnsureShell()
    {
        if (_shell != null)
            return;

        _shell = GetComponent<UIHoverPanelShell>();
        if (_shell == null)
        {
            Debug.LogError(
                "[UIPlayerStatusDetailPanel] UIHoverPanelShell missing. Bake onto Grp_PlayerStatusWindow DetailPanel.",
                this);
        }
    }

    void EnsureHoverLayout()
    {
        if (_rect == null)
            _rect = transform as RectTransform;
        if (_rect == null)
            return;

        // Positioner sets anchoredPosition in parent local space — center anchors required.
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
