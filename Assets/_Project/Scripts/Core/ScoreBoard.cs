namespace CounterTime.Core
{
    /// MATCH_RULES §1, 2025 IJF SOR.
    ///
    /// Yuko is a bare counter and has no promotion path anywhere in this class —
    /// that is the structural guarantee that 10 yuko still lose to 1 waza-ari.
    public sealed class ScoreBoard
    {
        readonly MatchRulesConfig _cfg;
        readonly int[] _yuko = new int[2];
        readonly int[] _wazaAri = new int[2];
        readonly int[] _shido = new int[2];
        readonly bool[] _ippon = new bool[2];

        public ScoreBoard(MatchRulesConfig cfg) => _cfg = cfg;

        public bool InGoldenScore { get; private set; }
        public bool IsFinished { get; private set; }
        public FighterId? Winner { get; private set; }

        public int Yuko(FighterId who) => _yuko[(int)who];
        public int WazaAri(FighterId who) => _wazaAri[(int)who];
        public int Shido(FighterId who) => _shido[(int)who];
        public bool HasIppon(FighterId who) => _ippon[(int)who];

        public void EnterGoldenScore() => InGoldenScore = true;

        public void Award(FighterId who, Judgement j)
        {
            if (IsFinished || j == Judgement.None) return;
            int i = (int)who;

            switch (j)
            {
                case Judgement.Yuko:
                    _yuko[i]++;
                    break;
                case Judgement.WazaAri:
                    _wazaAri[i]++;
                    if (_wazaAri[i] >= 2) _ippon[i] = true;   // awasete-ippon
                    break;
                case Judgement.Ippon:
                    _ippon[i] = true;
                    break;
            }

            // §1 golden score: the first score of ANY kind ends it, yuko included.
            if (_ippon[i] || InGoldenScore) Finish(who);
        }

        /// §1 — shido never awards points; the third one loses the match outright.
        public void AddShido(FighterId who)
        {
            if (IsFinished) return;
            int i = (int)who;
            _shido[i]++;
            if (_shido[i] >= _cfg.ShidoLimit) Finish(Opp(who));   // hansoku-make
        }

        /// §1 priority: ippon > waza-ari count > yuko count > null (⇒ golden score).
        public FighterId? WinnerAtTimeUp()
        {
            if (Winner.HasValue) return Winner;
            if (_ippon[0] != _ippon[1]) return _ippon[0] ? FighterId.A : FighterId.B;
            if (_wazaAri[0] != _wazaAri[1]) return _wazaAri[0] > _wazaAri[1] ? FighterId.A : FighterId.B;
            if (_yuko[0] != _yuko[1]) return _yuko[0] > _yuko[1] ? FighterId.A : FighterId.B;
            return null;
        }

        void Finish(FighterId winner)
        {
            IsFinished = true;
            Winner = winner;
        }

        static FighterId Opp(FighterId f) => f == FighterId.A ? FighterId.B : FighterId.A;
    }
}
