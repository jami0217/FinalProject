using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FinalProject.Gym.Core
{
    public sealed class NPCDialogueController : MonoBehaviour
    {
        [SerializeField] private Key talkKey = Key.F;
        [SerializeField] private Key ignoreKey = Key.G;
        [SerializeField, TextArea(1, 3)] private string[] friendlyLines =
        { "請問可以共用器材嗎？", "你的健身服真好看！", "你剛開始練嗎？不用急，慢慢來就好。" };
        private GymGameBridge bridge;
        private GymCoreUI ui;
        private GymVeteranNPC current;
        private GameObject previousSelection;
        private int openingFrame;
        private string lastLine;
        private bool ready;
        private readonly System.Random random = new System.Random();
        public bool IsDialogueOpen { get; private set; }

        public void Bind(GymGameBridge context, GymCoreUI view)
        {
            Unsubscribe();
            bridge = context; ui = view;
            if (ui == null || ui.talkButton == null || ui.ignoreButton == null || ui.dialogueCloseButton == null
                || ui.choicePanel == null || ui.dialoguePanel == null || ui.choiceText == null || ui.friendlyText == null)
            { Debug.LogWarning("Dialogue UI references are incomplete; recreate or connect GymCoreUI.", this); return; }
            ui.talkButton.onClick.AddListener(Talk);
            ui.ignoreButton.onClick.AddListener(Ignore);
            ui.dialogueCloseButton.onClick.AddListener(Finish);
            ready = true;
            TMPro.TMP_Text talkLabel = ui.talkButton.GetComponentInChildren<TMPro.TMP_Text>();
            TMPro.TMP_Text ignoreLabel = ui.ignoreButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (talkLabel != null) talkLabel.text = $"交談 [{talkKey}]";
            if (ignoreLabel != null) ignoreLabel.text = $"先不要 [{ignoreKey}]";
        }

        private void Update()
        {
            if (!ready || bridge == null || !bridge.isActiveAndEnabled || ui == null) return;
            Keyboard keyboard = Keyboard.current;
            if (IsDialogueOpen)
            {
                if (current == null || !current.isActiveAndEnabled || bridge.IsMinigameActive) { Close(false); return; }
                if (bridge.IsPaused) return;
                if (Time.frameCount != openingFrame && (Pressed(keyboard, talkKey) || Pressed(keyboard, Key.Escape))) Finish();
                return;
            }
            if (bridge.IsModal || bridge.IsMinigameActive)
            { if (ui.choicePanel != null) ui.choicePanel.SetActive(false); current = null; return; }
            current = bridge.NearestWaitingVeteran;
            if (ui.choicePanel == null || ui.choiceText == null) return;
            ui.choicePanel.SetActive(current != null);
            if (current == null) return;
            ui.choiceText.text = $"{current.DisplayName}停在附近。\n[{talkKey}] 交談　[{ignoreKey}] 先不要\n也可以直接走開，繼續訓練。";
            if (Pressed(keyboard, talkKey)) Talk();
            else if (Pressed(keyboard, ignoreKey)) Ignore();
        }

        private void Talk()
        {
            if (!ready || bridge == null || bridge.IsModal || bridge.IsMinigameActive || current == null || !current.BeginTalk()) return;
            IsDialogueOpen = true;
            openingFrame = Time.frameCount;
            ui.choicePanel.SetActive(false);
            ui.friendlyText.text = NextFriendlyLine();
            ui.dialoguePanel.SetActive(true);
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            bridge.RefreshInteractionLock();
        }
        private void Ignore()
        {
            if (current != null && !IsDialogueOpen) current.Dismiss();
            if (ui != null && ui.choicePanel != null) ui.choicePanel.SetActive(false);
            current = null;
        }
        private void Finish() { if (bridge != null && !bridge.IsPaused) Close(true); }
        public void Cancel() { Close(false); }
        private void Close(bool friendlyCompleted)
        {
            if (!IsDialogueOpen) return;
            IsDialogueOpen = false;
            if (ui != null && ui.dialoguePanel != null) ui.dialoguePanel.SetActive(false);
            if (current != null) current.Dismiss();
            current = null;
            if (friendlyCompleted && bridge != null && bridge.Anxiety != null) bridge.Anxiety.ApplyFriendlyRelief();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
            if (bridge != null) bridge.RefreshInteractionLock();
        }
        private string NextFriendlyLine()
        {
            List<string> options = new List<string>();
            if (friendlyLines != null) foreach (string line in friendlyLines)
                if (!string.IsNullOrWhiteSpace(line) && line != lastLine) options.Add(line);
            lastLine = options.Count > 0 ? options[random.Next(options.Count)] : "不用急，慢慢來就好。";
            return lastLine;
        }
        private static bool Pressed(Keyboard keyboard, Key key) => keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
        private void Unsubscribe()
        {
            ready = false;
            if (ui == null) return;
            if (ui.talkButton != null) ui.talkButton.onClick.RemoveListener(Talk);
            if (ui.ignoreButton != null) ui.ignoreButton.onClick.RemoveListener(Ignore);
            if (ui.dialogueCloseButton != null) ui.dialogueCloseButton.onClick.RemoveListener(Finish);
        }
        private void OnDisable() { Cancel(); Unsubscribe(); }
        private void OnEnable() { if (Application.isPlaying && bridge != null && ui != null) Bind(bridge, ui); }
        private void OnValidate()
        {
            if (talkKey == Key.None || talkKey == Key.E) talkKey = Key.F;
            if (ignoreKey == Key.None || ignoreKey == Key.E || ignoreKey == talkKey) ignoreKey = talkKey != Key.G ? Key.G : Key.F;
        }
    }
}
