using FinalProject.Gym.NPC;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    public sealed class GymInnerThoughts : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float anxietyThreshold = 30f;
        [SerializeField, Min(0f)] private float globalCooldown = 6f;
        [SerializeField, Min(0f)] private float sameNpcCooldown = 10f;
        [SerializeField, TextArea(1, 3)] private string[] thoughts =
        {
            "他為什麼一直看我？", "我是不是要被嘲笑了？", "他是不是覺得我做得不標準？",
            "我會不會擋到別人？", "大家是不是都比我厲害？"
        };
        private readonly OverthinkingSession session = new OverthinkingSession();
        private GymGameBridge bridge;
        private ThoughtBubbleUI view;
        private GymVeteranNPC source;
        public void Bind(GymGameBridge context, ThoughtBubbleUI ui) { bridge = context; view = ui; session.ActivateStone(false); }
        private void LateUpdate()
        {
            if (bridge == null || !bridge.isActiveAndEnabled || view == null) return;
            GymVeteranNPC nearest = bridge.NearestAnxietySource;
            if (bridge.IsModal || bridge.IsMinigameActive || nearest == null || bridge.Anxiety.Value < anxietyThreshold)
            { view.Hide(); source = null; return; }
            if (view.IsVisible)
            {
                if (source == null || !source.IsAnxietySource) { view.Hide(); source = null; }
                return;
            }
            if (!view.CanDisplay) return;
            if (session.TryThought(nearest.GetInstanceID(), Time.timeAsDouble, globalCooldown, sameNpcCooldown, thoughts, out string line))
            { source = nearest; view.Show(line); }
        }
        public void ResetRound() { session.Reset(); session.ActivateStone(false); source = null; if (view != null) view.Hide(); }
        private void OnDisable() { if (view != null) view.Hide(); source = null; }
    }
}
