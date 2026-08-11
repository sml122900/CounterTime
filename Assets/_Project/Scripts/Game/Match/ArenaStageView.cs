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
            go.transform.SetPositionAndRotation(new Vector3(0, 1.6f, -6f), Quaternion.identity);
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
