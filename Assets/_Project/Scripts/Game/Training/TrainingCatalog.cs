using System;
using System.Collections.Generic;
using UnityEngine;

namespace CounterTime.Game.Training
{
    /// <summary>
    /// DESIGN_training.md §4 — 종목 목록을 에셋으로 저작한다(§H 3.5 로스터/NodeMotionMap과
    /// 같은 패턴: 표현 데이터는 ScriptableObject, 코드에 하드코딩하지 않는다). 종목 하나 =
    /// <see cref="Entry"/> 하나 = 오버라이드 컨트롤러 하나. 종목 추가는 이 에셋에 항목을
    /// 늘리고 <see cref="Entry.overrideController"/>를 채우는 것만으로 끝난다 — 코드 변경 0.
    /// </summary>
    [CreateAssetMenu(menuName = "CounterTime/Training Catalog", fileName = "TrainingCatalog")]
    public sealed class TrainingCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string displayName;
            public RuntimeAnimatorController overrideController;
        }

        [SerializeField] List<Entry> disciplines = new List<Entry>();

        public IReadOnlyList<Entry> Disciplines => disciplines;

        public Entry Find(string id) => disciplines.Find(e => e.id == id);
    }
}
