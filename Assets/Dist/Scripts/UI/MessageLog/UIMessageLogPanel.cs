// ============================================================
// UIMessageLogPanel — 메시지 로그 ScrollRect + 카테고리/중요도 색
// ============================================================

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIMessageLogPanel : MonoBehaviour
{
    [SerializeField] ScrollRect _scrollRect;
    [SerializeField] TMP_Text _logText;
    [SerializeField] TMP_Text _headerTitle;
    [SerializeField] Color _combatColor = new Color(0.95f, 0.75f, 0.55f, 1f);
    [SerializeField] Color _statusColor = new Color(0.85f, 0.9f, 1f, 1f);
    [SerializeField] Color _systemColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] Color _criticalColor = new Color(1f, 0.45f, 0.4f, 1f);

    readonly StringBuilder _builder = new StringBuilder(2048);
    MessageLogViewModel _viewModel;
    bool _stickToBottom = true;

    public void Wire(ScrollRect scrollRect, TMP_Text logText, TMP_Text headerTitle = null)
    {
        _scrollRect = scrollRect;
        _logText = logText;
        _headerTitle = headerTitle;
    }

    public void BindViewModel(MessageLogViewModel viewModel) => _viewModel = viewModel;

    public void Hide() => gameObject.SetActive(false);

    public void RefreshHeaderTitle()
    {
        if (_headerTitle == null)
            return;

        DistUiFont.Apply(_headerTitle);
        _headerTitle.text = MessageLogLabels.Title;
    }

    public void Refresh()
    {
        if (_logText == null)
            return;

        _builder.Clear();
        IReadOnlyList<MessageLogEntry> entries = _viewModel != null
            ? _viewModel.Entries
            : null;

        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    _builder.Append('\n');

                MessageLogEntry entry = entries[i];
                Color color = ResolveColor(entry);
                _builder.Append("<color=#");
                _builder.Append(ColorUtility.ToHtmlStringRGBA(color));
                _builder.Append('>');
                _builder.Append(entry.Text);
                _builder.Append("</color>");
            }
        }

        if (_scrollRect != null)
            _stickToBottom = _scrollRect.verticalNormalizedPosition <= 0.01f
                || !_scrollRect.gameObject.activeInHierarchy;

        _logText.text = _builder.ToString();
        _logText.ForceMeshUpdate();

        if (_stickToBottom)
            Canvas.ForceUpdateCanvases();
        if (_stickToBottom && _scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 0f;
    }

    Color ResolveColor(MessageLogEntry entry)
    {
        if (entry.Importance == MessageLogImportance.Critical)
            return _criticalColor;

        switch (entry.Category)
        {
            case MessageLogCategory.Combat:
                return _combatColor;
            case MessageLogCategory.Status:
                return _statusColor;
            default:
                return _systemColor;
        }
    }
}
