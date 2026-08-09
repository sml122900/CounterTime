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
        /// <b>순서가 규칙이다</b> — 사다리는 첫 매치에서 끝나므로, 위에 있는 것이 아래를
        /// 굶길 수 있다. 그래서 <b>조건이 좁은 것부터</b> 검사한다 (§6.1 과 같은 모양).
        ///
        /// <code>
        /// H2  지도 받은 뒤 2턴 내        가장 좁다 — 지도가 나야 하고 창도 2턴이다
        /// H3  체력 &lt;50 이고 열세         둘 다 만족해야 한다
        /// H1  잡기 ≥ +1                  잡기 축의 절반
        /// H4  잡기 &lt; +1                  나머지 절반 — 가장 넓다
        /// H4b 잡기 &lt; +1 (예비)           H4 의 교체분이라 그 뒤
        /// </code>
        ///
        /// 실측 근거: H1 을 맨 앞에 뒀더니 H1 이 7회 발화하는 동안 H2·H3 이 <b>0회</b>
        /// 였다. B 의 습관 넷 중 둘이 실질적으로 관측 불가였다.
        /// </summary>
        public static readonly HabitId[] Habits =
        {
            HabitId.LegTechAfterShido,
            HabitId.TurtleWhenTired,
            HabitId.BigThrowOnGripEdge,
            HabitId.SeekGripRightSleeve,
            HabitId.SeekGripLeftSleeve,
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
