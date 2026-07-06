// ============================================================
// UIItemListRow — 아이템 리스트 한 행 바인딩
// ============================================================

using Garunnir.Runtime.Gameplay.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIItemListRow : MonoBehaviour
{
    [SerializeField] TMP_Text _categoryText;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _detailText;
    [SerializeField] Image _iconImage;
    [SerializeField] Sprite _emptyIconSprite;

    public void Bind(ItemStack stack)
    {
        if (stack?.Item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ItemDefinitionSO item = stack.Item;

        if (_categoryText != null)
            _categoryText.text = item.Category.ToString();
        if (_nameText != null)
            _nameText.text = UITextPresenter.GetItemName(item);
        if (_detailText != null)
            _detailText.text = $"x{stack.Count}  {stack.TotalWeight:0.##}kg  {stack.TotalVolume:0.##}L";

        if (_iconImage != null)
        {
            Sprite icon = item.Icon != null ? item.Icon : _emptyIconSprite;
            _iconImage.enabled = icon != null;
            _iconImage.sprite = icon;
        }
    }
}
