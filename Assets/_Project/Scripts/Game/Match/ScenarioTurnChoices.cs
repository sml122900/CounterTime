using CounterTime.Core;
using CounterTime.Core.Scenario;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 노드 → 한 선수의 턴 커밋(§2: 손 하나 + 코어 + 하체).
    ///
    /// 노드가 <b>밝힌 것만</b> 읽는다. 산문에서 추측하지 않는다 — 안 밝힌 항목은 중립이고,
    /// 행위자를 못 밝히면 아무 선수의 커밋도 바뀌지 않는다. 잘못 추측한 커밋은 잡기 값을
    /// 조용히 틀리게 만들고, 그 오차가 5단계 대조에서 엔진 탓으로 보이게 된다.
    /// </summary>
    public static class ScenarioTurnChoices
    {
        public static bool TryParse(ScenarioNode node, out FighterId who, out TurnChoice choice)
        {
            who = FighterId.A;
            choice = default;

            string t = node == null ? null : node.title;
            if (string.IsNullOrEmpty(t)) return false;

            // 제목이 행위자를 이름으로 밝히고, 안 밝히면 테두리 색이 밝힌다(파랑 A / 빨강 B).
            if (t.StartsWith("A:")) who = FighterId.A;
            else if (t.StartsWith("B:")) who = FighterId.B;
            else if (node.Kind == NodeKind.ChoiceA) who = FighterId.A;
            else if (node.Kind == NodeKind.ActionB) who = FighterId.B;
            else return false;

            // §2 극단적 방어 = 세 축 전부 수비. 노드가 이름으로 부르므로 이름으로 읽는다.
            if (t.Contains("극단적 방어"))
            {
                choice = new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend);
                return true;
            }

            var hand = HandOf(t);
            var own = OwnHandOf(t);

            // 뜯기는 '어느 손을' 이 본질인데 제목이 자주 생략한다 (m20 "① 즉시 뜯기").
            // 그 경우에만 본문을 본다 — m20 본문이 "잡힌 왼손을 비틀어 뺀다" 로 밝힌다.
            // 본문 전체를 항상 읽으면 산문 속 부위 언급까지 손 목표로 잡혀서 과탐지된다.
            if (hand == HandTarget.Tear && !NamesAHand(t))
                own = OwnHandOf(node.content ?? "");

            choice = new TurnChoice(hand, CoreOf(t), LegsOf(t), own);
            return true;
        }

        static bool NamesAHand(string t) => t.Contains("왼손") || t.Contains("오른손");

        /// 구체적인 이름이 먼저다 — 틀어잡기가 '잡기 유지'보다 앞서야 한다.
        static HandTarget HandOf(string t) =>
              t.Contains("틀어잡기")  ? HandTarget.TwistGrip
            : t.Contains("등싸잡기")  ? HandTarget.BackGrab
            : t.Contains("뜯기")      ? HandTarget.Tear
            : t.Contains("잡기 유지") ? HandTarget.MaintainGrip
            : t.Contains("회피")      ? HandTarget.Evade
            // 포획 = 상대 손을 낚아채는 것. "A 왼손 포획" 은 그 소매를 잡는 것이다.
            : t.Contains("오른소매") || (t.Contains("포획") && t.Contains("오른손")) ? HandTarget.RightSleeve
            : t.Contains("왼소매")   || (t.Contains("포획") && t.Contains("왼손"))   ? HandTarget.LeftSleeve
            : t.Contains("왼가슴") || t.Contains("안깃") ? HandTarget.LeftLapel
            : t.Contains("오른가슴")  ? HandTarget.RightLapel
            : HandTarget.None;

        static Hand OwnHandOf(string t) =>
              // "A 왼손 포획" 의 왼손은 잡히는 손이지 잡는 손이 아니다. 어느 손으로
              // 낚아챘는지는 노드가 안 밝히므로 기본값을 쓴다.
              t.Contains("포획") ? Hand.Right
            : t.Contains("왼손")   ? Hand.Left
            : t.Contains("오른손") ? Hand.Right
            : Hand.Right;

        static CoreStance CoreOf(string t) =>
              t.Contains("코어 공격") ? CoreStance.Aggressive
            : t.Contains("코어 수비") ? CoreStance.Defensive
            : CoreStance.Neutral;

        static LegAction LegsOf(string t) =>
              t.Contains("카운터 준비") || t.Contains("하체 카운터")   ? LegAction.CounterReady
            : t.Contains("하체 발기술") || t.Contains("하체 공격")     ? LegAction.Attack
            : t.Contains("하체 수비")                                  ? LegAction.Defend
            : LegAction.Neutral;
    }
}
