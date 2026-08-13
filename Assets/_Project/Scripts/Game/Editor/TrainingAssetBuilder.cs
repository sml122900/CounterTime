using System.Collections.Generic;
using System.IO;
using CounterTime.Game.Training;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CounterTime.Game.Editor
{
    /// <summary>
    /// DESIGN_training.md §3 — "모캡만 따면 바로 적용"의 스캐폴드 생성기. 공용
    /// <c>AC_Training.controller</c>(Ready/Rep/Strained/Collapse, placeholder 클립은
    /// 기존 <c>AC_Fighter_A.controller</c> 에서 그대로 재사용) + §4 13종 각각의
    /// <see cref="AnimatorOverrideController"/> + <see cref="TrainingCatalog"/> 에셋을
    /// 만든다.
    ///
    /// <b>이미 있는 에셋은 절대 다시 안 만든다</b> — 오버라이드 컨트롤러는 사람이 모캡
    /// 클립을 슬롯에 채워 넣는 자리라, 재실행이 그걸 덮어쓰면 작업이 통째로 날아간다.
    /// 이 메뉴는 "없으면 채운다"만 한다.
    /// </summary>
    static class TrainingAssetBuilder
    {
        const string ControllerPath = "Assets/_Project/Art/Animations/AC_Training.controller";
        const string OverrideFolderParent = "Assets/_Project/Art/Animations";
        const string OverrideFolderName = "Training";
        const string OverrideFolder = OverrideFolderParent + "/" + OverrideFolderName;
        const string CatalogPath = "Assets/_Project/ScriptableObjects/TrainingCatalog.asset";
        const string FighterAControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_A.controller";

        // DESIGN_training.md §4 표 그대로 — 순서·표기 문구 원문 유지.
        static readonly (string id, string displayName)[] Disciplines =
        {
            ("pull_up", "풀업(친업/도복잡고)"),
            ("tube_uchikomi", "튜브 우치코미(당기기/채기/기술손)"),
            ("crawl_push", "배밀기"),
            ("plate_spin", "원판 돌리기"),
            ("power_clean", "파워클린(훅훅 채기)"),
            ("grip_crush", "악력(추 감기/교차 잡고 던져 바꿔잡기/매달려 손 교차)"),
            ("rope_climb", "로프 오르기"),
            ("battle_rope", "배틀로프"),
            ("neck_harness", "넥 하니스"),
            ("farmers_walk", "파머스 워크"),
            ("pinch_grip", "핀치 그립(원판 집기)"),
            ("resistance_throw", "저항 던지기(메디신볼/샌드백 안고 메치기)"),
            ("squat_deadlift", "스쿼트/데드리프트"),
        };

        [MenuItem("CounterTime/Build Training Assets")]
        static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OverrideFolder))
                AssetDatabase.CreateFolder(OverrideFolderParent, OverrideFolderName);

            var baseController = BuildBaseController();

            var entries = new List<TrainingCatalog.Entry>();
            foreach (var (id, displayName) in Disciplines)
            {
                var over = BuildOverride(id, baseController);
                entries.Add(new TrainingCatalog.Entry { id = id, displayName = displayName, overrideController = over });
            }

            BuildCatalog(entries);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrainingAssetBuilder] 훈련 클립 스캐폴드 준비 완료 — " +
                      "AC_Training.controller + 13종 오버라이드 + TrainingCatalog.asset.");
        }

        static AnimatorController BuildBaseController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = controller.layers[0].stateMachine;

            var ready = sm.AddState(TrainingSessionController.StateReady);
            ready.motion = FindFighterClip("Fight Idle");
            sm.defaultState = ready;

            var rep = sm.AddState(TrainingSessionController.StateRep);
            rep.motion = FindFighterClip("Standing Melee Punch");

            var strained = sm.AddState(TrainingSessionController.StateStrained);
            strained.motion = FindFighterClip("Stumble Backwards");

            var collapse = sm.AddState(TrainingSessionController.StateCollapse);
            collapse.motion = FindFighterClip("Defeated");

            // 상태 사이 트랜지션은 없다 — TrainingSessionController 가 상태 이름으로 직접
            // CrossFade 한다(ScenarioMotionBinder/ArenaStageView 와 같은 이 프로젝트의
            // 기존 패턴, 새 파라미터·트랜지션 그래프 불필요).
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip FindFighterClip(string stateName)
        {
            var fighterController = AssetDatabase.LoadAssetAtPath<AnimatorController>(FighterAControllerPath);
            if (fighterController == null)
            {
                Debug.LogWarning($"[TrainingAssetBuilder] {FighterAControllerPath} 를 못 찾음 — placeholder 클립 없이 진행.");
                return null;
            }
            foreach (var child in fighterController.layers[0].stateMachine.states)
            {
                if (child.state.name == stateName) return child.state.motion as AnimationClip;
            }
            Debug.LogWarning($"[TrainingAssetBuilder] AC_Fighter_A 에 상태 '{stateName}' 이 없음 — placeholder 클립 없이 진행.");
            return null;
        }

        static RuntimeAnimatorController BuildOverride(string id, AnimatorController baseController)
        {
            var path = $"{OverrideFolder}/AC_Training_{id}.overrideController";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (existing != null) return existing;

            var over = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(over, path);
            return over;
        }

        static void BuildCatalog(List<TrainingCatalog.Entry> entries)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TrainingCatalog>(CatalogPath);
            if (existing != null) return;

            var catalog = ScriptableObject.CreateInstance<TrainingCatalog>();
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("disciplines");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = entries[i].id;
                element.FindPropertyRelative("displayName").stringValue = entries[i].displayName;
                element.FindPropertyRelative("overrideController").objectReferenceValue = entries[i].overrideController;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!Directory.Exists("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
    }
}
