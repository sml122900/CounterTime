using System.Collections.Generic;
using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 이 턴을 엔진이 먹을 수 있는 교환들로 조립한다.
    ///
    /// <b>한 턴이 반드시 한 교환은 아니다.</b> 대조 모드의 m14 는 제목이 기술 넷을
    /// 나열해서 4교환으로 전개되고, 문서가 적은 체력 −10(3+1+3+3)은 그렇게 굴려야만
    /// 나온다. 그래서 반환이 목록이다.
    ///
    /// 조립기는 <see cref="ExchangeScript"/> 를 만들 뿐 판정하지 않는다 — 합과 등급은
    /// Core 의 JudgementCalculator 한 곳에서만 난다.
    /// </summary>
    public interface IExchangeAssembler
    {
        IReadOnlyList<ExchangeScript> Assemble();
    }
}
