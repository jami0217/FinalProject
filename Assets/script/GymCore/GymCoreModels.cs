using System;
using System.Collections.Generic;

namespace FinalProject.Gym.Core
{
    [Serializable]
    public sealed class AnxietySettings
    {
        public float initialValue = 20f;
        public float increasePerSecond = 8f;
        public float recoveryPerSecond = 3f;
        public float friendlyReduction = 40f;
        public float reassuranceSeconds = 4f;
        public float tenseThreshold = 30f;
        public float highThreshold = 60f;
        public float maximumDifficultyMultiplier = 1.35f;

        public AnxietySettings NormalizedCopy()
        {
            AnxietySettings copy = (AnxietySettings)MemberwiseClone();
            copy.initialValue = Clamp(initialValue, 0f, 100f, 20f);
            copy.increasePerSecond = Clamp(increasePerSecond, 0f, 100f, 8f);
            copy.recoveryPerSecond = Clamp(recoveryPerSecond, 0f, 100f, 3f);
            copy.friendlyReduction = Clamp(friendlyReduction, 0f, 100f, 40f);
            copy.reassuranceSeconds = Clamp(reassuranceSeconds, 0f, 60f, 4f);
            copy.tenseThreshold = Clamp(tenseThreshold, 1f, 98f, 30f);
            copy.highThreshold = Clamp(highThreshold, copy.tenseThreshold + 1f, 100f, 60f);
            copy.maximumDifficultyMultiplier = Clamp(maximumDifficultyMultiplier, 1f, 2f, 1.35f);
            return copy;
        }

