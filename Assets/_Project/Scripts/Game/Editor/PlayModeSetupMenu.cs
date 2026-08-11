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

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var go = new GameObject("LiveMatch", typeof(LiveMatchRunner), typeof(LiveMatchPanel));

            var runnerSo = new SerializedObject(go.GetComponent<LiveMatchRunner>());
            runnerSo.FindProperty("setupEnabled").boolValue = true;
            runnerSo.ApplyModifiedPropertiesWithoutUndo();

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
    }
}
