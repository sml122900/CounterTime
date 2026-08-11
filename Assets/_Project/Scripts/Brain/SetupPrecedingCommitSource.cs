using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// §4.4 [초안] — B 가 낼 큰기술 앞에 흔들기(setup) 턴을 자동으로 끼워 넣는 데코레이터.
    ///
    /// <see cref="HabitCatalogue"/>(습관 카탈로그)도 <see cref="HabitLadder"/>(읽기
    /// 사다리)도 이 파일에서 한 글자도 안 건드린다 — 감싸인 <see cref="HabitBrainCommitSource"/>
    /// 가 이미 결정한 커밋을 <b>한 턴 늦춰</b> 내보낼 뿐이다. 사다리 쪽에서 보면
    /// <c>TryNext</c> 호출 빈도(쿨다운·<c>HabitState.Turn</c> 이 세는 단위)는 그대로다 —
    /// 안에서 감싸인 소스는 실제 결정 하나당 정확히 한 번만 불린다.
    ///
    /// <b>알려진 한계 — 읽기 사다리와의 타이밍.</b> <see cref="HabitBrainCommitSource"/>
    /// 는 결정 즉시 <c>LiveReadResolver.NoteOpponentHabit</c> 를 부른다(그 호출 자체가
    /// 사다리 안쪽에 있어 이 데코레이터가 가로챌 수 없다 — 가로채려면 그 파일을 고쳐야
    /// 하는데 그건 금지 범위다). 그 결과 A 의 읽기는 흔들기(설치) 교환에서 이미 그
    /// 결정을 보고, 진짜 기술이 나오는 다음 교환에는 새 알림이 없다. 오늘 밤 범위 밖 —
    /// stage 6 가 이 데코레이터와 <c>LiveReadResolver</c> 를 실제로 엮을 때 같이 풀 것.
    ///
    /// <b>B 의 흔들기는 지금 100% 정직한 tell이다.</b> 흔들기가 보이면 다음 교환에
    /// 반드시 진짜 큰기술이 온다 — 페이크(흔들기만 하고 안 던지기)는 여기 없다. 후속 설계.
    /// </summary>
    public sealed class SetupPrecedingCommitSource : ICommitSource
    {
        readonly HabitBrainCommitSource _inner;
        readonly IBrainContext _brainContext;
        readonly bool _enabled;

        bool _hasPending;
        FighterId _pendingWho;
        TurnChoice _pendingChoice;
        TechniqueSpec _pendingTechnique;
        HabitDecision _pendingDecision;
        bool _pendingUsedFallback;

        /// 감싸인 소스와 같은 이름으로 낸다 — 이 데코레이터가 그 자리를 그대로 대신할 수
        /// 있어야 한다. 흔들기를 내보내는 턴에는 셋 다 "아무 일도 없음"이다
        /// (기술 없음 / <c>default</c> 결정 / 폴백 아님) — 그래야 <see cref="ObservationLedger"/>
        /// 가 같은 발화를 두 번 세지 않는다(<c>HabitDecision.Fired</c> 가 false 면
        /// <c>Record</c> 는 그냥 돌아간다).
        public TechniqueSpec LastTechnique { get; private set; }
        public HabitDecision LastDecision { get; private set; }
        public bool LastUsedFallback { get; private set; }

        public SetupPrecedingCommitSource(HabitBrainCommitSource inner, IBrainContext brainContext,
                                          bool enabled)
        {
            _inner = inner;
            _brainContext = brainContext;
            _enabled = enabled;
        }

        public bool TryNext(out FighterId who, out TurnChoice choice)
        {
            if (_hasPending)
            {
                _hasPending = false;
                who = _pendingWho;
                choice = _pendingChoice;
                LastTechnique = _pendingTechnique;
                LastDecision = _pendingDecision;
                LastUsedFallback = _pendingUsedFallback;
                return true;
            }

            bool got = _inner.TryNext(out who, out choice);
            var technique = _inner.LastTechnique;

            // 게이트: §4.4 대로 그립이 하나도 없으면 흔들기 자체가 성립하지 않는다 —
            // 그러면 끼워 넣어봐야 Core 에서 no-op 이니 그냥 원래 턴을 그대로 보낸다.
            // OwnEffectiveGrip 은 부호 있는 값(§3, −2…+2)이라 그립을 "쥐고 있다"의
            // 완벽한 대리는 아니지만, 이 자리에서 나오는 유일한 BigThrow(H1/H2)는 둘 다
            // 자기 조건으로 이미 grip ≥ +1 을 요구하므로 실제로는 항상 일치한다.
            if (_enabled && got && IsBigThrow(technique) && _brainContext.OwnEffectiveGrip >= 1)
            {
                _hasPending = true;
                _pendingWho = who;
                _pendingChoice = choice;
                _pendingTechnique = technique;
                _pendingDecision = _inner.LastDecision;
                _pendingUsedFallback = _inner.LastUsedFallback;

                choice = new TurnChoice(HandTarget.Setup, CoreStance.Neutral, LegAction.Neutral,
                    choice.OwnHand);
                LastTechnique = null;
                LastDecision = default;
                LastUsedFallback = false;
                return true;
            }

            LastTechnique = technique;
            LastDecision = _inner.LastDecision;
            LastUsedFallback = _inner.LastUsedFallback;
            return got;
        }

        static bool IsBigThrow(TechniqueSpec t) => t != null && t.Cost == ActionCost.BigThrow;
    }
}
