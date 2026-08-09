using System;
using System.Text.RegularExpressions;
using CounterTime.Core;
using CounterTime.Core.Scenario;
using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 시나리오 노드 → 엔진 입력. 노드 본문의 <c>[판정식]</c> 줄에서 공격자·읽기 값·기술급만
    /// 뽑아 Core 의 <see cref="ExchangeScript"/> 를 조립한다.
    ///
    /// 판정은 여기서 절대 하지 않는다 — 합과 등급은 Core 의 JudgementCalculator 한 곳에서만 난다.
    /// 잡기·체력·수비자세 항도 여기서 만들지 않는다. 전부 엔진 상태에서 나온다.
    ///
    /// §2 카운터 준비 게이트도 여기서 검사하지 않는다. MatchEngine 이 이미 강제하고 있고,
    /// 두 곳에서 검사하면 나중에 한 곳만 고치는 사고가 난다.
    ///
    /// Core 의 <see cref="ExchangeScript"/> 는 sealed 라 상속할 수 없다 — 그래서 이 클래스는
    /// 파생이 아니라 조립기다. JSON·정규식 파싱이 들어가므로 Core 가 아닌 Game 에 있다.
    /// </summary>
    public sealed class ScenarioExchangeScript
    {
        // "[판정식] …" / "[판정식/A] …" / "[판정식/B] …" — 접미사가 없으면 공격자는 A.
        static readonly Regex FormulaLine = new Regex(@"\[판정식(?:/(?<who>[AB]))?\](?<terms>[^\n]*)");
        // 읽기 항: 수싸움0 / 수싸움+2 / 완전간파+3 / 읽힘-3 — 숫자가 곧 ReadLevel 값이다.
        static readonly Regex ReadTerm = new Regex(@"(?:완전간파|읽힘|수싸움)\s*(?<v>[+-]?\d+)");
        // 기술급 항: 기술+1 / 기술급+2 / 시그니처+2 — 숫자가 곧 TechniqueClass 값이다.
        static readonly Regex TechTerm = new Regex(@"(?:시그니처|기술급|기술)\s*(?<v>[+-]?\d+)");

        readonly MatchRulesConfig _cfg;

        public ScenarioExchangeScript(MatchRulesConfig cfg)
        {
            _cfg = cfg;
        }

        /// <summary>
        /// 노드에 <c>[판정식]</c> 줄이 없으면 이번 교환에는 기술이 없다 — 잡기싸움 턴(§2 5초).
        /// 있으면 기술 교환(§2 3초)으로 조립한다.
        /// </summary>
        public ExchangeScript For(ScenarioNode node)
        {
            var line = node == null ? null : node.content;
            var m = string.IsNullOrEmpty(line) ? null : FormulaLine.Match(line);
            if (m == null || !m.Success)
                return new ExchangeScript { DurationA = _cfg.GripTurnSec, DurationB = _cfg.GripTurnSec };

            string terms = m.Groups["terms"].Value;
            return new ExchangeScript
            {
                Attacker = m.Groups["who"].Value == "B" ? FighterId.B : FighterId.A,
                Read = ReadOf(terms, node.id),
                Technique = new TechniqueSpec(node.id, ActionCost.BigThrow, ClassOf(terms, node.id)),
                DurationA = _cfg.ExchangeSec,
                DurationB = _cfg.ExchangeSec,
            };
        }

        static ReadLevel ReadOf(string terms, string nodeId)
        {
            var m = ReadTerm.Match(terms);
            if (!m.Success) return ReadLevel.Neutral;
            int v = int.Parse(m.Groups["v"].Value);
            // 사다리 밖 값은 문서 오타다. 조용히 캐스팅하면 대조에서 영영 안 보인다.
            if (!Enum.IsDefined(typeof(ReadLevel), v))
                Debug.LogWarning($"{nodeId}: 수싸움 {v} 는 ReadLevel 사다리(-3/0/+2/+3) 밖이다 — 문서 확인 필요.");
            return (ReadLevel)v;
        }

        static TechniqueClass ClassOf(string terms, string nodeId)
        {
            var m = TechTerm.Match(terms);
            if (!m.Success) return TechniqueClass.Standard;
            int v = int.Parse(m.Groups["v"].Value);
            if (!Enum.IsDefined(typeof(TechniqueClass), v))
                Debug.LogWarning($"{nodeId}: 기술급 {v} 는 0/1/2 밖이다 — 문서 확인 필요.");
            return (TechniqueClass)v;
        }
    }
}
