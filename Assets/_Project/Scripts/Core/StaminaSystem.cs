using System;

namespace CounterTime.Core
{
    /// MATCH_RULES §5. Stamina is 0–100 per fighter; hitting 0 is not a KO (§5.5).
    public sealed class StaminaSystem
    {
        readonly MatchRulesConfig _cfg;
        readonly int[] _stamina = { 100, 100 };

        public StaminaSystem(MatchRulesConfig cfg) => _cfg = cfg;

        public int Get(FighterId who) => _stamina[(int)who];

        /// §5.1. The grip multiplier applies to big throws ONLY — counters ride the
        /// opponent's force, pokes and tears are too cheap to scale.
        public void PayAttack(FighterId who, TechniqueSpec spec, int effectiveGrip)
        {
            float cost = BaseCost(spec.Cost);
            if (spec.Cost == ActionCost.BigThrow)
                cost *= _cfg.GripMultiplier[effectiveGrip + 2];
            cost *= 1f - _cfg.MasteryCostReductionPerLevel * spec.MasteryLevel;
            Spend(who, Round(cost));
        }

        /// §4 counter settlement — flat cost, never multiplied. Called only once the
        /// counter judgment is confirmed; the countered attacker gets no refund.
        public void PayCounter(FighterId who) => Spend(who, _cfg.CounterCost);

        public void PayDefense(FighterId who, DefenseQuality q) => Spend(who, q switch
        {
            DefenseQuality.Clean   => _cfg.DefendCleanCost,
            DefenseQuality.Neutral => _cfg.DefendNeutralCost,
            _                      => _cfg.DefendPushedCost,
        });

        /// §5.2 — the turtling death spiral: extra drain for every turn held.
        public void DrainExtremeDefense(FighterId who) => Spend(who, _cfg.ExtremeDefenseDrainPerTurn);

        /// §5.3. "Once per matte" is the caller's contract — MatchEngine calls this
        /// exactly once when matte is declared.
        public void RecoverMatte(FighterId who) => Gain(who, _cfg.MatteRecovery);

        /// §5.3 — advantaged side burning clock; +2 per completed 10 s.
        public void RecoverStall(FighterId who, int seconds) =>
            Gain(who, seconds / 10 * _cfg.StallRecoveryPer10s);

        /// §5.5 — at 0 the fighter may only defend or poke.
        public bool IsExhaustedLockout(FighterId who) => _stamina[(int)who] == 0;

        int BaseCost(ActionCost c) => c switch
        {
            ActionCost.BigThrow => _cfg.BigThrowCost,
            ActionCost.Poke     => _cfg.PokeCost,
            ActionCost.Counter  => _cfg.CounterCost,
            _                   => _cfg.TearCost,
        };

        void Spend(FighterId who, int amount)
        {
            int i = (int)who;
            _stamina[i] = Math.Max(0, _stamina[i] - amount);
        }

        void Gain(FighterId who, int amount)
        {
            int i = (int)who;
            _stamina[i] = Math.Min(100, _stamina[i] + amount);
        }

        // 5 × 1.5 = 7.5 must land on 8 (design doc: "unfavorable big throw = 8").
        static int Round(float v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);
    }
}
