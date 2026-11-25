using System;
using FinalProject.Gym.Core;

public static class GymCoreModelTests
{
    private static int checks;
    public static int Main()
    {
        try
        {
            AnxietyRatesClampAndFreeze();
            FriendlyReliefAndFrameRate();
            FiveStationProgressAndRestart();
            InvalidStationConfigurations();
            VeteranApproachTalkAndIgnore();
            VeteranTimeoutLeashAndFreeze();
            IgnoringDialogueStillAllowsVictory();
            NormalizeSettings();
            Console.WriteLine($"PASS: {checks} assertions across 8 gym core scenarios.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void AnxietyRatesClampAndFreeze()
    {
        AnxietyModel anxiety = new AnxietyModel(new AnxietySettings());
        Near(anxiety.Value, 20f, "Initial anxiety");
        anxiety.Tick(1f, true, false);
        Near(anxiety.Value, 28f, "Nearby veteran raises by 8/s");
        anxiety.Tick(1f, false, false);
        Near(anxiety.Value, 25f, "No nearby veteran recovers by 3/s");
        anxiety.Tick(100f, true, false);
        Near(anxiety.Value, 100f, "Upper clamp");
        anxiety.Tick(100f, false, false);
        Near(anxiety.Value, 0f, "Lower clamp");
        anxiety.SetValue(64f);
        anxiety.Tick(100f, true, true);
        Near(anxiety.Value, 64f, "Dialogues/paused/minigame freeze must preserve value");
        anxiety.SetValue(float.NaN);
        Near(anxiety.Value, 64f, "Invalid external value must not corrupt anxiety");
        anxiety.SetValue(20f); Near(anxiety.GetDifficultyMultiplier(), 1f, "Low anxiety has normal difficulty");
        anxiety.SetValue(100f); Near(anxiety.GetDifficultyMultiplier(), 1.35f, "Difficulty remains bounded");
    }

    private static void FriendlyReliefAndFrameRate()
    {
        AnxietyModel anxiety = new AnxietyModel(new AnxietySettings());
        anxiety.SetValue(100f); anxiety.ApplyFriendlyRelief();
        Near(anxiety.Value, 60f, "Friendly talk reduces 40 once");
        anxiety.Tick(100f, true, true);
        Near(anxiety.Value, 60f, "Suspending must not consume reassurance time");
        anxiety.Tick(4f, true, false);
        Near(anxiety.Value, 48f, "Reassurance prevents immediate renewed increase");
        anxiety.Tick(1f, true, false);
        Near(anxiety.Value, 56f, "Normal increase resumes after reassurance");
        AnxietyModel one = new AnxietyModel(new AnxietySettings());
        AnxietyModel many = new AnxietyModel(new AnxietySettings());
        one.SetValue(100f); many.SetValue(100f);
        one.ApplyFriendlyRelief(); many.ApplyFriendlyRelief();
        one.Tick(5f, true, false);
        for (int i = 0; i < 500; i++) many.Tick(0.01f, true, false);
        Check(Math.Abs(one.Value - many.Value) < 0.002f, "Reassurance boundaries must be frame-rate independent.");
        anxiety.Reset(); Near(anxiety.Value, 20f, "Restart restores initial anxiety");
    }

    private static void FiveStationProgressAndRestart()
    {
        GymProgressModel progress = new GymProgressModel(GymStationDefinition.Defaults());
        Check(progress.TotalCount == 5 && progress.CompletedCount == 0 && !progress.AllCompleted, "Round begins at 0/5.");
        Check(progress.StartStation("Squat") && progress.GetState("Squat") == GymStationState.InProgress, "Entering station marks InProgress.");
        Check(progress.CompletedCount == 0, "Entering/returning without success must not count completion.");
        Check(progress.CompleteStation("Squat"), "First success counts.");
        for (int i = 0; i < 100; i++) Check(!progress.CompleteStation("Squat"), "Repeated success must not double-count.");
        Check(progress.StartStation("Squat") && progress.GetState("Squat") == GymStationState.Completed, "Replaying completed station preserves state.");
        Check(!progress.StartStation("squat") && !progress.CompleteStation("Unknown"), "Stable IDs are case-sensitive and unknown IDs do not count.");
        foreach (GymStationDefinition station in progress.Stations) progress.CompleteStation(station.id);
        Check(progress.CompletedCount == 5 && progress.AllCompleted, "Five unique stations produce victory.");
        progress.Reset();
        Check(progress.CompletedCount == 0 && !progress.AllCompleted && !progress.HasAnyProgress, "Restart resets round progress.");
        foreach (GymStationDefinition station in progress.Stations)
            Check(progress.GetState(station.id) == GymStationState.NotStarted, "Every station resets to NotStarted.");
    }

    private static void InvalidStationConfigurations()
    {
        RejectStations(null); RejectStations(new GymStationDefinition[0]);
        GymStationDefinition[] duplicate = GymStationDefinition.Defaults(); duplicate[4].id = "Squat"; RejectStations(duplicate);
        GymStationDefinition[] empty = GymStationDefinition.Defaults(); empty[2].id = " "; RejectStations(empty);
        GymStationDefinition[] missing = GymStationDefinition.Defaults(); missing[2] = null; RejectStations(missing);
        GymStationDefinition[] definitions = GymStationDefinition.Defaults();
        GymProgressModel copy = new GymProgressModel(definitions);
        definitions[0].id = "ChangedAfterConstruction";
        Check(copy.Contains("Dumbbell") && !copy.Contains("ChangedAfterConstruction"), "Model must not mutate with authoring data.");
    }
    private static void RejectStations(GymStationDefinition[] stations)
    {
        bool rejected = false;
        try { new GymProgressModel(stations); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Malformed station configuration must be rejected.");
    }

    private static void VeteranApproachTalkAndIgnore()
    {
        VeteranApproachModel veteran = new VeteranApproachModel(new VeteranApproachSettings());
        Check(veteran.State == VeteranState.Wandering, "Veteran begins with existing wandering.");
        veteran.Tick(0.02f, 3f, 0f, false);
        Check(veteran.State == VeteranState.Approaching, "Player in range starts approach, not dialogue.");
        Check(!veteran.BeginTalk(), "Cannot talk before the veteran stops.");
        veteran.Tick(0.02f, 1.25f, 0f, false);
        Check(veteran.State == VeteranState.Waiting, "Veteran waits at safe stopping distance.");
        Check(veteran.BeginTalk() && !veteran.BeginTalk(), "Talking must start only once per prompt.");
        veteran.Tick(100f, 1f, 0f, false);
        Check(veteran.State == VeteranState.Talking, "Dialogue is closed by the dialogue controller, never force-repeated.");
        veteran.Dismiss();
        Check(veteran.State == VeteranState.Leaving, "Friendly close or decline makes the veteran leave.");
        veteran.Tick(0.02f, 1f, 0f, false);
        Check(veteran.State == VeteranState.Cooldown, "Returning home starts cooldown.");
        veteran.Tick(14f, 1f, 0f, false);
        Check(veteran.State == VeteranState.Cooldown, "Cooldown prevents immediate retrigger.");
        veteran.Tick(1f, 1f, 0f, false);
        Check(veteran.State == VeteranState.Wandering, "Veteran can become available later.");
        veteran.Tick(0.02f, 1.25f, 0f, false); veteran.Tick(0.02f, 1.25f, 0f, false);
        veteran.Dismiss();
        Check(veteran.State == VeteranState.Leaving, "Ignoring does not force dialogue.");
    }

    private static void VeteranTimeoutLeashAndFreeze()
    {
        VeteranApproachModel veteran = new VeteranApproachModel(new VeteranApproachSettings());
        veteran.Tick(0.02f, 3f, 0f, false);
        veteran.Tick(100f, 3f, 0f, true);
        Check(veteran.State == VeteranState.Approaching, "Pause/minigame freeze must not consume chase timeout.");
        veteran.Tick(8f, 3f, 0f, false);
        Check(veteran.State == VeteranState.Leaving, "Blocked approaches must time out.");
        veteran.Tick(8f, 3f, 4f, false);
        Check(veteran.State == VeteranState.Cooldown, "Blocked returns must also time out.");
        veteran.Reset(); veteran.Tick(0.02f, 3f, 0f, false); veteran.Tick(0.02f, 7f, 1f, false);
        Check(veteran.State == VeteranState.Leaving, "Veteran must not chase beyond range.");
        veteran.Reset(); veteran.Tick(0.02f, 3f, 0f, false); veteran.Tick(0.02f, 3f, 7f, false);
        Check(veteran.State == VeteranState.Leaving, "Veteran must obey its home leash.");
        veteran.Reset(); veteran.Tick(0.02f, 3f, 0f, false); veteran.Tick(0.02f, 1f, 0f, false); veteran.Tick(8f, 1f, 0f, false);
        Check(veteran.State == VeteranState.Leaving, "Ignoring a prompt must expire without trapping the player.");
    }

    private static void IgnoringDialogueStillAllowsVictory()
    {
        GymProgressModel progress = new GymProgressModel(GymStationDefinition.Defaults());
        AnxietyModel anxiety = new AnxietyModel(new AnxietySettings());
        foreach (GymStationDefinition station in progress.Stations)
        {
            anxiety.Tick(20f, true, false); // No friendly relief is ever requested.
            progress.StartStation(station.id); progress.CompleteStation(station.id);
        }
        Check(progress.AllCompleted && progress.CompletedCount == 5, "Talking must never be a prerequisite for winning.");
        Check(anxiety.Value == 100f && anxiety.GetDifficultyMultiplier() <= 1.35f, "High anxiety stays bounded and reversible.");
        anxiety.Tick(40f, false, false); Near(anxiety.Value, 0f, "Leaving restores calm without dialogue");
    }
    private static void NormalizeSettings()
    {
        AnxietySettings invalid = new AnxietySettings { initialValue = float.NaN, tenseThreshold = 200f, highThreshold = -1f, maximumDifficultyMultiplier = 20f };
        AnxietySettings safe = invalid.NormalizedCopy();
        Check(safe.initialValue == 20f && safe.highThreshold > safe.tenseThreshold && safe.maximumDifficultyMultiplier <= 2f, "Anxiety settings must remain finite and ordered.");
        VeteranApproachSettings settings = new VeteranApproachSettings { stopDistance = -1f, detectionRadius = 0f, chaseLimit = 0f, cooldown = 0f };
        VeteranApproachSettings normalized = settings.NormalizedCopy();
        Check(normalized.stopDistance >= 0.8f && normalized.detectionRadius > normalized.stopDistance
            && normalized.chaseLimit >= normalized.detectionRadius && normalized.cooldown >= 1f, "Approach configuration must remain safe.");
    }
    private static void Near(float actual, float expected, string message) { Check(Math.Abs(actual - expected) < 0.0001f, message); }
    private static void Check(bool value, string message) { checks++; if (!value) throw new InvalidOperationException(message); }
}
