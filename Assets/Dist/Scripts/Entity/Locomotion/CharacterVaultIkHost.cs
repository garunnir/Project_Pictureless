// ============================================================
// CharacterVaultIkHost — Mantle 벽 립 손 IK (Animator와 동일 GO)
// ============================================================

using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CharacterVaultIkHost : MonoBehaviour
{
    Animator _animator;
    CharacterVaultHost _vaultHost;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        ResolveVaultHost();
    }

    void ResolveVaultHost()
    {
        if (_vaultHost != null)
            return;

        Transform body = transform;
        while (body != null)
        {
            if (body.TryGetComponent(out CharacterVaultHost host))
            {
                _vaultHost = host;
                return;
            }

            if (body.TryGetComponent(out CharacterBodyRoot _))
                break;

            body = body.parent;
        }

        _vaultHost = GetComponentInParent<CharacterVaultHost>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null)
            return;

        if (_vaultHost == null)
            ResolveVaultHost();

        if (_vaultHost == null || !_vaultHost.IsMantleIkActive)
            return;

        if (!VaultMantleIkTargets.TryGetHandTargets(
                _vaultHost.ActiveCandidate,
                _vaultHost.VaultCellSize,
                out Vector3 leftHand,
                out Vector3 rightHand,
                out Quaternion handRotation))
            return;

        float weight = VaultConsts.ResolveMantleIkWeight(_vaultHost.Progress01);
        if (weight <= 0f)
            return;

        ApplyHandIk(AvatarIKGoal.LeftHand, leftHand, handRotation, weight);
        ApplyHandIk(AvatarIKGoal.RightHand, rightHand, handRotation, weight);
    }

    void ApplyHandIk(AvatarIKGoal goal, Vector3 position, Quaternion rotation, float weight)
    {
        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight * VaultConsts.MantleIkRotationWeightScale);
        _animator.SetIKPosition(goal, position);
        _animator.SetIKRotation(goal, rotation);
    }
}
