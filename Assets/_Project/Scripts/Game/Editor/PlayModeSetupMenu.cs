using CounterTime.Game.Match;
using CounterTime.Game.Meta;
using CounterTime.Game.Training;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CounterTime.Game.Editor
{
    /// <summary>
    /// Play 모드 확인 준비용 2클릭 메뉴. 새 빈 씬(저장 안 함, Main.unity 무수정)에
    /// <see cref="LiveMatchRunner"/>+<see cref="LiveMatchPanel"/> 을 가진 "LiveMatch"
    /// GameObject 와, §H 3.5b 부터는 그 위에서 지도/대회 흐름을 관장하는
    /// <see cref="MetaFlowController"/> 를 가진 "MetaFlow" GameObject 를 함께 만든다.
    /// 진입점은 이제 지도다 — LiveMatch 는 <b>비활성 상태로</b> 만들어 대진표에서
    /// "경기 시작"을 눌러야 켜진다(<see cref="MetaFlowController.StartMatch"/> 참조).
    /// B 브레인 연결은 <see cref="LiveMatchRunner"/> 의 Awake/Build 가 이미 자동으로
    /// 하므로 여기서 따로 배선할 것이 없다.
    /// </summary>
    static class PlayModeSetupMenu
    {
        // LiveMatchPanel.KoreanFontResourceName 과 같은 에셋을 가리켜야 한다 — 저 상수는
        // Awake 안전망(Resources.Load)이, 이 경로는 여기(AssetDatabase.LoadAssetAtPath,
        // 에디터 전용)가 쓴다. 두 곳이 각자 문자열을 들고 있으면 나중에 파일을 옮길 때
        // 한쪽만 고치고 잊어버리기 쉽다 — 이 문자열 결합으로 묶어 그 여지를 없앤다.
        const string FontPath = "Assets/_Project/Art/Fonts/Resources/" + LiveMatchPanel.KoreanFontResourceName + ".asset";

        // ArenaStageView 가 무대를 새로 짓는 데 쓰는 원본 에셋 — Main.unity 의
        // Fighter_A/B 가 실은 이 FBX 들의 PrefabInstance 라는 걸 씬 YAML 실측으로
        // 확인했으므로(2026-08-12), 프리팹화 없이 그대로 Instantiate 대상으로 쓴다.
        const string FighterAModelPath = "Assets/_Project/Art/Characters/Y Bot.fbx";
        const string FighterBModelPath = "Assets/_Project/Art/Characters/Ch30_nonPBR.fbx";
        const string FighterAControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_A.controller";
        const string FighterBControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_B.controller";
        const string FloorMaterialPath = "Assets/_Project/Art/M_Stage_Floor.mat";

        // 훈련 클립 스캐폴드 — CounterTime/Build Training Assets(TrainingAssetBuilder)가
        // 미리 만들어둬야 하는 에셋. 여기서는 없으면 경고만 하고 넘어간다(재생성은 그
        // 메뉴 몫 — 여기서 만들면 오버라이드 컨트롤러에 채운 모캡을 덮어쓸 위험이 있다).
        const string TrainingControllerPath = "Assets/_Project/Art/Animations/AC_Training.controller";
        const string TrainingCatalogPath = "Assets/_Project/ScriptableObjects/TrainingCatalog.asset";

        [MenuItem("CounterTime/Play Mode Setup")]
        static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[PlayModeSetupMenu] 에디터가 Play 전환 중이라 Setup 을 건너뜀 — " +
                                  "Play/Stop 이 끝난 뒤 다시 실행할 것.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[PlayModeSetupMenu] 씬 저장 확인에서 취소되어 Setup 을 중단함 — " +
                                  "기존 LiveMatch 오브젝트가 있어도 이번 실행으로 갱신되지 않았다. " +
                                  "그 상태로 Play 를 누르면 안 됨 — 다시 실행할 것.");
                return;
            }

            // Game View 의 커스텀 렌더 해상도는 에디터 세션에 걸쳐 남는다 — 지난 세션이
            // 좁은 화면 레이아웃 테스트로 719x404 등을 남겨두면, 다음에 Play 를 누르는
            // 사람은 CanvasScaler(참조 해상도 1920x1080) 가 그 실제 해상도에 맞춰 scaleFactor
            // 를 낮춰버려 UI 텍스트 전체가 저해상도로 렌더된 뒤 창 크기로 늘어나 보인다
            // (2026-08-12 "UI 텍스트까지 도트화" 신고 — Canvas.scaleFactor=0.374 로 실측
            // 확인, RT 파이프라인과는 무관). 매번 CanvasScaler 참조 해상도와 맞춰 리셋한다.
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "CounterTime Play");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            // ArenaStageView 가 짓는 무대(바닥+파이터)는 전용 카메라로 RT 에만 그린다 —
            // 이 카메라는 원래 목적(카메라가 하나도 없으면 URP 가 화면을 검게 그리던
            // f241b7b 버그의 회피)만 남기고, 같은 씬의 무대를 화면 전체에 중복으로
            // 그리지 않도록 아무 것도 보지 않게 한다.
            camGo.GetComponent<Camera>().cullingMask = 0;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var go = new GameObject("LiveMatch",
                typeof(LiveMatchRunner), typeof(ArenaStageView), typeof(LiveMatchPanel));

            var runnerSo = new SerializedObject(go.GetComponent<LiveMatchRunner>());
            runnerSo.FindProperty("setupEnabled").boolValue = true;
            // §2b v4.2 — on for Play verification of the new hand-wheel flow.
            runnerSo.FindProperty("gripRevisionEnabled").boolValue = true;
            runnerSo.ApplyModifiedPropertiesWithoutUndo();

            var stageSo = new SerializedObject(go.GetComponent<ArenaStageView>());
            AssignAsset<GameObject>(stageSo, "fighterAModel", FighterAModelPath);
            AssignAsset<GameObject>(stageSo, "fighterBModel", FighterBModelPath);
            AssignAsset<RuntimeAnimatorController>(stageSo, "fighterAController", FighterAControllerPath);
            AssignAsset<RuntimeAnimatorController>(stageSo, "fighterBController", FighterBControllerPath);
            AssignAsset<Material>(stageSo, "floorMaterial", FloorMaterialPath);
            stageSo.ApplyModifiedPropertiesWithoutUndo();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                var panelSo = new SerializedObject(go.GetComponent<LiveMatchPanel>());
                panelSo.FindProperty("koreanFont").objectReferenceValue = font;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[PlayModeSetupMenu] 한글 폰트를 못 찾음: {FontPath}");
            }

            // §H 3.5b — 진입점이 지도로 바뀌었다. LiveMatch 는 대회에서 "경기 시작"을
            // 누르기 전까지 비활성이어야 한다 — 활성 상태로 Play 를 시작하면 Awake 가
            // MetaFlow 보다 먼저/나중 어느 순서로도 돌 수 있어(같은 프레임 안이라도
            // 씬의 두 루트 오브젝트 사이 Awake 순서는 보장되지 않는다) 화면이 잠깐
            // LiveMatch 로 나타날 위험이 있다. 편집 모드에서 미리 꺼두면 Unity 가
            // Awake 자체를 MetaFlowController.StartMatch 가 SetActive(true) 할 때까지
            // 아예 미룬다 — 순서 운에 기대지 않는 확정적인 방법.
            go.SetActive(false);

            // DESIGN_training.md §5-A — 훈련 3D 스테이지. LiveMatch 와 같은 이유로
            // 비활성 상태로 미리 지어두고, MetaFlowController.StartTrainingRun 이
            // SetActive(true) 할 때 Awake(EnsureBuilt 포함)가 그 자리에서 완주한다.
            var trainingGo = new GameObject("Training", typeof(TrainingStageView), typeof(TrainingSessionController));
            var trainingStageSo = new SerializedObject(trainingGo.GetComponent<TrainingStageView>());
            AssignAsset<GameObject>(trainingStageSo, "fighterModel", FighterAModelPath);
            AssignAsset<RuntimeAnimatorController>(trainingStageSo, "defaultController", TrainingControllerPath);
            AssignAsset<Material>(trainingStageSo, "floorMaterial", FloorMaterialPath);
            trainingStageSo.ApplyModifiedPropertiesWithoutUndo();
            trainingGo.SetActive(false);

            var trainingCatalog = AssetDatabase.LoadAssetAtPath<TrainingCatalog>(TrainingCatalogPath);
            if (trainingCatalog == null)
                Debug.LogWarning("[PlayModeSetupMenu] 훈련 카탈로그를 못 찾음 — 먼저 " +
                                  "CounterTime > Build Training Assets 를 실행할 것: " + TrainingCatalogPath);

            var metaGo = new GameObject("MetaFlow", typeof(MetaFlowController));
            var metaSo = new SerializedObject(metaGo.GetComponent<MetaFlowController>());
            if (font != null) metaSo.FindProperty("koreanFont").objectReferenceValue = font;
            metaSo.FindProperty("liveMatchRunner").objectReferenceValue = go.GetComponent<LiveMatchRunner>();
            metaSo.FindProperty("liveMatchPanel").objectReferenceValue = go.GetComponent<LiveMatchPanel>();
            metaSo.FindProperty("liveMatchRoot").objectReferenceValue = go;
            metaSo.FindProperty("trainingSession").objectReferenceValue = trainingGo.GetComponent<TrainingSessionController>();
            if (trainingCatalog != null) metaSo.FindProperty("trainingCatalog").objectReferenceValue = trainingCatalog;
            metaSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = metaGo;

            Debug.Log("[PlayModeSetupMenu] 지도/대회 흐름 준비 완료 — LiveMatch 는 비활성(경기 시작 시 켜짐), " +
                      "흔들기 on, 한글 폰트 연결. Play 버튼만 누르면 된다.");
        }

        static void AssignAsset<T>(SerializedObject so, string propertyName, string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = asset;
            }
            else
            {
                Debug.LogWarning($"[PlayModeSetupMenu] {propertyName} 에셋을 못 찾음: {path}");
            }
        }
    }
}
