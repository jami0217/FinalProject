using FinalProject.Gym.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FinalProject.Gym.Core
{
    public static class GymCoreFactory
    {
        public static GameObject CreateSystem()
        {
            GameObject root = new GameObject("GymAnxietyCore");
            root.SetActive(false);
            CreateUI(root.transform);
            root.AddComponent<NPCDialogueController>();
            root.AddComponent<AnxietyEffectsController>();
            root.AddComponent<AnxietyHUD>();
            root.AddComponent<GymProgressHUD>();
            root.AddComponent<GymInnerThoughts>();
            root.AddComponent<GymGameBridge>();
            root.SetActive(true);
            return root;
        }

        public static GymCoreUI CreateUI(Transform parent)
        {
            GameObject root = new GameObject("GymCoreCanvas", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 280;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            GymCoreUI ui = root.AddComponent<GymCoreUI>();
            ui.vignette = Rect("AnxietyVignette", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<AnxietyVignetteGraphic>();
            ui.vignette.SetIntensity(0f);
            Color panelColor = new Color32(19, 36, 51, 238);
            RectTransform anxiety = Panel("AnxietyHUD", root.transform, new Vector2(0.7f, 0.925f), new Vector2(0.985f, 0.99f), panelColor);
            ui.anxietyText = Label("AnxietyText", anxiety, "焦慮 20/100　平穩", new Vector2(0.03f, 0.4f), new Vector2(0.97f, 0.98f), 22f);
            RectTransform track = Panel("Track", anxiety, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.31f), new Color32(58, 72, 86, 255));
            ui.anxietyFill = Panel("Fill", track, Vector2.zero, new Vector2(0.2f, 1f), new Color32(83, 197, 142, 255)).GetComponent<Image>();
            ui.reduceMotionToggle = MakeToggle(root.transform);

            RectTransform progress = Panel("ProgressHUD", root.transform, new Vector2(0.012f, 0.67f), new Vector2(0.29f, 0.985f), panelColor);
            ui.progressPanel = progress.gameObject;
            ui.progressText = Label("ProgressText", progress, "完成器材 0/5", new Vector2(0.04f, 0.76f), new Vector2(0.96f, 0.98f), 27f);
            ui.stationStatusText = Label("StationStates", progress, string.Empty, new Vector2(0.06f, 0.04f), new Vector2(0.96f, 0.76f), 20f);
            ui.stationStatusText.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform choice = Panel("VeteranChoice", root.transform, new Vector2(0.69f, 0.35f), new Vector2(0.985f, 0.62f), panelColor);
            ui.choicePanel = choice.gameObject;
            ui.choiceText = Label("ChoiceText", choice, string.Empty, new Vector2(0.04f, 0.39f), new Vector2(0.96f, 0.96f), 23f);
            ui.talkButton = Button("TalkButton", choice, "交談 [F]", new Vector2(0.05f, 0.08f), new Vector2(0.47f, 0.31f));
            ui.ignoreButton = Button("IgnoreButton", choice, "先不要 [G]", new Vector2(0.53f, 0.08f), new Vector2(0.95f, 0.31f));

            RectTransform dialogue = Panel("FriendlyDialogue", root.transform, Vector2.zero, Vector2.one, new Color32(9, 20, 31, 250));
            ui.dialoguePanel = dialogue.gameObject;
            Label("Title", dialogue, "原來，是友善的招呼。", new Vector2(0.15f, 0.68f), new Vector2(0.85f, 0.82f), 36f);
            ui.friendlyText = Label("FriendlyText", dialogue, string.Empty, new Vector2(0.16f, 0.43f), new Vector2(0.84f, 0.65f), 32f);
            ui.dialogueCloseButton = Button("ContinueButton", dialogue, "繼續訓練", new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.35f));

            RectTransform win = Panel("VictoryPanel", root.transform, Vector2.zero, Vector2.one, new Color32(9, 20, 31, 255));
            ui.victoryPanel = win.gameObject;
            Label("Title", win, "傲人好身材！", new Vector2(0.15f, 0.67f), new Vector2(0.85f, 0.82f), 46f);
            Label("Message", win, "你完成了五項訓練。\n每一小步，都在接近理想中的自己。", new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.64f), 29f);
            ui.restartButton = Button("RestartButton", win, "重新開始", new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.35f));
            ui.thoughtUI = NPCOverthinkingFactory.CreateThoughtUI(root.transform);
            ui.HideWindows();
            if (Application.isPlaying && EventSystem.current == null)
            {
                GameObject events = new GameObject("GymCoreEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(root.transform, false);
            }
            root.SetActive(true);
            return ui;
        }

        private static Toggle MakeToggle(Transform parent)
        {
            RectTransform rect = Panel("ReduceMotion", parent, new Vector2(0.7f, 0.88f), new Vector2(0.985f, 0.917f), new Color32(29, 57, 70, 245));
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = rect.GetComponent<Image>();
            rect.GetComponent<Image>().raycastTarget = true;
            toggle.graphic = Panel("Checkmark", rect, new Vector2(0.02f, 0.18f), new Vector2(0.075f, 0.82f), new Color32(83, 197, 142, 255)).GetComponent<Image>();
            Label("Label", rect, "減少動態效果", new Vector2(0.1f, 0f), Vector2.one, 20f);
            Navigation navigation = toggle.navigation; navigation.mode = Navigation.Mode.None; toggle.navigation = navigation;
            toggle.isOn = false;
            return toggle;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = Rect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color;
            image.raycastTarget = name == "FriendlyDialogue" || name == "VictoryPanel";
            return rect;
        }
        private static TMP_Text Label(string name, Transform parent, string text, Vector2 min, Vector2 max, float size)
        {
            TMP_Text label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = size;
            label.enableAutoSizing = true; label.fontSizeMin = size * 0.65f; label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return label;
        }
        private static Button Button(string name, Transform parent, string text, Vector2 min, Vector2 max)
        {
            RectTransform rect = Panel(name, parent, min, max, new Color32(39, 100, 110, 255));
            Image image = rect.GetComponent<Image>(); image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            Label("Label", rect, text, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 23f);
            return button;
        }
    }
}
