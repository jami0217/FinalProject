using System.Collections.Generic;
using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    public sealed class OverthinkingManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Leave empty to find the existing Player component at runtime.")]
        private Transform player;
        [SerializeField] private ThoughtBubbleUI thoughtUI;
        [SerializeField, Min(0f)] private float globalCooldown = 6f;
        [SerializeField, Min(0f)] private float sameNpcCooldown = 10f;
        [SerializeField, TextArea(1, 3)] private string[] thoughts =
        {
            "他是在看我嗎？", "我是不是很怪？", "他會不會覺得我是新手……",
            "是不是我動作做錯了？", "沒事，應該只是我想太多。"
        };
        [SerializeField] private bool dialogueActive;
        [SerializeField] private bool paused;
        [SerializeField] private bool externallySuppressed;
        [SerializeField, Tooltip("Toggle during Play to reset the stone event and cooldowns once.")]
        private bool resetStateOnNextUpdate;

        private OverthinkingSession session;
        private NPCThoughtTrigger2D activeSource;
        private readonly HashSet<object> thoughtSuppressors = new HashSet<object>();
        private readonly HashSet<object> runtimeSuppressors = new HashSet<object>();
        public bool IsOverthinkingEnabled => Session.IsEnabled;
        public bool IsSuppressed => dialogueActive || paused || externallySuppressed || runtimeSuppressors.Count > 0 || Time.timeScale <= 0f
            || (GymGameManager.Instance != null && GymGameManager.Instance.IsMiniGameOpen);
        private OverthinkingSession Session => session ?? (session = new OverthinkingSession());

        private void Start()
        {
            if (player == null)
            {
                Player existing = FindFirstObjectByType<Player>();
                if (existing != null) player = existing.transform;
            }
            EnsureUI();
            if (thoughtUI != null && !thoughtUI.CanDisplay)
                Debug.LogWarning("Thought UI is disabled or is missing its Message / Canvas Group references.", this);
            HashSet<string> unique = new HashSet<string>();
            if (thoughts != null)
                foreach (string text in thoughts) if (!string.IsNullOrWhiteSpace(text)) unique.Add(text.Trim());
            if (unique.Count < 2) Debug.LogWarning("Add at least two different non-empty thoughts to avoid consecutive repeats.", this);
        }

        private void LateUpdate()
        {
            if (resetStateOnNextUpdate) { resetStateOnNextUpdate = false; ResetState(); }
            if (IsSuppressed || (activeSource != null && !activeSource.IsPlayerNearby)) ClearThought();
            if (thoughtUI != null && !thoughtUI.IsVisible) activeSource = null;
        }

        public void ConfigureUI(ThoughtBubbleUI value) { thoughtUI = value; }
        public bool AcceptsPlayer(Transform candidate) => candidate != null && (player == null || candidate == player);

        public bool TriggerStone(Transform candidate, bool allowRepeat)
        {
            return isActiveAndEnabled && !IsSuppressed && AcceptsPlayer(candidate) && Session.ActivateStone(allowRepeat);
        }

        public bool TryShowThought(NPCThoughtTrigger2D source, Transform candidate)
        {
            if (!isActiveAndEnabled || source == null || IsSuppressed || thoughtSuppressors.Count > 0 || !AcceptsPlayer(candidate)) return false;
            EnsureUI();
            if (thoughtUI == null || !thoughtUI.CanDisplay || thoughtUI.IsVisible) return false;
            if (!Session.TryThought(source.GetInstanceID(), Time.timeAsDouble, globalCooldown, sameNpcCooldown, thoughts, out string text)) return false;
            activeSource = source;
            thoughtUI.Show(text);
            return true;
        }

        public void PlayerLeft(NPCThoughtTrigger2D source)
        {
            if (activeSource == source) ClearThought();
        }

        public void SetDialogueActive(bool active) { dialogueActive = active; if (active) ClearThought(); }
        public void SetPaused(bool value) { paused = value; if (value) ClearThought(); }
        public void SetExternalSuppression(bool value) { externallySuppressed = value; if (value) ClearThought(); }

        // Scoped ownership lets new systems release only their own suppression.
        public void SetThoughtChannelSuppressed(object owner, bool value)
        {
            if (owner == null) return;
            if (value) { thoughtSuppressors.Add(owner); ClearThought(); } else thoughtSuppressors.Remove(owner);
        }
        public void SetRuntimeSuppressed(object owner, bool value)
        {
            if (owner == null) return;
            if (value) { runtimeSuppressors.Add(owner); ClearThought(); } else runtimeSuppressors.Remove(owner);
        }

        [ContextMenu("Reset Stone And Thought State")]
        public void ResetState() { Session.Reset(); ClearThought(); }

        private void ClearThought()
        {
            if (thoughtUI != null) thoughtUI.Hide();
            activeSource = null;
        }

        private void EnsureUI()
        {
            if (thoughtUI == null) thoughtUI = NPCOverthinkingFactory.CreateThoughtUI(transform);
        }

        private void OnDisable() { ClearThought(); }
        private void OnValidate()
        {
            globalCooldown = Mathf.Max(0f, globalCooldown);
            sameNpcCooldown = Mathf.Max(0f, sameNpcCooldown);
        }
    }
}
