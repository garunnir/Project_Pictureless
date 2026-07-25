// ============================================================
// BodyEffectTicker — 채널 delta로 효과 지속·출혈 추가 피해
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
public sealed class BodyEffectTicker : MonoBehaviour
{
    const float BleedIntervalSeconds = 1f;
    const int BleedDamagePerTick = 1;

    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    CharacterBodyHost _bodyHost;
    float _bleedTimer;
    readonly System.Collections.Generic.List<BodyPartEffect> _scratch = new(32);

    void Awake() => _bodyHost = GetComponent<CharacterBodyHost>();

    void Update()
    {
        ICharacterBody body = _bodyHost != null ? _bodyHost.Body : null;
        if (body == null)
            return;

        float dt = TimeScaleService.Delta(_timeChannel);
        if (dt <= 0f)
            return;

        body.TickEffectDurations(dt);
        TickBleed(body, dt);
    }

    void TickBleed(ICharacterBody body, float dt)
    {
        _bleedTimer += dt;
        if (_bleedTimer < BleedIntervalSeconds)
            return;
        _bleedTimer = 0f;

        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
        {
            BodyPartNode root = roots[r];
            if (root == null || !root.HasCondition)
                continue;

            _scratch.Clear();
            body.CollectEffectsUnder(root.PartId, _scratch, includeDescendants: true);
            bool hasBleed = false;
            for (int i = 0; i < _scratch.Count; i++)
            {
                if (_scratch[i].EffectId != BodyPartEffectIds.Bleed)
                    continue;
                hasBleed = true;
                break;
            }

            if (!hasBleed)
                continue;

            BodyDamageService.ApplyHit(body, root.PartId, BleedDamagePerTick);
        }
    }
}
