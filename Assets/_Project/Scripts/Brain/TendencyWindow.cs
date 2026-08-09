using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// §3 내 성향 — A 의 최근 커밋 창. 키는 <c>(Core, Legs)</c> 쌍이다. 손은 안 본다:
    /// 잡기 손은 국면이 정하지 성향이 정하지 않는다.
    ///
    /// <b>창은 지난 커밋만 든다.</b> 이번 턴 커밋은 창에 넣지 않고 인자로 받아 창과
    /// 견준다 — §3 이 "A's current commit matches it" 이라고 쓴 그대로다. 이 순서
    /// 덕분에 "기록 먼저냐 판정 먼저냐" 라는 조용한 순서 계약이 안 생긴다.
    ///
    /// 눈금이 둘이다 (§2):
    /// <list type="bullet">
    /// <item><b>경고선</b> — 창 안 4/6 최빈. B 가 공격할 때만 read +2 (4행).</item>
    /// <item><b>위험선</b> — 창 앞머리 3개가 완전동일. 거기 더해 <b>A 자신의 공격이
    /// 읽힌다</b> (5행).</item>
    /// </list>
    ///
    /// 두 눈금 다 <b>이번 커밋이 그 패턴과 같을 때만</b> 선다. 방금 패턴을 깬 선수를
    /// 옛 패턴으로 읽어서 벌하면, 예고된 위험선이 거짓말이 된다 (§3 노출 막대는
    /// 읽히기 <i>전에</i> 보여야 한다).
    /// </summary>
    public sealed class TendencyWindow
    {
        /// §3 — 창 길이 6턴.
        public const int WindowTurns = 6;
        /// §3 — 창 안 이만큼이면 최빈. 경고선.
        public const int MajorityCount = 4;
        /// §3 — 이만큼 연속이면 위험선.
        public const int ConsecutiveCount = 3;

        /// 앞머리(index 0)가 가장 최근이다.
        readonly List<(CoreStance Core, LegAction Legs)> _recent =
            new List<(CoreStance, LegAction)>(WindowTurns);

        /// <summary>§5 ScoutingLevel 0 = 빈 창으로 시작. 시딩은 이 생성자로 들어온다.</summary>
        public TendencyWindow(IEnumerable<TurnChoice> seed = null)
        {
            if (seed == null) return;
            foreach (var c in seed) Record(c);
        }

        public int Count => _recent.Count;

        /// <summary>한 턴이 끝났다. 7턴째가 들어오면 가장 오래된 것이 떨어진다.</summary>
        public void Record(in TurnChoice commit)
        {
            _recent.Insert(0, Key(commit));
            if (_recent.Count > WindowTurns) _recent.RemoveAt(_recent.Count - 1);
        }

        /// <summary>경고선 — 이번 커밋의 쌍이 창 안에 <see cref="MajorityCount"/> 번 이상 있다.</summary>
        public bool MajorityMatches(in TurnChoice commit)
        {
            var k = Key(commit);
            int n = 0;
            foreach (var e in _recent) if (e.Equals(k)) n++;
            return n >= MajorityCount;
        }

        /// <summary>
        /// 위험선 — 창 앞머리 <see cref="ConsecutiveCount"/> 개가 전부 이번 커밋과 같다.
        /// </summary>
        public bool ConsecutiveMatches(in TurnChoice commit)
        {
            if (_recent.Count < ConsecutiveCount) return false;
            var k = Key(commit);
            for (int i = 0; i < ConsecutiveCount; i++)
                if (!_recent[i].Equals(k)) return false;
            return true;
        }

        /// §3 — 성향은 코어·하체 쌍이다. 손은 키가 아니다.
        static (CoreStance, LegAction) Key(in TurnChoice c) => (c.Core, c.Legs);
    }
}
