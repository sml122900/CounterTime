using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// OPPONENT_BRAIN §6 — 결과 노드를 그릇 하나로 승격한다: (판정, 기술 Id, 되치기 여부)
    /// → 서술 문구. Core 의 <see cref="JudgementScored"/> 가 유일한 진실이고, 이 표는
    /// 그 결과를 무슨 말로 보여줄지만 고른다.
    ///
    /// 초기 데이터는 비어 있다 — 문구 키 배정은 사람 몫이다
    /// (<c>docs/step4-narration-worksheet.md</c> 참조). <see cref="TechniqueSpec"/> 에
    /// 표시명 필드가 없다는 것은 4단계 (a) 덤프로 확인된 사실이다 — 여기서도 표시명을
    /// 새로 만들지 않고 <c>TechniqueId</c> 를 그대로 쓴다.
    ///
    /// 엔트리가 없으면 문구를 지어내지 않는다. 기계적 폴백만 낸다.
    /// </summary>
    public sealed class ResultNarrationTable
    {
        public readonly struct Key
        {
            public readonly Judgement Judgement;
            public readonly string TechniqueId;
            public readonly bool IsCounter;

            public Key(Judgement judgement, string techniqueId, bool isCounter)
            {
                Judgement = judgement;
                TechniqueId = techniqueId ?? "";
                IsCounter = isCounter;
            }

            public override bool Equals(object obj) =>
                obj is Key k && k.Judgement == Judgement && k.TechniqueId == TechniqueId && k.IsCounter == IsCounter;

            public override int GetHashCode() =>
                ((int)Judgement * 397 ^ TechniqueId.GetHashCode()) * 397 ^ IsCounter.GetHashCode();
        }

        public readonly struct Result
        {
            public readonly string Text;
            public readonly bool IsFallback;

            public Result(string text, bool isFallback)
            {
                Text = text;
                IsFallback = isFallback;
            }
        }

        readonly Dictionary<Key, string> _entries = new Dictionary<Key, string>();

        /// 사람이 아침에 <c>docs/step4-narration-worksheet.md</c> 를 채우고 나면 여기서
        /// 등록한다. 되치기는 반드시 별개 키다 — 같은 (판정, 기술) 라도 누가 던졌는지에
        /// 따라 서술이 완전히 달라진다.
        public void Add(Judgement judgement, string techniqueId, bool isCounter, string text) =>
            _entries[new Key(judgement, techniqueId, isCounter)] = text;

        public Result Narrate(Judgement judgement, string techniqueId, bool isCounter)
        {
            var key = new Key(judgement, techniqueId, isCounter);
            if (_entries.TryGetValue(key, out var text))
                return new Result(text, false);
            return new Result($"{key.TechniqueId} — {JudgementName(judgement)}", true);
        }

        static string JudgementName(Judgement j)
        {
            switch (j)
            {
                case Judgement.Yuko: return "유효";
                case Judgement.WazaAri: return "절반";
                case Judgement.Ippon: return "한판";
                default: return "노포인트";
            }
        }
    }
}
