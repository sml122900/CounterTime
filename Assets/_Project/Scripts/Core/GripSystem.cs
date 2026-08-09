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

        static int Ix(Hand h) => (int)h - 1;
        static FighterId Opp(FighterId f) => f == FighterId.A ? FighterId.B : FighterId.A;

        /// `ownHand` takes a grip of `kind`. `opponentHand` is the sleeve being held,
        /// or Hand.None for a lapel/back grip (§3.2 tracks lock state on hands only).
        public void Grab(FighterId who, Hand ownHand, Hand opponentHand, GripKind kind)
        {
            _holding[(int)who, Ix(ownHand)] = kind;
            _target[(int)who, Ix(ownHand)] = opponentHand;
            if (opponentHand != Hand.None && _lockOn[(int)Opp(who), Ix(opponentHand)] == GripLock.Free)
                _lockOn[(int)Opp(who), Ix(opponentHand)] = GripLock.Held;
        }

        /// §3.1 — capped(my holds) − capped(their holds), clamped to ±2.
        public int GripScore(FighterId who)
        {
            int mine = Math.Min(2, Value(who));
            int theirs = Math.Min(2, Value(Opp(who)));
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

        /// §2 — 틀어잡기/등싸잡기 need the other hand to already hold something.
        public bool CanUseConversion(FighterId who) => Value(who) > 0;

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
        }

        int Value(FighterId who) =>
            (int)_holding[(int)who, Ix(Hand.Left)] + (int)_holding[(int)who, Ix(Hand.Right)];

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
