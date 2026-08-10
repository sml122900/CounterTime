using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 플레이 모드의 읽기 출처 — <c>OPPONENT_BRAIN §2</c> 사다리 6행을 그대로 구현한다.
    ///
    /// <b>배타적이다. 첫 매칭에서 끝나고 절대 합산하지 않는다</b> (§6.1 과 같은 모양).
    /// 순서가 곧 규칙이다: A 가 3회 관측으로 <i>번</i> 지식(1~3행)이, A 자신의 게으름이
    /// B 에게 거저 준 지식(4·5행)을 이긴다.
    ///
    /// <b>반환값은 BASE 읽기다</b> — §4.2 카운터 준비 감쇄 <i>이전</i>. 감쇄는 Core 가
    /// 건다. 여기서 선적용하면 두 칸 내려가고 에러는 안 난다 (<see cref="IReadResolver"/>
    /// 계약).
    /// </summary>
    public sealed class LiveReadResolver : IReadResolver
    {
        readonly ObservationLedger _ledger;
        readonly TendencyWindow _window;

        HabitDecision _bHabit;

        public LiveReadResolver(ObservationLedger ledger, TendencyWindow window)
        {
            _ledger = ledger;
            _window = window;
        }

        /// <summary>
        /// 이번 교환에 B 가 무엇을 냈는지 알린다. 커밋이 실제로 교환에 제출되는
        /// 시점(예: <c>LiveMatchRunner.SubmitA</c>)에서, 그 교환에 쓰일 결정으로 불러야
        /// 한다 — <c>HabitBrainCommitSource.TryNext</c> 가 사다리를 굴린 시점이 아니다.
        /// 쿠즈시 데코레이터(<see cref="KuzushiPrecedingCommitSource"/>)가 결정을 한
        /// 교환 늦게 낼 수 있어서, 결정 시점과 제출 시점이 다를 수 있다.
        ///
        /// <b>매 교환 부른다</b> — 습관이 안 나온 턴(쿠즈시 설치 교환 포함)은
        /// <c>default</c> 로. 안 지우면 지난 턴의 발화가 이번 턴 읽기로 새고, 그건
        /// 조용히 틀린다.
        /// </summary>
        public void NoteOpponentHabit(in HabitDecision decision) => _bHabit = decision;

        public ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b)
        {
            // 발화하지 않은 습관은 어느 쪽에도 읽기를 주지 않는다. 조건만 섰거나
            // 25% 에 걸린 턴은 tell 이 없었다는 뜻이다.
            bool fired = _bHabit.ConditionHeld && _bHabit.Fired;
            int observations = fired ? _ledger.Observations(_bHabit.Habit.Id) : 0;
            bool certain = fired && _ledger.IsConfirmed(_bHabit.Habit.Id);
            bool suspected = observations > 0 && !certain;
            bool nonAttacking = fired && _bHabit.Habit.Action.Technique == null;

            // 1 — B 가 공격하는데 A 가 그 습관을 확정으로 안다. B 가 읽힌다.
            if (attacker == FighterId.B && certain) return ReadLevel.GotRead;

            // 2·3 — A 가 공격하고 B 는 비공격 습관을 냈다. 심증은 여기까지만 쓰인다.
            if (attacker == FighterId.A && nonAttacking)
            {
                if (certain) return ReadLevel.FullRead;
                if (suspected) return ReadLevel.Read;
            }

            // 4 — 성향 창. B 가 공격할 때만이다.
            if (attacker == FighterId.B)
            {
                if (_window.ConsecutiveMatches(a)) return ReadLevel.FullRead;
                if (_window.MajorityMatches(a)) return ReadLevel.Read;
            }

            // 5 — A 가 예측 가능해졌다. 위험선(3연속)에서만이고 경고선(4/6)에서는 아니다.
            if (attacker == FighterId.A && _window.ConsecutiveMatches(a)) return ReadLevel.GotRead;

            // 6
            return ReadLevel.Neutral;
        }
    }
}
