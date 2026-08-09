using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 진도윤 — 유도 1경기 상대. <c>OPPONENT_BRAIN §4.1</c> 의 습관 셋을 그대로 켠다.
    /// 조건·행동 본문은 <see cref="HabitCatalogue"/> 에 있다.
    ///
    /// 반환값은 <b>불변</b>이라 여러 경기가 공유해도 된다. 경기마다 새로 만드는 것은
    /// <see cref="HabitState"/> 다.
    /// </summary>
    public static class Jindoyun
    {
        /// <summary>
        /// <b>순서가 규칙이다</b> — 사다리는 첫 매치에서 끝난다.
        ///
        /// H1 과 H4 는 조건이 정확히 배타적이라(잡기 ≥ +1 / &lt; +1) 서로 다투지 않는다.
        /// H2 를 H4 위에 두는 것은 의도다: 지도를 받은 직후에는 잡으러 가는 것보다
        /// 만회가 급하다.
        ///
        /// 예비 둘은 맨 뒤다. H4b 가 H3 보다 앞인 것도 의도 — 폐기가 났을 때 B 는
        /// 먼저 다른 소매를 시도하고, 그것도 안 되면 틀어박힌다.
        /// </summary>
        public static readonly HabitId[] Habits =
        {
            HabitId.BigThrowOnGripEdge,
            HabitId.LegTechAfterShido,
            HabitId.SeekGripRightSleeve,
            HabitId.SeekGripLeftSleeve,
            HabitId.TurtleWhenTired,
        };

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            // §5 — 필드는 구현하되 0 으로 출하한다. 커리어 작업은 데이터를 바꾸지
            // 코드를 바꾸지 않는다.
            return new OpponentDefinition("진도윤", habits, scoutingLevel: 0);
        }
    }
}
