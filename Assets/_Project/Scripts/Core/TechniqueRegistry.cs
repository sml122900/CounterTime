using System.Collections.Generic;

namespace CounterTime.Core
{
    /// §B — the 12-technique roster, Core-pure and hardcoded (no ScriptableObject
    /// wrapper — CORE_DESIGN_MatchEngine.md's v4.0 revision deferred that until a real
    /// need for data-driven authoring is confirmed). Replaces the 7 scattered literal
    /// call sites the pre-§H-step-2 code had for TechniqueClass.
    public static class TechniqueRegistry
    {
        static readonly Dictionary<string, TechniqueDef> All = Build();

        public static TechniqueDef Get(string id) =>
            id != null && All.TryGetValue(id, out var def) ? def : null;

        /// §F — mastery level decides TechniqueClass. Boundaries are ⚙ (proposed initial
        /// values, MatchRulesConfig candidates) — 0..MasteryStandardLevel-1 Sloppy,
        /// MasteryStandardLevel..MasterySignatureLevel-1 Standard, the rest Signature.
        public static TechniqueClass ClassFromMastery(int level, MatchRulesConfig cfg) =>
            level >= cfg.MasterySignatureLevel ? TechniqueClass.Signature
          : level >= cfg.MasteryStandardLevel  ? TechniqueClass.Standard
          : TechniqueClass.Sloppy;

        // §B JudoCategory values below are the teaching-screen's five standard judo
        // categories, assigned by technique name/character (전통 유도 분류 기준
        // 최선의 추정) — match logic never reads this field (§B, confirmed), so a
        // misclassification here has zero engine impact and can be corrected freely.
        static Dictionary<string, TechniqueDef> Build()
        {
            var d = new Dictionary<string, TechniqueDef>();
            void Add(TechniqueDef t) => d[t.Id] = t;

            // 견제성 발기술 — Owner A (§B table, all four)
            Add(new TechniqueDef("모두걸기", "모두걸기", AlgoClass.FeintFootsweep, JudoCategory.Foot, ActionCost.Poke));
            Add(new TechniqueDef("발목받치기", "발목받치기", AlgoClass.FeintFootsweep, JudoCategory.Foot, ActionCost.Poke));
            Add(new TechniqueDef("나오는발차기", "나오는발차기", AlgoClass.FeintFootsweep, JudoCategory.Foot, ActionCost.Poke));
            Add(new TechniqueDef("발뒤축걸기", "발뒤축걸기", AlgoClass.FeintFootsweep, JudoCategory.Foot, ActionCost.Poke));

            // 큰 기술 — Owner A
            Add(new TechniqueDef("밭다리걸기", "밭다리걸기", AlgoClass.BigThrow, JudoCategory.Foot, ActionCost.BigThrow));
            Add(new TechniqueDef("허리후리기", "허리후리기", AlgoClass.BigThrow, JudoCategory.Hip, ActionCost.BigThrow));
            Add(new TechniqueDef("안오금띄기", "안오금띄기", AlgoClass.BigThrow, JudoCategory.Foot, ActionCost.BigThrow));
            Add(new TechniqueDef("어깨 매치기", "어깨 매치기 (변칙 어깨)", AlgoClass.BigThrow, JudoCategory.Variant, ActionCost.BigThrow));

            // 큰 기술 — Owner B (habit-driven; registry carries the definition per §B
            // roster regardless of which fighter's brain currently uses it)
            Add(new TechniqueDef("업어치기", "업어치기", AlgoClass.BigThrow, JudoCategory.Hand, ActionCost.BigThrow));
            Add(new TechniqueDef("안뒤축감아치기", "안뒤축감아치기", AlgoClass.BigThrow, JudoCategory.Foot, ActionCost.BigThrow));
            Add(new TechniqueDef("허리 감아치기", "허리 감아치기", AlgoClass.BigThrow, JudoCategory.Hip, ActionCost.BigThrow));
            Add(new TechniqueDef("말아업어치기", "말아업어치기", AlgoClass.BigThrow, JudoCategory.Variant, ActionCost.BigThrow,
                surpriseExempt: true));

            return d;
        }
    }
}
