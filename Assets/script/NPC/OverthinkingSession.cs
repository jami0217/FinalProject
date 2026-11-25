using System;
using System.Collections.Generic;

namespace FinalProject.Gym.NPC
{
    // Pure session rules, independent of Unity triggers and UI.
    public sealed class OverthinkingSession
    {
        private readonly Random random;
        private readonly Dictionary<int, double> lastNpcTimes = new Dictionary<int, double>();
        private double lastGlobalTime = double.NegativeInfinity;
        private string lastThought;
        public bool IsEnabled { get; private set; }

        public OverthinkingSession(Random random = null) { this.random = random ?? new Random(); }

        public bool ActivateStone(bool allowRepeat)
        {
            if (IsEnabled && !allowRepeat) return false;
            IsEnabled = true;
            return true;
        }

        public bool TryThought(int npcId, double now, double globalCooldown, double npcCooldown,
            IReadOnlyList<string> messages, out string thought)
        {
            thought = null;
            if (!IsEnabled || double.IsNaN(now) || double.IsInfinity(now) || messages == null) return false;
            if (now - lastGlobalTime < Math.Max(0.0, globalCooldown)) return false;
            if (lastNpcTimes.TryGetValue(npcId, out double last) && now - last < Math.Max(0.0, npcCooldown)) return false;

            List<string> choices = new List<string>();
            HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                string line = message.Trim();
                if (line != lastThought && unique.Add(line)) choices.Add(line);
            }
            if (choices.Count == 0) return false;
            thought = choices[random.Next(choices.Count)];
            lastThought = thought;
            lastGlobalTime = now;
            lastNpcTimes[npcId] = now;
            return true;
        }

        public void Reset()
        {
            IsEnabled = false;
            lastGlobalTime = double.NegativeInfinity;
            lastNpcTimes.Clear();
            lastThought = null;
        }
    }
}
