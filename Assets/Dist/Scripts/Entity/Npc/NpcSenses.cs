// ============================================================
// NpcSenses — 주변 CharacterBodyHost 탐지 (플레이어 Body 우선)
// ============================================================

using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NpcSenses : MonoBehaviour
{
    [SerializeField, Min(0f)] float _detectRadius = 10f;
    [SerializeField, Min(0f)] float _loseRadius = 14f;
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    CharacterBodyHost _target;
    float _distanceToTarget = float.MaxValue;

    public CharacterBodyHost Target => _target;
    public bool HasTarget => _target != null;
    public float DistanceToTarget => _distanceToTarget;
    public float DetectRadius => _detectRadius;
    public float LoseRadius => _loseRadius;

    void Update()
    {
        if (TimeScaleService.Delta(_timeChannel) <= 0f)
            return;

        RefreshTarget();
    }

    void RefreshTarget()
    {
        if (_target != null)
        {
            if (!IsUsableTarget(_target))
            {
                ClearTarget();
            }
            else
            {
                _distanceToTarget = HorizontalDistance(_target.transform.position);
                if (_distanceToTarget > _loseRadius)
                    ClearTarget();
                return;
            }
        }

        CharacterBodyHost best = null;
        float bestDist = float.MaxValue;
        CharacterBodyHost[] hosts = FindObjectsByType<CharacterBodyHost>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < hosts.Length; i++)
        {
            CharacterBodyHost host = hosts[i];
            if (!IsUsableTarget(host) || host.transform == transform)
                continue;
            if (!IsPreferredHostile(host))
                continue;

            float dist = HorizontalDistance(host.transform.position);
            if (dist > _detectRadius || dist >= bestDist)
                continue;

            best = host;
            bestDist = dist;
        }

        _target = best;
        _distanceToTarget = best != null ? bestDist : float.MaxValue;
    }

    public void ClearTarget()
    {
        _target = null;
        _distanceToTarget = float.MaxValue;
    }

    bool IsPreferredHostile(CharacterBodyHost host)
    {
        ICharacterBody body = host.Body;
        return body != null && ReferenceEquals(body, GameplayData.Body);
    }

    static bool IsUsableTarget(CharacterBodyHost host)
    {
        if (host == null || !host.isActiveAndEnabled)
            return false;
        ICharacterBody body = host.Body;
        return body != null && !body.IsDeadState;
    }

    float HorizontalDistance(Vector3 world)
    {
        Vector3 offset = world - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }
}
