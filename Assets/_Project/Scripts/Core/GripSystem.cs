using System;

namespace CounterTime.Core
{
    /// MATCH_RULES §3. Owns both fighters' hand connections.
    ///
    /// Two facts are tracked per fighter per hand:
    ///   Holding    — what MY hand grips on the opponent (feeds the grip score)
    ///   LockOn     — the state of the OPPONENT's grip on MY hand (feeds the lock cap)
    /// They are different directions of the same fight and must not be collapsed:
    /// §3.1 scores what you hold, §3.2 locks what is held on you.
    public sealed class GripSystem
    {
        const int Hands = 2;   // index by Hand.Left-1 / Hand.Right-1

        readonly GripKind[,] _holding = new GripKind[2, Hands];
        readonly Hand[,] _target = new Hand[2, Hands];      // which opponent hand my hand grips
        readonly GripLock[,] _lockOn = new GripLock[2, Hands];
        /// §2 v4.2 — 견고화(solidify): a hand that already holds something can be
        /// reinforced for +1. Pre-v4.2 code never sets this (only the new
        /// GripRevisionGate-gated flow calls <see cref="Solidify"/>), so it stays
        /// all-false and Value() below is a no-op change for every existing path.
        readonly bool[,] _solid = new bool[2, Hands];
        /// §2 v4.2 — Value()'s pre-clamp per-side cap. Old cap was 2 (== Dominant,
        /// the old system's single-hand maximum). New per-hand maximum is
        /// BackGrip(3) + solid(+1) = 4, so the cap is raised to match the same
        /// "one hand at its ceiling already saturates this side" rule. This does
        /// NOT change GripScore's output for any pre-v4.2 case: the pre-v4.2 max
        /// Value() was 2 (one Dominant, or two Normal), so raising the cap only
        /// ever affects totals that could not occur before v4.2 — verified by the
        /// existing GripSystemTests still passing unmodified (SideTotal_CapsAtTwo:
        /// capped-mine goes from min(2,4)=2 to min(4,4)=4, but the final
        /// Clamp(mine-theirs,-2,2) already saturates diff=4 down to 2 either way).
        const int SideCap = 4;

        static int Ix(Hand h) => (int)h - 1;
        static FighterId Opp(FighterId f) => f == FighterId.A ? FighterId.B : FighterId.A;
        static Hand Other(Hand h) => h == Hand.Left ? Hand.Right : Hand.Left;

        /// `ownHand` takes a grip of `kind`. `opponentHand` is the sleeve being held,
        /// or Hand.None for a lapel/back grip (§3.2 tracks lock state on hands only).
        public void Grab(FighterId who, Hand ownHand, Hand opponentHand, GripKind kind)
        {
            _holding[(int)who, Ix(ownHand)] = kind;
            _target[(int)who, Ix(ownHand)] = opponentHand;
            // §2 v4.2 — a fresh grab replaces whatever this hand held before, so any
            // 견고화 bonus it had earned does not carry over. Pre-v4.2 callers never
            // set _solid true in the first place, so this is a no-op for them.
            _solid[(int)who, Ix(ownHand)] = false;
            if (opponentHand != Hand.None && _lockOn[(int)Opp(who), Ix(opponentHand)] == GripLock.Free)
                _lockOn[(int)Opp(who), Ix(opponentHand)] = GripLock.Held;
        }

        /// §3.1 — capped(my holds) − capped(their holds), clamped to ±2.
        public int GripScore(FighterId who)
        {
            int mine = Math.Min(SideCap, Value(who));
            int theirs = Math.Min(SideCap, Value(Opp(who)));
            return Math.Clamp(mine - theirs, -2, 2);
        }

        /// §3.2 (patched) — if EITHER of my hands is Locked, cap at −1.
        public int EffectiveGrip(FighterId who)
        {
            int raw = GripScore(who);
            return AnyHandLocked(who) ? Math.Min(raw, -1) : raw;
        }

        public bool AnyHandLocked(FighterId who) =>
            _lockOn[(int)who, Ix(Hand.Left)] == GripLock.Locked ||
            _lockOn[(int)who, Ix(Hand.Right)] == GripLock.Locked;

        public GripLock LockState(FighterId who, Hand hand) => _lockOn[(int)who, Ix(hand)];

