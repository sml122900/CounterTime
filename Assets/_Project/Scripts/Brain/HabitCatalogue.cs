using CounterTime.Core;

namespace CounterTime.Brain
{
    /// <summary>
    /// 습관 <see cref="HabitId"/> → 정의. 조건과 행동은 규칙이라 여기 코드로 산다.
    ///
    /// 저작 가능한 것은 <b>어느 습관을 켤지</b>와 <b>발화확률</b>뿐이고, 그건 Game 쪽 SO 가
    /// 정한다. H1 의 "≥ +1" 을 인스펙터에서 만지게 하면 규칙이 에셋으로 새어나간다.
    /// </summary>
    public static class HabitCatalogue
    {
        public const float DefaultFireProbability = 0.75f;

        public static HabitDefinition Build(HabitId id, MatchRulesConfig cfg,
                                            float fireProbability = DefaultFireProbability)
        {
            switch (id)
            {
                // H1 — 자기 유효 잡기가 우위면 업어치기. 초대하는 대응: 잡기를 내주고
                // 카운터 준비를 선언하는 것.
                //
                // 쿨다운 3 — 잡기 우위를 잡았다고 매 턴 던지는 선수는 없고, 매 턴 나오는
                // 신호는 읽을 값이 없다. 실측에서 27교환에 11번 떠서 경기를 정규시간의
                // 1/4 만에 끝냈다. 목표는 한 경기 5~7회다.
                case HabitId.BigThrowOnGripEdge:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            TurnChoice.Neutral,
                            new TechniqueSpec("업어치기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        fireProbability,
                        minTurnsBetweenFirings: 3);

                // H2 — 지도 뒤 2턴 내에 안뒤축감아치기로 만회한다. 초대하는 대응: 기다리기.
                // "2턴 내" = TurnsSinceOwnShido 가 0 과 1, 정확히 두 번이다. <= 2 는
                // 세 번이 되어 틀린다 — 턴의 정의는 IHabitContext 에 못 박아뒀다.
                //
                // 견제기가 아니라 진짜 시도다. 견제기는 판정을 발화하지 않아서, 읽혀도
                // 벌 방법이 없는 습관이 된다 — 공략 대상이 되는 것이 습관의 존재 이유다.
                case HabitId.LegTechAfterShido:
                    return new HabitDefinition(id,
                        ctx => ctx.TurnsSinceOwnShido.HasValue && ctx.TurnsSinceOwnShido.Value < 2,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("안뒤축감아치기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        fireProbability);

                // H3 — 예비. 폐기가 나기 전엔 사다리에 오르지 않는다.
                // 체력 <50 '그리고' 상대보다 낮을 때만이다. 단순 열세는 턴의 절반쯤
                // 참이라 B 가 영구히 틀어박히고, §6.1(+3/턴)·§5.2(−1/턴)가 B 를
                // 샌드백으로 갈아버린다. 50 은 새 숫자가 아니라 §4 가 이미 쓰는 구간이다.
                case HabitId.TurtleWhenTired:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnStamina < cfg.StaminaBonusTiredThreshold
                            && ctx.OwnStamina < ctx.OpponentStamina,
                        new HabitAction(
                            // §2b v4.2 최소 적응 — GripRevisionGate 아래서 회피가 실제로
                            // 뭔가를 막으려면 존이 있어야 한다(없으면 IsZoneDefended 가
                            // 항상 false). "똑똑한" 존 선택 로직은 범위 밖(행동 품질 개선
                            // 아님) — 가장 기초적인 방어인 양소매를 기본값으로 굳힌다.
                            // 게이트 off 일 땐 그냥 안 읽히는 필드라 §2a 동작은 무변화.
                            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend,
                                Hand.Right, DefenseZone.LeftSleeve, DefenseZone.RightSleeve)),
                        fireProbability,
                        isReserve: true);

                // H4 — 잡기가 우위가 아니면 소매를 노린다. 기술이 없으므로 <b>비공격
                // 습관</b>이고, 그래서 사다리 2행(A 공격 + B 비공격 습관 + 확정 →
                // 완전간파)의 대상이 된다. MATCH_RULES §4.1 카논 케이스 3 —
                // "상대가 잡으러 오는 순간 틀어잡고 밭다리 = 7 = 한판" 이 그 장면이고,
                // 이 습관이 없으면 플레이에서 도달할 수 없다.
                //
                // H1 과 조건이 정확히 배타적이다(≥ +1 / < +1). 둘이 같은 턴에 다투지
                // 않으므로 사다리 순서가 둘 사이에서는 의미가 없다.
                // §2b v4.2 후속(2026-08-14 밤 6차, 작성 오류 정정) — OwnHand=Left.
                // "오른소매 노림"(상대 오른소매가 표적)의 기본 맞잡기는 정잡기다 —
                // 내 손이 표적과 반대측이어야 정(§2b "내 오른손→상대 왼쪽"과 대칭
                // 규칙 그대로: 표적 Right ↔ 내손 Left). OwnHand=Right로 짰던 원래
                // 코드는 매핑 아티팩트로 항상 교차였다 — 실유도 기본 맞잡기와도,
                // 1차 지시서 예시 의도와도 어긋났다(예시 문구 자체는 반대로
                // 적혀 있었다는 게 이번 세션에서 확인됨, 위 STATUS 참조).
                case HabitId.SeekGripRightSleeve:
                    return SeekGrip(id, cfg, fireProbability, HandTarget.RightSleeve, Hand.Left, isReserve: false);

                // H4b — 예비 교체분. 폐기는 삭제가 아니다: 잡기를 아예 안 가면 B 가
                // 자멸한다. 간파당했으니 반대 소매로 바꾼다 — 플레이어는 새 부위를
                // 0 부터 다시 관측해야 하고, 그래서 읽기 루프가 살아 있다.
                // §2b v4.2 후속 — 같은 정정, 반대 표적. OwnHand=Right(표적 Left ↔ 내손
                // Right = 정).
                case HabitId.SeekGripLeftSleeve:
                    return SeekGrip(id, cfg, fireProbability, HandTarget.LeftSleeve, Hand.Right, isReserve: true);

                // ── §H 3.5b 로스터 확장 (ROSTER.md) ─────────────────────────────

                // 32강 황도철 H — 조건 없음, 신뢰도 90%(ROSTER 제안값). 쿨다운 2로
                // 매 턴 반복만 막는다 — "가장 단순·가장 시끄러운 습관"이라 예외적으로
                // 조건 자체가 없다(다른 습관은 전부 어떤 상태를 요구한다).
                case HabitId.BigThrowAlways:
                    return new HabitDefinition(id,
                        ctx => true,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("밭다리걸기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        fireProbability,
                        minTurnsBetweenFirings: 2);

                // 16강 문준배 H — "가끔 허리후리기 반격". 낮은 신뢰도(⚙ 0.35)+긴 쿨다운
                // (5)으로 진짜 드물게만 나온다. 나머지 턴은 이 습관도 안 서므로 §4 3단
                // 폴백(정의상 큰기술이 없다)이 "웬만하면 방어·중립"을 무료로 만든다 —
                // 그래서 별도의 "항상 방어" 습관을 안 만들었다.
                case HabitId.CounterThrowOnGripEdge:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("허리후리기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        0.35f,
                        minTurnsBetweenFirings: 5);

                // 8강 서도현 H1 — 상대 흔들기 중이면 카운터 준비(신뢰도 70%, ROSTER
                // 제안값). 기술 없음 — 카운터 준비 자체가 "받는" 행동이다(§2).
                case HabitId.CounterReadyOnOpponentSetup:
                    return new HabitDefinition(id,
                        ctx => ctx.OpponentSetupActive,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.CounterReady)),
                        0.70f);

                // 8강 서도현 H2 — 밸런스 온전(§S 임계 이상)이면 방어 유지. H1 보다
                // 넓은 조건이라 사다리에서 H1 뒤에 둔다(진도윤 H1/H2 와 같은 원칙 —
                // 좁은 것 먼저).
                case HabitId.StayDefensiveWhenBalanceHigh:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnBalance >= cfg.BalanceOnsetThreshold,
                        new HabitAction(
                            // §2b v4.2 최소 적응 — TurtleWhenTired와 같은 이유·같은 기본값.
                            new TurnChoice(HandTarget.Evade, CoreStance.Defensive, LegAction.Defend,
                                Hand.Right, DefenseZone.LeftSleeve, DefenseZone.RightSleeve)),
                        fireProbability);

