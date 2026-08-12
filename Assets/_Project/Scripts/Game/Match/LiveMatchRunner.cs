using System;
using System.Collections.Generic;
using CounterTime.Brain;
using CounterTime.Core;
using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 6단계 — 시나리오 노드 없이 굴러가는 실전 매치. <see cref="MatchRunner"/> 는 42노드
    /// 대조 전용이라 이 러너와는 완전히 별개다: 여기엔 <c>ScenarioGraph</c> 도
    /// <c>ScriptedAssembler</c> 도 없다.
    ///
    /// A 는 <see cref="LiveAssembler"/> 로 UI 입력을 받고, B 는 습관 브레인
    /// (<see cref="HabitBrainCommitSource"/>, §4.4 [초안] 플래그가 켜져 있으면
    /// <see cref="SetupPrecedingCommitSource"/> 로 감싼다)이 스스로 정한다.
    ///
    /// §H 2단계 — A 의 기술 지정은 <see cref="TechniqueRegistry"/> 기반이다
    /// (<see cref="SubmitA"/> 의 <c>techniqueId</c> 인자). 다만 <b>기술 탭 UI 자체는
    /// 이 단계 범위 밖</b>(§H 2b) — <c>LiveMatchPanel</c> 은 아직 그 인자를 채워 주지
    /// 않으므로, 실제 Play 화면에서 손 축=공격을 골라도 기술 미지정으로 공격 커밋이
    /// 안 열린다(아래 §A.2 참조). 데이터·엔진 배선은 여기서 끝나 있으니 2b 는 UI만
    /// 붙이면 된다.
    ///
    /// §A.2 확정 — "Legs 공격 = 기술 선택", 기술을 명시적으로 지정한 커밋만 공격으로
    /// 친다("공격자 판정 통일"). 그 전엔 하드코딩된 자리표시자(옛
    /// <c>PlaceholderATechnique</c>, <c>PlayModeSmokeTests</c> 가 쓰던 것과 같은
    /// "밭다리" 고정값)로 Legs=공격을 낸 턴마다 무조건 공격이 성립했다 — 그 하드코딩은
    /// 이 단계에서 제거됐다.
    ///
    /// <b>동시 공격 충돌.</b> <c>ExchangeScript.Attacker</c> 는 한 교환에 한 명뿐이다 —
    /// A 가 Legs=공격을 선언한 같은 교환에 B 의 브레인도 기술을 냈다면 Core 데이터
    /// 모델 자체가 "누가 먼저"를 표현 못 한다. <c>PlayModeSmokeTests</c> 가 이미
    /// "B 가 내면 B, 아니면 A" 로 정해뒀던 우선순위를 그대로 물려받았다 — 새로 지어낸
    /// 규칙이 아니라 기존 선례를 재사용한 것이고, 진짜 해소 규칙(선제권 등)은 범위 밖.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiveMatchRunner : MonoBehaviour
    {
        [SerializeField] RefereeProfile refereeProfile = RefereeProfile.Standard;
        [SerializeField] int randomSeed = 12345;
        [SerializeField] bool setupEnabled = false;

        MatchRulesConfig _cfg;
        SystemRandomSource _rng;
        ObservationLedger _ledger;
        TendencyWindow _window;
        LiveReadResolver _resolver;
        HabitState _habitState;
        MatchBrainContext _bContext;
        HabitBrainCommitSource _bInner;
        SetupPrecedingCommitSource _bSource;
        readonly ResultNarrationTable _narration = ResultNarrationTable.CreateDefault();

        public MatchEngine Engine { get; private set; }
        public LiveAssembler A { get; private set; }
        public TendencyWindow Tendency => _window;
        /// HUD 표시용 — 흔들기 게이트가 켜져 있는지 판단하는 데만 쓴다.
        public MatchRulesConfig Config => _cfg;
        /// 수 노트 HUD 용 — 습관별 관측 횟수(심증/확정 판정).
        public ObservationLedger Ledger => _ledger;
        /// 수 노트 HUD 용 — 상대 정의(습관 목록)와 폐기 여부.
        public HabitState OpponentHabits => _habitState;

        /// 화면·로그가 붙는 지점 — Core 이벤트를 원본 그대로 낸다.
        public event Action<MatchEvent> Presented;
        /// 판정이 났을 때만 — 서술 사전을 거친 결과(폴백 여부 포함).
        public event Action<ResultNarrationTable.Result> Narrated;
        /// §9.1 — 이 교환에서 굳히기가 새로 열렸다. UI 는 정상 입력을 멈추고
        /// 5초 2지선다로 전환해야 한다.
        public event Action OsaekomiOpened;
        /// §9.4 — 굳히기가 빠져나가기로 끝났다(한판으로 경기가 끝난 경우는 MatchEnded
        /// 가 대신 Presented 로 나간다).
        public event Action OsaekomiClosed;
        /// [v1 임시 규칙, MATCH_RULES §2] A 가 Legs=공격을 선언한 같은 교환에 B 의
        /// 브레인도 기술을 냈다 — 한 교환엔 공격자가 하나뿐이라 B 가 선행하고 A 의
        /// 공격은 이번 교환에 성립하지 않는다. 선제권 판정은 미결.
        public event Action APreempted;

        void Awake() => Build();

        public void Build()
        {
            _cfg = new MatchRulesConfig { SetupGateEnabled = setupEnabled };
            _rng = new SystemRandomSource(randomSeed);
            Engine = new MatchEngine(_cfg, refereeProfile, _rng);
            A = new LiveAssembler(Engine, _cfg, FighterId.A);

            _ledger = new ObservationLedger();
            _window = new TendencyWindow();
            _resolver = new LiveReadResolver(_ledger, _window);

            var opponent = Jindoyun.Create(_cfg);
            _habitState = new HabitState(opponent);
            _bContext = new MatchBrainContext(Engine, FighterId.B);
            _bInner = new HabitBrainCommitSource(FighterId.B, _habitState, _bContext, _bContext, _rng);
            _bSource = new SetupPrecedingCommitSource(_bInner, _bContext, setupEnabled);
        }

        /// §H 2단계 — 레지스트리 조회로 A 의 기술을 조립한다. AuthoredClass 는
        /// null 로 남겨 MatchEngine 이 숙련도로 파생하게 한다(§F, 지금은 IMasteryStore
        /// stub 이 항상 0 이라 Sloppy 로 나온다 — 하드코딩 "Standard" 자리표시자와 다른
        /// 값이지만, 미숙련 기술이 어설프게 나가는 게 숙련도 시스템의 의도된 동작이다).
        /// id 가 레지스트리에 없으면 null — 잘못된 지정도 "공격 커밋 없음"으로 접힌다.
        static TechniqueSpec BuildLiveTechnique(string techniqueId)
        {
            var def = TechniqueRegistry.Get(techniqueId);
            return def == null ? null : new TechniqueSpec(def.Id, def.BaseCost, null, def.SurpriseExempt);
        }

        /// <summary>A 가 이번 교환의 커밋을 확정했다 — 굳히기가 열려 있는 동안은
        /// no-op(§9 가드가 <c>ResolveExchange</c> 를 그 상태로 부르면 던진다;
        /// <see cref="SubmitOsaekomi"/> 를 대신 쓸 것). <paramref name="techniqueId"/>
        /// 는 <see cref="LegAction.Attack"/> 을 실제 공격으로 성립시키는 유일한 방법이다
        /// (§A.2 "공격자 판정 통일") — 기술 탭 UI(§H 2b)가 아직 없어 지금은 아무도 이
        /// 인자를 채우지 않는다.</summary>
        public void SubmitA(TurnChoice a, string techniqueId = null)
        {
            if (Engine.Osaekomi.Active || Engine.Board.IsFinished) return;

            _bSource.TryNext(out _, out var b);
            // 커밋이 실제로 이번 교환에 제출되는 시점 — 결정 시점(TryNext 내부, 사다리가
            // 굴러간 순간)이 아니다. 흔들기 설치 교환에서는 LastDecision 이 default(발화
            // 없음)이므로, 여기서 그대로 알리면 그 교환엔 습관 알림이 안 나가고 다음
            // 교환(진짜 기술 제출)에서 알림이 나간다.
            _resolver.NoteOpponentHabit(_bSource.LastDecision);

            bool bAttacks = _bSource.LastTechnique != null;
            // §A.2/§H 2단계 — "기술을 명시적으로 지정한 커밋만 공격"으로 통일. B 쪽은
            // 이미 이 기준이었다(LastTechnique 는 습관/폴백이 실제로 기술을 낼 때만
            // non-null) — A 쪽도 같은 기준으로 맞춘다: Legs=공격을 골랐어도 기술
            // 지정이 없으면 공격 커밋이 아니다.
            TechniqueSpec liveTechnique = !bAttacks && a.Legs == LegAction.Attack
                ? BuildLiveTechnique(techniqueId) : null;
            bool aAttacks = liveTechnique != null;
            // [v1 임시 규칙] 동시 공격 — A 도 실제로 기술을 지정했는데 B 가 먼저 낸 경우.
            // §H 2b — 예전엔 `a.Legs == LegAction.Attack`만 봤다(기술 지정 요구가 생기기
            // 전 기준 그대로). 기술 미지정이면 애초에 "불발될 A 의 공격"이 없으므로
            // (§H 2단계 "공격자 판정 통일") 조건도 같은 기준(liveTechnique != null)으로
            // 좁혔다 — aAttacks 를 만드는 바로 그 조건 재사용, 새 파생 아님.
            if (bAttacks && liveTechnique != null) APreempted?.Invoke();
            FighterId? attacker = bAttacks ? FighterId.B : aAttacks ? FighterId.A : (FighterId?)null;
            TechniqueSpec technique = bAttacks ? _bSource.LastTechnique
                                    : aAttacks ? liveTechnique : null;

            ReadLevel read = attacker.HasValue ? _resolver.For(attacker.Value, a, b) : ReadLevel.Neutral;

            var script = new ExchangeScript { Attacker = attacker, Technique = technique, Read = read };
            IReadOnlyList<MatchEvent> events = Engine.ResolveExchange(a, b, script);

            foreach (var e in events)
            {
                Presented?.Invoke(e);
                if (e is JudgementScored j)
                    Narrated?.Invoke(_narration.Narrate(j.Judgement, technique?.Id ?? "", j.IsCounter));
            }

            // §4.2 피해 — 관측 기록보다 먼저 본다(HabitState.RegisterDamage 계약).
            if (_bSource.LastDecision.Fired)
                foreach (var e in events)
                    if (e is JudgementScored j2 && j2.ScoredBy == FighterId.A
                        && (j2.IsCounter || j2.Judgement != Judgement.None))
                        _habitState.RegisterDamage(_bSource.LastDecision.Habit.Id, _ledger);

            _ledger.Record(_bSource.LastDecision);
            _bContext.ObserveExchange(events);
            // §A.2 확정 — 기술 턴은 (Core,Legs) 성향 채널에 안 남는다(중립 placeholder로
            // 대체하지도 않는다 — 그냥 기록 안 함). 기술 빈도 채널은 §C.2(6단계)에서.
            if (!aAttacks) _window.Record(a);

            if (Engine.Osaekomi.Active) OsaekomiOpened?.Invoke();
        }

        /// <summary>§9.3 — 5초 마크 하나. <paramref name="defenderChoice"/> 는 굳히기
        /// 아래 깔린 쪽의 선택, <paramref name="holderChoice"/> 는 누른 쪽의 선택이다 —
        /// 어느 쪽이 A 인지는 <see cref="MatchEngine.OsaekomiState.Holder"/> 로 UI 가
        /// 미리 판단해서 A 몫만 실제 입력으로 받고 반대쪽은 채워 넣어야 한다(B 굳히기
        /// AI 는 아직 없다 — 아래 참고).</summary>
        public void SubmitOsaekomi(OsaekomiDefenderChoice defenderChoice, OsaekomiHolderChoice holderChoice)
        {
            if (!Engine.Osaekomi.Active) return;

            var events = Engine.ResolveOsaekomiExchange(defenderChoice, holderChoice);
            foreach (var e in events) Presented?.Invoke(e);

            if (!Engine.Osaekomi.Active) OsaekomiClosed?.Invoke();
        }

        /// <summary>B 가 굳히기 쪽(홀더 또는 디펜더)일 때 쓸 자리표시자 정책 — 항상
        /// 어긋남(빠져나가기×따라가기 조합의 반대쪽) 이 되게 고른다. <c>PlayModeSmokeTests</c>
        /// 의 headless 루프가 쓰던 것과 같은 고정 정책이다: 진짜 B 굳히기 AI 는 없다.
        /// UI 는 A 의 실제 역할(<see cref="MatchEngine.OsaekomiState.Holder"/> 기준)에
        /// 맞는 쪽만 사람에게 묻고, 반대쪽은 이 메서드로 채운다.</summary>
        public OsaekomiDefenderChoice BDefenderPolicy() => OsaekomiDefenderChoice.Escape;
        public OsaekomiHolderChoice BHolderPolicy() => OsaekomiHolderChoice.Follow;
    }
}
