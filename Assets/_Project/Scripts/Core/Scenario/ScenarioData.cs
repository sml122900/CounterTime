using System;

namespace CounterTime.Core.Scenario
{
    /// <summary>시나리오 보드가 내보낸 JSON 한 벌.</summary>
    [Serializable]
    public class ScenarioData
    {
        public ScenarioNode[] nodes;
        public ScenarioEdge[] edges;
    }

    /// <summary>보드의 노드 하나. 필드 이름은 JSON 키와 그대로 맞춘다.</summary>
    [Serializable]
    public class ScenarioNode
    {
        public string id;
        public string title;
        public string content;
        public string contentHtml;
        public float x;
        public float y;
        public string border;

        /// <summary>테두리 색으로 판별한 노드 종류.</summary>
        public NodeKind Kind => NodeKinds.FromBorder(border);
    }

    [Serializable]
    public class ScenarioEdge
    {
        public string from;
        public string to;
    }

    public enum NodeKind
    {
        Unknown = 0,
        /// <summary>검정 — 상황·서술.</summary>
        Narration,
        /// <summary>파랑 — A(틀어잡기 선수)의 선택.</summary>
        ChoiceA,
        /// <summary>빨강 — B(업어치기 선수)의 행동·위기.</summary>
        ActionB,
        /// <summary>초록 — 판정·결과.</summary>
        Judgement
    }

    public static class NodeKinds
    {
        public const string NarrationBorder = "#000000";
        public const string ChoiceABorder = "#2f6bad";
        public const string ActionBBorder = "#c9402f";
        public const string JudgementBorder = "#2e8b57";

        public static NodeKind FromBorder(string border)
        {
            if (string.IsNullOrEmpty(border)) return NodeKind.Unknown;

            switch (border.Trim().ToLowerInvariant())
            {
                case NarrationBorder: return NodeKind.Narration;
                case ChoiceABorder: return NodeKind.ChoiceA;
                case ActionBBorder: return NodeKind.ActionB;
                case JudgementBorder: return NodeKind.Judgement;
                default: return NodeKind.Unknown;
            }
        }
    }
}
