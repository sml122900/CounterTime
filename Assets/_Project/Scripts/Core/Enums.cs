namespace CounterTime.Core
{
    public enum FighterId { A, B }
    public enum Hand { None, Left, Right }
    public enum HandTarget { None, RightSleeve, LeftSleeve, LeftLapel, RightLapel, Evade,
                             TwistGrip /*틀어잡기*/, BackGrab /*등싸잡기*/, Tear /*뜯기*/,
                             MaintainGrip /*잡기 유지*/, Setup /*흔들기, §4.4 초안*/ }
    public enum CoreStance { Aggressive, Neutral, Defensive }
    public enum LegAction  { Attack, Neutral, Defend, CounterReady }
    public enum GripKind   { None, Normal, Dominant }          // value 0 / 1 / 2
    public enum GripLock   { Free, Held, Locked }
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
