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
        for (int r = roots.Count - 1; r >= 0; r--)
            TickBleedOrganicSubtree(body, roots[r]);
    }

    void TickBleedOrganicSubtree(ICharacterBody body, BodyPartNode node)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return;

        if (node.HasCondition)
        {
            _scratch.Clear();
            CollectOrganicEffects(node, _scratch);
            bool hasBleed = false;
            for (int i = 0; i < _scratch.Count; i++)
            {
                if (_scratch[i].EffectId != BodyPartEffectIds.Bleed)
                    continue;
                hasBleed = true;
                break;
            }

            if (hasBleed)
                BodyDamageService.ApplyHit(body, node.PartId, BleedDamagePerTick);
        }
    }

    static void CollectOrganicEffects(BodyPartNode node, System.Collections.Generic.List<BodyPartEffect> into)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return;

        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
            into.Add(effects[i]);

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            CollectOrganicEffects(children[i], into);
    }
}
