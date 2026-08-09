using System.Collections.Generic;

namespace CounterTime.Brain
{
    /// <summary>
    /// §2 — 습관별 관측 카운터. 수 노트가 읽는 장부다.
    ///
    /// <b>관측은 조건이 섰고 B 가 실제로 그 습관을 실행한 턴에만 +1 이다.</b>
    /// 조건만 섰고 B 가 딴짓을 한 턴은 0 이다 — 안 일어난 일을 본 척하면 플레이어가
    /// 없는 습관을 읽게 되고, 그건 읽기 시스템 전체를 거짓말로 만든다.
    ///
    /// 그 규칙이 호출 규약이 아니라 구조로 서 있다: <see cref="Record"/> 는
    /// <see cref="HabitDecision"/> 을 받고 <see cref="HabitDecision.Fired"/> 만 센다.
    /// 발화 여부를 부르는 쪽이 판단할 자리가 없다.
    /// </summary>
    public sealed class ObservationLedger
    {
        /// §2 — 관측 3회 이상이면 확정(수 노트에 기록), 1~2 면 심증.
        public const int CertainObservations = 3;

        readonly Dictionary<HabitId, int> _counts = new Dictionary<HabitId, int>();

        public int Observations(HabitId id) => _counts.TryGetValue(id, out int c) ? c : 0;

        /// <summary>수 노트에 올라와 있는가 (§2 확정). 파생 지점은 여기 하나다.</summary>
        public bool IsConfirmed(HabitId id) => Observations(id) >= CertainObservations;

        public void Record(in HabitDecision decision)
        {
            if (!decision.Fired) return;
            _counts[decision.Habit.Id] = Observations(decision.Habit.Id) + 1;
        }
    }
}
