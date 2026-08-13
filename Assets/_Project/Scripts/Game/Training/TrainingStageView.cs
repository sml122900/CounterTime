using UnityEngine;

namespace CounterTime.Game.Training
{
    /// <summary>
    /// 훈련 화면의 저해상도 렌더 — <see cref="CounterTime.Game.Match.ArenaStageView"/> 와
    /// 같은 패턴(전용 카메라+바닥+조명 → RenderTexture, 12fps 스텝)을 캐릭터 1명으로 축소한
    /// 것. 컨트롤러(<see cref="Animator.runtimeAnimatorController"/>)는 종목이 바뀔 때마다
    /// <see cref="SetController"/> 로 교체한다 — <see cref="AnimatorOverrideController"/> 도
    /// <c>RuntimeAnimatorController</c> 라 이 한 메서드로 오버라이드 스왑까지 전부 된다.
    /// </summary>
    public sealed class TrainingStageView : MonoBehaviour
    {
        const int StageWidth = 320;
        const int StageHeight = 180;
        const float StepSeconds = 1f / 12f;

        [SerializeField] GameObject fighterModel;
        [SerializeField] RuntimeAnimatorController defaultController;
        [SerializeField] Material floorMaterial;

        Animator _animator;
        float _accum;
        bool _built;

        public RenderTexture Output { get; private set; }
        public Animator Animator => _animator;

        void Awake() => EnsureBuilt();

        /// <see cref="MetaFlowController"/>가 <see cref="TrainingSessionController"/> 보다
        /// 먼저 Output/Animator 를 읽을 수 있어 <see cref="ArenaStageView.EnsureBuilt"/> 와
        /// 같은 이유로 멱등 진입점을 둔다.
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            Output = new RenderTexture(StageWidth, StageHeight, 24) { antiAliasing = 1, filterMode = FilterMode.Point };

            BuildCamera();
            BuildFloor();
            BuildLight();
            BuildFighter();
        }

        void Update()
        {
            _accum += Time.deltaTime;
            while (_accum >= StepSeconds)
            {
                _accum -= StepSeconds;
                if (_animator != null) _animator.Update(StepSeconds);
            }
        }

        void OnDestroy()
        {
            if (Output != null) Output.Release();
        }

        public void SetController(RuntimeAnimatorController controller)
        {
            if (_animator == null) return;
            _animator.runtimeAnimatorController = controller != null ? controller : defaultController;
        }

        void BuildCamera()
        {
            var go = new GameObject("TrainingStageCamera", typeof(Camera));
            // 캐릭터 1명, 카메라 정면 — ArenaStageView 의 측면 2인 배치와 달리 정면
            // 훈련 영상 구도를 노린다. 값은 초기 배치(⚙) — 사람이 Play 로 프레이밍을
            // 확인·조정할 여지를 남긴다(ArenaStageView 처럼 반복 실측까지는 이번 범위 밖).
            go.transform.SetPositionAndRotation(new Vector3(0, 1.5f, -3.2f), Quaternion.identity);
            var cam = go.GetComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            cam.allowMSAA = false;
            cam.targetTexture = Output;
        }

        void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TrainingFloor";
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (floorMaterial != null) floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        }

        void BuildLight()
        {
            var go = new GameObject("TrainingLight", typeof(Light));
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
        }

        void BuildFighter()
        {
            if (fighterModel == null) return;
            // 정면(-Z 쪽 카메라)을 보도록 180도 돌린다 — 원본 Main.unity 배치(측면 대치)와
            // 달리 훈련 화면은 캐릭터가 카메라를 정면으로 보는 구도.
            var instance = Instantiate(fighterModel, Vector3.zero, Quaternion.Euler(0, 180, 0));
            _animator = instance.GetComponent<Animator>();
            _animator.runtimeAnimatorController = defaultController;
            _animator.applyRootMotion = false;
            _animator.enabled = false;
        }
    }
}
