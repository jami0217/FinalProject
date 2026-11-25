using System;
using FinalProject.Gym.Squat;

public static class SquatGameModelTests
{
    private static int checks;

    public static int Main()
    {
        try
        {
            BoundariesAndHits();
            ExhaustionRequiresEightAdditionalEdges();
            SameFrameAndFeedbackCannotDoubleCount();
            FiveRepsCompleteOnlyOnce();
            PointerReflectsAndAccelerationIsFrameIndependent();
            SettingsAreNormalizedWithoutChangingTheInspectorCopy();
            Console.WriteLine($"PASS: {checks} assertions across 6 squat gameplay scenarios.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static SquatGameSettings Settings() => new SquatGameSettings
    {
        sweepSpeed = 1f, sweepAcceleration = 0f, maximumSweepSpeed = 1f
    };

    private static void BoundariesAndHits()
    {
        float[] positions = { 0f, 0.15f, 0.1501f, 0.3999f, 0.4f, 0.5f, 0.6f, 0.6001f, 0.8499f, 0.85f, 1f };
        SquatHitResult[] expected = { SquatHitResult.Exhausted, SquatHitResult.Exhausted,
            SquatHitResult.Good, SquatHitResult.Good, SquatHitResult.Perfect, SquatHitResult.Perfect,
            SquatHitResult.Perfect, SquatHitResult.Good, SquatHitResult.Good,
            SquatHitResult.Exhausted, SquatHitResult.Exhausted };
        for (int i = 0; i < positions.Length; i++)
        {
            SquatGameModel game = new SquatGameModel(Settings());
            Check(game.State == SquatGameState.Ready, "Initial state must be Ready.");
            game.Begin();
            game.Tick(positions[i], false, 1);
            game.Tick(0f, true, 2);
            Check(game.LastResult == expected[i], "Incorrect zone at " + positions[i]);
            Check(game.CompletedReps == (expected[i] == SquatHitResult.Exhausted ? 0 : 1), "Incorrect hit count.");
        }
    }

    private static void ExhaustionRequiresEightAdditionalEdges()
    {
        SquatGameModel game = new SquatGameModel(Settings());
        game.Begin();
        game.Tick(0f, true, 0);
        Check(game.State == SquatGameState.Exhausted && game.RecoveryPresses == 0,
            "The red hit must not count as the first recovery press.");
        for (int frame = 1; frame <= 120; frame++) game.Tick(1f / 60f, false, frame);
        Check(game.RecoveryPresses == 0 && game.PointerPosition == 0f && game.CompletedReps == 0,
            "Holding a key without new down edges must not advance recovery or the pointer.");
        for (int i = 1; i < 8; i++) game.Tick(0f, true, 120 + i);
        Check(game.CompletedReps == 0 && game.RecoveryPresses == 7, "Seven recovery presses must not finish a rep.");
        game.Tick(0f, true, 128);
        Check(game.CompletedReps == 1 && game.State == SquatGameState.Feedback,
            "Eight additional edges must finish exactly one rep.");
        Check(game.LastResult == SquatHitResult.Recovered && game.RecoveryProgress == 1f, "Recovery feedback must be complete.");
    }

    private static void SameFrameAndFeedbackCannotDoubleCount()
    {
        SquatGameModel game = new SquatGameModel(Settings());
        game.Begin();
        game.Tick(0f, true, 0);
        game.Tick(0f, true, 1);
        game.Tick(0f, true, 1);
        Check(game.RecoveryPresses == 1, "Repeated calls in one frame must not double-count input.");
        SquatGameModel good = new SquatGameModel(Settings());
        good.Begin();
        good.Tick(0.25f, false, 1);
        good.Tick(0f, true, 2);
        for (int i = 3; i < 20; i++) good.Tick(0.001f, true, i);
        Check(good.CompletedReps == 1 && good.State == SquatGameState.Feedback, "Feedback must ignore timing/mash input.");
    }

    private static void FiveRepsCompleteOnlyOnce()
    {
        SquatGameModel game = new SquatGameModel(Settings());
        game.Begin();
        int frame = 0;
        for (int rep = 1; rep <= 5; rep++)
        {
            game.Tick(0.5f, false, frame++);
            game.Tick(0f, true, frame++);
            Check(game.CompletedReps == rep && game.State == SquatGameState.Feedback, "One green hit must add one rep.");
            game.Tick(0.71f, true, frame++);
            Check(game.State == (rep == 5 ? SquatGameState.Completed : SquatGameState.Sweeping),
                "A feedback-ending press must not leak into the next rep.");
        }
        for (int i = 0; i < 100; i++) game.Tick(1f, true, frame++);
        game.Begin();
        Check(game.CompletedReps == 5 && game.State == SquatGameState.Completed, "Completion must not add more reps or restart itself.");
    }

    private static void PointerReflectsAndAccelerationIsFrameIndependent()
    {
        SquatGameModel game = new SquatGameModel(Settings());
        game.Begin();
        game.Tick(1.2f, false, 1);
        Check(Math.Abs(game.PointerPosition - 0.8f) < 0.00001f, "Pointer must reflect at the right boundary.");
        game.Tick(1f, false, 2);
        Check(Math.Abs(game.PointerPosition - 0.2f) < 0.00001f, "Pointer must reflect at the left boundary.");
        game.Tick(1000f, false, 3);
        Check(game.PointerPosition >= 0f && game.PointerPosition <= 1f, "Long frames must stay inside the bar.");
        SquatGameSettings accelerating = Settings();
        accelerating.sweepSpeed = 0.5f;
        accelerating.sweepAcceleration = 1f;
        accelerating.maximumSweepSpeed = 1.5f;
        SquatGameModel single = new SquatGameModel(accelerating);
        SquatGameModel many = new SquatGameModel(accelerating);
        single.Begin(); many.Begin();
        single.Tick(8f, false, 0);
        for (int i = 0; i < 480; i++) many.Tick(1f / 60f, false, i);
        Check(Math.Abs(single.PointerPosition - many.PointerPosition) < 0.00001f,
            "Acceleration and speed cap must not depend on frame rate.");
    }

    private static void SettingsAreNormalizedWithoutChangingTheInspectorCopy()
    {
        SquatGameSettings raw = Settings();
        raw.repsToWin = 0; raw.recoveryPressesRequired = -4;
        raw.leftDangerEnd = 0.9f; raw.perfectStart = 0f;
        raw.perfectEnd = float.NaN; raw.rightDangerStart = 0f;
        raw.sweepSpeed = float.PositiveInfinity;
        SquatGameSettings safe = raw.NormalizedCopy();
        Check(safe.repsToWin == 1 && safe.recoveryPressesRequired == 1, "Goals must remain reachable.");
        Check(0f < safe.leftDangerEnd && safe.leftDangerEnd < safe.perfectStart
            && safe.perfectStart < safe.perfectEnd && safe.perfectEnd < safe.rightDangerStart
            && safe.rightDangerStart < 1f, "Color boundaries must remain ordered and visible.");
        Check(raw.repsToWin == 0 && float.IsNaN(raw.perfectEnd), "Normalization must not mutate the source settings.");
        SquatGameModel game = new SquatGameModel(safe);
        game.Begin();
        bool rejected = false;
        try { game.Tick(-1f, false, 1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "Invalid frame deltas must be rejected.");
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
