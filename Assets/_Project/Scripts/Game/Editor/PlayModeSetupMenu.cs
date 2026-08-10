using CounterTime.Game.Match;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CounterTime.Game.Editor
{
    /// <summary>
    /// Stage 6 Play 모드 확인 준비용 2클릭 메뉴. 새 빈 씬(저장 안 함, Main.unity 무수정)에
    /// <see cref="LiveMatchRunner"/>+<see cref="LiveMatchPanel"/> 을 가진 GameObject 하나를
    /// 만들고 쿠즈시 플래그를 켠다. B 브레인 연결은 두 컴포넌트의 Awake/Build 가 이미
    /// 자동으로 하므로(<see cref="LiveMatchRunner.Build"/> 참조) 여기서 따로 배선할 것이 없다.
    ///
    /// <see cref="LiveMatchPanel.BuildCanvas"/> 가 런타임에 만드는 캔버스에는
    /// GraphicRaycaster 가 없다 — 코드 자체의 누락이고, 이 메뉴는 <c>LiveMatchPanel.cs</c>
    /// 를 고치는 대신 Play 진입 시점에 캔버스를 찾아 하나 붙이는 것으로 우회한다.
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

            EditorApplication.playModeStateChanged -= AttachGraphicRaycasterOnPlay;
            EditorApplication.playModeStateChanged += AttachGraphicRaycasterOnPlay;

            Debug.Log("[PlayModeSetupMenu] LiveMatch 준비 완료 — 쿠즈시 on, 한글 폰트 연결. " +
                      "Play 버튼만 누르면 된다 (GraphicRaycaster는 Play 진입 시 자동 부착).");
        }

        static void AttachGraphicRaycasterOnPlay(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= AttachGraphicRaycasterOnPlay;

            var canvasGo = GameObject.Find("LiveMatch/LiveMatchCanvas");
            if (canvasGo != null && canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();
        }
    }
}
