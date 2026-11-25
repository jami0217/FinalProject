using UnityEngine;

namespace FinalProject.Gym.Core
{
    public sealed class AnxietyHUD : MonoBehaviour
    {
        private GymGameBridge bridge;
        private GymCoreUI ui;
        private AnxietyManager source;
        public void Bind(GymGameBridge context, GymCoreUI view)
        {
            Unsubscribe(); bridge = context; ui = view;
            source = bridge.Anxiety;
            source.OnAnxietyChanged += Refresh;
            if (ui.reduceMotionToggle != null)
            {
                ui.reduceMotionToggle.SetIsOnWithoutNotify(bridge.Effects.ReduceMotion);
                ui.reduceMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            }
            Refresh(bridge.Anxiety.Value);
        }
        private void Refresh(float value)
        {
            if (ui == null || bridge == null) return;
            if (ui.anxietyText != null) ui.anxietyText.text = $"焦慮 {Mathf.FloorToInt(value)}/100　{bridge.Anxiety.LevelLabel}";
            if (ui.anxietyFill != null)
            {
                ui.anxietyFill.rectTransform.anchorMax = new Vector2(bridge.Anxiety.AnxietyNormalized, 1f);
                ui.anxietyFill.rectTransform.offsetMin = ui.anxietyFill.rectTransform.offsetMax = Vector2.zero;
                ui.anxietyFill.color = value < bridge.Anxiety.Settings.tenseThreshold ? new Color32(83, 197, 142, 255)
                    : value < bridge.Anxiety.Settings.highThreshold ? new Color32(238, 195, 91, 255) : new Color32(225, 110, 98, 255);
            }
        }
        private void SetReducedMotion(bool value) { if (bridge != null && bridge.Effects != null) bridge.Effects.ReduceMotion = value; }
        private void Unsubscribe()
        {
            if (source != null) source.OnAnxietyChanged -= Refresh;
            source = null;
            if (ui != null && ui.reduceMotionToggle != null) ui.reduceMotionToggle.onValueChanged.RemoveListener(SetReducedMotion);
        }
        private void OnDisable() { Unsubscribe(); }
        private void OnEnable() { if (Application.isPlaying && bridge != null && ui != null) Bind(bridge, ui); }
    }
}
