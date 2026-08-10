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

    /// §9.1/§9.2 — osaekomi entry. `Adv` and `EscapesNeeded` are frozen at entry;
    /// nothing recomputes them mid-hold.
    public sealed class OsaekomiEntered : MatchEvent
    {
        public readonly FighterId Holder;
        public readonly int Adv;
        public readonly int EscapesNeeded;

        public OsaekomiEntered(FighterId holder, int adv, int escapesNeeded)
        {
            Holder = holder; Adv = adv; EscapesNeeded = escapesNeeded;
        }
    }

    /// §9.3 — a hold duration threshold (5/10/20s) was crossed. Distinct from
    /// JudgementScored: an osaekomi score is not derived from the §4 formula, so
    /// Sum/Terms/IsCounter would not mean anything here.
    public sealed class OsaekomiScoreAwarded : MatchEvent
    {
        public readonly FighterId Holder;
        public readonly Judgement Judgement;
        public readonly int HeldSeconds;

        public OsaekomiScoreAwarded(FighterId holder, Judgement judgement, int heldSeconds)
        {
            Holder = holder; Judgement = judgement; HeldSeconds = heldSeconds;
        }
    }

    /// §9.3 — one escape exchange resolved. `EscapeCredited` is true on 어긋남
    /// (the cross pairs); false on 맞물림 (the hold continues).
    public sealed class OsaekomiEscapeResolved : MatchEvent
    {
        public readonly FighterId Defender;
        public readonly OsaekomiDefenderChoice DefenderChoice;
        public readonly OsaekomiHolderChoice HolderChoice;
        public readonly bool EscapeCredited;
        public readonly int EscapesSoFar;

        public OsaekomiEscapeResolved(FighterId defender, OsaekomiDefenderChoice defenderChoice,
                                      OsaekomiHolderChoice holderChoice, bool escapeCredited, int escapesSoFar)
        {
            Defender = defender; DefenderChoice = defenderChoice; HolderChoice = holderChoice;
            EscapeCredited = escapeCredited; EscapesSoFar = escapesSoFar;
        }
    }

    /// §9.4 — the hold broke via escape (escapesNeeded reached). NOT emitted when
    /// the hold ends because an osaekomi ippon was scored — that ends the match via
    /// MatchEnded instead, and there is nothing left to "escape" from.
    public sealed class OsaekomiEnded : MatchEvent
    {
        public readonly FighterId Defender;
        public OsaekomiEnded(FighterId defender) => Defender = defender;
    }

    /// §4.4 [초안] — a kuzushi hand action resolved (gate passed). Its effect (next
    /// exchange's BigThrow keeps its TechniqueClass instead of downgrading to Sloppy)
    /// is silent — this event exists so a HUD can show the setup landed.
    public sealed class KuzushiDeclared : MatchEvent
    {
        public readonly FighterId Who;
        public KuzushiDeclared(FighterId who) => Who = who;
    }

    public sealed class MatchEnded : MatchEvent
    {
        public readonly FighterId? Winner;
        public MatchEnded(FighterId? winner) => Winner = winner;
    }
}
