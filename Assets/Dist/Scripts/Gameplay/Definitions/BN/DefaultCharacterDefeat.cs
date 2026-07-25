// ============================================================
// DefaultCharacterDefeat — Body·Skills 구독 후 IsDefeated 래치
// ============================================================

using System;

namespace Garunnir.Runtime.Gameplay.Data
{
    /// <summary>
    /// 사망은 종결 상태다: 한 번 IsDefeated가 되면 <see cref="Revive"/> 전까지 유지(래치).
    /// 재평가는 Body.Changed / Skills.Refreshed·Collapsed 이벤트에서만 (hot-path 폴링 없음).
    /// </summary>
    public sealed class DefaultCharacterDefeat : ICharacterDefeat, IDisposable
    {
        readonly IPlayerBody _body;
        readonly ICharacterSkills _skills;

        public event Action Changed;

        public bool IsDefeated { get; private set; }
        public DefeatCause Cause { get; private set; }

        public DefaultCharacterDefeat(IPlayerBody body, ICharacterSkills skills)
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
