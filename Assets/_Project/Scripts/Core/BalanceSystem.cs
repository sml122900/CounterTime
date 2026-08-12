using System;

namespace CounterTime.Core
{
    /// MATCH_RULES §S. Balance is the third fighter resource alongside grip (§3) and
    /// stamina (§5) — 0..BalanceMax per fighter. Mirrors StaminaSystem's shape on
    /// purpose (§H step 3 design note: Core holds no Core-stance-selection state, so
    /// Balance belongs at Stamina/Grip's tier, not alongside a UI selection).
    ///
    /// Unlike stamina there is no "0 = special lockout" rule — §S doesn't define one,
    /// so Balance just floors at 0 like any other drainable resource.
    public sealed class BalanceSystem
    {
        readonly MatchRulesConfig _cfg;
        readonly int[] _balance;

        public BalanceSystem(MatchRulesConfig cfg)
        {
            _cfg = cfg;
            _balance = new[] { cfg.BalanceMax, cfg.BalanceMax };
        }

        public int Get(FighterId who) => _balance[(int)who];

        public void Drain(FighterId who, int amount) => Spend(who, amount);

        /// §S — +⚙ on a successful defense.
        public void RecoverDefenseSuccess(FighterId who) => Gain(who, _cfg.BalanceRecoverDefense);

        /// §S (v4.1, RESOLVED §8) — 그쳐(matte, §4.3)와 굳히기 탈출(§9.4) 둘 다 부분
        /// 회복만 준다 — 체력의 <see cref="StaminaSystem.RecoverMatte"/> 와 같은 성격,
        /// 별도 트랙. 완전 리셋(100)은 <see cref="ResetBetweenMatches"/> 뿐이다.
        public void RecoverMattePartial(FighterId who) => Gain(who, _cfg.BalanceRecoverMatte);

        /// §S — the ONLY full reset. Between matches only; no current caller (career/
        /// save-layer scope, §H step 3.5) — exists so that seam is ready when it lands.
        public void ResetBetweenMatches(FighterId who) => _balance[(int)who] = _cfg.BalanceMax;

        void Spend(FighterId who, int amount)
        {
            int i = (int)who;
            _balance[i] = Math.Max(0, _balance[i] - amount);
        }

        void Gain(FighterId who, int amount)
        {
            int i = (int)who;
            _balance[i] = Math.Min(_cfg.BalanceMax, _balance[i] + amount);
        }
    }
}
