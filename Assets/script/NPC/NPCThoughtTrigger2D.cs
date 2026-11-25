using System.Collections.Generic;
using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class NPCThoughtTrigger2D : MonoBehaviour
    {
        [SerializeField] private OverthinkingManager manager;
        [SerializeField, Min(0.1f)] private float perceptionRadius = 1.5f;
        private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();
        public bool IsPlayerNearby
        {
            get
            {
                playerContacts.RemoveWhere(IsInvalidContact);
                return playerContacts.Count > 0;
            }
        }

        public void Configure(OverthinkingManager value, float radius)
        {
            manager = value;
            perceptionRadius = Mathf.Max(0.1f, radius);
            ConfigureCollider();
        }

        private void Awake() { ConfigureCollider(); }
        private void Start()
        {
            if (manager == null) manager = GetComponentInParent<OverthinkingManager>();
            if (manager == null) manager = FindFirstObjectByType<OverthinkingManager>();
            if (manager == null) Debug.LogWarning("NPC thought trigger needs an OverthinkingManager reference.", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (manager == null || !NPCPhysics2D.TryGetPlayer(other, out Transform player) || !manager.AcceptsPlayer(player)) return;
            if (playerContacts.Add(other) && playerContacts.Count == 1) manager.TryShowThought(this, player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (playerContacts.Remove(other) && playerContacts.Count == 0 && manager != null) manager.PlayerLeft(this);
        }

        private void OnDisable()
        {
            playerContacts.Clear();
            if (manager != null) manager.PlayerLeft(this);
        }

        private void ConfigureCollider()
        {
            CircleCollider2D zone = GetComponent<CircleCollider2D>();
            zone.isTrigger = true;
            zone.radius = perceptionRadius;
        }

        private static bool IsInvalidContact(Collider2D contact) => contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy;
        private void OnValidate() { perceptionRadius = Mathf.Max(0.1f, perceptionRadius); }
    }
}
