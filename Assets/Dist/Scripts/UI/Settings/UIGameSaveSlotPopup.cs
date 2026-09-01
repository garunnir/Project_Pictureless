// ============================================================
// UIGameSaveSlotPopup — 10슬롯 저장/불러오기 모달 View
// ============================================================

using System;
using IsoTilemap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIGameSaveSlotPopup : MonoBehaviour
{
    public enum Mode
    {
        Save,
        Load
    }

    [SerializeField] RectTransform _root;
    [SerializeField] TMP_Text _title;
    [SerializeField] Button _closeButton;
    [SerializeField] Button[] _slotButtons;
    [SerializeField] TMP_Text[] _slotTitles;
    [SerializeField] TMP_Text[] _slotSubtitles;
    [SerializeField] GameObject _confirmPanel;
    [SerializeField] TMP_Text _confirmMessage;
    [SerializeField] Button _confirmYesButton;
    [SerializeField] Button _confirmNoButton;

    Mode _mode;
    int _pendingSlotIndex = -1;
    bool _bound;

    public event Action<int> SlotChosen;
    public event Action CloseRequested;

    public void Wire(
        RectTransform root,
        TMP_Text title,
        Button closeButton,
        Button[] slotButtons,
        TMP_Text[] slotTitles,
        TMP_Text[] slotSubtitles,
        GameObject confirmPanel,
        TMP_Text confirmMessage,
        Button confirmYesButton,
        Button confirmNoButton)
    {
        _root = root;
        _title = title;
        _closeButton = closeButton;
        _slotButtons = slotButtons;
        _slotTitles = slotTitles;
        _slotSubtitles = slotSubtitles;
        _confirmPanel = confirmPanel;
        _confirmMessage = confirmMessage;
        _confirmYesButton = confirmYesButton;
        _confirmNoButton = confirmNoButton;
    }

    void Awake()
    {
        BindControls(true);
        HideConfirm();
    }

    void OnDestroy() => BindControls(false);

    public void Open(Mode mode, GameSaveSlotInfo[] slots)
    {
        _mode = mode;
        _pendingSlotIndex = -1;
        HideConfirm();
        RefreshLabels();
        ApplySlotStates(slots);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        HideConfirm();
        gameObject.SetActive(false);
    }

    public void RefreshLabels()
    {
        if (_title != null)
        {
            DistUiFont.Apply(_title);
            _title.text = _mode == Mode.Save
                ? GameSaveLabels.PopupSaveTitle
                : GameSaveLabels.PopupLoadTitle;
        }

        SetButtonLabel(_closeButton, GameSaveLabels.Close);
        SetButtonLabel(_confirmYesButton, GameSaveLabels.ConfirmYes);
        SetButtonLabel(_confirmNoButton, GameSaveLabels.ConfirmNo);
    }

    void ApplySlotStates(GameSaveSlotInfo[] slots)
    {
        if (_slotButtons == null)
            return;

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            Button button = _slotButtons[i];
            if (button == null)
                continue;

            GameSaveSlotInfo info = slots != null && i < slots.Length
                ? slots[i]
                : default;

            bool occupied = info.HasData;
            bool interactable = _mode == Mode.Save || occupied;
            button.interactable = interactable;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = occupied || _mode == Mode.Save
                    ? GameSaveSlotPopupLayout.SlotColor
                    : GameSaveSlotPopupLayout.SlotEmptyColor;
            }

            if (_slotTitles != null && i < _slotTitles.Length && _slotTitles[i] != null)
            {
                DistUiFont.Apply(_slotTitles[i]);
                _slotTitles[i].text = GameSaveLabels.FormatSlotTitle(i + 1);
            }

            if (_slotSubtitles != null && i < _slotSubtitles.Length && _slotSubtitles[i] != null)
            {
                DistUiFont.Apply(_slotSubtitles[i]);
                _slotSubtitles[i].text = GameSaveLabels.FormatSlotSubtitle(info);
            }
        }
    }

    void BindControls(bool bind)
    {
        if (bind == _bound)
            return;

        if (_closeButton != null)
        {
            if (bind)
                _closeButton.onClick.AddListener(OnCloseClicked);
            else
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        if (_confirmYesButton != null)
        {
            if (bind)
                _confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
            else
                _confirmYesButton.onClick.RemoveListener(OnConfirmYesClicked);
        }

        if (_confirmNoButton != null)
        {
            if (bind)
                _confirmNoButton.onClick.AddListener(HideConfirm);
            else
                _confirmNoButton.onClick.RemoveListener(HideConfirm);
        }

        if (_slotButtons != null)
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                Button button = _slotButtons[i];
                if (button == null)
                    continue;

                int slotIndex = i;
                if (bind)
                    button.onClick.AddListener(() => OnSlotClicked(slotIndex));
                else
                    button.onClick.RemoveAllListeners();
            }
        }

        _bound = bind;
    }

    void OnSlotClicked(int slotIndex)
    {
        GameSaveSlotInfo info = GameSaveSlotService.QuerySlotInfo(slotIndex);

        if (_mode == Mode.Load)
        {
            if (!info.HasData)
                return;

            ShowConfirm(slotIndex, GameSaveLabels.ConfirmLoad);
            return;
        }

        if (info.HasData)
        {
            ShowConfirm(slotIndex, GameSaveLabels.ConfirmOverwrite);
            return;
        }

        SlotChosen?.Invoke(slotIndex);
    }

    void ShowConfirm(int slotIndex, string message)
    {
        _pendingSlotIndex = slotIndex;
        if (_confirmPanel != null)
            _confirmPanel.SetActive(true);

        if (_confirmMessage != null)
        {
            DistUiFont.Apply(_confirmMessage);
            _confirmMessage.text = message;
        }
    }

    void HideConfirm()
    {
        _pendingSlotIndex = -1;
        if (_confirmPanel != null)
            _confirmPanel.SetActive(false);
    }

    void OnConfirmYesClicked()
    {
        if (_pendingSlotIndex < 0)
            return;

        int slotIndex = _pendingSlotIndex;
        HideConfirm();
        SlotChosen?.Invoke(slotIndex);
    }

    void OnCloseClicked() => CloseRequested?.Invoke();

    static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        DistUiFont.Apply(label);
        label.text = text;
    }
}
