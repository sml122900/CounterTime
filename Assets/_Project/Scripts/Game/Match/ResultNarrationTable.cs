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

        /// <c>docs/step4-narration-worksheet.md</c> 배정을 옮긴 8개 엔트리.
        /// m27·m34는 등록하지 않는다 — 사유는 워크시트 원문에 있다 (폴백 유지).
        /// m18도 등록하지 않는다 — 문구가 횟수("세 번째")를 명시해 오정보가 되므로
        /// 시나리오 개정 대기 (docs/42-node-audit.md §6).
        public static ResultNarrationTable CreateDefault()
        {
            var table = new ResultNarrationTable();

            // m18("같은 밭다리, 세 번째...")은 등록하지 않는다 — 문구가 횟수를 명시해서
            // play 모드(브레인 경로)의 첫 되치기에도 그대로 뜨면 오정보가 된다.
            // 시나리오 개정 대기 — docs/42-node-audit.md §6 목록 참조.

            table.Add(Judgement.Yuko, "업어치기", isCounter: true,
                "들어오는 회전을 그대로 당겨 눕힌다. 되치기 체력 -2 (상대 힘 이용). " +
                "소매를 쥔 채라 곁누르기 연계 가능.");

            table.Add(Judgement.Yuko, "업어치기", isCounter: false,
                "B의 업어치기가 반쯤 들어갔다. 잡기 열세라 회전이 얕아 유효. " +
                "유효는 몇 개를 쌓아도 절반이 되지 않는다 — 그러나 골든스코어에선 이것만으로 이긴다.");

            table.Add(Judgement.None, "밭다리", isCounter: false,
                "굳힌 상대에겐 점수가 안 난다 — 대신 지도가 난다.");
            table.Add(Judgement.None, "허리후리기", isCounter: false,
                "굳힌 상대에겐 점수가 안 난다 — 대신 지도가 난다.");

            table.Add(Judgement.WazaAri, "밭다리", isCounter: false,
                "자세를 풀던 B가 반 박자 늦었다. 회전 90도. 그대로 굳히기 진입.");

            table.Add(Judgement.WazaAri, "말아업어치기", isCounter: false,
                "2025 개정으로 합법화된 그 기술. B 체력 30↓ 1회 한정 발동. " +
                "소매를 말아쥐고 몸째 구른다 — 정석 업어치기와 초동이 같다.");

            table.Add(Judgement.Ippon, "말아업어치기", isCounter: true,
                "\"초동이 같아도 말아쥔 손목은 숨길 수 없다.\" 회전을 흘리고 등 뒤를 취한다.");

            table.Add(Judgement.Ippon, "밭다리", isCounter: false,
                "종반. B가 A의 가슴깃을 잡으러 손을 뻗는 중 — 방어가 완전히 비었다. " +
                "체력 보정 없이도 한판은 난다 — 한판은 지치게 해서가 아니라 '읽어서' 만드는 것.");

            return table;
        }

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
