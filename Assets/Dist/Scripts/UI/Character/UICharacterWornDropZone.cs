// ============================================================
// UICharacterWornDropZone — 착용 목록 영역 인벤 드롭 → Wear
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class UICharacterWornDropZone : MonoBehaviour, IDropHandler
{
    public static UICharacterWornDropZone Ensure(RectTransform wornRoot)
    {
        if (wornRoot == null)
            return null;

        if (!wornRoot.TryGetComponent(out UICharacterWornDropZone zone))
            zone = wornRoot.gameObject.AddComponent<UICharacterWornDropZone>();

        zone.EnsureRaycastTarget();
        return zone;
    }

    void EnsureRaycastTarget()
    {
        if (!TryGetComponent(out Image image))
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        image.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GearInventoryDrop.TryWearFromActiveDrag();
    }
}
