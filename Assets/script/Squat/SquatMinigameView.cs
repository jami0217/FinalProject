using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FinalProject.Gym.Squat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class SquatMinigameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text repCounter;
        [SerializeField] private TMP_Text instructions;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private TMP_Text recoveryLabel;
        [SerializeField] private TMP_Text completionSummary;
        [SerializeField] private RectTransform pointer;
        [SerializeField] private Image[] timingZones = new Image[5];
        [SerializeField] private Image recoveryFill;
        [SerializeField] private GameObject recoveryPanel;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private GameObject returnConfirmPanel;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button completionReturnButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button confirmReturnButton;

        public bool BindReferences()
        {
            Bind(ref title, "Panel/Title");
            Bind(ref repCounter, "Panel/RepCounter");
            Bind(ref instructions, "Panel/Instructions");
            Bind(ref feedback, "Panel/Feedback");
            Bind(ref recoveryLabel, "Panel/RecoveryPanel/RecoveryLabel");
            Bind(ref completionSummary, "CompletionPanel/Summary");
            Bind(ref pointer, "Panel/TimingBar/Pointer");
            Bind(ref recoveryFill, "Panel/RecoveryPanel/RecoveryTrack/RecoveryFill");
            Bind(ref returnButton, "Panel/ReturnButton");
            Bind(ref completionReturnButton, "CompletionPanel/ReturnButton");
            Bind(ref continueButton, "ReturnConfirmPanel/ContinueButton");
            Bind(ref confirmReturnButton, "ReturnConfirmPanel/ConfirmReturnButton");
            if (recoveryPanel == null) recoveryPanel = FindObject("Panel/RecoveryPanel");
            if (completionPanel == null) completionPanel = FindObject("CompletionPanel");
            if (returnConfirmPanel == null) returnConfirmPanel = FindObject("ReturnConfirmPanel");

            string[] zoneNames = { "RedLeft", "YellowLeft", "Green", "YellowRight", "RedRight" };
            if (timingZones == null || timingZones.Length != 5) timingZones = new Image[5];
            for (int i = 0; i < timingZones.Length; i++)
                Bind(ref timingZones[i], "Panel/TimingBar/" + zoneNames[i]);

            bool valid = title != null && repCounter != null && instructions != null && feedback != null
                && recoveryLabel != null && completionSummary != null && pointer != null && recoveryFill != null
                && recoveryPanel != null && completionPanel != null && returnConfirmPanel != null
                && returnButton != null && completionReturnButton != null && continueButton != null
                && confirmReturnButton != null;
            foreach (Image zone in timingZones) valid &= zone != null;
            return valid;
        }

        public void SetVisible(bool visible)
        {
            GetComponent<Canvas>().enabled = visible;
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = visible;
        }

        public void BindButtons(UnityAction requestReturn, UnityAction resume, UnityAction confirmReturn)
        {
            UnbindButtons(requestReturn, resume, confirmReturn);
            returnButton.onClick.AddListener(requestReturn);
            completionReturnButton.onClick.AddListener(requestReturn);
            continueButton.onClick.AddListener(resume);
            confirmReturnButton.onClick.AddListener(confirmReturn);
        }

        public void UnbindButtons(UnityAction requestReturn, UnityAction resume, UnityAction confirmReturn)
        {
            if (returnButton != null) returnButton.onClick.RemoveListener(requestReturn);
            if (completionReturnButton != null) completionReturnButton.onClick.RemoveListener(requestReturn);
            if (continueButton != null) continueButton.onClick.RemoveListener(resume);
            if (confirmReturnButton != null) confirmReturnButton.onClick.RemoveListener(confirmReturn);
        }

        public void ApplyFont(TMP_FontAsset font)
        {
            if (font == null) return;
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true)) label.font = font;
        }

        public void ConfigureZones(SquatGameSettings settings, float? perfectStart = null, float? perfectEnd = null)
        {
            float[] edges = { 0f, settings.leftDangerEnd, perfectStart ?? settings.perfectStart,
                perfectEnd ?? settings.perfectEnd, settings.rightDangerStart, 1f };
            Color red = new Color32(226, 87, 87, 255);
            Color yellow = new Color32(238, 195, 91, 255);
            Color green = new Color32(83, 197, 142, 255);
            Color[] colors = { red, yellow, green, yellow, red };
            for (int i = 0; i < timingZones.Length; i++)
            {
                RectTransform rect = timingZones[i].rectTransform;
                rect.anchorMin = new Vector2(edges[i], 0f);
                rect.anchorMax = new Vector2(edges[i + 1], 1f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                timingZones[i].color = colors[i];
            }
        }

        public void Render(SquatGameModel model, string keyLabel, bool confirmReturn)
        {
            title.text = "深蹲挑戰";
            repCounter.text = $"深蹲 {model.CompletedReps}/{model.RepGoal}";
            instructions.text = model.State == SquatGameState.Exhausted
                ? $"力竭！連按{keyLabel}恢復！"
                : model.State == SquatGameState.Feedback ? "準備下一次深蹲…"
                : model.State == SquatGameState.Completed ? "挑戰完成！" : $"{keyLabel}：停止指針";
            feedback.text = model.LastResult == SquatHitResult.Perfect ? "完美！+1"
                : model.LastResult == SquatHitResult.Good ? "成功！+1"
                : model.LastResult == SquatHitResult.Exhausted ? "力竭！"
                : model.LastResult == SquatHitResult.Recovered ? "恢復成功！+1" : string.Empty;
            pointer.anchorMin = new Vector2(model.PointerPosition, -0.2f);
            pointer.anchorMax = new Vector2(model.PointerPosition, 1.2f);
            pointer.offsetMin = new Vector2(-3f, 0f);
            pointer.offsetMax = new Vector2(3f, 0f);
            recoveryPanel.SetActive(model.State == SquatGameState.Exhausted);
            recoveryLabel.text = $"連按{keyLabel}：{model.RecoveryPresses}/{model.RecoveryTarget}";
            recoveryFill.fillAmount = model.RecoveryProgress;
            recoveryFill.rectTransform.anchorMin = Vector2.zero;
            recoveryFill.rectTransform.anchorMax = new Vector2(model.RecoveryProgress, 1f);
            recoveryFill.rectTransform.offsetMin = recoveryFill.rectTransform.offsetMax = Vector2.zero;
            completionPanel.SetActive(model.State == SquatGameState.Completed);
            completionSummary.text = $"完成 {model.CompletedReps}/{model.RepGoal} 次深蹲！\n已記錄至健身房完成進度。";
            returnConfirmPanel.SetActive(confirmReturn);
        }

        private void Bind<T>(ref T reference, string path) where T : Component
        {
            if (reference == null)
            {
                Transform child = transform.Find(path);
                if (child != null) reference = child.GetComponent<T>();
            }
        }

        private GameObject FindObject(string path)
        {
            Transform child = transform.Find(path);
            return child != null ? child.gameObject : null;
        }
    }
}