                // 4강 임준철 H1 — 그립 우세 시 어깨 매치기 or 발뒤축걸기 "번갈아"
                // (ROSTER). 정확한 매 순간 50/50 무작위 대신, 같은 조건에 쿨다운 2를
                // 나란히 준 두 습관으로 근사했다 — 쿨다운은 실제로 낸 턴부터 세므로
                // (HabitState.NoteFired) 어깨 매치기가 뜨면 다음 기회엔 쿨다운에
                // 걸려 발뒤축걸기 차례가 오고, 그 다음엔 다시 어깨 매치기 차례가
                // 온다(그립 우세가 매 턴 유지된다는 전제하에 엄격한 A-B-A-B). 신뢰도
                // 60%(ROSTER 제안값) 는 두 습관 다 동일 적용.
                case HabitId.ShoulderThrowOnGripEdge:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("어깨 매치기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        0.60f,
                        minTurnsBetweenFirings: 2);
                case HabitId.HeelHookOnGripEdge:
                    // §B 레지스트리 확인 — 발뒤축걸기는 견제 발기술(Poke)로 등록돼 있다
                    // (TechniqueRegistry.cs, AlgoClass.FeintFootsweep). ROSTER 의 2지선다
                    // 한쪽은 진짜 시도(BigThrow), 다른 쪽은 견제기라 비용/판정 성격이
                    // 다르다 — 레지스트리를 따랐다(Core §B 확정 표, 손 안 댐).
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("발뒤축걸기", ActionCost.Poke, TechniqueClass.Sloppy)),
                        0.60f,
                        minTurnsBetweenFirings: 2);

                // 4강 임준철 H2 — 지도 뒤 2턴 내 견제 발기술로 만회. 진도윤 H2 와 조건은
                // 같지만 진짜 시도(BigThrow)가 아니라 견제기(Poke)다.
                case HabitId.FeintAfterShido:
                    return new HabitDefinition(id,
                        ctx => ctx.TurnsSinceOwnShido.HasValue && ctx.TurnsSinceOwnShido.Value < 2,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("나오는발차기", ActionCost.Poke, TechniqueClass.Sloppy)),
                        fireProbability);

                // 동메달전 오재훈 H — "특출난 축 없이 고루" = 큰기술 두 개를 같은
                // 쿨다운 교대 기법으로 섞는다(임준철과 같은 근사, 위 주석 참조). 중간
                // 신뢰도(⚙ 0.5) — ROSTER 가 숫자를 안 줘서 32강(0.9)과 8강(0.6~0.7)
                // 사이 중간값으로 골랐다.
                case HabitId.MixedBigThrowA:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("허리후리기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        0.5f,
                        minTurnsBetweenFirings: 2);
                case HabitId.MixedBigThrowB:
                    return new HabitDefinition(id,
                        ctx => ctx.OwnEffectiveGrip >= 1,
                        new HabitAction(
                            new TurnChoice(HandTarget.None, CoreStance.Neutral, LegAction.Attack),
                            new TechniqueSpec("밭다리걸기", ActionCost.BigThrow, TechniqueClass.Standard)),
                        0.5f,
                        minTurnsBetweenFirings: 2);

                default:
                    return null;
            }
        }

        /// 부위만 다른 두 잡기 노림 습관. 조건은 같다 — 잡기가 우위가 아니면 잡으러 간다.
        static HabitDefinition SeekGrip(HabitId id, MatchRulesConfig cfg, float fireProbability,
                                        HandTarget target, Hand ownHand, bool isReserve) =>
            new HabitDefinition(id,
                ctx => ctx.OwnEffectiveGrip < 1,
                new HabitAction(new TurnChoice(target, CoreStance.Neutral, LegAction.Neutral, ownHand)),
                fireProbability,
                isReserve);
    }
}
