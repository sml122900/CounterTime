using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 황도철 — 연습 대회 32강, "읽기 기초" 튜토리얼 상대(ROSTER.md §R1). 원형: 돌격형.
    /// 습관 하나뿐 — 가장 단순·가장 시끄러운 상대다. 변칙기 없음.
    ///
    /// 반환값은 불변이라 여러 경기가 공유해도 된다(<see cref="Jindoyun"/> 과 같은 계약).
    /// </summary>
    public static class HwangDochul
    {
        public static readonly HabitId[] Habits = { HabitId.BigThrowAlways };

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            // §5 — 필드는 구현하되 0 으로 출하한다(첫 대회, 첫 상대라 나를 미리 알 리
            // 없다 — ROSTER §R0).
            return new OpponentDefinition("황도철", habits, scoutingLevel: 0);
        }
    }
}
