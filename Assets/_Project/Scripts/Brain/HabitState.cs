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
        readonly Dictionary<HabitId, int> _lastFiredTurn = new Dictionary<HabitId, int>();
        readonly HashSet<HabitId> _retired = new HashSet<HabitId>();
        bool _reservesLit;
        /// §H 3.5b — <see cref="OpponentDefinition.Surprise"/> 의 "경기당 1회" 플래그.
        /// HabitId 로 세는 <see cref="_lastFiredTurn"/> 과 의도적으로 분리 — 변칙기는
        /// HabitId 가 없어서(관측 대상이 아니라서) 저 딕셔너리에 넣을 자리 자체가 없다.
        bool _surpriseUsed;

        public OpponentDefinition Opponent { get; }

        /// 이 경기에서 사다리가 몇 번 돌았는가. <see cref="HabitLadder.Next"/> 가 올린다.
        public int Turn { get; private set; } = -1;

        public HabitState(OpponentDefinition opponent) => Opponent = opponent;

        public int Damage(HabitId id) => _damage.TryGetValue(id, out int d) ? d : 0;

        public bool IsRetired(HabitId id) => _retired.Contains(id);

        /// 예비가 아닌 습관은 처음부터 켜져 있다. 예비는 폐기가 한 번 나야 켜진다.
        public bool IsLit(HabitDefinition habit) => !habit.IsReserve || _reservesLit;

        /// <summary>§4.1 쿨다운. 아직 안 지났으면 이번 턴에는 못 오른다.</summary>
        public bool IsOnCooldown(HabitDefinition habit) =>
            habit.MinTurnsBetweenFirings > 0
            && _lastFiredTurn.TryGetValue(habit.Id, out int last)
            && Turn - last < habit.MinTurnsBetweenFirings;

        /// 사다리에 오를 자격.
        public bool IsActive(HabitDefinition habit) =>
            !IsRetired(habit.Id) && IsLit(habit) && !IsOnCooldown(habit);

        /// <summary>사다리가 한 바퀴 돈다. 턴 계수와 쿨다운의 기준이다.</summary>
        internal void BeginTurn() => Turn++;

        /// <summary>이번 턴에 발화했다 — 쿨다운 시작점을 찍는다.</summary>
        internal void NoteFired(HabitId id) => _lastFiredTurn[id] = Turn;

        /// <summary>
        /// §4.2 — 이 습관이 대가를 치렀다(발화한 턴에 A 가 유효 이상을 냈거나 되치기에
        /// 성공했다). 임계에 닿으면 폐기하고 <b>예비를 점등</b>한다.
        ///
        /// <b>선행조건: A 가 그 습관을 이미 확정으로 알고 있어야 한다.</b> 확정 전의
        /// 피해는 읽어서 낸 게 아니라 운이고, B 는 자기가 읽혔다는 걸 모른다. 이게
        /// 없으면 루프가 산술적으로 완주 불가다 — 확정은 4번째 발화가 필요한데 폐기는
        /// 2회면 되므로, <b>벌줄 수 있는 습관은 읽히기 전에 먼저 사라진다</b>.
        /// 원장을 인자로 받는 이유가 그것이다: 부르는 쪽에 판단할 자리를 안 준다.
        ///
        /// 교체가 없으면 교정당한 상대가 잡음으로 퇴화하고 읽을 것이 사라진다.
        /// 수 노트 항목은 지우지 않는다 — 폐기는 플래그일 뿐이고 폐기됨 표시는 UI 몫이다.
        /// </summary>
        /// <param name="ledger">이번 턴의 관측이 기록되기 <b>전</b>의 원장. 그래야 "A 가
        /// 읽을 수 있었던 상태에서 벌준 것" 만 세어진다.</param>
        public void RegisterDamage(HabitId id, ObservationLedger ledger)
        {
            if (!ledger.IsConfirmed(id)) return;
            if (_retired.Contains(id)) return;

            int d = Damage(id) + 1;
            _damage[id] = d;
            if (d < RetireAtDamage) return;

            _retired.Add(id);
            _reservesLit = true;
        }

        /// <summary>§H 3.5b — 변칙기 하드 규칙. 이 경기에서 아직 안 썼고 조건이 서면
        /// 소비하고 true. 관측 원장에 절대 안 닿는다(호출자는 이 결과를 습관 사다리와
        /// 별도로 다뤄야 한다) — 그게 "사전에 관측될 수 없다"의 전부다.</summary>
        public bool TryFireSurprise(IHabitContext ctx)
        {
            if (_surpriseUsed) return false;
            if (Opponent.Surprise == null) return false;
            if (!Opponent.Surprise.Condition(ctx)) return false;
            _surpriseUsed = true;
            return true;
        }
    }
}
