// ============================================================
// UIPlayerStatusDetailPanel ? ?? ?? ?? anatomy??? ?? ??
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
    readonly List<BodyPartEffect> _effectBuffer = new(16);

    public void Initialize(Canvas rootCanvas)
    {
        EnsureHoverLayout();
        EnsureShell();
        _shell.Initialize(rootCanvas);
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

        EnsureShell();
        BindPart(body, mainPartId);
        RebuildLayout();
        _shell.ShowNearAnchor(anchor, PartHoverStyle);
    }

    /// <summary>Gear 슬롯/행 호버 — 인벤식 DetailPanel 셸에 임의 본문.</summary>
    public void ShowText(string body, RectTransform anchor)
    {
        if (string.IsNullOrEmpty(body) || anchor == null)
        {
            Hide();
            return;
        }

        EnsureShell();
        if (_bodyText != null)
            _bodyText.text = body;
        RebuildLayout();
        _shell.ShowNearAnchor(anchor, PartHoverStyle);
    }

    void BindPart(ICharacterBody body, string mainPartId)
    {
        if (!body.TryGet(mainPartId, out BodyPartNode node))
        {
            if (_bodyText != null)
            {
                _bodyText.text =
                    $"{PlayerStatusLabels.GetPartName(mainPartId)}\n" +
                    PlayerStatusLabels.Lost;
            }

            return;
        }

        _builder.Clear();
        _builder.Append(PlayerStatusLabels.DetailHeader);
        _builder.Append('\n');
        _builder.Append(PlayerStatusLabels.GetPartName(node.PartId));
        if (node.HasCondition)
        {
            _builder.Append("  ");
            _builder.Append(
                PlayerStatusLabels.FormatCondition(
                    node.ConditionCur,
                    node.ConditionMax));
        }

        _builder.Append("\n\n");
        _builder.Append(PlayerStatusLabels.DetailSubparts);
        _builder.Append('\n');
        AppendSubtree(node, 0);

        _builder.Append('\n');
        _builder.Append(PlayerStatusLabels.DetailEffects);
        _builder.Append('\n');
        _effectBuffer.Clear();
        body.CollectEffectsUnder(mainPartId, _effectBuffer, includeDescendants: true);
        if (_effectBuffer.Count == 0)
        {
            _builder.Append(PlayerStatusLabels.NoEffects);
        }
        else
        {
            for (int i = 0; i < _effectBuffer.Count; i++)
            {
                BodyPartEffect effect = _effectBuffer[i];
                _builder.Append("- ");
                _builder.Append(PlayerStatusLabels.GetEffectName(effect.EffectId));
                if (effect.Intensity > 1)
                {
                    _builder.Append(" x");
                    _builder.Append(effect.Intensity);
                }

                _builder.Append('\n');
            }
        }

        if (_bodyText != null)
            _bodyText.text = _builder.ToString();
    }

    void AppendSubtree(BodyPartNode node, int depth)
    {
        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
        {
            BodyPartNode child = children[i];
            for (int d = 0; d < depth; d++)
                _builder.Append("  ");
            _builder.Append("- ");
            _builder.Append(PlayerStatusLabels.GetPartName(child.PartId));

            IReadOnlyList<BodyPartEffect> effects = child.Effects;
            if (effects.Count > 0)
            {
                _builder.Append(" (");
                for (int e = 0; e < effects.Count; e++)
                {
                    if (e > 0)
                        _builder.Append(", ");
                    _builder.Append(PlayerStatusLabels.GetEffectName(effects[e].EffectId));
                }

                _builder.Append(')');
            }

            _builder.Append('\n');
            AppendSubtree(child, depth + 1);
        }
    }

    public void Wire(TMP_Text bodyText)
    {
        _bodyText = bodyText;
        _shell = null;
    }

    void EnsureShell()
    {
        if (_shell != null)
            return;

        _shell = GetComponent<UIHoverPanelShell>();
        if (_shell == null)
            _shell = gameObject.AddComponent<UIHoverPanelShell>();
    }

    void EnsureHoverLayout()
    {
        if (_rect == null)
            _rect = transform as RectTransform;
        if (_rect == null)
            return;

        // Positioner sets anchoredPosition in parent local space ? center anchors required.
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