        internal static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = fallback;
            return Math.Max(min, Math.Min(max, value));
        }
    }

    public sealed class AnxietyModel
    {
        private readonly AnxietySettings settings;
        private float reassuranceRemaining;
        public float Value { get; private set; }
        public float Normalized => Value / 100f;
        public AnxietyModel(AnxietySettings value) { settings = (value ?? new AnxietySettings()).NormalizedCopy(); Reset(); }
        public void Reset() { Value = settings.initialValue; reassuranceRemaining = 0f; }
        public void SetValue(float value) { Value = AnxietySettings.Clamp(value, 0f, 100f, Value); }
        public void ApplyFriendlyRelief()
        {
            SetValue(Value - settings.friendlyReduction);
            reassuranceRemaining = settings.reassuranceSeconds;
        }
        public void Tick(float deltaTime, bool veteranNearby, bool suspended)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (suspended) return;
            // Integrate a grace-period boundary without making the result frame-rate dependent.
            float recoveryTime = Math.Min(deltaTime, reassuranceRemaining);
            reassuranceRemaining = Math.Max(0f, reassuranceRemaining - deltaTime);
            SetValue(Value - settings.recoveryPerSecond * recoveryTime);
            float remaining = deltaTime - recoveryTime;
            SetValue(Value + remaining * (veteranNearby ? settings.increasePerSecond : -settings.recoveryPerSecond));
        }
        public float GetDifficultyMultiplier()
        {
            float amount = Math.Max(0f, (Value - settings.tenseThreshold) / (100f - settings.tenseThreshold));
            return 1f + amount * (settings.maximumDifficultyMultiplier - 1f);
        }
    }

    public enum GymStationState { NotStarted, InProgress, Completed }

    [Serializable]
    public sealed class GymStationDefinition
    {
        public string id;
        public string displayName;
        public GymStationDefinition(string id, string name) { this.id = id; displayName = name; }
        public static GymStationDefinition[] Defaults() => new[]
        {
            new GymStationDefinition("Dumbbell", "啞鈴"), new GymStationDefinition("Treadmill", "跑步機"),
            new GymStationDefinition("Squat", "深蹲"), new GymStationDefinition("Bike", "健身車"),
            new GymStationDefinition("LegPress", "腿推機")
        };
    }

    public sealed class GymProgressModel
    {
        private readonly Dictionary<string, GymStationState> states = new Dictionary<string, GymStationState>(StringComparer.Ordinal);
        private readonly GymStationDefinition[] stations;
        public IReadOnlyList<GymStationDefinition> Stations => stations;
        public int CompletedCount { get; private set; }
        public int TotalCount => stations.Length;
        public bool AllCompleted => CompletedCount == TotalCount;
        public bool HasAnyProgress
        {
            get { foreach (GymStationState state in states.Values) if (state != GymStationState.NotStarted) return true; return false; }
        }

        public GymProgressModel(IReadOnlyList<GymStationDefinition> definitions)
        {
            if (definitions == null || definitions.Count != 5) throw new ArgumentException("Configure exactly five stations.");
            stations = new GymStationDefinition[5];
            for (int i = 0; i < stations.Length; i++)
            {
                GymStationDefinition item = definitions[i];
                if (item == null || string.IsNullOrWhiteSpace(item.id)) throw new ArgumentException("Every station needs a non-empty ID.");
                string id = item.id.Trim();
                if (states.ContainsKey(id)) throw new ArgumentException("Station IDs must be unique: " + id);
                stations[i] = new GymStationDefinition(id, string.IsNullOrWhiteSpace(item.displayName) ? id : item.displayName);
                states.Add(id, GymStationState.NotStarted);
            }
        }
        public bool Contains(string id) => id != null && states.ContainsKey(id);
        public GymStationState GetState(string id) => Contains(id) ? states[id] : GymStationState.NotStarted;
        public bool StartStation(string id)
        {
            if (!Contains(id)) return false;
            if (states[id] == GymStationState.NotStarted) states[id] = GymStationState.InProgress;
            return true;
        }
        public bool CompleteStation(string id)
        {
            if (!Contains(id) || states[id] == GymStationState.Completed) return false;
            states[id] = GymStationState.Completed;
            CompletedCount++;
            return true;
        }
        public void Reset()
        {
            foreach (GymStationDefinition station in stations) states[station.id] = GymStationState.NotStarted;
            CompletedCount = 0;
        }
    }

    public enum VeteranState { Wandering, Approaching, Waiting, Talking, Leaving, Cooldown }

    [Serializable]
    public sealed class VeteranApproachSettings
    {
        public float detectionRadius = 4f;
        public float stopDistance = 1.25f;
        public float chaseLimit = 6f;
        public float homeLeash = 6f;
        public float approachTimeout = 8f;
        public float waitingTimeout = 8f;
        public float returnTimeout = 8f;
        public float cooldown = 15f;
        public VeteranApproachSettings NormalizedCopy()
        {
            VeteranApproachSettings copy = (VeteranApproachSettings)MemberwiseClone();
            copy.stopDistance = AnxietySettings.Clamp(stopDistance, 0.8f, 5f, 1.25f);
            copy.detectionRadius = AnxietySettings.Clamp(detectionRadius, copy.stopDistance + 0.25f, 20f, 4f);
            copy.chaseLimit = AnxietySettings.Clamp(chaseLimit, copy.detectionRadius, 30f, 6f);
            copy.homeLeash = AnxietySettings.Clamp(homeLeash, copy.stopDistance, 30f, 6f);
            copy.approachTimeout = AnxietySettings.Clamp(approachTimeout, 0.5f, 60f, 8f);
            copy.waitingTimeout = AnxietySettings.Clamp(waitingTimeout, 0.5f, 60f, 8f);
            copy.returnTimeout = AnxietySettings.Clamp(returnTimeout, 0.5f, 60f, 8f);
            copy.cooldown = AnxietySettings.Clamp(cooldown, 1f, 120f, 15f);
            return copy;
        }
    }

    public sealed class VeteranApproachModel
    {
        private readonly VeteranApproachSettings settings;
        private float elapsed;
        public VeteranState State { get; private set; } = VeteranState.Wandering;
        public VeteranApproachModel(VeteranApproachSettings configuration) { settings = (configuration ?? new VeteranApproachSettings()).NormalizedCopy(); }
        public void Tick(float deltaTime, float playerDistance, float homeDistance, bool frozen)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (frozen) return;
            elapsed += deltaTime;
            switch (State)
            {
                case VeteranState.Wandering:
                    if (playerDistance <= settings.detectionRadius && homeDistance <= settings.homeLeash) Change(VeteranState.Approaching);
                    break;
                case VeteranState.Approaching:
                    if (playerDistance > settings.chaseLimit || homeDistance > settings.homeLeash || elapsed >= settings.approachTimeout) Change(VeteranState.Leaving);
                    else if (playerDistance <= settings.stopDistance + 0.05f) Change(VeteranState.Waiting);
                    break;
                case VeteranState.Waiting:
                    if (playerDistance > settings.stopDistance + 1.2f || elapsed >= settings.waitingTimeout) Change(VeteranState.Leaving);
                    break;
                case VeteranState.Leaving:
                    if (homeDistance <= 0.2f || elapsed >= settings.returnTimeout) Change(VeteranState.Cooldown);
                    break;
                case VeteranState.Cooldown:
                    if (elapsed >= settings.cooldown) Change(VeteranState.Wandering);
                    break;
            }
        }
        public bool BeginTalk()
        {
            if (State != VeteranState.Waiting) return false;
            Change(VeteranState.Talking);
            return true;
        }
        public void Dismiss() { if (State != VeteranState.Cooldown) Change(VeteranState.Leaving); }
        public void Reset() { Change(VeteranState.Wandering); }
        private void Change(VeteranState state) { State = state; elapsed = 0f; }
    }
}
