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
    /// 만들고 쿠즈시 플래그를 켠다. B 브레인 연결은 두 컴포넌트의 Awake/Build 가 이미
    /// 자동으로 하므로(<see cref="LiveMatchRunner.Build"/> 참조) 여기서 따로 배선할 것이 없다.
    /// </summary>
    static class PlayModeSetupMenu
    {
        const string FontPath = "Assets/_Project/Art/Fonts/NotoSansKR SDF.asset";

        [MenuItem("CounterTime/Play Mode Setup")]
        static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var go = new GameObject("LiveMatch", typeof(LiveMatchRunner), typeof(LiveMatchPanel));

            var runnerSo = new SerializedObject(go.GetComponent<LiveMatchRunner>());
            runnerSo.FindProperty("kuzushiEnabled").boolValue = true;
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

            Debug.Log("[PlayModeSetupMenu] LiveMatch 준비 완료 — 쿠즈시 on, 한글 폰트 연결. " +
                      "Play 버튼만 누르면 된다.");
        }
    }
}
