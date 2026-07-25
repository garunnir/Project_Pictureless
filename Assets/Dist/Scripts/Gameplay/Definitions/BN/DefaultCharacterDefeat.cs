// ============================================================
// DefaultCharacterDefeat ??BodyÂ·Skills êµ¬ë… ??IsDefeated ?˜ì¹˜
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// ?¬ë§?€ ì¢…ê²° ?íƒœ?? ??ë²?IsDefeatedê°€ ?˜ë©´ <see cref="Revive"/> ?„ê¹Œì§€ ? ì?(?˜ì¹˜).
    /// ?¬í‰ê°€??Body.Changed / Skills.RefreshedÂ·Collapsed ?´ë²¤?¸ì—?œë§Œ (hot-path ?´ë§ ?†ìŒ).
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
            Changed?.Invoke();
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
