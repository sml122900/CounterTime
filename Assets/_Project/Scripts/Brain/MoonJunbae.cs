using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 문준배 — 연습 대회 16강, "밸런스 게이트" 튜토리얼 상대(ROSTER.md §R2). 원형:
    /// 안정형 수비. 습관 하나(낮은 확률·긴 쿨다운의 반격) — 나머지 턴은 습관이 아예
    /// 안 뜨므로 §4 3단 폴백(정의상 큰기술이 없다)이 "웬만하면 방어·중립"을 무료로
    /// 만든다. 변칙기 없음.
    /// </summary>
    public static class MoonJunbae
    {
        public static readonly HabitId[] Habits = { HabitId.CounterThrowOnGripEdge };

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            return new OpponentDefinition("문준배", habits, scoutingLevel: 0);
        }
    }
}
