using System.Collections;
using System.Collections.Generic;
using FinalProject.Gym.NPC;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class GymGameBridge : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private GymCoreUI ui;
        [SerializeField] private GymStationDefinition[] stations = GymStationDefinition.Defaults();
        [SerializeField] private AnxietySettings anxietySettings = new AnxietySettings();
        [SerializeField] private bool freezeAnxietyDuringMinigames = true;
        [SerializeField] private bool createNpcSupportIfMissing = true;
        [SerializeField, Range(0, 10)] private int veteranCount = 2;
        [SerializeField] private NPCWander2D[] veteranCandidates;
        [SerializeField] private VeteranApproachSettings automaticVeteranSettings = new VeteranApproachSettings();
        [SerializeField, Min(0.05f)] private float automaticVeteranSpeed = 0.8f;
        [SerializeField] private bool freezeVeteransDuringMinigames = true;
        [SerializeField] private bool paused;
        private readonly List<GymVeteranNPC> veterans = new List<GymVeteranNPC>();
        private readonly List<OverthinkingManager> legacyThoughts = new List<OverthinkingManager>();
        private bool ownsContext;
        private bool savedHud;
        private bool playerLocked;
        private bool playerWasEnabled;
        public static GymGameBridge Instance { get; private set; }
        public Player Player => player;
        public GymGameManager Game { get; private set; }
        public GymProgressManager Progress { get; private set; }
        public AnxietyManager Anxiety { get; private set; }
        public NPCDialogueController Dialogue { get; private set; }
        public AnxietyEffectsController Effects { get; private set; }
        public bool IsMinigameActive => Game != null && Game.IsMiniGameOpen;
        public bool IsDialogueOpen => Dialogue != null && Dialogue.IsDialogueOpen;
        public bool IsPaused => paused || Time.timeScale <= 0f;
        public bool IsModal => IsPaused || IsDialogueOpen || (Progress != null && Progress.AllCompleted);
        public bool IsVictoryVisible => Progress != null && Progress.AllCompleted && !IsMinigameActive && !IsDialogueOpen;
        public bool ShouldFreezeVeterans => IsModal || (freezeVeteransDuringMinigames && IsMinigameActive);
        public GymVeteranNPC NearestAnxietySource => Nearest(false);
        public GymVeteranNPC NearestWaitingVeteran => Nearest(true);
        public float AnxietyNormalized => Anxiety != null ? Anxiety.AnxietyNormalized : 0f;
        public float GetDifficultyMultiplier() => Anxiety != null ? Anxiety.GetDifficultyMultiplier() : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Only one active GymGameBridge is allowed. This duplicate core component and its UI were disabled.", this);
                if (ui == null) ui = GetComponentInChildren<GymCoreUI>(true);
                if (ui != null) ui.gameObject.SetActive(false);
                enabled = false;
                return;
            }
            Instance = this;
            ownsContext = true;
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            Game = GymGameManager.EnsureInstance();
            Progress = Game.Progress;
            Progress.ConfigureStations(stations);
            Anxiety = Game.GetComponent<AnxietyManager>();
            if (Anxiety == null) Anxiety = Game.gameObject.AddComponent<AnxietyManager>();
            Anxiety.Configure(anxietySettings, freezeAnxietyDuringMinigames);
            if (player == null) player = FindFirstObjectByType<Player>();
            if (player == null) Debug.LogWarning("Gym core needs the existing Player component. Assign Player in the Inspector.", this);
            if (ui == null) ui = GetComponentInChildren<GymCoreUI>(true);
            if (ui == null) ui = GymCoreFactory.CreateUI(transform);
            ui.gameObject.SetActive(true);
            ui.ApplyFont();
            Dialogue = GetOrAdd<NPCDialogueController>();
            Effects = GetOrAdd<AnxietyEffectsController>();
            Effects.Bind(this, ui.vignette);
            Dialogue.Bind(this, ui);
            GetOrAdd<AnxietyHUD>().Bind(this, ui);
            GetOrAdd<GymProgressHUD>().Bind(this, ui);
            GetOrAdd<GymInnerThoughts>().Bind(this, ui.thoughtUI);
            savedHud = Game.UseExternalHud;
            Game.UseExternalHud = true;

            OverthinkingManager existing = FindFirstObjectByType<OverthinkingManager>();
            if (existing == null && createNpcSupportIfMissing)
            {
                GameObject support = NPCOverthinkingFactory.CreateSystemRoot();
                support.transform.SetParent(transform, true);
            }
            CollectLegacyThoughts();
            // NPCSpawner2D.Start creates its instances after scene loading.
            yield return null;
            CollectLegacyThoughts();
            List<NPCWander2D> candidates = new List<NPCWander2D>();
            if (veteranCandidates != null)
                foreach (NPCWander2D candidate in veteranCandidates) if (candidate != null && !candidates.Contains(candidate)) candidates.Add(candidate);
            if (candidates.Count == 0)
            {
                foreach (NPCWander2D candidate in FindObjectsByType<NPCWander2D>(FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == gameObject.scene) candidates.Add(candidate);
                if (player != null) candidates.Sort((a, b) => Vector2.SqrMagnitude((Vector2)(a.transform.position - player.transform.position))
                    .CompareTo(Vector2.SqrMagnitude((Vector2)(b.transform.position - player.transform.position))));
            }
            foreach (GymVeteranNPC veteran in FindObjectsByType<GymVeteranNPC>(FindObjectsSortMode.None))
                if (veteran.gameObject.scene == gameObject.scene) veteran.Initialize(this);
            for (int i = 0; i < candidates.Count && veterans.Count < veteranCount; i++)
            {
                GymVeteranNPC veteran = candidates[i].GetComponent<GymVeteranNPC>();
                if (veteran == null)
                {
                    veteran = candidates[i].gameObject.AddComponent<GymVeteranNPC>();
                    veteran.ConfigureApproach(automaticVeteranSettings, automaticVeteranSpeed);
                }
                if (veteran.enabled) veteran.Initialize(this);
            }
            if (veteranCount > 0 && veterans.Count == 0) Debug.LogWarning("No veteran NPCs found. Check NPCSpawner2D or assign Veteran Candidates.", this);
            RefreshInteractionLock();
        }

        private T GetOrAdd<T>() where T : Component
        { T component = GetComponent<T>(); return component != null ? component : gameObject.AddComponent<T>(); }
        private void CollectLegacyThoughts()
        {
            foreach (OverthinkingManager manager in FindObjectsByType<OverthinkingManager>(FindObjectsSortMode.None))
            {
                if (manager.gameObject.scene != gameObject.scene || legacyThoughts.Contains(manager)) continue;
                legacyThoughts.Add(manager);
                manager.SetThoughtChannelSuppressed(this, true);
            }
        }
        public void RegisterVeteran(GymVeteranNPC veteran) { if (veteran != null && !veterans.Contains(veteran)) veterans.Add(veteran); }
        public void UnregisterVeteran(GymVeteranNPC veteran) { veterans.Remove(veteran); }
        private GymVeteranNPC Nearest(bool waiting)
        {
            GymVeteranNPC best = null;
            float distance = float.PositiveInfinity;
            foreach (GymVeteranNPC veteran in veterans)
                if (veteran != null && (waiting ? veteran.CanTalk : veteran.IsAnxietySource) && veteran.PlayerDistance < distance)
                { best = veteran; distance = veteran.PlayerDistance; }
            return best;
        }
        private void LateUpdate() { if (ownsContext) RefreshInteractionLock(); }
        public void RefreshInteractionLock()
        {
            if (!ownsContext || Game == null) return;
            Game.InteractionsBlocked = IsModal;
            bool shouldLock = IsModal && !IsMinigameActive;
            if (player != null)
            {
                if (shouldLock && !playerLocked) { playerWasEnabled = player.enabled; playerLocked = true; }
                if (shouldLock) player.enabled = false;
                else if (playerLocked) { player.enabled = playerWasEnabled; playerLocked = false; }
            }
            foreach (OverthinkingManager manager in legacyThoughts)
                if (manager != null) manager.SetRuntimeSuppressed(this, IsModal);
        }
        public void SetPaused(bool value) { paused = value; RefreshInteractionLock(); }
        public void RestartRound()
        {
            if (Game == null) return;
            paused = false;
            if (Dialogue != null) Dialogue.Cancel();
            Game.ResetForNewRound();
            Progress.ConfigureStations(stations);
            Anxiety.ResetRound();
            foreach (GymVeteranNPC veteran in veterans) if (veteran != null) veteran.ResetForNewRound();
            GetOrAdd<GymInnerThoughts>().ResetRound();
            foreach (OverthinkingManager manager in legacyThoughts) if (manager != null) manager.ResetState();
            RefreshInteractionLock();
        }
        private void OnDisable()
        {
            if (!ownsContext) return;
            StopAllCoroutines();
            if (Dialogue != null) Dialogue.Cancel();
            if (ui != null) { ui.HideWindows(); ui.gameObject.SetActive(false); }
            if (Effects != null) Effects.ClearImmediately();
            for (int i = veterans.Count - 1; i >= 0; i--) if (veterans[i] != null) veterans[i].Unbind();
            veterans.Clear();
            foreach (OverthinkingManager manager in legacyThoughts) if (manager != null)
            { manager.SetThoughtChannelSuppressed(this, false); manager.SetRuntimeSuppressed(this, false); }
            legacyThoughts.Clear();
            if (playerLocked && player != null) player.enabled = playerWasEnabled;
            playerLocked = false;
            if (Game != null) { Game.UseExternalHud = savedHud; Game.InteractionsBlocked = false; }
            if (Instance == this) Instance = null;
            ownsContext = false;
        }
    }
}
