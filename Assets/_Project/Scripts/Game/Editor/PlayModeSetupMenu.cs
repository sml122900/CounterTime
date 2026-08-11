using CounterTime.Game.Match;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CounterTime.Game.Editor
{
    /// <summary>
    /// Stage 6 Play 모드 확인 준비용 2클릭 메뉴. 새 빈 씬(저장 안 함, Main.unity 무수정)에
    /// <see cref="LiveMatchRunner"/>+<see cref="LiveMatchPanel"/> 을 가진 GameObject 하나를
    /// 만들고 흔들기 플래그를 켠다. B 브레인 연결은 두 컴포넌트의 Awake/Build 가 이미
    /// 자동으로 하므로(<see cref="LiveMatchRunner.Build"/> 참조) 여기서 따로 배선할 것이 없다.
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

            Selection.activeGameObject = go;

            Debug.Log("[PlayModeSetupMenu] LiveMatch 준비 완료 — 흔들기 on, 한글 폰트 연결. " +
                      "Play 버튼만 누르면 된다.");
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
