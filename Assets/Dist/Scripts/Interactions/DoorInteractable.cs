// ============================================================
// DoorInteractable — 문 열림 상태·Animator 토글 (액션은 Catalog)
// ============================================================

using UnityEngine;

namespace Interactions
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(TileObjectInteractionTarget))]
    public class DoorInteractable : MonoBehaviour
    {
        [SerializeField] bool isOpen;

        Animator _doorAnimator;

        public bool IsOpen => isOpen;

        public string ToggleActionLabel =>
            isOpen ? InteractionLabels.DoorClose : InteractionLabels.DoorOpen;

        void Awake()
        {
            _doorAnimator = GetComponent<Animator>();
            if (_doorAnimator != null)
                _doorAnimator.SetBool("isOpen", isOpen);
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (_doorAnimator != null)
                _doorAnimator.SetBool("isOpen", isOpen);
        }
    }
}
