// ============================================================
// UICharacterHandActionMenu — 들기 슬롯 RMB 액션/내려놓기 미니 메뉴
// ============================================================

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UICharacterHandActionMenu : MonoBehaviour
{
    static UICharacterHandActionMenu _instance;

    RectTransform _root;
    CharacterGearService _gear;
    string _itemId;
    WieldSlotId _slot;
    Action _onChanged;

    public static void Show(
        CharacterGearService gear,
        string itemId,
        WieldSlotId slot,
        Vector2 screenPosition,
        Canvas canvas,
        Action onChanged)
    {
        if (gear == null || string.IsNullOrEmpty(itemId) || canvas == null)
            return;

        if (_instance == null)
        {
            GameObject go = new("CharacterHandActionMenu");
            _instance = go.AddComponent<UICharacterHandActionMenu>();
            _instance.Build(canvas);
        }

        _instance.Open(gear, itemId, slot, screenPosition, canvas, onChanged);
    }

    public static void HideActive()
    {
        if (_instance != null)
            _instance.Hide();
    }

    void Build(Canvas canvas)
    {
        transform.SetParent(canvas.transform, false);
        _root = gameObject.AddComponent<RectTransform>();
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);
        bg.raycastTarget = true;

        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 2f;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddRow(CharacterGearLabels.ActionBash, () => SetAction(WeaponAction.Bashing));
        AddRow(CharacterGearLabels.ActionCut, () => SetAction(WeaponAction.Cutting));
        AddRow(CharacterGearLabels.ActionGun, () => SetAction(WeaponAction.Gun));
        AddRow(CharacterGearLabels.ActionNone, () => SetAction(null));
        AddRow(CharacterGearLabels.Unwield, () =>
        {
            _gear?.TryBeginUnwieldSlot(_slot, toFloor: false);
            Hide();
            _onChanged?.Invoke();
        });
        AddRow(CharacterGearLabels.DropFloor, () =>
        {
            _gear?.TryBeginUnwieldSlot(_slot, toFloor: true);
            Hide();
            _onChanged?.Invoke();
        });

        gameObject.SetActive(false);
    }

    void AddRow(string label, Action onClick)
    {
        GameObject row = new("Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        row.transform.SetParent(transform, false);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 26f;
        le.preferredHeight = 26f;
        le.minWidth = 120f;
        Image img = row.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Button btn = row.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        GameObject labelGo = new("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGo.transform.SetParent(row.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8f, 0f);
        labelRt.offsetMax = new Vector2(-8f, 0f);
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = GearConstants.UiFontSizeContextRow;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        DistUiFont.Apply(tmp);
    }

    void Open(
        CharacterGearService gear,
        string itemId,
        WieldSlotId slot,
        Vector2 screenPosition,
        Canvas canvas,
        Action onChanged)
    {
        _gear = gear;
        _itemId = itemId;
        _slot = slot;
        _onChanged = onChanged;

        if (transform.parent != canvas.transform)
            transform.SetParent(canvas.transform, false);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        RectTransform canvasRt = canvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 local))
        {
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0f, 1f);
            _root.anchoredPosition = local;
        }
    }

    void SetAction(WeaponAction? action)
    {
        _gear?.TrySetHandAction(_itemId, action);
        Hide();
        _onChanged?.Invoke();
    }

    void Hide()
    {
        gameObject.SetActive(false);
        _gear = null;
        _itemId = null;
        _onChanged = null;
    }

    void Update()
    {
        if (!gameObject.activeSelf)
            return;

        if (InputManager.Instance != null
            && InputManager.Instance.TryReadCancelPerformedThisFrame(out bool canceled)
            && canceled)
            Hide();
    }
}
