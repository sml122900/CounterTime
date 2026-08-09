using System;
using CounterTime.Core;
using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 4b 소비 배선 — play mode 전용. <see cref="MatchRunner.JudgementNarrated"/> 를
    /// 구독해 <see cref="ResultNarrationTable"/> 을 쿼리하고 결과를 밖으로 알린다.
    ///
    /// 대조 모드(스크립트 경로) 씬·테스트는 이 컴포넌트를 붙이지 않는다 — 그래서
    /// <see cref="ResultNarrationTable"/> 은 대조 모드에서 한 번도 인스턴스화되지 않는다.
    /// 격리는 "이벤트를 안 쏜다"가 아니라 "이 표를 쿼리하는 건 이 컴포넌트뿐이고, 이
    /// 컴포넌트는 play mode 씬에만 있다"로 지킨다 — MatchRunner 는 어느 쪽이든 같은
    /// 클래스이기 때문이다.
    ///
    /// 문구를 실제로 그리는 UI는 6단계(A 전용 입력 UI) 배선 때 여기에 붙는다. 오늘 밤은
    /// 그릇과 배선까지만 — <c>docs/step4-narration-worksheet.md</c> 가 채워지기 전에는
    /// 표가 비어 있으므로 전부 폴백이다.
    /// </summary>
    [RequireComponent(typeof(MatchRunner))]
    public sealed class ResultNarrationPresenter : MonoBehaviour
    {
        [SerializeField] ResultNarrationTable table = new ResultNarrationTable();

        MatchRunner _runner;

        public event Action<ResultNarrationTable.Result> Narrated;

        void Awake() => _runner = GetComponent<MatchRunner>();
        void OnEnable() => _runner.JudgementNarrated += OnJudgementNarrated;
        void OnDisable() => _runner.JudgementNarrated -= OnJudgementNarrated;

        void OnJudgementNarrated(JudgementScored j, string techniqueId) =>
            Narrated?.Invoke(table.Narrate(j.Judgement, techniqueId, j.IsCounter));
    }
}