        /// §7 — which of the opponent's hands is currently gripping `victim`'s `hand`,
        /// or None. The engine reads this BEFORE a tear resolves so it can emit an
        /// explicit "this hold ended" event: the HUD may not infer a release from the
        /// absence of an event, and it cannot see inside GripSystem.
        public Hand HolderHandGripping(FighterId victim, Hand hand)
        {
            FighterId holder = Opp(victim);
            foreach (var h in new[] { Hand.Left, Hand.Right })
                if (_target[(int)holder, Ix(h)] == hand && _holding[(int)holder, Ix(h)] != GripKind.None)
                    return h;
            return Hand.None;
        }

        /// §2 — 틀어잡기/등싸잡기 need the OTHER hand to already hold something; the
        /// hand performing the conversion cannot supply its own prerequisite (a hand
        /// cannot self-convert without the other hand holding a grip).
        public bool CanUseConversion(FighterId who, Hand convertingHand) =>
            _holding[(int)who, Ix(Other(convertingHand))] != GripKind.None;

        /// §4.4 [초안] — 흔들기(setup) 게이트: 자기 그립이 하나라도 있어야 선언 가능.
        public bool HasAnyGrip(FighterId who) => Value(who) > 0;

        /// §2 v4.2 — 견고화: reinforce a hand that already holds something for +1 to
        /// Value(). No-op (returns false) if the hand holds nothing to reinforce.
        public bool Solidify(FighterId who, Hand hand)
        {
            if (_holding[(int)who, Ix(hand)] == GripKind.None) return false;
            _solid[(int)who, Ix(hand)] = true;
            return true;
        }

        public bool IsSolid(FighterId who, Hand hand) => _solid[(int)who, Ix(hand)];

        /// §2 v4.2 — does this specific hand currently hold anything (any tier)?
        /// Distinct from <see cref="HasAnyGrip"/>, which asks "either hand" — the
        /// v4.2 flow needs the per-hand answer ("이미 잡은 손 선택").
        public bool Holds(FighterId who, Hand hand) => _holding[(int)who, Ix(hand)] != GripKind.None;

        /// §2 v4.2 — true if `hand` currently holds a 교차잡기(cross-side) grip: my
        /// hand and the targeted side are the same (Hand.Left/Right doubles as both
        /// "which of my hands" and "which side was targeted" — see MatchEngine's
        /// TargetSideOf). Only meaningful for grabs made via the v4.2 resolver, which
        /// is the only caller that ever passes a real (non-None) opponentHand for
        /// lapel/back-grip targets — pre-v4.2 lapel/back grabs always pass Hand.None
        /// (SleeveToHand), so this is always false for them, which is correct: the
        /// mirror/cross distinction did not exist before v4.2.
        public bool IsCrossOrientation(FighterId who, Hand hand) =>
            _holding[(int)who, Ix(hand)] != GripKind.None && _target[(int)who, Ix(hand)] == hand;

        /// §3.2 — `who` tries to tear the opponent's grip off their own `hand`.
        /// Pure read-based in v1: no RNG.
        public TearOutcome ResolveTear(FighterId who, Hand hand, bool opponentChoseMaintain)
        {
            ref GripLock state = ref _lockOn[(int)who, Ix(hand)];
            if (state == GripLock.Locked) return TearOutcome.Impossible;
            if (state == GripLock.Free) return TearOutcome.Freed;   // nothing to tear

            if (opponentChoseMaintain)
            {
                state = GripLock.Locked;
                return TearOutcome.Locked;
            }

            state = GripLock.Free;
            ReleaseGripOn(who, hand);
            return TearOutcome.Freed;
        }

        /// §3.2 — matte resets every connection.
        public void OnMatte()
        {
            Array.Clear(_holding, 0, _holding.Length);
            Array.Clear(_target, 0, _target.Length);
            Array.Clear(_lockOn, 0, _lockOn.Length);
            Array.Clear(_solid, 0, _solid.Length);
        }

        int Value(FighterId who) =>
            (int)_holding[(int)who, Ix(Hand.Left)] + (int)_holding[(int)who, Ix(Hand.Right)]
            + (_solid[(int)who, Ix(Hand.Left)] ? 1 : 0) + (_solid[(int)who, Ix(Hand.Right)] ? 1 : 0);

        /// Drop whichever opponent hand was gripping `victim`'s `hand`.
        void ReleaseGripOn(FighterId victim, Hand hand)
        {
            FighterId holder = Opp(victim);
            foreach (var h in new[] { Hand.Left, Hand.Right })
            {
                if (_target[(int)holder, Ix(h)] != hand) continue;
                _holding[(int)holder, Ix(h)] = GripKind.None;
                _target[(int)holder, Ix(h)] = Hand.None;
            }
        }
    }
}
