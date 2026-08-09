using System.Collections.Generic;

namespace CounterTime.Brain
{
    /// <summary>
    /// 상대 한 명. 습관 목록과 사전 정보 수준을 든다.
    ///
    /// <see cref="HabitDefinition"/> 과 마찬가지로 <b>한 경기치</b>다 — 폐기·점등이
    /// 여기 걸린 습관들에 쌓인다.
    /// </summary>
    public sealed class OpponentDefinition
    {
        /// §4.2 — 피해 카운터가 이 값에 닿으면 폐기. v1 튜너블.
        public const int RetireAtDamage = 2;

        public readonly string Name;
        public readonly IReadOnlyList<HabitDefinition> Habits;

        /// <summary>
        /// §5 — 0…3. 유일한 효과는 §3 성향 창의 초기 시딩이고, 그 배선은 3단계다.
        /// v1 은 필드만 두고 진도윤을 <b>0</b> 으로 출하한다. 커리어 작업은 코드가
        /// 아니라 데이터를 바꾼다.
        /// </summary>
        public readonly int ScoutingLevel;

        public OpponentDefinition(string name, IReadOnlyList<HabitDefinition> habits, int scoutingLevel = 0)
        {
            Name = name;
            Habits = habits;
            ScoutingLevel = scoutingLevel;
        }

        /// <summary>
        /// §4.2 — 이 습관이 대가를 치렀다. 임계에 닿으면 폐기하고 <b>예비를 점등</b>한다.
        ///
        /// 교체가 없으면 교정당한 상대가 잡음으로 퇴화하고 읽을 것이 사라진다.
        /// 수 노트 항목은 지우지 않는다 — 폐기는 <see cref="HabitDefinition.IsRetired"/>
        /// 플래그일 뿐이고, 폐기됨 표시는 UI 몫이다.
        /// </summary>
        public void RegisterDamage(HabitId id)
        {
            foreach (var h in Habits)
            {
                if (h.Id != id) continue;
                if (h.TakeDamage(RetireAtDamage)) LightReserves();
                return;
            }
        }

        void LightReserves()
        {
            foreach (var h in Habits)
                if (h.IsReserve) h.Light();
        }
    }
}
