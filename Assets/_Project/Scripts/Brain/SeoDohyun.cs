using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 서도현 — 연습 대회 8강, "카운터 압박"(ROSTER.md §R3). 원형: 수비형/카운터형,
    /// 튜토리얼 졸업. 순서가 규칙이다 — H1(상대 흔들기 중, 좁은 조건)이 H2(내 밸런스
    /// 온전, 넓은 조건)보다 먼저다(<see cref="Jindoyun"/> 과 같은 "좁은 것부터" 원칙).
    /// 변칙기 없음.
    /// </summary>
    public static class SeoDohyun
    {
        public static readonly HabitId[] Habits =
        {
            HabitId.CounterReadyOnOpponentSetup,
            HabitId.StayDefensiveWhenBalanceHigh,
        };

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            return new OpponentDefinition("서도현", habits, scoutingLevel: 0);
        }
    }
}
