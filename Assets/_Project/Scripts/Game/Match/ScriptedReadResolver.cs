using System.Collections.Generic;
using CounterTime.Brain;
using CounterTime.Core;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 대조 모드(스크립트 경로)의 읽기 출처 — 노드의 <c>[판정식]</c> 줄이 정한 값.
    ///
    /// 그 줄을 파싱하는 것은 <see cref="ScriptedAssembler"/> 다. 여기서 정규식을 다시
    /// 쓰지 않는다 — 읽기 파생이 두 곳이 되는 순간 한 곳만 고치는 사고가 난다. 조립된
    /// 결과를 받으므로 노드를 다시 파싱하지도 않는다.
    ///
    /// 한 노드가 여러 교환으로 전개돼도(m14 의 4교환) 읽기는 노드 하나가 정하므로 값이
    /// 같다. 그래서 첫 교환의 값이 곧 이 턴의 읽기다.
    /// </summary>
    public sealed class ScriptedReadResolver : IReadResolver
    {
        readonly ReadLevel _read;

        public ScriptedReadResolver(IReadOnlyList<ExchangeScript> assembled)
        {
            _read = assembled.Count > 0 ? assembled[0].Read : ReadLevel.Neutral;
        }

        /// <summary>
        /// 노드가 이미 값을 정해뒀다 — 대조 모드는 커밋에서 읽기를 유도하지 않는다.
        /// 그 유도가 3단계 <c>LiveReadResolver</c> 의 일이다.
        ///
        /// 반환값은 <b>기저 읽기</b>다. §4.2 카운터 준비 감쇄는 Core 가 건다 —
        /// 방어자가 카운터 준비를 선언했더라도 여기서 미리 깎지 않는다.
        /// </summary>
        public ReadLevel For(FighterId attacker, TurnChoice a, TurnChoice b) => _read;
    }
}
