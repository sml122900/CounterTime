using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>사다리가 이번 턴에 내린 결론. 하나의 습관, 혹은 아무것도 아님.</summary>
    public readonly struct HabitDecision
    {
        /// <c>null</c> = 조건이 성립한 습관이 하나도 없다 → §4 사다리 3단(폴백)으로 내려간다.
        public readonly HabitDefinition Habit;
        /// 조건이 성립했고 발화 확률까지 통과했는가.
        public readonly bool Fired;

        public HabitDecision(HabitDefinition habit, bool fired)
        {
            Habit = habit;
            Fired = fired;
        }

        /// 조건은 섰다. <see cref="Fired"/> 가 false 면 25% 의 빗나감이다.
        public bool ConditionHeld => Habit != null;
    }

    /// <summary>
    /// §4 사다리 2단 — 습관. <b>첫 매치에서 끝난다. 절대 합산하지 않는다</b>
    /// (§6.1 심판 사다리와 같은 모양, 같은 이유).
    ///
    /// 1단(하드 게이트)과 3단(폴백 가중 무작위)은 3단계다. 여기는 습관만 본다.
    /// </summary>
    public sealed class HabitLadder
    {
        readonly IRandomSource _rng;

        public HabitLadder(IRandomSource rng) => _rng = rng;

        /// <param name="state">경기 상태. 어느 상대인지도 여기서 나온다 — 정의와 상태를
        /// 따로 받으면 짝이 안 맞는 조합을 넘길 수 있다.</param>
        public HabitDecision Next(HabitState state, IHabitContext ctx)
        {
            // 사다리 한 바퀴 = 한 턴. 쿨다운이 이걸 기준으로 센다. 여기서 올리므로
            // 부르는 쪽이 턴 증가를 잊을 수가 없다.
            state.BeginTurn();

            foreach (var h in state.Opponent.Habits)
            {
                // 폐기됐거나, 아직 안 켜진 예비거나, 쿨다운 중이면 오르지 않는다.
                // 건너뛰고 다음 칸으로 간다 — 배타성은 그대로다.
                if (!state.IsActive(h)) continue;
                if (!h.Condition(ctx)) continue;

                // 여기서 return 하는 것이 '합산하지 않는다'의 전부다. 아래 습관들은
                // 조건이 서 있어도 이번 턴에 쳐다보지 않는다.
                bool fired = _rng.NextFloat() < h.FireProbability;
                // 쿨다운은 실제로 낸 턴부터 센다. 25% 에 걸린 턴은 아무 일도 안 났다.
                if (fired) state.NoteFired(h.Id);
                return new HabitDecision(h, fired);
            }
            return default;
        }
    }
}
