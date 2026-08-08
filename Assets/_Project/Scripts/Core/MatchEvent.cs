namespace CounterTime.Core
{
    /// Immutable outward signals. Core never presents anything — CounterTime.Game
    /// maps these to 연출/HUD.
    public abstract class MatchEvent { }

    /// §7 (patched) — the grip HUD is a hard requirement, so the body part being held
    /// is carried explicitly here rather than left implicit in the animation.
    public sealed class GripChanged : MatchEvent
    {
        public readonly FighterId Who;
        public readonly Hand OwnHand;
        public readonly HandTarget Part;
        public readonly GripKind Kind;

        public GripChanged(FighterId who, Hand ownHand, HandTarget part, GripKind kind)
        {
            Who = who; OwnHand = ownHand; Part = part; Kind = kind;
        }
    }

    public sealed class TearResolved : MatchEvent
    {
        public readonly FighterId Who;
        public readonly Hand Hand;
        public readonly TearOutcome Outcome;

        public TearResolved(FighterId who, Hand hand, TearOutcome outcome)
        {
            Who = who; Hand = hand; Outcome = outcome;
        }
    }

    /// §4.2 — emitted when a CounterReady declaration actually bit. `Who` is the
    /// fighter who declared it; the read that moved is the ATTACKER's, downward.
    public sealed class CounterReadyApplied : MatchEvent
    {
        public readonly FighterId Who;
        public readonly ReadLevel AttackerReadBefore;
        public readonly ReadLevel AttackerReadAfter;

        public CounterReadyApplied(FighterId who, ReadLevel before, ReadLevel after)
        {
            Who = who; AttackerReadBefore = before; AttackerReadAfter = after;
        }
    }

    public sealed class JudgementScored : MatchEvent
    {
        public readonly FighterId ScoredBy;
        public readonly Judgement Judgement;
        public readonly int Sum;
        public readonly bool IsCounter;
        /// The five §4 terms in order, exposed so the coach panel can explain a result.
        public readonly int[] Terms;

        public JudgementScored(FighterId scoredBy, Judgement judgement, int sum,
                               bool isCounter, int[] terms)
        {
            ScoredBy = scoredBy; Judgement = judgement; Sum = sum;
            IsCounter = isCounter; Terms = terms;
        }
    }

    public sealed class ShidoGiven : MatchEvent
    {
        public readonly FighterId Who;
        public readonly int Count;
        public readonly bool FromFeint;

        public ShidoGiven(FighterId who, int count, bool fromFeint)
        {
            Who = who; Count = count; FromFeint = fromFeint;
        }
    }

    public sealed class RefereeTellChanged : MatchEvent
    {
        public readonly FighterId Who;
        public readonly RefereeTell Tell;

        public RefereeTellChanged(FighterId who, RefereeTell tell)
        {
            Who = who; Tell = tell;
        }
    }

    public sealed class MatteCalled : MatchEvent { }

    public sealed class MatchEnded : MatchEvent
    {
        public readonly FighterId? Winner;
        public MatchEnded(FighterId? winner) => Winner = winner;
    }
}
