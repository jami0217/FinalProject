using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinalProject.Gym.NPC
{
    public static class NPCOverthinkingFactory
    {
        public static GameObject CreateSystemRoot()
        {
            GameObject root = new GameObject("NPCOverthinkingSystem");
            root.SetActive(false);
            OverthinkingManager manager = root.AddComponent<OverthinkingManager>();
            NPCSpawner2D spawner = root.AddComponent<NPCSpawner2D>();
            Collider2D area = CreateDefaultArea(root.transform);
            Transform pointsRoot = new GameObject("SpawnPoints").transform;
            pointsRoot.SetParent(root.transform, false);
            Vector2[] positions = { new Vector2(-3f, 1.8f), new Vector2(-0.8f, 1.8f),
                new Vector2(2.5f, 2f), new Vector2(-3f, -1.7f), new Vector2(0.7f, -1.8f) };
            Transform[] points = new Transform[positions.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new GameObject("SpawnPoint_" + (i + 1)).transform;
                points[i].SetParent(pointsRoot, false);
                points[i].localPosition = positions[i];
            }
            manager.ConfigureUI(CreateThoughtUI(root.transform));
            spawner.Configure(manager, area, points);
            CreateStone(root.transform, manager, new Vector2(2.6f, -0.7f));
            root.SetActive(true);
            return root;
        }

        public static BoxCollider2D CreateDefaultArea(Transform parent)
        {
            GameObject area = new GameObject("WalkableArea", typeof(BoxCollider2D));
            area.transform.SetParent(parent, false);
            BoxCollider2D collider = area.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(12f, 7f);
            collider.isTrigger = true;
            return collider;
        }

        public static ThoughtBubbleUI CreateThoughtUI(Transform parent)
        {
            GameObject root = new GameObject("ThoughtBubbleCanvas", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            Image panel = Rect("ThoughtPanel", root.transform, new Vector2(0.17f, 0.19f), new Vector2(0.83f, 0.33f)).gameObject.AddComponent<Image>();
            panel.color = new Color32(24, 41, 58, 242);
            panel.raycastTarget = false;
            Label("Title", panel.transform, "心裡話", new Vector2(0.03f, 0.65f), new Vector2(0.97f, 0.96f), 17f);
            TMP_Text message = Label("Message", panel.transform, string.Empty, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.65f), 27f);
            ThoughtBubbleUI view = root.AddComponent<ThoughtBubbleUI>();
            view.Configure(message, group);
            root.SetActive(true);
            return view;
        }

        public static NPCWander2D CreateNPC(Transform parent, Vector2 position, int colorIndex)
        {
            GameObject root = new GameObject("GymNPC_" + (colorIndex + 1));
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(position.x, position.y, parent != null ? parent.position.z : 0f);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.22f;
            Sprite sprite = CreatePlaceholderSprite();
            SpriteRenderer visual = Visual(root.transform, sprite, new Vector2(0.42f, 0.62f));
            Color32[] colors = { new Color32(94, 176, 205, 255), new Color32(211, 145, 119, 255),
                new Color32(150, 182, 129, 255), new Color32(170, 147, 201, 255), new Color32(218, 189, 111, 255) };
            visual.color = colors[Mathf.Abs(colorIndex) % colors.Length];
            visual.sortingOrder = 5;
            NPCWander2D wander = root.AddComponent<NPCWander2D>();
            wander.Configure(null, null, Physics2D.DefaultRaycastLayers, 1.6f, 0.65f, visual, sprite);
            CreateDetectionZone(root.transform);
            root.SetActive(true);
            return wander;
        }

        public static NPCThoughtTrigger2D CreateDetectionZone(Transform parent)
        {
            GameObject zone = new GameObject("DetectionZone", typeof(CircleCollider2D));
            zone.transform.SetParent(parent, false);
            CircleCollider2D collider = zone.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.5f;
            return zone.AddComponent<NPCThoughtTrigger2D>();
        }

        public static StoneSurpriseTrigger2D CreateStone(Transform parent, OverthinkingManager manager, Vector2 position)
        {
            GameObject stone = new GameObject("StoneTrigger");
            stone.SetActive(false);
            stone.transform.SetParent(parent, false);
            stone.transform.position = new Vector3(position.x, position.y, parent.position.z);
            CircleCollider2D trigger = stone.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            Sprite sprite = CreatePlaceholderSprite();
            SpriteRenderer visual = Visual(stone.transform, sprite, new Vector2(0.6f, 0.4f));
            visual.color = new Color32(144, 150, 158, 255);
            visual.sortingOrder = 4;
            RectTransform indicator = Rect("ExclamationCanvas", stone.transform, Vector2.zero, Vector2.one);
            indicator.localPosition = new Vector3(0f, 0.85f, 0f);
            indicator.sizeDelta = new Vector2(80f, 80f);
            indicator.localScale = Vector3.one * 0.01f;
            Canvas canvas = indicator.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 80;
            indicator.sizeDelta = new Vector2(80f, 80f);
            indicator.localScale = Vector3.one * 0.01f;
            indicator.localPosition = new Vector3(0f, 0.85f, 0f);
            TMP_Text mark = Label("Exclamation", indicator, "!", Vector2.zero, Vector2.one, 64f);
            mark.color = new Color32(255, 218, 92, 255);
            StoneSurpriseTrigger2D script = stone.AddComponent<StoneSurpriseTrigger2D>();
            script.Configure(manager, indicator.gameObject, sprite);
            stone.SetActive(true);
            return script;
        }

        // Runtime-only placeholder sprites do not create or replace project art assets.
        public static Sprite CreatePlaceholderSprite()
        {
            if (!Application.isPlaying) return null;
            Texture2D texture = Texture2D.whiteTexture;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }

        private static SpriteRenderer Visual(Transform parent, Sprite sprite, Vector2 size)
        {
            GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, string text, Vector2 min, Vector2 max, float size)
        {
            TMP_Text label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text; label.fontSize = size;
            label.enableAutoSizing = true; label.fontSizeMin = size * 0.7f; label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }
    }
}
