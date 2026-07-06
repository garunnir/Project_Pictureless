// ============================================================
// UIContainerSlot — 사이드바 컨테이너 슬롯 버튼
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIContainerSlot : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] TMP_Text _label;
    [SerializeField] Image _iconImage;
    [SerializeField] Image _highlight;

    InventoryContainer _container;
    Action<InventoryContainer> _onSelected;

    void Awake()
    {
        if (_button != null)
            _button.onClick.AddListener(OnClick);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    public void Bind(
        InventoryContainer container,
        bool selected,
        Action<InventoryContainer> onSelected)
    {
        _container = container;
        _onSelected = onSelected;

        if (container?.Definition == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ContainerDefinitionSO def = container.Definition;

        if (_label != null)
            _label.text = UITextPresenter.GetContainerName(def);

        if (_iconImage != null)
        {
            _iconImage.sprite = def.Icon;
            _iconImage.enabled = def.Icon != null;
        }

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (_highlight == null)
            return;

        _highlight.gameObject.SetActive(selected);
        _highlight.enabled = selected;
    }

    void OnClick() => _onSelected?.Invoke(_container);
}
