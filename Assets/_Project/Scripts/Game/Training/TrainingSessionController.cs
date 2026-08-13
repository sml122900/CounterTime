using System;
using UnityEngine;

namespace CounterTime.Game.Training
{
    /// <summary>
    /// DESIGN_training.md §2 — 능동 렙 메커니즘. v1은 "누르면 1렙" 단순형(타이밍 채점 없음,
    /// §2 [설계 판단] 제안 그대로). 렙마다 피로가 쌓이고, 임계(⚙)를 넘으면 Strained 로
    /// 갈아타며 재생 속도가 느려지고, 한계(⚙)에 닿으면 Collapse 재생 후 세션 종료.
    ///
    /// 상태 전환은 <see cref="Animator"/> 그래프의 파라미터/트랜지션이 아니라 이 스크립트가
    /// 상태 이름으로 직접 CrossFade 한다 — 이 프로젝트가 이미 쓰는 패턴
    /// (<c>ScenarioMotionBinder</c>/<c>ArenaStageView</c>)과 같다. 종목별 차이는 전부
    /// <see cref="TrainingStageView.SetController"/> 로 갈아끼우는 <c>AnimatorOverrideController</c>
    /// 가 만든다 — 이 스크립트는 종목을 모른다(상태 이름 4개만 안다).
    /// </summary>
    [RequireComponent(typeof(TrainingStageView))]
    public sealed class TrainingSessionController : MonoBehaviour
    {
        public const string StateReady = "Ready";
        public const string StateRep = "Rep";
        public const string StateStrained = "Strained";
        public const string StateCollapse = "Collapse";

        // ⚙ 제안 초기치 — 플레이 감으로 튜닝 대상 (DESIGN_training §2 "전부 ⚙").
        [SerializeField] float fatiguePerRep = 7f;
        [SerializeField] float fatigueLimit = 100f;
        [SerializeField] float strainedStartFatigue = 55f;
        [SerializeField] float collapsedAnimatorSpeed = 0.55f;
        [SerializeField] float crossfadeBlend = 0.06f;

        public readonly struct Result
        {
            public readonly string DisciplineId;
            public readonly int RepCount;
            public Result(string disciplineId, int repCount)
            {
                DisciplineId = disciplineId;
                RepCount = repCount;
            }
        }

        TrainingStageView _stage;
        string _disciplineId;

        public int RepCount { get; private set; }
        public float Fatigue { get; private set; }
        public float FatigueLimit => fatigueLimit;
        public bool Active { get; private set; }
        public RenderTexture Output => _stage.Output;

        public event Action<Result> SessionEnded;

        void Awake()
        {
            _stage = GetComponent<TrainingStageView>();
            // TrainingStageView 도 같은 GameObject 의 컴포넌트라 Awake 순서가 보장되지
            // 않는다 — LiveMatchPanel/ArenaStageView 와 같은 이유로 여기서 직접 완성시켜둔다.
            _stage.EnsureBuilt();
        }

        public void BeginSession(TrainingCatalog.Entry entry)
        {
            _disciplineId = entry.id;
            RepCount = 0;
            Fatigue = 0f;
            Active = true;
            _stage.SetController(entry.overrideController);
            if (_stage.Animator != null)
            {
                _stage.Animator.speed = 1f;
                _stage.Animator.CrossFade(StateReady, crossfadeBlend, 0, 0f);
            }
        }

        /// 렙 입력 1회 — 스페이스/엔터/클릭 어느 쪽에서 오든 이 한 진입점으로 모인다.
        public void OnRepInput()
        {
            if (!Active) return;

            RepCount++;
            Fatigue = Mathf.Min(fatigueLimit, Fatigue + fatiguePerRep);

            var anim = _stage.Animator;
            if (Fatigue >= fatigueLimit)
            {
                Active = false;
                if (anim != null)
                {
                    anim.speed = 1f;
                    anim.CrossFade(StateCollapse, crossfadeBlend, 0, 0f);
                }
                SessionEnded?.Invoke(new Result(_disciplineId, RepCount));
                return;
            }

            if (anim != null)
            {
                anim.speed = Mathf.Lerp(1f, collapsedAnimatorSpeed, Fatigue / fatigueLimit);
                string state = Fatigue >= strainedStartFatigue ? StateStrained : StateRep;
                anim.CrossFade(state, crossfadeBlend, 0, 0f);
            }
        }
    }
}
