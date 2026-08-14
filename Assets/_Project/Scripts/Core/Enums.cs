namespace CounterTime.Core
{
    public enum FighterId { A, B }
    public enum Hand { None, Left, Right }
    public enum HandTarget { None, RightSleeve, LeftSleeve, LeftLapel, RightLapel, Evade,
                             TwistGrip /*틀어잡기*/, BackGrab /*등싸잡기*/, Tear /*뜯기*/,
                             MaintainGrip /*잡기 유지*/, Setup /*흔들기, §4.4 초안*/,
                             Solidify /*견고화, §2 v4.2 — 이미 잡은 손을 강화*/ }
    public enum CoreStance { Aggressive, Neutral, Defensive }
    public enum LegAction  { Attack, Neutral, Defend, CounterReady }
    // value 0/1/2/2/3. Lapel and Dominant share the value 2 on purpose — Dominant is
    // the OLD (pre-v4.2) conversion tier (틀어잡기/등싸잡기 via CanUseConversion) and
    // Lapel is the NEW (§2 v4.2) 가슴깃 tier; they never appear on the same code path
    // (Dominant only from MatchEngine.ResolveGrip's old switch, Lapel only from the
    // v4.2 matrix behind GripRevisionGate) so the numeric coincidence is harmless —
    // GripSystem.Value() only ever sums whichever one a given path actually stores.
    public enum GripKind   { None = 0, Normal = 1, Dominant = 2, Lapel = 2, BackGrip = 3 }
    public enum GripLock   { Free, Held, Locked }
    // §2 v4.2 — the six zones a defending hand can guard (Evade). Left/RightSleeve and
    // Left/RightLapel mirror the grab-target names above; Left/RightUpper are new —
    // they guard against 틀어잡기(TwistGrip/BackGrab), which has no zone concept before
    // v4.2. A hand's selectable subset is {its own-side sleeve, both lapels, its own-side
    // upper} — lapel zones are shared, sleeve/upper are hand-exclusive (task spec, §2).
    public enum DefenseZone { LeftSleeve, RightSleeve, LeftLapel, RightLapel, LeftUpper, RightUpper }
    public enum ReadLevel  { GotRead = -3, Neutral = 0, Read = 2, FullRead = 3 }
    public enum TechniqueClass { Sloppy = 0, Standard = 1, Signature = 2 } // judgment term
    public enum Judgement  { None, Yuko, WazaAri, Ippon }
    public enum RefereeProfile { Strict, Standard, Lenient }
    public enum MatchPhase { Standing, Osaekomi, Matte, GoldenScore, Finished }
    public enum ExchangeDurationRule { Max }   // Sum/Fixed reserved for later tuning
    public enum ActionCost { BigThrow, Poke, Counter, Tear }   // §5.1 cost rows
    public enum DefenseQuality { Clean, Neutral, Pushed }      // §5.2
    public enum TearOutcome { Freed, Locked, Impossible }      // §3.2

    // §9.3 — the defender's two escape attempts and the holder's two responses.
    public enum OsaekomiDefenderChoice { Reversal /*뒤집기*/, Escape /*빠져나가기*/ }
    public enum OsaekomiHolderChoice   { Follow /*따라가기*/, PressIn /*눌러붙기*/ }

    // §B — match-algorithm split. The only technique classification match logic reads.
    public enum AlgoClass { FeintFootsweep /*견제성 발기술*/, BigThrow /*큰 기술*/ }
    // §B — the five standard judo categories. Teaching/acquisition-screen display only;
    // match logic never reads this (§B, confirmed).
    public enum JudoCategory { Hand /*손기술*/, Foot /*발기술*/, Hip /*허리기술*/,
                               Sacrifice /*누우며 메치기*/, Variant /*변칙*/ }
}
