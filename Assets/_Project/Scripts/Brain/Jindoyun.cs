using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 진도윤 — 유도 1경기 상대. <c>OPPONENT_BRAIN §4.1</c> 을 그대로 옮긴 것이다.
    ///
    /// 조건과 행동이 코드에 있는 이유: 그것들은 규칙이지 저작 대상이 아니다. H1 의
    /// "≥ +1" 을 인스펙터에서 만지게 하면 규칙이 에셋으로 새어나간다. Game 쪽 SO 가
    /// 저작하는 것은 <b>어느 습관을 켤지 / 발화확률 / ScoutingLevel</b> 셋뿐이다.
    ///
    /// <b>경기마다 새로 부른다.</b> 반환되는 습관들은 피해 카운터와 폐기·점등 상태를
    /// 들고 있어서 경기 간 재사용이 곧 오염이다.
    /// </summary>
    public static class Jindoyun
    {
        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new[]
            {
                // H1 — 자기 유효 잡기가 우위면 업어치기. 초대하는 대응: 잡기를 내주고
                // 카운터 준비를 선언하는 것.
                new HabitDefinition(
                    HabitId.BigThrowOnGripEdge,
                    ctx => ctx.OwnEffectiveGrip >= 1,
                    new HabitAction(
                        TurnChoice.Neutral,
                        new TechniqueSpec("업어치기", ActionCost.BigThrow, TechniqueClass.Standard))),

                // H2 — 지도 뒤 2턴 내에 발기술로 만회한다. 초대하는 대응: 그냥 기다리기.
                // "2턴 내" = TurnsSinceOwnShido 가 0 과 1, 정확히 두 번이다. <= 2 는
                // 세 번이 되어 틀린다 — 턴의 정의는 IHabitContext 에 못 박아뒀다.
                new HabitDefinition(
                    HabitId.LegTechAfterShido,
                    ctx => ctx.TurnsSinceOwnShido.HasValue && ctx.TurnsSinceOwnShido.Value < 2,
                    new HabitAction(
                        new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                        new TechniqueSpec("발기술", ActionCost.Poke, TechniqueClass.Sloppy))),

                // H3 — 예비. 폐기가 나기 전엔 사다리에 오르지 않는다.
                // 체력 <50 '그리고' 상대보다 낮을 때만이다. 단순 열세는 턴의 절반쯤
                // 참이라 B 가 영구히 틀어박히고, §6.1(+3/턴)·§5.2(−1/턴)가 B 를
                // 샌드백으로 갈아버린다. 50 은 새 숫자가 아니라 §4 가 이미 쓰는 구간이다.
                new HabitDefinition(
                    HabitId.TurtleWhenTired,
                    ctx => ctx.OwnStamina < cfg.StaminaBonusTiredThreshold
                        && ctx.OwnStamina < ctx.OpponentStamina,
                    new HabitAction(
                        new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend)),
                    isReserve: true),
            };

            // §5 — 필드는 구현하되 0 으로 출하한다. 커리어 작업은 데이터를 바꾸지 코드를
            // 바꾸지 않는다.
            return new OpponentDefinition("진도윤", habits, scoutingLevel: 0);
        }
    }
}
