using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// §2b v4.3 — B 의 최소 뜯기 대응. <see cref="SetupPrecedingCommitSource"/> 와 같은
    /// 데코레이터 스타일이지만 <see cref="IBrainContext"/> 는 안 거친다 — 그 인터페이스
    /// 파일 자체가 "멤버 추가는 넣지 말고 보고할 것"이라 명시해서, 대신
    /// <see cref="MatchBrainContext"/> 가 이미 하는 것과 같은 패턴으로 <see cref="MatchEngine"/>
    /// 을 직접 받아 <c>GripSystem</c> 공개 API 만 읽는다.
    ///
    /// <c>GripRevisionGate</c> 켜졌고 잡힌 부위(소매/가슴깃만 — 틀어잡기는 3차 이월이라
    /// 대상에서 뺀다)가 있으면 ⚙확률(기본 30%)로 감싸인 커밋을 덮어쓰고 기본 뜯기
    /// (<see cref="TearMethod.Basic"/>, 소매=돌려서/가슴깃=한손과 동일 판정)를 시도한다 —
    /// 방법 선택 지능은 3차 이월, 견고면 그냥 실패한다("최소" 대응 그대로).
    /// </summary>
    public sealed class GripTearFallbackCommitSource : ICommitSource
    {
        readonly SetupPrecedingCommitSource _inner;
        readonly MatchEngine _engine;
        readonly FighterId _me;
        readonly bool _enabled;
        readonly float _chance;
        readonly IRandomSource _rng;

        public TechniqueSpec LastTechnique { get; private set; }
        public HabitDecision LastDecision { get; private set; }
        public bool LastUsedFallback { get; private set; }

        public GripTearFallbackCommitSource(SetupPrecedingCommitSource inner, MatchEngine engine,
                                            FighterId me, bool enabled, float chance, IRandomSource rng)
        {
            _inner = inner;
            _engine = engine;
            _me = me;
            _enabled = enabled;
            _chance = chance;
            _rng = rng;
        }

        public bool TryNext(out FighterId who, out TurnChoice choice)
        {
            bool got = _inner.TryNext(out who, out choice);
            LastTechnique = _inner.LastTechnique;
            LastDecision = _inner.LastDecision;
            LastUsedFallback = _inner.LastUsedFallback;
            if (!_enabled || !got) return got;

            var zones = HeldSleeveOrLapelZones();
            if (zones.Count == 0) return got;
            if (_rng.NextFloat() >= _chance) return got;

            // 결정론적 첫 항목(Left 손부터, 소매 우선 — HeldSleeveOrLapelZones 의
            // 고정 순회 순서) — 재현 가능해야 하므로 무작위로 더 안 고른다.
            DefenseZone zone = zones[0];
            Hand nominal = (zone == DefenseZone.LeftSleeve || zone == DefenseZone.LeftLapel)
                ? Hand.Left : Hand.Right;
            choice = new TurnChoice(HandTarget.Tear, choice.Core, choice.Legs, nominal,
                tearZone: zone, tearMethodChoice: TearMethod.Basic);
            LastTechnique = null;
            LastDecision = default;
            LastUsedFallback = false;
            return true;
        }

        /// §2b v4.3 — Core(MatchEngine)의 ZoneOfTierSide/HandHoldingZone과 같은
        /// 유도를 Brain 쪽에 작게 다시 쓴다(이 리포가 seam마다 작은 매핑을 중복해
        /// 두는 기존 관행 — GripTierLabelFor/ResolveGripTarget 등과 동일 이유).
        /// 상대(Opp(_me))의 두 손을 읽어 내(_me)가 뜯을 수 있는 소매/가슴깃 존만
        /// 반환 — 틀어잡기(Upper) 존은 3차 이월이라 제외.
        List<DefenseZone> HeldSleeveOrLapelZones()
        {
            FighterId opp = _me == FighterId.A ? FighterId.B : FighterId.A;
            var result = new List<DefenseZone>();
            foreach (var h in new[] { Hand.Left, Hand.Right })
            {
                var tier = _engine.Grip.Held(opp, h);
                if (tier == GripKind.None) continue;
                bool cross = _engine.Grip.IsCrossOrientation(opp, h);
                Hand side = cross ? h : (h == Hand.Left ? Hand.Right : Hand.Left);
                DefenseZone? zone = (tier, side) switch
                {
                    (GripKind.Normal, Hand.Right) => DefenseZone.RightSleeve,
                    (GripKind.Normal, Hand.Left) => DefenseZone.LeftSleeve,
                    (GripKind.Lapel, Hand.Right) => DefenseZone.RightLapel,
                    (GripKind.Lapel, Hand.Left) => DefenseZone.LeftLapel,
                    _ => null,   // BackGrip(틀어잡기) 제외 — 3차 이월
                };
                if (zone.HasValue) result.Add(zone.Value);
            }
            return result;
        }
    }
}
