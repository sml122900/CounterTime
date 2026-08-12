using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 임준철 — 연습 대회 4강, "예측 되치기 + 첫 변칙기"(ROSTER.md §R4). 원형: 심리형
    /// 2지선다(그립 중심). H1 이 두 습관(어깨 매치기/발뒤축걸기)으로 나뉘어 쿨다운
    /// 교대로 "번갈아"를 근사한다(<see cref="HabitCatalogue"/> 주석 참조) — 그래서
    /// 둘이 나란히 붙는다. H2(지도 후 견제 발기술)는 별개 조건이라 그 뒤.
    ///
    /// 변칙기 안오금띄기 — 내 밸런스가 낮을 때(⚙ 50 미만) 1회, 습관이 아니라 하드
    /// 규칙(<see cref="SurpriseTechnique"/>).
    /// </summary>
    public static class ImJunchul
    {
        public static readonly HabitId[] Habits =
        {
            HabitId.ShoulderThrowOnGripEdge,
            HabitId.HeelHookOnGripEdge,
            HabitId.FeintAfterShido,
        };

        /// ROSTER §R4 "내 밸런스가 낮을 때" — 절반 미만을 "낮다"로 골랐다(⚙, 실플레이
        /// 튜닝 대상). §S 온전 임계(70, MatchRulesConfig.BalanceOnsetThreshold)의 절반
        /// 근처라 "게이트가 열릴 수 있는 상태보다도 더 나쁘다"는 뜻이 되게 잡았다.
        const int LowBalanceThreshold = 50;

        public static OpponentDefinition Create(MatchRulesConfig cfg)
        {
            var habits = new HabitDefinition[Habits.Length];
            for (int i = 0; i < Habits.Length; i++) habits[i] = HabitCatalogue.Build(Habits[i], cfg);

            return new OpponentDefinition("임준철", habits, scoutingLevel: 0,
                surprise: new SurpriseTechnique("안오금띄기", ctx => ctx.OwnBalance < LowBalanceThreshold));
        }
    }
}
