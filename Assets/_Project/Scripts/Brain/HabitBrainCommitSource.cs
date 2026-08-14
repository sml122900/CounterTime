using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// §4 — B 의 커밋. <b>배타 사다리 3단</b>이고, 위에서 걸리면 아래는 안 본다.
    ///
    /// <code>
    /// 1. 하드 게이트   §5.5 체력 0 락아웃 / §3.2 Locked / §2 카운터 준비 → 불법 커밋 제거
    /// 2. 습관          조건 성립 → 0.75 로 발화, 첫 매치만
    /// 3. 폴백          잡기·체력·지도수·시계 가중 무작위
    /// </code>
    ///
    /// <b>게이트는 사다리 전체에 걸린다 — 습관도 못 뚫는다.</b> 2단이 낸 행동도 1단을
    /// 다시 통과한다. 안 그러면 체력 0 인 B 가 습관 때문에 큰기술을 낸다.
    ///
    /// 난수는 전부 <see cref="IRandomSource"/> 주입이다.
    /// </summary>
    public sealed class HabitBrainCommitSource : ICommitSource
    {
        readonly FighterId _me;
        readonly HabitState _state;
        readonly HabitLadder _ladder;
        readonly IHabitContext _habitContext;
        readonly IBrainContext _brainContext;
        readonly IRandomSource _rng;

        /// §H 3.5b 후속 (2026-08-13 밤 데이터 수집 §1 실측) — 폴백 잡기싸움이 항상
        /// 오른소매만 노렸더니 GripSystem.GripScore(min(2,보유)−min(2,상대보유)) 상,
        /// 양손을 다 노리는 상대에게 구조적으로 영구 열세가 됐다(3000교환 표본 중
        /// B 의 OwnEffectiveGrip>=1 이 2.12% 뿐). 좌우를 번갈아 두 손 다 노린다.
        HandTarget _nextGripHand = HandTarget.RightSleeve;

        /// 이번 턴 사다리 2단의 결론. 관측 원장이 이걸 받는다.
        ///
        /// <b>이 결정 시점 ≠ 교환 제출 시점.</b> 흔들기 데코레이터(<see cref="SetupPrecedingCommitSource"/>)가
        /// 감싸면 이 결정이 한 교환 늦게 나갈 수 있다 — 그래서 <see cref="LiveReadResolver.NoteOpponentHabit"/>
        /// 는 여기서 부르지 않는다. 실제로 그 교환에 제출되는 커밋을 아는 쪽(호출자)이,
        /// 제출 시점에 불러야 한다.
        public HabitDecision LastDecision { get; private set; }
        /// 이번 턴 커밋에 딸린 기술. <c>null</c> = 기술 없음(잡기싸움·방어 턴).
        public TechniqueSpec LastTechnique { get; private set; }
        /// 이번 턴이 폴백(3단)까지 내려갔는가. 계측용이다.
        public bool LastUsedFallback { get; private set; }

        public HabitBrainCommitSource(FighterId me, HabitState state, IHabitContext habitContext,
                                      IBrainContext brainContext, IRandomSource rng)
        {
            _me = me;
            _state = state;
            _habitContext = habitContext;
            _brainContext = brainContext;
            _rng = rng;
            _ladder = new HabitLadder(rng);
        }

        public bool TryNext(out FighterId who, out TurnChoice choice)
        {
            who = _me;

            // 사다리는 항상 그대로 돌린다 — HabitLadder.Next 가 턴 계수(BeginTurn)와
            // 쿨다운을 책임지므로, 변칙기 분기 때문에 이 호출을 건너뛰면 턴 계수가
            // 어긋난다. §H 3.5b — 변칙기가 이번 턴에 발화하면 사다리의 결론은 버리고
            // 변칙기로 덮어쓴다(아래). 두 조건이 정확히 같은 턴에 겹치면(드묾 — 변칙기
            // 조건은 체력<30/밸런스<50 같은 소모 상태라 자주 안 겹친다) 버려진 습관은
            // 이미 NoteFired 로 쿨다운이 시작된 채로 남는다 — 사소한 튜닝 오차로
            // 받아들였다(HabitLadder 의 "부르는 쪽이 턴 증가를 잊을 수 없다" 계약을
            // 지키는 대가).
            var decision = _ladder.Next(_state, _habitContext);

            if (_state.TryFireSurprise(_habitContext))
            {
                // §H 3.5b — 변칙기는 습관이 아니다(HabitId 없음, 관측 원장에 안 남는다).
                // LastDecision 을 비워 그 계약을 지킨다 — 러너가 이걸로 관측 원장에
                // 기록하면 안 되므로.
                LastDecision = default;
                LastUsedFallback = false;

                var surpriseAction = Gate(new HabitAction(
                    new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                    new TechniqueSpec(_state.Opponent.Surprise.TechniqueId, ActionCost.BigThrow,
                        TechniqueClass.Standard, surpriseExempt: true)));
                choice = surpriseAction.Commit;
                LastTechnique = surpriseAction.Technique;
                return true;
            }

            LastDecision = decision;

            HabitAction action = decision.Fired ? decision.Habit.Action : Fallback();
            LastUsedFallback = !decision.Fired;

            action = Gate(action);
            choice = action.Commit;
            LastTechnique = action.Technique;

            // B 는 언제나 무언가를 선언한다. 화면 밖인 적이 없다 — 그 구분은 노드가
            // 선수를 언급조차 안 한 대조 모드에서만 의미가 있다.
            return true;
        }

        // ── 1단 하드 게이트 ─────────────────────────────────────────────────────

        /// <summary>
        /// 불법 커밋을 걷어낸다. 습관이 낸 행동도 여기를 통과한다.
        /// </summary>
        HabitAction Gate(HabitAction action)
        {
            // §5.5 체력 0 — "방어와 견제기(비용 1)만 고를 수 있다". 큰기술은 못 낸다.
            if (_brainContext.OwnStaminaLockout && IsBigThrow(action.Technique))
                return new HabitAction(
                    new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend));

            // §3.2 Locked — "뜯기는 불가능하다". 걸어봐야 버리는 턴이다.
            if (_brainContext.OwnHasLockedHand && action.Commit.Hand == HandTarget.Tear)
                return new HabitAction(
                    new TurnChoice(HandTarget.None, action.Commit.Core, action.Commit.Legs),
                    action.Technique);

            // §2 카운터 준비 — 선언한 쪽은 그 턴에 기술이 해결되지 않는다. 엔진이 이미
            // 막지만, 기술을 들려 보내면 체력만 태우고 아무 일도 안 난다.
            if (action.Commit.Legs == LegAction.CounterReady && action.Technique != null)
                return new HabitAction(action.Commit);

            return action;
        }

        static bool IsBigThrow(TechniqueSpec t) => t != null && t.Cost == ActionCost.BigThrow;

        // ── 3단 폴백 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 폴백 후보. §4 는 "잡기·체력·지도수·시계로 가중" 까지만 정하고 숫자는 안 준다.
        ///
        /// <b>큰기술이 없다.</b> 판정을 발화하는 수는 이유가 있는 수여야 하고, 브레인에서
        /// 이유는 습관이다. 폴백은 정의상 고를 이유가 없는 턴이므로 거기서 나온 던지기는
        /// 아무도 읽을 수 없다. 그래서 공격 후보는 견제기 하나뿐이고, 그 결과
        /// <b>B 의 모든 큰기술이 습관에서 나온다 = 원리적으로 전부 읽을 수 있다.</b>
        /// </summary>
        enum Move { GripFight, Tear, Poke, Defend }

        /// <summary>
        /// <b>이 가중치는 문서에 없다.</b> §4 가 축 넷만 정하고 숫자를 안 줘서 v1 으로
        /// 지어낸 값이고, 튜닝은 여기 한 곳만 만지면 된다. 각 항이 어느 축인지 주석에
        /// 달아뒀다.
        /// </summary>
        int WeightOf(Move m)
        {
            int w = m == Move.GripFight ? 2 : 1;   // 기본값

            switch (m)
            {
                case Move.GripFight:
                    // 잡기 축. H4 가 25% 빗나간 턴에도 폴백이 잡기를 안 고르면 B 는
                    // 영영 잡기 +1 에 못 닿고, 그러면 H1 조건도 영영 안 선다 — 스모크에서
                    // 실제로 그렇게 나왔다(H1 발화 0~1회). 문턱을 H4 와 같은 +1 로 맞춘다.
                    if (_brainContext.OwnEffectiveGrip < 1) w += 3;
                    if (_brainContext.OwnEffectiveGrip < 0) w += 2;    // 밀리고 있으면 더
                    break;
                case Move.Poke:
                    if (_brainContext.OwnShidoCount >= 1) w += 2;      // 지도수: 활발해 보여야 한다
                    if (_brainContext.SecondsRemaining < 30) w += 2;   // 시계: 시간이 없으면 압박한다
                    if (_brainContext.OwnEffectiveGrip >= 1) w += 1;   // 잡기: 우위인데 습관이 안 떴다
                    break;
                case Move.Defend:
                    if (_brainContext.OwnStamina < 50) w += 2;         // 체력: 지치면 지킨다
                    if (_brainContext.OwnShidoCount >= 1) w -= 1;      // 지도수: 더 웅크리면 또 받는다
                    break;
                case Move.Tear:
                    break;
            }
            return w < 0 ? 0 : w;
        }

        HabitAction Fallback()
        {
            var moves = (Move[])System.Enum.GetValues(typeof(Move));

            int total = 0;
            foreach (var m in moves) total += WeightOf(m);
            if (total <= 0) return new HabitAction(TurnChoice.Neutral);

            int roll = _rng.NextInt(0, total);
            foreach (var m in moves)
            {
                roll -= WeightOf(m);
                if (roll < 0) return ActionFor(m);
            }
            return new HabitAction(TurnChoice.Neutral);
        }

        HabitAction ActionFor(Move m)
        {
            switch (m)
            {
                case Move.GripFight:
                    var hand = _nextGripHand;
                    // §3 — GripSystem.Grab 은 TurnChoice.OwnHand(내 손) 슬롯에 값을 쓴다
                    // (TurnChoice.Hand 는 상대 소매 쪽 표적일 뿐). 표적만 번갈고 OwnHand 를
                    // 기본값(Right)에 맡기면 매번 같은 내 손 슬롯을 덮어써 Value()가 절대
                    // 2를 못 넘는다 — 표적과 내 손을 같이 번갈아야 두 손 다 실제로 쌓인다.
                    //
                    // §2b v4.2 후속(2026-08-14 밤 6차, 작성 오류 정정) — 내 손을 표적과
                    // 반대측으로 매핑해야 정잡기(기본 맞잡기)다. 표적=RightSleeve(상대
                    // 오른쪽) ↔ 내손=Left, 표적=LeftSleeve(상대 왼쪽) ↔ 내손=Right —
                    // 예전엔 표적과 같은측(Right→Right/Left→Left)이라 폴백이 항상
                    // 교차만 냈다.
                    var ownHand = hand == HandTarget.RightSleeve ? Hand.Left : Hand.Right;
                    _nextGripHand = hand == HandTarget.RightSleeve
                        ? HandTarget.LeftSleeve : HandTarget.RightSleeve;
                    return new HabitAction(
                        new TurnChoice(hand, CoreStance.Neutral, LegAction.Neutral, ownHand));
                case Move.Tear:
                    return new HabitAction(
                        new TurnChoice(HandTarget.Tear, CoreStance.Neutral, LegAction.Neutral));
                case Move.Poke:
                    return new HabitAction(
                        new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                        new TechniqueSpec("견제 발기술", ActionCost.Poke, TechniqueClass.Sloppy));
                default:
                    return new HabitAction(
                        new TurnChoice(HandTarget.None, CoreStance.Defensive, LegAction.Defend));
            }
        }
    }
}
