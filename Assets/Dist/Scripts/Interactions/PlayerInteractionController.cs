using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Interactions
{
    [RequireComponent(typeof(CharacterState))]
    [RequireComponent(typeof(DirectionalRaycaster))]
    [RequireComponent(typeof(PlayerAimController))]
    public class PlayerInteractionController : MonoBehaviour
    {
        private IInteractable _currentTarget;
        private CharacterState _characterState;
        private DirectionalRaycaster _raycaster;
        private PlayerAimController _aimController;
        private PlayerPossessedInputHost _possessedInput;
        private Collider _lastHitCollider;
        private readonly Dictionary<Collider, IInteractable> _interactableCache = new();

        private void Awake()
        {
            _characterState = GetComponent<CharacterState>();
            _raycaster = GetComponent<DirectionalRaycaster>();
            _aimController = GetComponent<PlayerAimController>();
            TryGetComponent(out _possessedInput);
        }

        private void Start()
        {
            InputManager.Instance.PlayerInteractPerformed += OnInteract;
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.PlayerInteractPerformed -= OnInteract;
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            CharacterVaultHost vault = ResolveVaultHost();
            if (vault != null && vault.TryHandleInteractPress())
                return;

            TryInteractFocused();
        }

        /// <summary>E 짧은 탭(vault 홀드 미달) 시 CharacterVaultHost가 호출.</summary>
        public void TryInteractFocused()
        {
            if (_currentTarget == null) return;

            var interactor = ResolveInteractor();
            if (_currentTarget.CanInteract(interactor))
                _currentTarget.Interact(interactor);
        }

        private void LateUpdate()
        {
            UpdateInteractionTarget();
        }

        private void UpdateInteractionTarget()
        {
            CharacterState focusState = ResolveFocusState();
            if (focusState == null || !focusState.HasInteractionFocus)
            {
                _lastHitCollider = null;
                if (_currentTarget != null) ClearTarget();
                return;
            }

            Transform bodyTf = ResolveInteractor().transform;
            Vector3 origin = bodyTf.position + Vector3.up * _aimController.CastOriginYOffset;
            Vector3 direction = focusState.InteractionDir;
            float maxDistance = focusState.InteractionReach;

            if (!_raycaster.TrySphereCast(
                    origin,
                    direction,
                    _aimController.SphereRadius,
                    maxDistance,
                    out RaycastHit hit))
            {
                _lastHitCollider = null;
                if (_currentTarget != null) ClearTarget();
                return;
            }

            if (hit.collider == _lastHitCollider) return;
            _lastHitCollider = hit.collider;

            if (!_interactableCache.TryGetValue(hit.collider, out var interactable))
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
                _interactableCache[hit.collider] = interactable;
            }

            DebugLogController.LogPlayerInteraction(
                "Interaction SphereCast hit: " + hit.collider.gameObject.name,
                this);

            if (interactable != null)
            {
                if (interactable != _currentTarget) ChangeTarget(interactable);
            }
            else if (_currentTarget != null)
            {
                ClearTarget();
            }
        }

        private void ChangeTarget(IInteractable newTarget)
        {
            GameObject interactor = ResolveInteractor();
            if (_currentTarget != null)
                _currentTarget.OnUnfocus(interactor);

            _currentTarget = newTarget;
            _currentTarget.OnFocus(interactor);

            DebugLogController.LogPlayerInteraction(
                "Focused on: " + (newTarget as MonoBehaviour).gameObject.name,
                this);
        }

        private void ClearTarget()
        {
            _currentTarget.OnUnfocus(ResolveInteractor());
            _currentTarget = null;

            DebugLogController.LogPlayerInteraction("Unfocused", this);
        }

        CharacterVaultHost ResolveVaultHost()
        {
            if (_possessedInput == null)
                TryGetComponent(out _possessedInput);
            GameObject body = _possessedInput != null ? _possessedInput.Body : null;
            if (body == null)
                return null;
            return body.GetBodyComponent<CharacterVaultHost>();
        }

        CharacterState ResolveFocusState()
        {
            if (_possessedInput != null && _possessedInput.BodyState != null)
                return _possessedInput.BodyState;
            return _characterState;
        }

        GameObject ResolveInteractor()
        {
            if (_possessedInput != null && _possessedInput.Body != null)
                return _possessedInput.Body;
            return gameObject;
        }
    }
}
