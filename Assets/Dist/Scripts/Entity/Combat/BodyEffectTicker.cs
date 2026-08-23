// ============================================================
// BodyEffectTicker — 출혈→혈액·맵 drip, 감염 onset/레이스, 독소 감쇠
// ============================================================

using System.Collections.Generic;
using Garunnir.Runtime.Gameplay.Data;
using IsoTilemap;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterBodyHost))]
public sealed class BodyEffectTicker : MonoBehaviour
{
    [SerializeField] TimeScaleChannel _timeChannel = TimeScaleChannel.World;

    CharacterBodyHost _bodyHost;
    readonly List<BodyPartEffect> _scratch = new(32);
    readonly Dictionary<string, float> _bleedAgeByPart = new();
    float _dripAccum;

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
        TickBleedBlood(body, dt);
        TickInfectionOnset(body, dt);
        TickInfectionRace(body, dt);
        TickToxinClear(body, dt);
    }

    void TickBleedBlood(ICharacterBody body, float dt)
    {
        int intensitySum = 0;
        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
            intensitySum += SumBleedIntensityOrganic(roots[r]);

        if (intensitySum <= 0)
        {
            _dripAccum = 0f;
            return;
        }

        float drain = intensitySum * BodyIllness.BleedBloodPerIntensityPerSecond * dt;
        if (drain <= 0f)
            return;

        body.SetBlood01(body.Blood01 - drain);
        TryDripBlood(drain);
    }

    void TryDripBlood(float drain)
    {
        _dripAccum += drain;
        if (_dripAccum < MapBloodConsts.DripDrainThreshold)
            return;

        _dripAccum = 0f;
        MapBloodHost host = MapBloodHost.Runtime;
        if (host == null)
            return;

        Vector3 feet = transform.position;
        feet.x += Random.Range(-MapBloodConsts.DripJitterWorld, MapBloodConsts.DripJitterWorld);
        feet.z += Random.Range(-MapBloodConsts.DripJitterWorld, MapBloodConsts.DripJitterWorld);
        host.AddStamp(
            feet,
            Random.Range(0f, 360f),
            MapBloodConsts.DripScale,
            MapBloodConsts.DripAlpha);
    }

    void TickInfectionOnset(ICharacterBody body, float dt)
    {
        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
            TickInfectionOnsetNode(body, roots[r], dt);
    }

    void TickInfectionOnsetNode(ICharacterBody body, BodyPartNode node, float dt)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return;

        if (node.HasCondition)
        {
            int bleed = BleedIntensityOn(node);
            bool hasInfected = HasEffect(node, BodyPartEffectIds.Infected);
            if (bleed > 0 && !hasInfected)
            {
                _bleedAgeByPart.TryGetValue(node.PartId, out float age);
                age += dt;
                _bleedAgeByPart[node.PartId] = age;
                if (age >= BodyIllness.InfectedOnsetSeconds)
                    body.AddEffect(
                        node.PartId,
                        new BodyPartEffect(BodyPartEffectIds.Infected, 1, -1f));
            }
            else if (bleed <= 0)
                _bleedAgeByPart.Remove(node.PartId);
        }

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            TickInfectionOnsetNode(body, children[i], dt);
    }

    void TickInfectionRace(ICharacterBody body, float dt)
    {
        if (!HasAnyInfected(body))
        {
            if (body.InfectionProgress01 > 0f || body.InfectionImmunity01 > 0f)
            {
                body.SetInfectionProgress01(0f);
                body.SetInfectionImmunity01(0f);
            }

            return;
        }

        float progress = body.InfectionProgress01
                         + BodyIllness.InfectedProgressPerSecond * dt;
        float filtration = BodyCapacity.BloodFiltration(body);
        float immunity = body.InfectionImmunity01
                         + BodyIllness.ImmunityPerSecond * filtration * dt;

        if (immunity >= 1f)
        {
            ClearAllInfected(body);
            body.SetInfectionProgress01(0f);
            body.SetInfectionImmunity01(0f);
            return;
        }

        body.SetInfectionProgress01(progress);
        body.SetInfectionImmunity01(immunity);
    }

    void TickToxinClear(ICharacterBody body, float dt)
    {
        if (body.Toxin01 <= 0f)
            return;

        float filtration = BodyCapacity.BloodFiltration(body);
        float clear = BodyIllness.ToxinClearPerSecond * filtration * dt;
        if (clear <= 0f)
            return;

        body.SetToxin01(body.Toxin01 - clear);
    }

    int SumBleedIntensityOrganic(BodyPartNode node)
    {
        if (node == null || node.Kind == BodyPartKind.Prosthetic)
            return 0;

        int sum = BleedIntensityOn(node);
        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            sum += SumBleedIntensityOrganic(children[i]);
        return sum;
    }

    static int BleedIntensityOn(BodyPartNode node)
    {
        int sum = 0;
        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].EffectId != BodyPartEffectIds.Bleed)
                continue;
            int intensity = effects[i].Intensity;
            sum += intensity < 1 ? 1 : intensity;
        }

        return sum;
    }

    static bool HasEffect(BodyPartNode node, string effectId)
    {
        IReadOnlyList<BodyPartEffect> effects = node.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i].EffectId == effectId)
                return true;
        }

        return false;
    }

    bool HasAnyInfected(ICharacterBody body)
    {
        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
        {
            if (HasInfectedSubtree(roots[r]))
                return true;
        }

        return false;
    }

    static bool HasInfectedSubtree(BodyPartNode node)
    {
        if (node == null)
            return false;
        if (HasEffect(node, BodyPartEffectIds.Infected))
            return true;
        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (HasInfectedSubtree(children[i]))
                return true;
        }

        return false;
    }

    void ClearAllInfected(ICharacterBody body)
    {
        IReadOnlyList<BodyPartNode> roots = body.Roots;
        for (int r = 0; r < roots.Count; r++)
            ClearInfectedSubtree(body, roots[r]);
    }

    static void ClearInfectedSubtree(ICharacterBody body, BodyPartNode node)
    {
        if (node == null)
            return;

        if (HasEffect(node, BodyPartEffectIds.Infected))
        {
            _scratchForClear.Clear();
            IReadOnlyList<BodyPartEffect> effects = node.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].EffectId == BodyPartEffectIds.Infected)
                    continue;
                _scratchForClear.Add(effects[i]);
            }

            body.ClearEffectsOn(node.PartId);
            for (int i = 0; i < _scratchForClear.Count; i++)
                body.AddEffect(node.PartId, _scratchForClear[i]);
        }

        IReadOnlyList<BodyPartNode> children = node.Children;
        for (int i = 0; i < children.Count; i++)
            ClearInfectedSubtree(body, children[i]);
    }

    static readonly List<BodyPartEffect> _scratchForClear = new(16);
}
