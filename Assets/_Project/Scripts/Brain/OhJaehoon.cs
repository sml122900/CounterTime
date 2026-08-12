using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 오재훈 — 연습 대회 동메달전(4강 패배 시만), "만능형"(ROSTER.md §R6). 원형: 중상급
    /// 올라운더, 특출난 축 없이 고루 한다. 두 큰기술(허리후리기/밭다리걸기)이 같은
    /// 쿨다운 교대로 섞인다(<see cref="ImJunchul"/> 과 같은 근사, §HabitCatalogue 주석
    /// 참조) — 뚜렷한 단일 습관이 없다는 설계 의도 그대로. 변칙기 없음(ROSTER 확정 —
    /// 변칙기는 4강·결승만).
    /// </summary>
    public static class OhJaehoon
    {
        public static readonly HabitId[] Habits =
        {
            HabitId.MixedBigThrowA,
            HabitId.MixedBigThrowB,
        };

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            return new OpponentDefinition("오재훈", habits, scoutingLevel: 0);
        }
    }
}
