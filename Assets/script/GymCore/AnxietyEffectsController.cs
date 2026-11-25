using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalProject.Gym.Core
{
    public sealed class AnxietyEffectsController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool shakeEnabled = true;
        [SerializeField] private bool reduceMotion;
        [SerializeField, Range(0f, 0.1f)] private float maximumShake = 0.025f;
        [SerializeField, Range(1f, 30f)] private float shakeFrequency = 12f;
        private GymGameBridge bridge;
        private AnxietyVignetteGraphic vignette;
        private Camera appliedCamera;
        private Matrix4x4 originalMatrix;
        private bool matrixApplied;
        public event Action<float> OptionalDistortionIntensityChanged;
        public float Intensity { get; private set; }
        public float OptionalDistortionIntensity => reduceMotion ? 0f : Intensity;
        public bool ReduceMotion
        {
            get => reduceMotion;
            set
            {
                if (reduceMotion == value) return;
                reduceMotion = value;
                if (value) RestoreCamera();
                OptionalDistortionIntensityChanged?.Invoke(OptionalDistortionIntensity);
            }
        }

        public void Bind(GymGameBridge context, AnxietyVignetteGraphic graphic)
        {
            bridge = context; vignette = graphic;
            if (targetCamera == null) targetCamera = Camera.main;
        }
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginSrp;
            RenderPipelineManager.endCameraRendering += EndSrp;
            Camera.onPreCull += BeginBuiltin;
            Camera.onPostRender += EndBuiltin;
        }
        private void LateUpdate()
        {
            float previous = OptionalDistortionIntensity;
            float amount = 0f;
            if (bridge != null && bridge.isActiveAndEnabled && bridge.Anxiety != null && !bridge.IsModal)
                amount = Mathf.InverseLerp(bridge.Anxiety.Settings.tenseThreshold / 100f, 1f, bridge.Anxiety.AnxietyNormalized);
            Intensity = amount * amount;
            if (vignette != null) vignette.SetIntensity(Intensity);
            if (previous != OptionalDistortionIntensity) OptionalDistortionIntensityChanged?.Invoke(OptionalDistortionIntensity);
        }
        private void BeginSrp(ScriptableRenderContext context, Camera camera) { ApplyCamera(camera); }
        private void EndSrp(ScriptableRenderContext context, Camera camera) { if (camera == appliedCamera) RestoreCamera(); }
        private void BeginBuiltin(Camera camera) { if (GraphicsSettings.currentRenderPipeline == null) ApplyCamera(camera); }
        private void EndBuiltin(Camera camera) { if (GraphicsSettings.currentRenderPipeline == null && camera == appliedCamera) RestoreCamera(); }
        private void ApplyCamera(Camera camera)
        {
            if (bridge == null || !bridge.isActiveAndEnabled || camera != targetCamera || !shakeEnabled || reduceMotion || Intensity <= 0f) return;
            RestoreCamera();
            float t = Time.unscaledTime * shakeFrequency;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 17.3f) * 2f - 1f,
                Mathf.PerlinNoise(53.1f, t) * 2f - 1f, 0f) * (maximumShake * Intensity);
            appliedCamera = camera;
            originalMatrix = camera.worldToCameraMatrix;
            camera.worldToCameraMatrix = Matrix4x4.Translate(-offset) * originalMatrix;
            matrixApplied = true;
        }
        private void RestoreCamera()
        {
            if (matrixApplied && appliedCamera != null) appliedCamera.worldToCameraMatrix = originalMatrix;
            matrixApplied = false;
            appliedCamera = null;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginSrp;
            RenderPipelineManager.endCameraRendering -= EndSrp;
            Camera.onPreCull -= BeginBuiltin;
            Camera.onPostRender -= EndBuiltin;
            ClearImmediately();
        }
        public void ClearImmediately()
        {
            RestoreCamera();
            Intensity = 0f;
            if (vignette != null) vignette.SetIntensity(0f);
            OptionalDistortionIntensityChanged?.Invoke(0f);
        }
    }
}
