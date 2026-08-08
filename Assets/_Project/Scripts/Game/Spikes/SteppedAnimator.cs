using UnityEngine;

namespace CounterTime.Game
{
    /// Evaluates an Animator at a fixed low frame rate to produce a stepped,
    /// hand-animated look and to discard sub-frame mocap jitter.
    [RequireComponent(typeof(Animator))]
    public sealed class SteppedAnimator : MonoBehaviour
    {
        [SerializeField, Range(4f, 30f)] private float framesPerSecond = 12f;

        private Animator _animator;
        private float _accumulator;

        /// Current step rate. Setting it re-arms the accumulator so a live change
        /// in the Inspector or from a test harness takes effect on the next step.
        public float FramesPerSecond
        {
            get => framesPerSecond;
            set { framesPerSecond = value; _accumulator = 0f; }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.enabled = false; // stop Unity's automatic per-frame evaluation
        }

        private void OnDisable()
        {
            // Hand evaluation back to Unity so stage A (baseline) runs at full rate.
            if (_animator != null) _animator.enabled = true;
        }

        private void OnEnable()
        {
            if (_animator != null) _animator.enabled = false;
        }

        private void Update()
        {
            if (framesPerSecond <= 0f) return;

            float step = 1f / framesPerSecond;
            _accumulator += Time.deltaTime;

            while (_accumulator >= step)
            {
                _animator.Update(step);
                _accumulator -= step;
            }
        }
    }
}
