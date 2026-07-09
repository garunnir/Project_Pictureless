// ============================================================
// ContainerTileViewRegistrar — 컨테이너 TileView를 Applier registry에 등록
// ============================================================

using System;
using IsoTilemap;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(ContainerInteractable))]
public sealed class ContainerTileViewRegistrar : MonoBehaviour
{
    [Required, SerializeField] ContainerInteractable _interactable;
    [SerializeField] TileView _tileView;
    [SerializeField] Guid _presentationTileId;

    void Awake() => EnsureReferences();

    void OnEnable()
    {
        EnsureReferences();
        Register();
    }

    void OnDisable() => Unregister();

    void OnValidate()
    {
        EnsureReferences();
        if (_interactable != null && _presentationTileId == Guid.Empty)
            _presentationTileId = _interactable.PresentationTileId;
    }

    void EnsureReferences()
    {
        if (!_interactable)
            TryGetComponent(out _interactable);

        if (_tileView == null && _interactable != null)
            _tileView = _interactable.TileView;

        if (_tileView == null)
            _tileView = GetComponentInChildren<TileView>(true);
    }

    void Register()
    {
        if (_interactable == null || _tileView == null)
        {
            Debug.LogError(
                $"[ContainerTileViewRegistrar] TileView must be assigned on prefab for '{name}'.",
                this);
            return;
        }

        Guid tileId = ResolvePresentationTileId();
        if (tileId == Guid.Empty)
            return;

        _presentationTileId = tileId;
        ContainerTileViewRegistry.Instance.Register(tileId, _tileView, _interactable.Container.InstanceId);
    }

    void Unregister()
    {
        if (_interactable?.Container == null)
            return;

        Guid tileId = _presentationTileId != Guid.Empty
            ? _presentationTileId
            : _interactable.PresentationTileId;

        ContainerTileViewRegistry.Instance.Unregister(tileId, _interactable.Container.InstanceId);
    }

    Guid ResolvePresentationTileId()
    {
        if (_presentationTileId != Guid.Empty)
            return _presentationTileId;

        return _interactable != null ? _interactable.PresentationTileId : Guid.Empty;
    }
}
