using TMPro;
using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    public sealed class ThoughtBubbleUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text message;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_FontAsset chineseFont;
        [SerializeField, Min(0.5f)] private float displayDuration = 2.5f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.12f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;
        private TMP_FontAsset generatedFont;
        private bool checkedFont;
        private bool visible;
        private float elapsed;
        public bool IsVisible => visible && isActiveAndEnabled;
        public bool CanDisplay => isActiveAndEnabled && message != null && group != null;

        public void Configure(TMP_Text text, CanvasGroup canvasGroup) { message = text; group = canvasGroup; Hide(); }
        private void Awake() { ConfigureFont(); Hide(); }

        public void Show(string text)
        {
            if (message == null || group == null)
            {
                Debug.LogWarning("ThoughtBubbleUI needs Message (TMP) and Canvas Group references.", this);
                return;
            }
            ConfigureFont();
            message.text = text;
            elapsed = 0f;
            visible = true;
            group.alpha = fadeInDuration > 0f ? 0f : 1f;
            group.interactable = group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (!visible || group == null) return;
            elapsed += Time.deltaTime;
            if (elapsed >= displayDuration) { Hide(); return; }
            float fadeIn = fadeInDuration > 0f ? Mathf.Clamp01(elapsed / fadeInDuration) : 1f;
            float fadeOut = fadeOutDuration > 0f ? Mathf.Clamp01((displayDuration - elapsed) / fadeOutDuration) : 1f;
            group.alpha = Mathf.Min(fadeIn, fadeOut);
        }

        public void Hide()
        {
            visible = false;
            if (group != null) { group.alpha = 0f; group.interactable = group.blocksRaycasts = false; }
        }

        private void ConfigureFont()
        {
            if (chineseFont == null && !checkedFont)
            {
                checkedFont = true;
                string[] families = { "PingFang TC", "Microsoft JhengHei", "Noto Sans CJK TC", "Noto Sans CJK SC", "Arial Unicode MS" };
                foreach (string family in families)
                {
                    generatedFont = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);
                    if (generatedFont != null) break;
                }
                if (generatedFont == null)
                    Debug.LogWarning("Assign a Traditional Chinese TMP Font Asset to ThoughtBubbleUI > Chinese Font if text appears as squares.", this);
            }
            TMP_FontAsset font = chineseFont != null ? chineseFont : generatedFont;
            if (font != null)
                foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true)) label.font = font;
        }

        private void OnDisable() { Hide(); }
        private void OnValidate()
        {
            displayDuration = Mathf.Max(0.5f, displayDuration);
            fadeInDuration = Mathf.Clamp(fadeInDuration, 0f, displayDuration * 0.5f);
            fadeOutDuration = Mathf.Clamp(fadeOutDuration, 0f, displayDuration * 0.5f);
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
