using System;

namespace CounterTime.Brain
{
    /// <summary>
    /// A variant/surprise technique (진도윤의 말아업어치기, 임준철의 안오금띄기) — a
    /// <b>hard rule, not a habit</b>. Deliberately has no <see cref="HabitId"/>: anything
    /// with a HabitId is countable in the <see cref="ObservationLedger"/> and eventually
    /// shows up in the 수 노트, which would make the surprise observable — defeating the
    /// entire point (<c>MATCH_RULES §7</c>, confirmed by
    /// <c>HabitBrainTests.SurpriseTechIsNotAHabit_AndIsNeverLogged</c>).
    ///
    /// Fires at most once per match — the "경기당 1회" flag lives in
    /// <see cref="HabitState"/> (per-match state), not here (this type is immutable and
    /// shared across matches like <see cref="OpponentDefinition"/>).
    /// </summary>
    public sealed class SurpriseTechnique
    {
        public readonly string TechniqueId;
        public readonly Func<IHabitContext, bool> Condition;

        public SurpriseTechnique(string techniqueId, Func<IHabitContext, bool> condition)
        {
            TechniqueId = techniqueId;
            Condition = condition;
        }
    }
}
