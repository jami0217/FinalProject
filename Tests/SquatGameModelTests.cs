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
            GreenZonesMoveAndShrinkForFiveReps();
            RecoveryAndFeedbackKeepTheCurrentGreenZone();
            Console.WriteLine($"PASS: {checks} assertions across 8 squat gameplay scenarios.");
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
        SquatGameModel preview = new SquatGameModel(Settings(), new Random(1));
        preview.Begin();
        float start = preview.PerfectStart;
        float end = preview.PerfectEnd;
        float[] positions = { 0f, 0.15f, 0.1501f, (0.15f + start) * 0.5f,
            start, (start + end) * 0.5f, end, (end + 0.85f) * 0.5f, 0.8499f, 0.85f, 1f };
        SquatHitResult[] expected = { SquatHitResult.Exhausted, SquatHitResult.Exhausted,
            SquatHitResult.Good, SquatHitResult.Good, SquatHitResult.Perfect, SquatHitResult.Perfect,
            SquatHitResult.Perfect, SquatHitResult.Good, SquatHitResult.Good,
            SquatHitResult.Exhausted, SquatHitResult.Exhausted };
        for (int i = 0; i < positions.Length; i++)
        {
            SquatGameModel game = new SquatGameModel(Settings(), new Random(1));
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
            game.Tick((game.PerfectStart + game.PerfectEnd) * 0.5f, false, frame++);
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

    private static void GreenZonesMoveAndShrinkForFiveReps()
    {
        for (int seed = 0; seed < 24; seed++)
        {
            SquatGameSettings configuration = Settings();
            SquatGameModel game = new SquatGameModel(configuration, new Random(seed));
            game.Begin();
            int frame = 0;
            float previousWidth = 1f;
            float previousCenter = -1f;
            for (int rep = 0; rep < 5; rep++)
            {
                float width = game.PerfectEnd - game.PerfectStart;
                float center = (game.PerfectStart + game.PerfectEnd) * 0.5f;
                float expectedWidth = (configuration.perfectEnd - configuration.perfectStart)
                    * (float)Math.Pow(configuration.perfectWidthMultiplierPerRep, rep);
                Check(game.State == SquatGameState.Sweeping, "Each rep must start a sweep.");
                Check(width < previousWidth && Math.Abs(width - expectedWidth) < 0.00001f,
                    "Each later rep must have a strictly narrower green zone.");
                Check(Math.Abs(center - previousCenter) > 0.00001f, "Each rep must move the green center.");
                Check(game.PerfectStart > configuration.leftDangerEnd && game.PerfectEnd < configuration.rightDangerStart,
                    "Moving green must stay between the red zones.");
                Check(game.Classify(game.PerfectStart) == SquatHitResult.Perfect
                    && game.Classify(game.PerfectEnd) == SquatHitResult.Perfect,
                    "The current displayed boundaries must both count as green.");
                Check(game.Classify(game.PerfectStart - 0.001f) == SquatHitResult.Good
                    && game.Classify(game.PerfectEnd + 0.001f) == SquatHitResult.Good,
                    "The area directly outside the current green must remain yellow.");
                game.Tick(center, false, frame++);
                game.Tick(0f, true, frame++);
                Check(game.LastResult == SquatHitResult.Perfect && game.CompletedReps == rep + 1,
                    "Hitting the moved green center must add exactly one rep.");
                game.Tick(0.71f, false, frame++);
                previousWidth = width;
                previousCenter = center;
            }
            Check(game.State == SquatGameState.Completed && game.CompletedReps == 5,
                "The randomized challenge must still finish at five reps.");
            Check(configuration.perfectStart == 0.4f && configuration.perfectEnd == 0.6f,
                "Random zones must not change the saved Inspector settings.");
        }
    }

    private static void RecoveryAndFeedbackKeepTheCurrentGreenZone()
    {
        SquatGameModel game = new SquatGameModel(Settings(), new Random(42));
        game.Begin();
        float start = game.PerfectStart;
        float end = game.PerfectEnd;
        game.Tick(0f, true, 0);
        game.Begin();
        for (int frame = 1; frame <= 8; frame++)
        {
            game.Tick(0f, true, frame);
            Check(game.PerfectStart == start && game.PerfectEnd == end,
                "Recovery and repeated Begin calls must preserve the same green zone.");
        }
        game.Tick(0.2f, false, 9);
        Check(game.State == SquatGameState.Feedback && game.PerfectStart == start && game.PerfectEnd == end,
            "The hit zone must stay fixed until feedback finishes.");
        game.Tick(0.51f, false, 10);
        Check(game.State == SquatGameState.Sweeping && game.PerfectEnd - game.PerfectStart < end - start,
            "Only starting the next rep should roll and shrink green.");
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
