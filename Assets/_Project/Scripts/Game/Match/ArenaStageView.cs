using UnityEngine;

namespace CounterTime.Game.Match
{
    /// <summary>
    /// 6단계 — 경기 화면 저해상도 렌더 v1. 두 파이터를 Fight Idle 로 세워
    /// 320x180 <see cref="RenderTexture"/> 에 12fps 스텝으로 그린다. 연출·모션
    /// 매핑(노드별 클립 전환)은 이번 범위 밖 — <see cref="LiveMatchPanel"/> 이
    /// 이 텍스처를 RawImage 로 표시하는 것 말고는 아무 것도 하지 않는다.
    ///
    /// HUD·텍스트는 이 RT 안에 없다 — MATCH_RULES §7 이 요구하는 대로 잡기 상태
    /// 등은 전부 RenderTexture 밖(기존 UI 패널)에 남는다. 여기는 경기 장면만.
    /// </summary>
    public sealed class ArenaStageView : MonoBehaviour
    {
        public const int StageWidth = 320;
        public const int StageHeight = 180;
        const float StepSeconds = 1f / 12f;

        [SerializeField] GameObject fighterAModel;
        [SerializeField] GameObject fighterBModel;
        [SerializeField] RuntimeAnimatorController fighterAController;
        [SerializeField] RuntimeAnimatorController fighterBController;
        [SerializeField] Material floorMaterial;

        Animator _animA, _animB;
        float _accum;
        bool _built;

        public RenderTexture Output { get; private set; }

        void Awake() => EnsureBuilt();

        /// <summary><see cref="LiveMatchPanel"/> 은 이 컴포넌트의 <c>Output</c> 을
        /// 자기 <c>Awake</c> 에서 읽는다 — 같은 GameObject 위 두 컴포넌트의 Awake
        /// 순서는 Unity 가 보장하지 않으므로, 멱등하게 만들어 호출 쪽에서 먼저
        /// 불러 순서 문제를 없앤다.</summary>
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            Output = new RenderTexture(StageWidth, StageHeight, 24) { antiAliasing = 1, filterMode = FilterMode.Point };

            BuildCamera();
            BuildFloor();
            BuildLight();
            _animA = BuildFighter(fighterAModel, fighterAController, new Vector3(-1, 0, 0), Quaternion.identity);
            _animB = BuildFighter(fighterBModel, fighterBController, new Vector3(1, 0, 0), Quaternion.Euler(0, -90, 0));
        }

        void Update()
        {
            _accum += Time.deltaTime;
            while (_accum >= StepSeconds)
            {
                _accum -= StepSeconds;
                if (_animA != null) _animA.Update(StepSeconds);
                if (_animB != null) _animB.Update(StepSeconds);
            }
        }

        void OnDestroy()
        {
            if (Output != null) Output.Release();
        }

        void BuildCamera()
        {
            var go = new GameObject("ArenaStageCamera", typeof(Camera));
            // Main.unity 카메라(0,1.6,-6 / FOV45 / 수평)를 그대로 썼을 때 320x180 에서
            // 캐릭터 세로가 70.8px 로 실측돼 목표(≈100px)에 못 미쳤다. FOV 는 그대로 두고
            // 거리만 좁히고(-6→-4.25) 하향 11° 를 줘서 재실측 — A 100.9px/B 99.6px,
            // 좌우(57~232, 프레임 0..320)·상하(41~143, 프레임 0..180) 모두 클리핑 없음
            // (2026-08-12, ArenaStageCamera.WorldToScreenPoint 로 두 파이터 바운즈 직접 실측).
            //
            // 2026-08-12 후속 — 전체화면 배경 전환 후 B 가 우측 사이드바(화면폭의 약
            // 65% 지점부터 시작) 뒤로 완전히 가려진다는 신고. x=0 기준 두 파이터의 결합
            // RT-x 폭은 169px(320 중)로 사이드바 이전 안전폭(~208px)보다 좁았다 — 즉
            // 확대·축소 없이 옆으로 이동만 하면 됐다(세로 100px 규격 무관). x=0.6 으로
            // 이동해 재실측: B 우측 끝 RTx=201.5(안전선 207.8 대비 여유), A 좌측 끝
            // RTx=26.0(왼쪽 여유 충분), 세로 A 102.1px/B 100.9px(규격 유지). 16:9(1920x1080/
            // 1280x720/960x540/320x180) 및 근사치(719x404/600x338)에서 확인됨.
            // 4:3(1024x768) 은 EnvelopeParent 가 좌우 25% 를 잘라내는 창이라 이 x=0.6 로도
            // A 왼쪽이 화면 밖으로 나가고 B 도 사이드바에 다시 걸린다(실측 확인, 코드로
            // 안 고침) — 크롭 방식이 종횡비마다 달라 정적 카메라 하나로 모든 창 비율을
            // 동시에 만족시킬 수 없다. 16:9(주 타깃으로 추정)를 우선했다.
            go.transform.SetPositionAndRotation(new Vector3(0.6f, 1.6f, -4.25f), Quaternion.Euler(11f, 0, 0));
            var cam = go.GetComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f);
            cam.allowMSAA = false;
            cam.targetTexture = Output;
        }

        void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (floorMaterial != null) floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        }

        void BuildLight()
        {
            var go = new GameObject("ArenaLight", typeof(Light));
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
        }

        Animator BuildFighter(GameObject model, RuntimeAnimatorController controller, Vector3 pos, Quaternion rot)
        {
            if (model == null) return null;
            var instance = Instantiate(model, pos, rot);
            var animator = instance.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = false;
            return animator;
        }
    }
}
