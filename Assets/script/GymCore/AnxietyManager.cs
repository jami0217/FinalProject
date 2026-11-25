using System;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    [DisallowMultipleComponent]
    public sealed class AnxietyManager : MonoBehaviour
    {
        [SerializeField] private AnxietySettings settings = new AnxietySettings();
        [SerializeField] private bool freezeDuringMinigames = true;
        private AnxietyModel model;
        public event Action<float> OnAnxietyChanged;
        public float Value => Model.Value;
        public float AnxietyNormalized => Model.Normalized;
        public string LevelLabel => Value < settings.tenseThreshold ? "平穩" : Value < settings.highThreshold ? "緊張" : "高焦慮";
        public AnxietySettings Settings => settings;
        private AnxietyModel Model => model ?? (model = new AnxietyModel(settings));
        private void Update()
        {
            GymGameBridge bridge = GymGameBridge.Instance;
            if (bridge == null) return;
            bool suspended = bridge.IsModal || (freezeDuringMinigames && bridge.IsMinigameActive);
            float previous = Value;
            Model.Tick(Time.deltaTime, bridge.NearestAnxietySource != null, suspended);
            if (previous != Value) OnAnxietyChanged?.Invoke(Value);
        }
        public void Configure(AnxietySettings configuration, bool freezeInMinigames = true)
        {
            if (model != null) return; // Preserve a round when moving between scenes.
            settings = (configuration ?? new AnxietySettings()).NormalizedCopy();
            freezeDuringMinigames = freezeInMinigames;
        }
        public void ApplyFriendlyRelief() { Model.ApplyFriendlyRelief(); OnAnxietyChanged?.Invoke(Value); }
        public void ResetRound() { Model.Reset(); OnAnxietyChanged?.Invoke(Value); }
        public float GetDifficultyMultiplier() => Model.GetDifficultyMultiplier();
        public void SetValue(float value) { Model.SetValue(value); OnAnxietyChanged?.Invoke(Value); }
        private void OnValidate() { settings = (settings ?? new AnxietySettings()).NormalizedCopy(); }
    }
}
