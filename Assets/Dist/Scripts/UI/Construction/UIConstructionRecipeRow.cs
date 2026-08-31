// ============================================================
// UIConstructionRecipeRow — 본편 건설 레시피 목록 행
// ============================================================

using System;
using Garunnir.Runtime.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIConstructionRecipeRow : MonoBehaviour
{
    [SerializeField] Button _button;
    [SerializeField] TMP_Text _label;
    [SerializeField] Image _background;

    ConstructionData _data;
    Action<ConstructionData> _onSelect;
    Color _normal = Color.white;
    Color _selected = new Color(1f, 0.92f, 0.4f, 1f);

    public ConstructionData Data => _data;

    public void EnsureRuntimeChrome()
    {
        if (_button == null)
            _button = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();

        if (_background == null)
        {
            _background = gameObject.GetComponent<Image>();
            if (_background == null)
                _background = gameObject.AddComponent<Image>();
            _background.color = _normal;
        }

        if (_label == null)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(transform, false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 16f;
            DistUiFont.Apply(_label);
        }

        var selfRt = GetComponent<RectTransform>();
        selfRt.sizeDelta = new Vector2(0f, 28f);
    }

    public void Bind(ConstructionData data, Action<ConstructionData> onSelect)
    {
        _data = data;
        _onSelect = onSelect;

        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelect?.Invoke(_data));
        }

        if (_label != null)
        {
            DistUiFont.Apply(_label);
            _label.text = string.IsNullOrEmpty(data.display_name) ? data.id : data.display_name;
        }
    }

    public void SetSelected(bool selected)
    {
        if (_background != null)
            _background.color = selected ? _selected : _normal;
    }
}
