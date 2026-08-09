using System.Collections.Generic;

namespace CounterTime.Brain
{
    /// <summary>
    /// 상대 한 명의 <b>정의</b>. 불변이고, 여러 경기가 공유해도 안전하다.
    /// 경기 중에 쌓이는 것은 전부 <see cref="HabitState"/> 에 있다.
    ///
    /// Game 쪽 ScriptableObject 가 저작하는 것이 정확히 이 타입이다 — 습관 목록과
    /// <see cref="ScoutingLevel"/>.
    /// </summary>
    public sealed class OpponentDefinition
    {
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
    }
}
