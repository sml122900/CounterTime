using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 습관 <see cref="HabitId"/> → 정의. 조건과 행동은 규칙이라 여기 코드로 산다.
    ///
    /// 저작 가능한 것은 <b>어느 습관을 켤지</b>와 <b>발화확률</b>뿐이고, 그건 Game 쪽 SO 가
    /// 정한다. H1 의 "≥ +1" 을 인스펙터에서 만지게 하면 규칙이 에셋으로 새어나간다.
    /// </summary>
    public static class HabitCatalogue
    {
        public const float DefaultFireProbability = 0.75f;

        public static HabitDefinition Build(HabitId id, MatchRulesConfig cfg,
                                            float fireProbability = DefaultFireProbability)
        {
            switch (id)
            {
                // H1 — 자기 유효 잡기가 우위면 업어치기. 초대하는 대응: 잡기를 내주고
                // 카운터 준비를 선언하는 것.
                case HabitId.BigThrowOnGripEdge:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            TurnChoice.Neutral,
                            new TechniqueSpec("업어치기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        fireProbability);

                // H2 — 지도 뒤 2턴 내에 안뒤축감아치기로 만회한다. 초대하는 대응: 기다리기.
                // "2턴 내" = TurnsSinceOwnShido 가 0 과 1, 정확히 두 번이다. <= 2 는
                // 세 번이 되어 틀린다 — 턴의 정의는 IHabitContext 에 못 박아뒀다.
                //
                // 견제기가 아니라 진짜 시도다. 견제기는 판정을 발화하지 않아서, 읽혀도
                // 벌 방법이 없는 습관이 된다 — 공략 대상이 되는 것이 습관의 존재 이유다.
                case HabitId.LegTechAfterShido:
                    return new HabitDefinition(id,
                        ctx => ctx.TurnsSinceOwnShido.HasValue && ctx.TurnsSinceOwnShido.Value < 2,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("안뒤축감아치기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        fireProbability);

                // H3 — 예비. 폐기가 나기 전엔 사다리에 오르지 않는다.
                // 체력 <50 '그리고' 상대보다 낮을 때만이다. 단순 열세는 턴의 절반쯤
                // 참이라 B 가 영구히 틀어박히고, §6.1(+3/턴)·§5.2(−1/턴)가 B 를
                // 샌드백으로 갈아버린다. 50 은 새 숫자가 아니라 §4 가 이미 쓰는 구간이다.
                case HabitId.TurtleWhenTired:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnStamina < cfg.StaminaBonusTiredThreshold
                            && ctx.OwnStamina < ctx.OpponentStamina,
                        new HabitAction(
                            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend)),
                        fireProbability,
                        isReserve: true);

                default:
                    return null;
            }
        }
    }
}
