// ============================================================
// UIGameSaveSlotPopupController — 슬롯 모달 Overlay + ESC 소비 (priority 80)
// ============================================================

using Sirenix.OdinInspector;
using UnityEngine;

public sealed class UIGameSaveSlotPopupController : MonoBehaviour, IUiCancelConsumer
{
    [SerializeField, Required] UIGameSaveSlotPopup _popupPrefab;
    [SerializeField] UIGameSaveSlotPopup _popup;
    [SerializeField] Canvas _uiCanvas;
    [SerializeField] UICanvasLayerHost _layerHost;

    UIGameSaveSlotPopup.Mode _mode;
    bool _isOpen;
    UISettingsController _settingsController;

    public int CancelPriority => UiCancelPriority.ModalPopup;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        EnsureReferences();
        // AddComponent 직후 Configure 전에 Awake가 돌 수 있음 — prefab 없으면 스킵.
        if (_popupPrefab != null)
            EnsurePopup();
        if (_popup != null)
            _popup.gameObject.SetActive(false);
    }

    void OnEnable() => UiCancelRouter.Register(this);

    void OnDisable()
    {
        UiCancelRouter.Unregister(this);
        if (_isOpen)
            CloseInternal();
    }

    void OnDestroy()
    {
        if (_popup != null)
            Destroy(_popup.gameObject);
    }

    public void Configure(
        UISettingsController settingsController,
        Canvas canvas,
        UICanvasLayerHost layerHost,
        UIGameSaveSlotPopup popupPrefab = null)
    {
        _settingsController = settingsController;
        _uiCanvas = canvas;
        _layerHost = layerHost;
        if (popupPrefab != null)
            _popupPrefab = popupPrefab;
        EnsurePopup();
    }

    public bool TryHandleCancel()
    {
        if (!_isOpen)
            return false;

        Close();
        return true;
    }

    public void OpenSave()
    {
        Open(UIGameSaveSlotPopup.Mode.Save);
    }

    public void OpenLoad()
    {
        Open(UIGameSaveSlotPopup.Mode.Load);
    }

    void Open(UIGameSaveSlotPopup.Mode mode)
    {
        EnsurePopup();
        if (_popup == null)
        {
            Debug.LogError("[UIGameSaveSlotPopupController] Popup prefab is not assigned.", this);
            return;
        }

        _mode = mode;
        _popup.Open(mode, GameSaveSlotService.QuerySlotInfos());
        _popup.SlotChosen -= OnSlotChosen;
        _popup.SlotChosen += OnSlotChosen;
        _popup.CloseRequested -= Close;
        _popup.CloseRequested += Close;
        _isOpen = true;
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        CloseInternal();
    }

    void CloseInternal()
    {
        if (_popup != null)
        {
            _popup.SlotChosen -= OnSlotChosen;
            _popup.CloseRequested -= Close;
            _popup.Close();
        }

        _isOpen = false;
    }

    void OnSlotChosen(int slotIndex)
    {
        if (_mode == UIGameSaveSlotPopup.Mode.Save)
        {
            if (!GameSaveSlotService.TrySaveSlot(slotIndex, out string error))
            {
                Debug.LogWarning($"[UIGameSaveSlotPopupController] Save failed: {error}");
                return;
            }

            Close();
            return;
        }

        if (!GameSaveSlotService.TryLoadSlot(slotIndex, out string loadError))
        {
            Debug.LogWarning($"[UIGameSaveSlotPopupController] Load failed: {loadError}");
            return;
        }

        _settingsController?.Close();
        CloseInternal();
    }

    void EnsureReferences()
    {
        if (_uiCanvas == null)
            _uiCanvas = FindAnyObjectByType<Canvas>();
        if (_layerHost == null && _uiCanvas != null)
            _layerHost = _uiCanvas.GetComponent<UICanvasLayerHost>();
    }

    void EnsurePopup()
    {
        EnsureReferences();
        if (_popup != null || _uiCanvas == null)
            return;

        if (_popupPrefab == null)
        {
            // Configure 전 조기 Awake / 미배선 — Open 시점에 다시 시도하며 그때만 에러.
            return;
        }

        Transform overlayRoot = _layerHost != null
            ? _layerHost.GetLayerRoot(UICanvasLayer.Overlay)
            : _uiCanvas.transform;

        _popup = Instantiate(_popupPrefab, overlayRoot);
        _popup.name = "Grp_GameSaveSlotPopup";
        _popup.gameObject.SetActive(false);
    }
}
