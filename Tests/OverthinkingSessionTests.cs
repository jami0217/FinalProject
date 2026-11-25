using System;
using FinalProject.Gym.NPC;

public static class OverthinkingSessionTests
{
    private static int checks;
    private static readonly string[] Lines = { "他是在看我嗎？", "我是不是很怪？", "沒事，應該只是我想太多。" };

    public static int Main()
    {
        try
        {
            StoneActivationAndRepeat();
            GlobalAndNpcCooldowns();
            RejectedRequestsDoNotConsumeCooldown();
            AvoidConsecutiveDuplicateText();
            InvalidTextAndClock();
            ResetStartsANewRound();
            Console.WriteLine($"PASS: {checks} assertions across 6 overthinking session scenarios.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static OverthinkingSession Enabled()
    {
        OverthinkingSession session = new OverthinkingSession(new Random(123));
        session.ActivateStone(false);
        return session;
    }

    private static void StoneActivationAndRepeat()
    {
        OverthinkingSession session = new OverthinkingSession(new Random(1));
        Check(!session.IsEnabled, "Overthinking must start disabled.");
        Check(!session.TryThought(1, 0, 6, 10, Lines, out _), "NPC proximity before stone must be ignored.");
        Check(session.ActivateStone(false) && session.IsEnabled, "First stone event must enable thoughts.");
        Check(!session.ActivateStone(false), "Default stone must trigger only once.");
        Check(session.TryThought(1, 0, 6, 10, Lines, out _), "First eligible NPC entry must be accepted immediately.");
        Check(session.ActivateStone(true), "Allow Repeat should permit the stone visual again.");
        Check(!session.TryThought(1, 6, 6, 10, Lines, out _), "Repeating stone must not reset per-NPC cooldown.");
    }

    private static void GlobalAndNpcCooldowns()
    {
        OverthinkingSession session = Enabled();
        Check(session.TryThought(1, 0, 6, 10, Lines, out _), "First NPC must show a thought.");
        Check(!session.TryThought(2, 5.999, 6, 10, Lines, out _), "Global cooldown must block other NPCs.");
        Check(!session.TryThought(1, 6, 6, 10, Lines, out _), "Same NPC must still be on its own cooldown.");
        Check(session.TryThought(2, 6, 6, 10, Lines, out _), "Global cooldown boundary must be inclusive.");
        Check(session.TryThought(1, 12, 6, 10, Lines, out _), "Original NPC must become eligible later.");
        Check(!session.TryThought(1, 18, 6, 10, Lines, out _), "NPC cooldown must be recorded for every accepted entry.");
        Check(session.TryThought(3, 18, 6, 10, Lines, out _), "Other NPCs may show once global cooldown expires.");
        OverthinkingSession exact = Enabled();
        exact.TryThought(9, 0, 0, 10, Lines, out _);
        Check(exact.TryThought(9, 10, 0, 10, Lines, out _), "NPC cooldown boundary must be inclusive.");
    }

    private static void RejectedRequestsDoNotConsumeCooldown()
    {
        OverthinkingSession session = Enabled();
        session.TryThought(1, 0, 6, 10, Lines, out _);
        for (int i = 0; i < 100; i++)
            Check(!session.TryThought(2, 1, 6, 10, Lines, out _), "Spam inside cooldown must be rejected.");
        Check(session.TryThought(2, 6, 6, 10, Lines, out _), "Rejected requests must not postpone eligibility.");
        Check(!session.TryThought(2, 12, 6, 10, Lines, out _), "NPC 2 is cooling down.");
        Check(session.TryThought(3, 12, 6, 10, Lines, out _), "A rejected NPC must not consume the global slot.");
    }

    private static void AvoidConsecutiveDuplicateText()
    {
        OverthinkingSession session = Enabled();
        string previous = null;
        string[] duplicates = { " A ", "A", "B", "B", null, " " };
        for (int i = 0; i < 120; i++)
        {
            Check(session.TryThought(i % 5, i * 6, 6, 10, duplicates, out string line), "Valid pool should remain usable.");
            Check(line != previous, "Consecutive messages must differ by text, even with duplicate pool entries.");
            Check(line == "A" || line == "B", "Empty text and surrounding whitespace must be removed.");
            previous = line;
        }
    }

    private static void InvalidTextAndClock()
    {
        OverthinkingSession session = Enabled();
        Check(!session.TryThought(1, 0, 6, 10, null, out _), "Null pool must be safe.");
        Check(!session.TryThought(1, 0, 6, 10, new[] { " ", null }, out _), "Empty messages must be ignored.");
        Check(!session.TryThought(1, double.NaN, 6, 10, Lines, out _), "NaN clock must not corrupt cooldowns.");
        Check(!session.TryThought(1, double.PositiveInfinity, 6, 10, Lines, out _), "Infinite clock must be rejected.");
        Check(session.TryThought(1, 0, 6, 10, new[] { "唯一一句" }, out _), "A single valid line may appear initially.");
        Check(!session.TryThought(2, 6, 6, 10, new[] { "唯一一句" }, out _), "A one-line pool must not violate no-repeat.");
        Check(session.TryThought(2, 6, 6, 10, Lines, out _), "Adding valid alternatives must work without a rejected-request cooldown.");
    }

    private static void ResetStartsANewRound()
    {
        OverthinkingSession session = Enabled();
        session.TryThought(1, 0, 6, 10, Lines, out _);
        session.Reset();
        Check(!session.IsEnabled, "Reset must require another stone event.");
        Check(!session.TryThought(2, 10, 6, 10, Lines, out _), "Reset must stop thoughts before stone.");
        Check(session.ActivateStone(false), "Stone must be available again after reset.");
        Check(session.TryThought(1, 0, 6, 10, Lines, out _), "Reset must clear global/NPC cooldowns for a new round.");
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
