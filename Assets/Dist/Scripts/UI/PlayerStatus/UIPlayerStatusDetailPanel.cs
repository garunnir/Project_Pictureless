// ============================================================
// UIPlayerStatusDetailPanel — 메인 부위 호버 시 세부 anatomy·효과 보조 설명
// ============================================================

using System.Collections.Generic;
using System.Text;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;

public sealed class UIPlayerStatusDetailPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _bodyText;

    readonly StringBuilder _builder = new(256);
    readonly List<BodyPartEffect> _effectBuffer = new(16);

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowForPart(IPlayerBody body, string mainPartId)
    {
        if (body == null || string.IsNullOrEmpty(mainPartId))
        {
            Hide();
            return;
        }

        if (!body.TryGet(mainPartId, out BodyPartNode node))
        {
            if (_bodyText != null)
            {
                _bodyText.text =
                    $"{PlayerStatusLabels.GetPartName(mainPartId)}\n" +
                    PlayerStatusLabels.Lost;
            }
            gameObject.SetActive(true);
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

        gameObject.SetActive(true);
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
    }
}
