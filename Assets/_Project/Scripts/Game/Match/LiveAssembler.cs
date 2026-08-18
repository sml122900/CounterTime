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

        // ================= §2b v4.3 — 뜯기(GripRevisionGate) =================

        /// §2b v4.3 — 내 어느 존이 지금 상대에게 잡혀 있는지(견고 여부 포함). 틀어잡기
        /// 존도 포함한다 — 목록엔 표시하되 비활성 처리하는 건 호출자(UI) 몫("3차"
        /// 서브게임 범위 밖, 사용자 지시 그대로). MatchEngine의 ZoneOfTierSide/
        /// HandHoldingZone과 같은 유도를 여기 Game 쪽에 작게 다시 쓴다 — 이 리포가
        /// seam마다 작은 매핑을 중복해 두는 기존 관행(GripTierLabelFor/
        /// ResolveGripTarget 등과 동일 이유).
        public IReadOnlyList<(DefenseZone Zone, bool Solid)> TearableZones()
        {
            FighterId opp = _me == FighterId.A ? FighterId.B : FighterId.A;
            var result = new List<(DefenseZone, bool)>();
            foreach (var h in new[] { Hand.Left, Hand.Right })
            {
                var tier = _engine.Grip.Held(opp, h);
                if (tier == GripKind.None) continue;
                bool cross = _engine.Grip.IsCrossOrientation(opp, h);
                Hand side = cross ? h : (h == Hand.Left ? Hand.Right : Hand.Left);
                DefenseZone? zone = ZoneOfTierSide(tier, side);
                if (zone.HasValue) result.Add((zone.Value, _engine.Grip.IsSolid(opp, h)));
            }
            return result;
        }

        static DefenseZone? ZoneOfTierSide(GripKind tier, Hand side) => (tier, side) switch
        {
            (GripKind.Normal, Hand.Right) => DefenseZone.RightSleeve,
            (GripKind.Normal, Hand.Left) => DefenseZone.LeftSleeve,
            (GripKind.Lapel, Hand.Right) => DefenseZone.RightLapel,
            (GripKind.Lapel, Hand.Left) => DefenseZone.LeftLapel,
            (GripKind.BackGrip, Hand.Right) => DefenseZone.RightUpper,
            (GripKind.BackGrip, Hand.Left) => DefenseZone.LeftUpper,
            _ => null,
        };

        static bool IsSleeveZone(DefenseZone z) => z == DefenseZone.LeftSleeve || z == DefenseZone.RightSleeve;

        /// §2b v4.3 소결정 (a)/(b) — zone 타입과 현재 내 손 상태로 지금 고를 수 있는
        /// 방법만 낸다. 소매: Basic·Forceful은 항상, Assisted는 반대 손 자유일 때만.
        /// 가슴깃: Basic은 항상, Forceful·Assisted는 양손 자유일 때만. 틀어잡기 존은
        /// 호출자가 애초에 이 메서드를 안 부르는 게 맞다(3차 서브게임 범위 밖) —
        /// 방어적으로 Basic 하나만 반환한다.
        public IReadOnlyList<TearMethod> TearMethodsFor(DefenseZone zone)
        {
            var methods = new List<TearMethod> { TearMethod.Basic };
            if (IsSleeveZone(zone))
            {
                methods.Add(TearMethod.Forceful);
                Hand nominal = zone == DefenseZone.LeftSleeve ? Hand.Left : Hand.Right;
                Hand other = nominal == Hand.Left ? Hand.Right : Hand.Left;
                if (!_engine.Grip.Holds(_me, other)) methods.Add(TearMethod.Assisted);
            }
            else if (zone == DefenseZone.LeftLapel || zone == DefenseZone.RightLapel)
            {
                bool bothFree = !_engine.Grip.Holds(_me, Hand.Left) && !_engine.Grip.Holds(_me, Hand.Right);
                if (bothFree) { methods.Add(TearMethod.Forceful); methods.Add(TearMethod.Assisted); }
            }
            return methods;
        }

        /// §2b v4.3 [설계 판단] — 소매는 그 존의 손 자신(Basic/Forceful, "돌려서/
        /// 팔꿈치로"는 잡힌 손 자신의 동작) 또는 반대 손(Assisted, "다른 손으로")이
        /// 동작한다. 가슴깃은 손 매핑이 없어 존의 명목상 같은 쪽(RightLapel→Right)
        /// 으로 고정한다 — UI가 "어느 손?"을 따로 안 물어도 되게(사용자 스펙의
        /// 흐름도 부위→방법 2단만 말하지 손 선택 3단은 없다).
        static Hand TearActingHand(DefenseZone zone, TearMethod method)
        {
            Hand nominal = (zone == DefenseZone.LeftSleeve || zone == DefenseZone.LeftLapel
                || zone == DefenseZone.LeftUpper) ? Hand.Left : Hand.Right;
            if (IsSleeveZone(zone) && method == TearMethod.Assisted)
                return nominal == Hand.Left ? Hand.Right : Hand.Left;
            return nominal;
        }

        public TurnChoice BuildTear(DefenseZone zone, TearMethod method, CoreStance core, LegAction legs) =>
            new TurnChoice(HandTarget.Tear, core, legs, TearActingHand(zone, method),
                tearZone: zone, tearMethodChoice: method);
    }
}
