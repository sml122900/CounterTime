using System.Collections;
using System.IO;
using UnityEngine;

namespace CounterTime.Game
{
    /// Look-dev evidence capture for the low-res render spike.
    /// Writes the current frame twice: once at native RenderTexture size, once
    /// upscaled x4 with nearest-neighbour only (a smoothed upscale would
    /// misrepresent the pixel grid this spike is testing).
    public sealed class SpikeCapture : MonoBehaviour
    {
        [Tooltip("Stage label used as the filename prefix, e.g. A_baseline.")]
        public string stageTag = "A_baseline";

        public Camera sourceCamera;
        [Tooltip("Leave empty for the full-resolution baseline stage.")]
        public RenderTexture lowResTarget;
        public SteppedAnimator fpsSource;
        [SerializeField] private KeyCode captureKey = KeyCode.C;
        [SerializeField] private int baselineWidth = 1280;
        [SerializeField] private int baselineHeight = 720;
        [Tooltip("MSAA samples for the full-resolution baseline stage only.")]
        [SerializeField] private int baselineMsaaSamples = 4;
        [SerializeField] private int upscaleFactor = 4;

        private const string CaptureDir = "Assets/_Project/Spikes/LowResRender/Captures";

        private void Update()
        {
            // This project runs the Input System package, where the legacy Input class
            // throws. Without the legacy manager, capture from the component's
            // context menu (right-click the SpikeCapture header > Capture) instead.
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(captureKey)) Capture();
#endif
        }

        /// Callable from the Inspector context menu, a key press, or an editor command.
        [ContextMenu("Capture")]
        public void Capture() => StartCoroutine(CaptureRoutine());

        /// Captures N consecutive rendered frames. Stepping is a temporal effect —
        /// a single still cannot show the difference between stage B and stage C.
        public void CaptureBurst(int frames) => StartCoroutine(BurstRoutine(frames));

        private IEnumerator BurstRoutine(int frames)
        {
            string tag = stageTag;
            for (int i = 0; i < frames; i++)
            {
                stageTag = $"{tag}_f{i:00}";
                yield return CaptureRoutine();
            }
            stageTag = tag;
        }

        private IEnumerator CaptureRoutine()
        {
            yield return new WaitForEndOfFrame(); // let the camera finish this frame

            bool baseline = lowResTarget == null;
            RenderTexture rt = lowResTarget;
            if (baseline)
            {
                rt = RenderTexture.GetTemporary(baselineWidth, baselineHeight, 24,
                    RenderTextureFormat.Default, RenderTextureReadWrite.Default, baselineMsaaSamples);
                RenderTexture prevTarget = sourceCamera.targetTexture;
                sourceCamera.targetTexture = rt;
                sourceCamera.Render();
                sourceCamera.targetTexture = prevTarget;
            }

            Texture2D native = ReadBack(rt);
            Directory.CreateDirectory(CaptureDir);

            int fps = fpsSource != null && fpsSource.isActiveAndEnabled
                ? Mathf.RoundToInt(fpsSource.FramesPerSecond)
                : Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f));
            string stamp = System.DateTime.Now.ToString("HHmmss");
            string basename = $"{stageTag}_{fps}fps_{stamp}";

            File.WriteAllBytes($"{CaptureDir}/{basename}.png", native.EncodeToPNG());

            if (!baseline)
            {
                Texture2D big = NearestUpscale(native, upscaleFactor);
                File.WriteAllBytes($"{CaptureDir}/{basename}_x{upscaleFactor}.png", big.EncodeToPNG());
                Destroy(big);
            }

            Destroy(native);
            if (baseline) RenderTexture.ReleaseTemporary(rt);

            Debug.Log($"[SpikeCapture] wrote {basename}");
        }

        private static Texture2D ReadBack(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        private static Texture2D NearestUpscale(Texture2D src, int factor)
        {
            int w = src.width * factor, h = src.height * factor;
            Color32[] s = src.GetPixels32();
            var d = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = (y / factor) * src.width;
                int outRow = y * w;
                for (int x = 0; x < w; x++) d[outRow + x] = s[row + x / factor];
            }
            var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
            dst.SetPixels32(d);
            dst.Apply();
            return dst;
        }
    }
}
