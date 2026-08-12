using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 엔진 상태 → 브레인이 보는 두 면. 한 객체가 둘을 다 구현하지만 <b>면은 여전히
    /// 둘</b>이다: 습관 조건은 <see cref="IHabitContext"/> 타입으로만 이걸 받으므로
    /// 지도 개수·시계·게이트 멤버가 보이지 않는다.
    ///
    /// 파생은 없다. 전부 Core 에게 물어서 그대로 넘긴다 — 단 하나
    /// <see cref="TurnsSinceOwnShido"/> 만 여기서 센다. Core 가 그 값을 안 들고 있고,
    /// <b>어떤 규칙도 그 값을 쓰지 않기 때문에</b> 들어서도 안 된다. 규칙 엔진에
    /// 브레인 상태를 넣으면 MATCH_RULES 가 아닌 것이 Core 에 사는 것이 된다.
    /// </summary>
    public sealed class MatchBrainContext : IHabitContext, IBrainContext
    {
        readonly MatchEngine _engine;
        readonly FighterId _me;
        readonly FighterId _foe;

        int? _turnsSinceShido;

        public MatchBrainContext(MatchEngine engine, FighterId me)
        {
            _engine = engine;
            _me = me;
            _foe = me == FighterId.A ? FighterId.B : FighterId.A;
        }

        // ── IHabitContext ───────────────────────────────────────────────────────

        public int OwnEffectiveGrip => _engine.Grip.EffectiveGrip(_me);
        public int OwnStamina => _engine.Stamina.Get(_me);
        public int OpponentStamina => _engine.Stamina.Get(_foe);
        public int? TurnsSinceOwnShido => _turnsSinceShido;
        public bool OpponentSetupActive => _engine.IsSetupActive(_foe);
        public int OwnBalance => _engine.Balance.Get(_me);

        // ── IBrainContext ───────────────────────────────────────────────────────

        public int OwnShidoCount => _engine.Board.Shido(_me);
        public int SecondsRemaining => _engine.Clock.RemainingSeconds;
        public bool OwnStaminaLockout => _engine.Stamina.IsExhaustedLockout(_me);
        public bool OwnHasLockedHand => _engine.Grip.AnyHandLocked(_me);

        // ── 턴 계수 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 교환 하나가 해결될 때마다 그 이벤트를 넘긴다. <b>교환을 소비하지 않는 턴에는
        /// 부르지 않는다</b> — 1턴 = 해결된 교환 1회라는 것이
        /// <see cref="IHabitContext.TurnsSinceOwnShido"/> 에 못 박아둔 정의다.
        ///
        /// 이번 교환에서 지도가 났으면 값은 <b>0</b> 이 된다(증가시킨 뒤 덮어쓴다).
        /// 그래서 지도 다음 커밋이 0 을 보고, 그 다음이 1 을 본다 — H2 의 "2턴 내"가
        /// 정확히 그 두 번이다.
        /// </summary>
        public void ObserveExchange(IReadOnlyList<MatchEvent> events)
        {
            if (_turnsSinceShido.HasValue) _turnsSinceShido++;

            foreach (var e in events)
                if (e is ShidoGiven s && s.Who == _me) _turnsSinceShido = 0;
        }
    }
}
