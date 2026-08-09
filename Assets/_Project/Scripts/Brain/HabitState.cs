using System.Collections.Generic;

namespace CounterTime.Brain
{
    /// <summary>
    /// 한 경기 동안 습관들에 쌓이는 것 — 피해 카운터와 폐기·점등. 수명은
    /// <see cref="ObservationLedger"/> 와 같다: 경기 하나.
    ///
    /// <see cref="OpponentDefinition"/> 은 여러 경기가 공유해도 안전하다(불변).
    /// 오염되는 것은 전부 여기 모여 있고, 새 경기는 이 객체를 새로 만들면 끝난다.
    /// </summary>
    public sealed class HabitState
    {
        /// §4.2 — 피해 카운터가 이 값에 닿으면 폐기. v1 튜너블.
        public const int RetireAtDamage = 2;

        readonly Dictionary<HabitId, int> _damage = new Dictionary<HabitId, int>();
        readonly HashSet<HabitId> _retired = new HashSet<HabitId>();
        bool _reservesLit;

        public OpponentDefinition Opponent { get; }

        public HabitState(OpponentDefinition opponent) => Opponent = opponent;

        public int Damage(HabitId id) => _damage.TryGetValue(id, out int d) ? d : 0;

        public bool IsRetired(HabitId id) => _retired.Contains(id);

        /// 예비가 아닌 습관은 처음부터 켜져 있다. 예비는 폐기가 한 번 나야 켜진다.
        public bool IsLit(HabitDefinition habit) => !habit.IsReserve || _reservesLit;

        /// 사다리에 오를 자격.
        public bool IsActive(HabitDefinition habit) => !IsRetired(habit.Id) && IsLit(habit);

        /// <summary>
        /// §4.2 — 이 습관이 대가를 치렀다(발화한 턴에 A 가 유효 이상을 냈거나 되치기에
        /// 성공했다). 임계에 닿으면 폐기하고 <b>예비를 점등</b>한다.
        ///
        /// 교체가 없으면 교정당한 상대가 잡음으로 퇴화하고 읽을 것이 사라진다.
        /// 수 노트 항목은 지우지 않는다 — 폐기는 플래그일 뿐이고 폐기됨 표시는 UI 몫이다.
        /// </summary>
        public void RegisterDamage(HabitId id)
        {
            if (_retired.Contains(id)) return;

            int d = Damage(id) + 1;
            _damage[id] = d;
            if (d < RetireAtDamage) return;

            _retired.Add(id);
            _reservesLit = true;
        }
    }
}
