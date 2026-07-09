// ============================================================
// SpriteOutlineFocusVisual — Interactable 포커스 스프라이트 색상 적용
// ============================================================

using UnityEngine;

namespace Interactions
{
    public sealed class SpriteOutlineFocusVisual : MonoBehaviour, IInteractableFocusVisual
    {
        [SerializeField] SpriteRenderer _spriteRenderer;
        [SerializeField] Color _focusedColor = Color.green;
        [SerializeField] Color _unfocusedColor = Color.white;

        void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        public void OnFocusVisual(GameObject interactor)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _focusedColor;
        }

        public void OnUnfocusVisual(GameObject interactor)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _unfocusedColor;
        }
    }
}
