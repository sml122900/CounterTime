using System;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>습관 식별자. 수 노트·관측 원장·경기 상태가 전부 이걸로 센다.</summary>
    public enum HabitId
    {
        /// 자기 유효 잡기 ≥ +1 → 업어치기.
        BigThrowOnGripEdge,
        /// 지도 받은 뒤 2턴 내 → 안뒤축감아치기로 만회.
        LegTechAfterShido,
        /// 예비 — 폐기 시 점등. 체력 &lt;50 이고 상대보다 낮으면 극단적 방어.
        TurtleWhenTired,
        /// 자기 유효 잡기 &lt; +1 → 오른소매를 노린다. 기술 없음(비공격 습관).
        SeekGripRightSleeve,
        /// 예비 — SeekGripRightSleeve 가 폐기되면 이쪽으로 넘어간다. 같은 조건, 왼소매.
        SeekGripLeftSleeve,

        // ── §H 3.5b 로스터 확장 (ROSTER.md §R1~§R6) ──────────────────────────────
        // 진도윤 다섯 개(위)는 무수정. 아래는 5명 신규 상대의 습관이다 — 전부 상대별로
        // 배타적으로만 쓰인다(한 OpponentDefinition 안에서만 등장, HabitCatalogue.Build
        // 스위치 케이스로 분리).

        /// 32강 황도철 H — 조건 없음(항상 후보). "발이 열리면 거의 항상 밭다리" =
        /// 가장 단순한 상대라 트리거 자체가 없다는 뜻으로 해석했다.
        BigThrowAlways,
        /// 16강 문준배 H — 자기 유효 잡기 ≥ +1 이면 허리후리기로 반격. 낮은 발화확률+
        /// 긴 쿨다운으로 "가끔"을 낸다 — 나머지 턴은 습관이 아예 안 뜨므로 폴백(§4
        /// 3단, 큰기술 없음)이 자연히 "웬만하면 방어" 를 만든다(별도 방어 습관 불필요).
        CounterThrowOnGripEdge,
        /// 8강 서도현 H1 — 상대가 흔들기(Setup) 중이면 카운터 준비. ROSTER 의 "내가
        /// 큰 기술을 예고하면" = 상대의 흔들기 상태(IHabitContext.OpponentSetupActive).
        CounterReadyOnOpponentSetup,
        /// 8강 서도현 H2 — 자기 밸런스가 온전 임계 이상이면 방어를 유지.
        StayDefensiveWhenBalanceHigh,
        /// 4강 임준철 H1(전반) — 자기 유효 잡기 ≥ +1 이면 어깨 매치기. HeelHookOnGripEdge
        /// 와 조건이 같고 쿨다운 2로 서로를 밀어내 번갈아 나온다(아래 습관 참조 — 정확한
        /// 50/50 무작위 대신 쿨다운 기반 교대로 근사했다, STATUS 판단 필요 목록 참조).
        ShoulderThrowOnGripEdge,
        /// 4강 임준철 H1(후반) — ShoulderThrowOnGripEdge 와 짝. 같은 조건, 발뒤축걸기.
        HeelHookOnGripEdge,
        /// 4강 임준철 H2 — 지도 뒤 2턴 내 견제 발기술로 만회. 진도윤 H2(LegTechAfterShido)
        /// 와 조건은 같지만 진짜 시도(BigThrow)가 아니라 견제기(Poke)라 별도 Id.
        FeintAfterShido,
        /// 동메달전 오재훈 H(전반) — 자기 유효 잡기 ≥ +1 이면 허리후리기. MixedBigThrowB
        /// 와 쿨다운 2로 번갈아 나온다("특출난 축 없이 고루" — 중간 신뢰도).
        MixedBigThrowA,
        /// 동메달전 오재훈 H(후반) — MixedBigThrowA 와 짝. 같은 조건, 밭다리걸기.
        MixedBigThrowB,
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
    /// 습관 하나의 <b>정의</b>. 불변이다 — 상태 필드가 하나도 없다.
    ///
    /// 피해 카운터·폐기·점등은 <see cref="HabitState"/> 가 든다. 그 분리가 규약이 아니라
    /// 타입인 이유: Unity 의 ScriptableObject 는 플레이 모드 중 변경이 에셋에 남는
    /// 물건이라, 정의가 상태를 들면 지난 경기의 폐기가 <b>에러 없이</b> 다음 경기로
    /// 따라붙는다. 정확히 조용히 빠지는 부류다. 여기에 상태가 없으면 그 사고를 내려면
    /// 누군가 새 필드를 만들어야 한다.
    ///
    /// 조건과 행동은 규칙이라 코드에 산다. Game 쪽 SO 가 저작하는 것은 어느 습관을
    /// 켤지 / 발화확률 / ScoutingLevel 뿐이다 — H1 의 "≥ +1" 은 저작 대상이 아니다.
    /// </summary>
    public sealed class HabitDefinition
    {
        public readonly HabitId Id;
        public readonly Func<IHabitContext, bool> Condition;
        public readonly HabitAction Action;
        /// §4 — 0.75 이지 1.0 이 아니다. 1.0 이면 확정 습관이 B 를 결정론으로 만들고
        /// 경기가 조회표가 된다. 25% 의 빗나감이 알려진 습관을 계속 긴장시킨다.
        public readonly float FireProbability;
        /// 예비 습관은 점등되기 전엔 사다리에 오르지 않는다 (§4.2).
        public readonly bool IsReserve;

        /// <summary>
        /// §4.1 — 발화 사이 최소 턴 간격. 0 이면 제한 없음.
        ///
        /// 매 턴 나오는 신호는 읽을 값이 없다. 실측에서 H1 이 27교환에 11번 떴는데,
        /// 그건 습관이 아니라 강박이고 경기를 정규시간의 1/4 만에 끝냈다. 쿨다운 중인
        /// 습관은 <b>건너뛰고 사다리 다음 칸으로 간다</b> — 배타성은 그대로다.
        /// </summary>
        public readonly int MinTurnsBetweenFirings;

        public HabitDefinition(HabitId id, Func<IHabitContext, bool> condition, HabitAction action,
                               float fireProbability = 0.75f, bool isReserve = false,
                               int minTurnsBetweenFirings = 0)
        {
            Id = id;
            Condition = condition;
            Action = action;
            FireProbability = fireProbability;
            IsReserve = isReserve;
            MinTurnsBetweenFirings = minTurnsBetweenFirings;
        }
    }
}
