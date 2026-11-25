using System;

namespace FinalProject.Gym.Squat
{
    public enum SquatGameState { Ready, Sweeping, Exhausted, Feedback, Completed }
    public enum SquatHitResult { None, Perfect, Good, Exhausted, Recovered }

    [Serializable]
    public sealed class SquatGameSettings
    {
        public int repsToWin = 5;
        public int recoveryPressesRequired = 8;
        public float sweepSpeed = 0.7f;
        public float sweepAcceleration = 0.15f;
        public float maximumSweepSpeed = 2f;
        public float feedbackDuration = 0.7f;
        public float leftDangerEnd = 0.15f;
        public float perfectStart = 0.4f;
        public float perfectEnd = 0.6f;
        public float perfectWidthMultiplierPerRep = 0.8f;
        public float rightDangerStart = 0.85f;

        public SquatGameSettings NormalizedCopy()
        {
            SquatGameSettings copy = (SquatGameSettings)MemberwiseClone();
            copy.repsToWin = Math.Max(1, Math.Min(999, repsToWin));
            copy.recoveryPressesRequired = Math.Max(1, Math.Min(100, recoveryPressesRequired));
            copy.sweepSpeed = Clamp(sweepSpeed, 0.05f, 5f, 0.7f);
            copy.sweepAcceleration = Clamp(sweepAcceleration, 0f, 5f, 0.15f);
            copy.maximumSweepSpeed = Clamp(maximumSweepSpeed, copy.sweepSpeed, 10f, 2f);
            copy.feedbackDuration = Clamp(feedbackDuration, 0.05f, 10f, 0.7f);
            copy.leftDangerEnd = Clamp(leftDangerEnd, 0.02f, 0.4f, 0.15f);
            copy.perfectStart = Clamp(perfectStart, copy.leftDangerEnd + 0.02f, 0.6f, 0.4f);
            copy.perfectEnd = Clamp(perfectEnd, copy.perfectStart + 0.02f, 0.8f, 0.6f);
            copy.perfectWidthMultiplierPerRep = Clamp(perfectWidthMultiplierPerRep, 0.5f, 0.95f, 0.8f);
            copy.rightDangerStart = Clamp(rightDangerStart, copy.perfectEnd + 0.02f, 0.98f, 0.85f);
            return copy;
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = fallback;
            return Math.Max(min, Math.Min(max, value));
        }
    }

    // No Unity dependencies: timing, boundaries and rep counting can be tested independently.
    public sealed class SquatGameModel
    {
        private readonly SquatGameSettings settings;
        private readonly Random random;
        private double sweepElapsed;
        private float feedbackElapsed;
        private int lastFrame = -1;

        public SquatGameState State { get; private set; } = SquatGameState.Ready;
        public SquatHitResult LastResult { get; private set; }
        public int CompletedReps { get; private set; }
        public int RecoveryPresses { get; private set; }
        public float PointerPosition { get; private set; }
        public float PerfectStart { get; private set; }
        public float PerfectEnd { get; private set; }
        public int RepGoal => settings.repsToWin;
        public int RecoveryTarget => settings.recoveryPressesRequired;
        public float RecoveryProgress => (float)RecoveryPresses / RecoveryTarget;

        public SquatGameModel(SquatGameSettings configuration, Random random = null)
        {
            settings = (configuration ?? new SquatGameSettings()).NormalizedCopy();
            this.random = random ?? new Random();
            PerfectStart = settings.perfectStart;
            PerfectEnd = settings.perfectEnd;
        }

        public void Begin()
        {
            if (State == SquatGameState.Ready) StartSweep();
        }

        // pressedThisFrame must be a key-down edge, never a held-key value.
        public void Tick(float deltaTime, bool pressedThisFrame, int frame)
        {
            if (frame == lastFrame) return;
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            lastFrame = frame;

            switch (State)
            {
                case SquatGameState.Sweeping:
                    if (pressedThisFrame)
                    {
                        LastResult = Classify(PointerPosition);
                        if (LastResult == SquatHitResult.Exhausted)
                        {
                            RecoveryPresses = 0;
                            State = SquatGameState.Exhausted;
                        }
                        else FinishRep(LastResult);
                    }
                    else
                    {
                        sweepElapsed += deltaTime;
                        double phase = TravelDistance(sweepElapsed) % 2.0;
                        PointerPosition = (float)(phase <= 1.0 ? phase : 2.0 - phase);
                    }
                    break;
                case SquatGameState.Exhausted:
                    if (pressedThisFrame && ++RecoveryPresses >= RecoveryTarget)
                        FinishRep(SquatHitResult.Recovered);
                    break;
                case SquatGameState.Feedback:
                    feedbackElapsed += deltaTime;
                    if (feedbackElapsed >= settings.feedbackDuration)
                    {
                        if (CompletedReps >= RepGoal) State = SquatGameState.Completed;
                        else StartSweep();
                    }
                    break;
            }
        }

        public SquatHitResult Classify(float position)
        {
            if (position <= settings.leftDangerEnd || position >= settings.rightDangerStart)
                return SquatHitResult.Exhausted;
            if (position >= PerfectStart && position <= PerfectEnd)
                return SquatHitResult.Perfect;
            return SquatHitResult.Good;
        }

        private double TravelDistance(double elapsed)
        {
            double speed = settings.sweepSpeed;
            double acceleration = settings.sweepAcceleration;
            if (acceleration == 0.0) return speed * elapsed;
            double timeToCap = (settings.maximumSweepSpeed - speed) / acceleration;
            double acceleratingTime = Math.Min(elapsed, timeToCap);
            return speed * acceleratingTime + 0.5 * acceleration * acceleratingTime * acceleratingTime
                + settings.maximumSweepSpeed * Math.Max(0.0, elapsed - timeToCap);
        }

        private void StartSweep()
        {
            RollPerfectZone();
            PointerPosition = 0f;
            sweepElapsed = 0.0;
            RecoveryPresses = 0;
            LastResult = SquatHitResult.None;
            State = SquatGameState.Sweeping;
        }

        private void RollPerfectZone()
        {
            float previousCenter = (PerfectStart + PerfectEnd) * 0.5f;
            float initialWidth = settings.perfectEnd - settings.perfectStart;
            float width = Math.Max(0.001f, initialWidth
                * (float)Math.Pow(settings.perfectWidthMultiplierPerRep, CompletedReps));
            float minimumStart = settings.leftDangerEnd + 0.02f;
            float maximumEnd = settings.rightDangerStart - 0.02f;
            float maximumStart = Math.Max(minimumStart, maximumEnd - width);
            float start = minimumStart + (maximumStart - minimumStart) * (float)random.NextDouble();

            // Keep each new squat visibly different without rerolling during recovery or pause.
            if (CompletedReps > 0 && Math.Abs(start + width * 0.5f - previousCenter) < 0.04f)
            {
                float leftDistance = Math.Abs(minimumStart + width * 0.5f - previousCenter);
                float rightDistance = Math.Abs(maximumStart + width * 0.5f - previousCenter);
                start = leftDistance > rightDistance ? minimumStart : maximumStart;
            }

            PerfectStart = start;
            PerfectEnd = Math.Min(maximumEnd, start + width);
        }

        private void FinishRep(SquatHitResult result)
        {
            CompletedReps++;
            LastResult = result;
            feedbackElapsed = 0f;
            State = SquatGameState.Feedback;
        }
    }
}
