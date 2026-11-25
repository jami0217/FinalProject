using System.Collections.Generic;
using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    public sealed class StoneSurpriseTrigger2D : MonoBehaviour
    {
        [SerializeField] private OverthinkingManager manager;
        [SerializeField] private GameObject exclamationIndicator;
        [SerializeField] private bool allowRepeat;
        [SerializeField, Min(0.1f)] private float exclamationDuration = 1.5f;
        [SerializeField, Min(0f)] private float popHeight = 0.12f;
        private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();
        private Vector3 indicatorOrigin;
        private float remaining;
        private Sprite generatedSprite;

        public void Configure(OverthinkingManager value, GameObject indicator, Sprite ownedPlaceholder = null)
        {
            manager = value;
            exclamationIndicator = indicator;
            generatedSprite = ownedPlaceholder;
            if (indicator != null) { indicatorOrigin = indicator.transform.localPosition; indicator.SetActive(false); }
        }

        private void Start()
        {
            SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null && visual.sprite == null)
            {
                generatedSprite = NPCOverthinkingFactory.CreatePlaceholderSprite();
                visual.sprite = generatedSprite;
            }
            if (manager == null) manager = GetComponentInParent<OverthinkingManager>();
            if (manager == null) manager = FindFirstObjectByType<OverthinkingManager>();
            if (exclamationIndicator != null)
            {
                indicatorOrigin = exclamationIndicator.transform.localPosition;
                exclamationIndicator.SetActive(false);
            }
            else Debug.LogWarning("StoneSurpriseTrigger2D needs a world-space exclamation indicator.", this);
            if (manager == null) Debug.LogWarning("Stone trigger needs an OverthinkingManager reference.", this);
            Collider2D sensor = GetComponent<Collider2D>();
            if (sensor == null || !sensor.enabled || !sensor.isTrigger)
                Debug.LogWarning("StoneTrigger needs an enabled Collider2D with Is Trigger checked.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (manager == null || !NPCPhysics2D.TryGetPlayer(other, out Transform player) || !manager.AcceptsPlayer(player)) return;
            playerContacts.RemoveWhere(contact => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy);
            if (!playerContacts.Add(other) || playerContacts.Count != 1) return;
            if (!manager.TriggerStone(player, allowRepeat)) return;
            remaining = exclamationDuration;
            if (exclamationIndicator != null) exclamationIndicator.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other) { playerContacts.Remove(other); }
        private void LateUpdate()
        {
            if (remaining <= 0f) return;
            if (manager == null || manager.IsSuppressed) { HideIndicator(); return; }
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { HideIndicator(); return; }
            if (exclamationIndicator == null) return;
            float progress = 1f - remaining / exclamationDuration;
            exclamationIndicator.transform.localPosition = indicatorOrigin + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * popHeight);
            Camera camera = Camera.main;
            if (camera != null) exclamationIndicator.transform.rotation = camera.transform.rotation;
        }

        private void HideIndicator()
        {
            remaining = 0f;
            if (exclamationIndicator != null)
            {
                exclamationIndicator.SetActive(false);
                exclamationIndicator.transform.localPosition = indicatorOrigin;
            }
        }
        private void OnDisable() { playerContacts.Clear(); HideIndicator(); }
        private void OnDestroy() { if (generatedSprite != null) Destroy(generatedSprite); }
        private void OnValidate()
        {
            exclamationDuration = Mathf.Max(0.1f, exclamationDuration);
            popHeight = Mathf.Max(0f, popHeight);
        }
    }
}
