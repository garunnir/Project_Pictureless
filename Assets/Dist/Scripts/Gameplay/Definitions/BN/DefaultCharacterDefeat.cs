// ============================================================
// DefaultCharacterDefeat ? Body?Skills OR ? IsDefeated ??
// ============================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// ????? ?? ????? ????IsDefeated? ??? <see cref="Revive"/> ???? ????(???).
    /// ??????Body.Changed / Skills.Refreshed?Collapsed ????????? (hot-path ??? ???).
    /// </summary>
    public sealed class DefaultCharacterDefeat : ICharacterDefeat, IDisposable
    {
        readonly ICharacterBody _body;
        readonly ICharacterSkills _skills;

        public event Action Changed;

        public bool IsDefeated { get; private set; }
        public DefeatCause Cause { get; private set; }

        public DefaultCharacterDefeat(ICharacterBody body, ICharacterSkills skills)
        {
            _body = body;
            _skills = skills;

            if (_body != null)
                _body.Changed += OnSourceChanged;
            if (_skills != null)
            {
                _skills.Refreshed += OnSourceChanged;
                _skills.Collapsed += OnSourceChanged;
            }

            Evaluate();
        }

        public void Revive()
        {
            IsDefeated = false;
            Cause = DefeatCause.None;
            Evaluate();
            Changed?.Invoke();
        }

        void OnSourceChanged() => Evaluate();

        void Evaluate()
        {
            if (IsDefeated)
                return;

            DefeatCause cause = DefeatCause.None;

            if (_body != null && _body.IsDeadState)
                cause = DefeatCause.BodyFatal;
            else if (_skills != null && _skills.IsCollapsed)
                cause = DefeatCause.StatCollapse;

            if (cause == DefeatCause.None)
                return;

            IsDefeated = true;
            Cause = cause;
            if (cause == DefeatCause.StatCollapse)
                LogStatCollapse();
            Changed?.Invoke();
        }

        void LogStatCollapse()
        {
            var sb = new StringBuilder(192);
            sb.Append("[StatCollapse]");
            if (_body != null)
            {
                sb.Append(" bodyDead=").Append(_body.IsDeadState);
                sb.Append(" con=").Append(BodyCapacity.Consciousness(_body).ToString("0.###"));
            }

            if (_skills != null)
            {
                sb.Append(" attrs");
                for (int i = 0; i < AttributeIds.All.Length; i++)
                {
                    string id = AttributeIds.All[i];
                    sb.Append(' ').Append(id).Append('=')
                        .Append(_skills.Level(id)).Append('/').Append(_skills.BaseLevel(id));
                }
            }

            if (_body != null)
            {
                var fx = new List<BodyPartEffect>(16);
                IReadOnlyList<BodyPartNode> roots = _body.Roots;
                for (int i = 0; i < roots.Count; i++)
                    _body.CollectEffectsUnder(roots[i].PartId, fx, includeDescendants: true);

                if (fx.Count > 0)
                {
                    sb.Append(" fx");
                    for (int i = 0; i < fx.Count; i++)
                    {
                        BodyPartEffect e = fx[i];
                        sb.Append(' ').Append(e.EffectId).Append('*').Append(e.Intensity);
                    }
                }
            }

            Debug.LogWarning(sb.ToString());
        }

        public void Dispose()
        {
            if (_body != null)
                _body.Changed -= OnSourceChanged;
            if (_skills != null)
            {
                _skills.Refreshed -= OnSourceChanged;
                _skills.Collapsed -= OnSourceChanged;
            }
        }
    }
}
