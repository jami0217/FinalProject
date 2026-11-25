using FinalProject.Gym.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinalProject.Gym.Core
{
    // References to the independent core Canvas; no existing Canvas is repurposed.
    public sealed class GymCoreUI : MonoBehaviour
    {
        public TMP_FontAsset chineseFont;
        public Image anxietyFill;
        public TMP_Text anxietyText;
        public Toggle reduceMotionToggle;
        public TMP_Text progressText;
        public TMP_Text stationStatusText;
        public GameObject progressPanel;
        public GameObject choicePanel;
        public TMP_Text choiceText;
        public Button talkButton;
        public Button ignoreButton;
        public GameObject dialoguePanel;
        public TMP_Text friendlyText;
        public Button dialogueCloseButton;
        public GameObject victoryPanel;
        public Button restartButton;
        public ThoughtBubbleUI thoughtUI;
        public AnxietyVignetteGraphic vignette;
        private TMP_FontAsset generatedFont;

        public void HideWindows()
        {
            if (choicePanel != null) choicePanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (thoughtUI != null) thoughtUI.Hide();
        }
        public void ApplyFont()
        {
            if (chineseFont == null && generatedFont == null)
            {
                string[] families = { "PingFang TC", "Microsoft JhengHei", "Noto Sans CJK TC", "Noto Sans CJK SC", "Arial Unicode MS" };
                foreach (string family in families)
                {
                    generatedFont = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                    if (generatedFont != null) break;
                }
                if (generatedFont == null) Debug.LogWarning("GymCoreUI: assign a Traditional Chinese TMP Font Asset if characters appear as squares.", this);
            }
            TMP_FontAsset font = chineseFont != null ? chineseFont : generatedFont;
            if (font != null) foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) text.font = font;
        }
        private void OnDestroy()
        {
            if (generatedFont == null) return;
            foreach (Texture2D atlas in generatedFont.atlasTextures) if (atlas != null) Destroy(atlas);
            if (generatedFont.material != null) Destroy(generatedFont.material);
            Destroy(generatedFont);
        }
    }
}
