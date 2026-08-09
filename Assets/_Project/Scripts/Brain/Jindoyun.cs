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
        public static readonly HabitId[] Habits =
        {
            HabitId.BigThrowOnGripEdge,
            HabitId.LegTechAfterShido,
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
