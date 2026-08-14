using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 6단계 — UI 입력을 <see cref="TurnChoice"/> 로 조립하고, 국면에 맞는 유효 손
    /// 선택지를 낸다. UnityEngine 을 안 쓴다 — MonoBehaviour 없이도, 즉 UI 없이도
    /// 가짜 입력을 직접 주입해 테스트할 수 있는 구조다.
    ///
    /// 코어(3)·하체(4)는 §2 표에 게이트가 없다 — 언제나 전부 유효하다. 손은 국면에
    /// 따라 열리고 닫힌다.
    /// </summary>
    public sealed class LiveAssembler
    {
        static readonly CoreStance[] AllCoreOptions =
            { CoreStance.Aggressive, CoreStance.Neutral, CoreStance.Defensive };
        static readonly LegAction[] AllLegOptions =
            { LegAction.Attack, LegAction.Neutral, LegAction.Defend, LegAction.CounterReady };

        /// §H 2b — A's own technique roster (§B "Owner: A", all 8: the 4 견제성 발기술
        /// plus the 4 큰 기술 A can throw). `TechniqueDef` deliberately has no Owner
        /// field (CORE_DESIGN_MatchEngine.md's v4.0 revision left it out) — ownership is
        /// a Game-side authoring fact, not match-engine data, so it lives here instead.
        /// Grouped by AlgoClass (견제성 발기술 first, 큰 기술 second) so a UI that lays
        /// these out in order gets the split for free.
        static readonly string[] ATechniqueIds =
        {
            "모두걸기", "발목받치기", "나오는발차기", "발뒤축걸기",
            "밭다리걸기", "허리후리기", "안오금띄기", "어깨 매치기",
        };

        readonly MatchEngine _engine;
        readonly MatchRulesConfig _cfg;
        readonly FighterId _me;

        public LiveAssembler(MatchEngine engine, MatchRulesConfig cfg, FighterId me)
        {
            _engine = engine;
            _cfg = cfg;
            _me = me;
        }

        public IReadOnlyList<CoreStance> CoreOptions() => AllCoreOptions;
        public IReadOnlyList<LegAction> LegOptions() => AllLegOptions;

        /// §A.2/§H 2b — always all 8, no in-match gating (none was asked for; stamina
        /// lockout etc. are enforced downstream by Core, same as every other axis here).
        public IReadOnlyList<TechniqueDef> TechniqueOptions()
        {
            var list = new List<TechniqueDef>(ATechniqueIds.Length);
            foreach (var id in ATechniqueIds) list.Add(TechniqueRegistry.Get(id));
            return list;
        }

        /// <summary>
        /// §2 — 상대 지정 넷 + 회피는 언제나 열려 있다. 틀어잡기/등싸잡기는 <paramref
        /// name="ownHand"/> 이 아닌 다른 손이 이미 소매·깃을 잡고 있을 때만(§2
        /// "unlocked conversions", §H 0단계 CanUseConversion 반대 손 버그 수정 반영).
        /// 뜯기는 <paramref name="ownHand"/> 자신이 상대에게 잡혀 있을 때만 — 손이
        /// 둘로 나뉜 이상(§A.1) "아무 손이나 잡혔으면"이 아니라 "이 손이 잡혔으면"이
        /// 맞는 질문이다. 흔들기는 [초안] §4.4 대로 자기 그립이 있고(어느 손이든)
        /// 플래그가 켜져 있을 때만.
        ///
        /// 뜯기·잡기 유지는 §2 표 자체에는 없고 §3.2 서술로만 나온다 — 여기 게이트는
        /// 그 서술("당신의 뜯기 vs 상대의 그 턴 선택", "상대가 잡기 유지를 고르면")에서
        /// 추론한 것이라 문서가 표로 못박아둔 사실이 아니다. 사람 확인 필요
        /// (docs/overnight-2026-08-11.md 판단 대기 참조) — 그래서 "구현 말고 보고"
        /// 대상이지만, 이 두 선택지 없이는 §3.2 전체가 UI 에서 시험조차 못 되므로
        /// 최선의 추론으로 일단 열어 두고 보고했다.
        /// </summary>
        public IReadOnlyList<HandTarget> HandOptions(Hand ownHand)
        {
            var options = new List<HandTarget>
            {
                HandTarget.RightSleeve, HandTarget.LeftSleeve,
                HandTarget.LeftLapel, HandTarget.RightLapel, HandTarget.Evade,
            };

            if (_engine.Grip.CanUseConversion(_me, ownHand))
            {
                options.Add(HandTarget.TwistGrip);
                options.Add(HandTarget.BackGrab);
            }

            if (_engine.Grip.LockState(_me, ownHand) != GripLock.Free) options.Add(HandTarget.Tear);
            if (_engine.Grip.HasAnyGrip(_me)) options.Add(HandTarget.MaintainGrip);

            if (_cfg.BalanceGate && _engine.Grip.HasAnyGrip(_me))
                options.Add(HandTarget.Setup);

            return options;
        }

        public TurnChoice Build(HandTarget hand, CoreStance core, LegAction legs, Hand ownHand = Hand.Right) =>
            new TurnChoice(hand, core, legs, ownHand);

        // ================= §2 v4.2 — 잡기 개편 1차 (GripRevisionGate) =================

        /// §2 v4.2 — top-level hand choice under the gate: 오른손/왼손/회피. Evade is no
        /// longer one value among a per-hand list (§2's old "상대 지정 넷 + 회피" table) —
        /// it is a THIRD sibling to picking a hand at all, since it now commits both
        /// hands to a defended zone at once (§2 "회피: 손마다 1존, 총 2존"), which a
        /// single-hand option list cannot express. Only meaningful when
        /// <see cref="MatchRulesConfig.GripRevisionGate"/> is on — the gate-off flow
        /// keeps using <see cref="HandOptions"/> exactly as before.
        public bool RevisedFlowActive => _cfg.GripRevisionGate;

        /// §2 v4.2 — the six grab targets (3 tiers × 정/교차) plus 견고화 if this
        /// specific hand already holds something ("이미 잡은 손 선택: 견고하게 잡기
        /// 추가" — added to the list, not a replacement, so re-targeting stays
        /// available too). 뜯기 is deliberately absent — 2차 범위(작업 지시 명시).
        /// 흔들기(Setup) rides along unchanged if BalanceGate is separately on, same
        /// gate/label as the pre-v4.2 flow.
        public IReadOnlyList<HandTarget> HandOptionsRevised(Hand ownHand)
        {
            var options = new List<HandTarget>
            {
                HandTarget.RightSleeve, HandTarget.LeftSleeve,
                HandTarget.LeftLapel, HandTarget.RightLapel,
                HandTarget.TwistGrip, HandTarget.BackGrab,
            };
            if (_engine.Grip.Holds(_me, ownHand)) options.Add(HandTarget.Solidify);
            if (_cfg.BalanceGate && _engine.Grip.HasAnyGrip(_me)) options.Add(HandTarget.Setup);
            return options;
        }

        /// §2 v4.2 — the four zones a given hand may guard (§2: sleeve/upper are
        /// hand-exclusive, both lapel zones are shared). Caller is responsible for
        /// "중복 불가" (excluding whatever the OTHER hand already picked) — that is a
        /// UI selection-flow constraint, not something either zone list or
        /// <see cref="BuildEvade"/> enforces on its own.
        public IReadOnlyList<DefenseZone> EvadeZoneOptions(Hand hand) => hand == Hand.Left
            ? new[] { DefenseZone.LeftSleeve, DefenseZone.LeftLapel, DefenseZone.RightLapel, DefenseZone.LeftUpper }
            : new[] { DefenseZone.RightSleeve, DefenseZone.LeftLapel, DefenseZone.RightLapel, DefenseZone.RightUpper };

        public TurnChoice BuildEvade(DefenseZone leftZone, DefenseZone rightZone, CoreStance core, LegAction legs) =>
            new TurnChoice(HandTarget.Evade, core, legs, Hand.Right, leftZone, rightZone);
    }
}
