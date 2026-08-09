using System;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>습관 식별자. 수 노트·관측 원장이 이걸로 센다.</summary>
    public enum HabitId
    {
        /// 자기 유효 잡기 ≥ +1 → 업어치기.
        BigThrowOnGripEdge,
        /// 지도 받은 뒤 2턴 내 → 발기술 만회.
        LegTechAfterShido,
        /// 예비 — 폐기 시 점등. 체력 &lt;50 이고 상대보다 낮으면 극단적 방어.
        TurtleWhenTired,
    }

    /// <summary>
    /// 습관이 발화했을 때 B 가 내는 것. 커밋(§2 세 축)과 기술은 별개다 —
    /// 극단적 방어처럼 기술이 없는 행동이 있고, 업어치기처럼 커밋 축으로는 표현되지
    /// 않는 기술이 있다.
    /// </summary>
    public sealed class HabitAction
    {
        public readonly TurnChoice Commit;
        /// <c>null</c> = 이 턴에 기술을 내지 않는다.
        public readonly TechniqueSpec Technique;

        public HabitAction(TurnChoice commit, TechniqueSpec technique = null)
        {
            Commit = commit;
            Technique = technique;
        }
    }

    /// <summary>
    /// 습관 하나. <b>한 경기치 상태를 든다</b> — 피해 카운터와 폐기·점등 플래그가
    /// 경기 중에 바뀐다. 그래서 이 객체를 경기 간에 재사용하면 안 된다.
    ///
    /// Game 쪽 ScriptableObject 래퍼는 <b>데이터만</b> 저작하고(어느 습관을 켤지 /
    /// 발화확률 / ScoutingLevel) 경기마다 이 객체를 새로 찍는다. 조건과 행동은 규칙이라
    /// 코드에 산다 — H1 의 "≥ +1" 은 저작 대상이 아니다.
    /// (<see cref="TechniqueSpec"/> ↔ SO 래퍼와 같은 패턴.)
    /// </summary>
    public sealed class HabitDefinition
    {
        public readonly HabitId Id;
        public readonly Func<IHabitContext, bool> Condition;
        public readonly HabitAction Action;
        /// §4 — 0.75 이지 1.0 이 아니다. 1.0 이면 확정 습관이 B 를 결정론으로 만들고
        /// 경기가 조회표가 된다. 25% 의 빗나감이 알려진 습관을 계속 긴장시킨다.
        public readonly float FireProbability;
        /// 예비 습관은 점등되기 전엔 사다리에 오르지 않는다.
        public readonly bool IsReserve;

        /// §4.2 — 이 습관이 발화한 턴에 A 가 유효 이상을 내거나 되치기에 성공하면 +1.
        public int DamageCount { get; private set; }
        /// 카운터가 2 에 닿으면 그 경기 동안 영구 폐기.
        public bool IsRetired { get; private set; }
        /// 예비 습관이 점등됐는가. 예비가 아닌 습관은 처음부터 켜져 있다.
        public bool IsLit { get; private set; }

        public HabitDefinition(HabitId id, Func<IHabitContext, bool> condition, HabitAction action,
                               float fireProbability = 0.75f, bool isReserve = false)
        {
            Id = id;
            Condition = condition;
            Action = action;
            FireProbability = fireProbability;
            IsReserve = isReserve;
            IsLit = !isReserve;
        }

        /// 사다리에 오를 자격. 폐기됐거나, 예비인데 아직 안 켜졌으면 오르지 않는다.
        public bool IsActive => !IsRetired && IsLit;

        /// <summary>피해 1회. 임계에 닿으면 폐기한다. 반환값 = 이번 호출로 폐기됐는가.</summary>
        internal bool TakeDamage(int retireAt)
        {
            if (IsRetired) return false;
            DamageCount++;
            if (DamageCount < retireAt) return false;
            IsRetired = true;
            return true;
        }

        internal void Light() => IsLit = true;
    }
}
