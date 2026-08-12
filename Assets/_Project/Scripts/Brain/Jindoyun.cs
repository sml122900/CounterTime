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

        /// §H 3.5b — 결승 상향(ROSTER.md §R5): "습관 신뢰도를 약간 낮춰 더 섞는다" ⚙.
        /// 기본값(HabitCatalogue.DefaultFireProbability, 0.75)보다 낮다 — 예측하기
        /// 살짝 어려워진 최종 보스 손잡이 하나.
        const float FinalBossFireProbability = 0.65f;

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++)
                habits[i] = HabitCatalogue.Build(Habits[i], cfg, FinalBossFireProbability);

            // §H 3.5b — 결승 상향: "결승쯤 오면 내 앞선 경기를 봤다"는 서사로
            // scoutingLevel 을 1 로 시드한다(ROSTER §R5, ⚙). §5 필드는 구현하되 지금은
            // 아무 데도 안 읽혀 경기에 영향이 없다 — §5/6 이 §3 성향 창을 실제로
            // 시딩할 때 점등된다(3.5a 세션의 "필드는 구현하되 값만 채운다" 관행과 같다).
            // 말아업어치기(변칙기, 습관 아님) — 체력<30, 경기당 1회(§H 3.5b, 기존
            // 설계 문서 그대로 실제 배선).
            return new OpponentDefinition("진도윤", habits, scoutingLevel: 1,
                surprise: new SurpriseTechnique("말아업어치기", ctx => ctx.OwnStamina < 30));
        }
    }
}
