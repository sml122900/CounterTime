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

        /// <summary>
        /// §2 — 상대 지정 넷 + 회피는 언제나 열려 있다. 틀어잡기/등싸잡기는 다른 손이
        /// 이미 소매·깃을 잡고 있을 때만(§2 "unlocked conversions"). 흔들기는 [초안]
        /// §4.4 대로 자기 그립이 있고 플래그가 켜져 있을 때만.
        ///
        /// 뜯기·잡기 유지는 §2 표 자체에는 없고 §3.2 서술로만 나온다 — 여기 게이트는
        /// 그 서술("당신의 뜯기 vs 상대의 그 턴 선택", "상대가 잡기 유지를 고르면")에서
        /// 추론한 것이라 문서가 표로 못박아둔 사실이 아니다. 사람 확인 필요
        /// (docs/overnight-2026-08-11.md 판단 대기 참조) — 그래서 "구현 말고 보고"
        /// 대상이지만, 이 두 선택지 없이는 §3.2 전체가 UI 에서 시험조차 못 되므로
        /// 최선의 추론으로 일단 열어 두고 보고했다.
        /// </summary>
        public IReadOnlyList<HandTarget> HandOptions()
        {
            var options = new List<HandTarget>
            {
                HandTarget.RightSleeve, HandTarget.LeftSleeve,
                HandTarget.LeftLapel, HandTarget.RightLapel, HandTarget.Evade,
            };

            if (_engine.Grip.CanUseConversion(_me))
            {
                options.Add(HandTarget.TwistGrip);
                options.Add(HandTarget.BackGrab);
            }

            if (AnyHandHeldOnMe()) options.Add(HandTarget.Tear);
            if (_engine.Grip.HasAnyGrip(_me)) options.Add(HandTarget.MaintainGrip);

            if (_cfg.SetupGateEnabled && _engine.Grip.HasAnyGrip(_me))
                options.Add(HandTarget.Setup);

            return options;
        }

        bool AnyHandHeldOnMe() =>
            _engine.Grip.LockState(_me, Hand.Left) != GripLock.Free ||
            _engine.Grip.LockState(_me, Hand.Right) != GripLock.Free;

        public TurnChoice Build(HandTarget hand, CoreStance core, LegAction legs, Hand ownHand = Hand.Right) =>
            new TurnChoice(hand, core, legs, ownHand);
    }
}
