using System.Collections.Generic;
using CounterTime.Brain;
using CounterTime.Core;
using UnityEngine;

namespace CounterTime.Game.Brain
{
    /// <summary>
    /// 상대 한 명을 에셋으로 저작한다. <b>저작하는 것은 셋뿐이다</b> — 어느 습관을 켤지,
    /// 각 습관의 발화확률, 그리고 ScoutingLevel.
    ///
    /// 조건과 행동은 규칙이라 <see cref="HabitCatalogue"/> 에 코드로 있다. H1 의 "≥ +1"
    /// 같은 것을 인스펙터로 빼면 규칙이 에셋으로 새어나가고, 그때부터 MATCH_RULES 가
    /// 단일 출처가 아니게 된다.
    ///
    /// <see cref="OpponentDefinition"/> 은 불변이라 이 에셋이 만든 것을 여러 경기가
    /// 공유해도 된다. 경기마다 새로 찍는 것은 <see cref="HabitState"/> 하나다.
    /// </summary>
    [CreateAssetMenu(fileName = "Opponent", menuName = "CounterTime/Opponent")]
    public sealed class OpponentAsset : ScriptableObject
    {
        [System.Serializable]
        public struct HabitEntry
        {
            public HabitId habit;
            [Range(0f, 1f)] public float fireProbability;
        }

        [SerializeField] string opponentName = "진도윤";

        /// §5 — 0…3. 유일한 효과는 §3 성향 창의 초기 시딩이다. v1 진도윤은 0.
        [SerializeField, Range(0, 3)] int scoutingLevel;

        [SerializeField]
        HabitEntry[] habits =
        {
            new HabitEntry { habit = HabitId.BigThrowOnGripEdge, fireProbability = HabitCatalogue.DefaultFireProbability },
            new HabitEntry { habit = HabitId.LegTechAfterShido,  fireProbability = HabitCatalogue.DefaultFireProbability },
            new HabitEntry { habit = HabitId.TurtleWhenTired,    fireProbability = HabitCatalogue.DefaultFireProbability },
        };

        /// <summary>에셋 → 불변 정의. 경기 시작마다 부를 필요는 없다.</summary>
        public OpponentDefinition ToDefinition(MatchRulesConfig cfg)
        {
            var built = new List<HabitDefinition>(habits.Length);
            foreach (var e in habits)
            {
                var def = HabitCatalogue.Build(e.habit, cfg, e.fireProbability);
                if (def == null)
                {
                    Debug.LogWarning($"{name}: {e.habit} 는 HabitCatalogue 에 없다 — 건너뛴다.", this);
                    continue;
                }
                built.Add(def);
            }
            return new OpponentDefinition(opponentName, built, scoutingLevel);
        }

        /// <summary>경기 하나치 상태. 에셋은 이걸 들지 않는다 — 들면 플레이 모드 변경이
        /// 에셋에 남아 지난 경기의 폐기가 다음 경기로 따라온다.</summary>
        public static HabitState NewMatchState(OpponentDefinition definition) =>
            new HabitState(definition);
    }
}
