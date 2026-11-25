using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinalProject.Gym.Squat
{
    public static class SquatMinigameUIFactory
    {
        public static SquatMinigameController Create(Transform parent = null)
        {
            GameObject root = new GameObject("SquatMinigameUI", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            Color background = new Color32(10, 21, 34, 255);
            Color panelColor = new Color32(24, 41, 58, 255);
            Picture("Backdrop", root.transform, Vector2.zero, Vector2.one, background);
            RectTransform panel = Picture("Panel", root.transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), panelColor).rectTransform;
            Label("Title", panel, "深蹲挑戰", new Vector2(0.06f, 0.82f), new Vector2(0.68f, 0.96f), 42f, TextAlignmentOptions.MidlineLeft);
            Label("RepCounter", panel, "深蹲 0/5", new Vector2(0.7f, 0.83f), new Vector2(0.94f, 0.95f), 30f);
            Label("Instructions", panel, "空白鍵：停止指針", new Vector2(0.08f, 0.65f), new Vector2(0.92f, 0.77f), 28f);

            RectTransform bar = Rect("TimingBar", panel, new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.59f));
            string[] zones = { "RedLeft", "YellowLeft", "Green", "YellowRight", "RedRight" };
            foreach (string name in zones) Picture(name, bar, Vector2.zero, Vector2.one, Color.white).raycastTarget = false;
            Image pointer = Picture("Pointer", bar, new Vector2(0f, -0.2f), new Vector2(0f, 1.2f), Color.white);
            pointer.raycastTarget = false;
            pointer.rectTransform.offsetMin = new Vector2(-3f, 0f);
            pointer.rectTransform.offsetMax = new Vector2(3f, 0f);
            Label("BarLegend", panel, "紅：力竭　／　黃：成功　／　綠：完美", new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.45f), 20f);
            Label("Feedback", panel, string.Empty, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.37f), 30f);

            RectTransform recovery = Rect("RecoveryPanel", panel, new Vector2(0.13f, 0.09f), new Vector2(0.87f, 0.27f));
            Label("RecoveryLabel", recovery, "連按空白鍵：0/8", new Vector2(0f, 0.5f), Vector2.one, 23f);
            Image track = Picture("RecoveryTrack", recovery, new Vector2(0f, 0.1f), new Vector2(1f, 0.4f), new Color32(49, 67, 84, 255));
            Picture("RecoveryFill", track.transform, Vector2.zero, Vector2.one, new Color32(83, 197, 142, 255)).raycastTarget = false;
            MakeButton("ReturnButton", panel, "返回健身房", new Vector2(0.74f, 0.02f), new Vector2(0.96f, 0.085f));

            RectTransform completed = Picture("CompletionPanel", root.transform, Vector2.zero, Vector2.one, background).rectTransform;
            Label("Heading", completed, "深蹲挑戰完成！", new Vector2(0.15f, 0.65f), new Vector2(0.85f, 0.8f), 44f);
            Label("Summary", completed, "完成 5/5 次深蹲！", new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.62f), 28f);
            MakeButton("ReturnButton", completed, "返回健身房", new Vector2(0.34f, 0.25f), new Vector2(0.66f, 0.35f));

            RectTransform confirm = Picture("ReturnConfirmPanel", root.transform, Vector2.zero, Vector2.one, background).rectTransform;
            Label("Heading", confirm, "返回健身房？", new Vector2(0.15f, 0.65f), new Vector2(0.85f, 0.8f), 40f);
            Label("Summary", confirm, "本次 Play 中的深蹲次數與恢復進度會保留。\n再次靠近深蹲器材按 E，即可繼續。", new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.62f), 25f);
            MakeButton("ContinueButton", confirm, "繼續挑戰", new Vector2(0.23f, 0.25f), new Vector2(0.47f, 0.35f));
            MakeButton("ConfirmReturnButton", confirm, "返回健身房", new Vector2(0.53f, 0.25f), new Vector2(0.77f, 0.35f));

            recovery.gameObject.SetActive(false);
            completed.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
            SquatMinigameView view = root.AddComponent<SquatMinigameView>();
            view.BindReferences();
            view.ConfigureZones(new SquatGameSettings().NormalizedCopy());
            SquatMinigameController controller = root.AddComponent<SquatMinigameController>();
            root.SetActive(true);
            return controller;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image Picture(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            Image image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, string text, Vector2 min, Vector2 max,
            float size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.65f;
            label.fontSizeMax = size;
            label.alignment = alignment;
            label.color = new Color32(239, 245, 250, 255);
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max)
        {
            Image image = Picture(name, parent, min, max, new Color32(42, 96, 109, 255));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Navigation navigation = button.navigation;
            // Space belongs to timing/mashing, so focused buttons must not also submit it.
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            Label("Label", image.transform, text, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 23f);
            return button;
        }
    }
}
