using System.Collections.Generic;
using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(OverthinkingManager))]
    public sealed class NPCSpawner2D : MonoBehaviour
    {
        [SerializeField] private OverthinkingManager manager;
        [SerializeField, Range(0, 30)] private int npcCount = 5;
        [SerializeField, Tooltip("Optional prefab with NPCWander2D on its root; leave empty for the included placeholder.")]
        private GameObject npcPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Tooltip("A trigger collider defining the walkable floor, not a wall collider.")]
        private Collider2D walkableArea;
        [SerializeField] private LayerMask obstacleLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField, Min(0.3f)] private float minimumSpawnSpacing = 1f;
        [SerializeField, Min(0.1f)] private float wanderRadius = 1.6f;
        [SerializeField, Min(0.05f)] private float moveSpeed = 0.65f;
        [SerializeField, Min(0.1f)] private float perceptionRadius = 1.5f;
        [SerializeField] private bool createStoneIfMissing = true;
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly List<Vector2> occupiedSpawns = new List<Vector2>();
        private readonly System.Random random = new System.Random();
        private Transform player;

        public void Configure(OverthinkingManager value, Collider2D area, Transform[] points)
        { manager = value; walkableArea = area; spawnPoints = points; }

        private void Start()
        {
            if (manager == null) manager = GetComponent<OverthinkingManager>();
            Player existingPlayer = FindFirstObjectByType<Player>();
            if (existingPlayer != null) player = existingPlayer.transform;
            if (walkableArea == null) walkableArea = NPCOverthinkingFactory.CreateDefaultArea(transform);
            if (!walkableArea.isTrigger)
            {
                Debug.LogWarning("NPC Walkable Area should be a separate trigger collider. It is excluded from NPC obstacle checks; its settings were not changed.", this);
            }
            Physics2D.SyncTransforms();
            if (createStoneIfMissing && GetComponentInChildren<StoneSurpriseTrigger2D>(true) == null)
            {
                if (TryFindSpot(-1, 0.75f, out Vector2 position))
                    NPCOverthinkingFactory.CreateStone(transform, manager, position);
                else Debug.LogWarning("No safe stone position found. Add a StoneTrigger manually inside the walkable area.", this);
            }

            float bodyRadius = PrefabRadius();
            if (bodyRadius <= 0f) return;
            int spawned = 0;
            for (int i = 0; i < npcCount; i++)
            {
                if (!TryFindSpot(i, bodyRadius + 0.03f, out Vector2 position)) continue;
                NPCWander2D wander;
                if (npcPrefab == null) wander = NPCOverthinkingFactory.CreateNPC(transform, position, i);
                else
                {
                    GameObject instance = Instantiate(npcPrefab, new Vector3(position.x, position.y, transform.position.z), Quaternion.identity, transform);
                    wander = instance.GetComponent<NPCWander2D>();
                }
                wander.Configure(manager, walkableArea, obstacleLayers, wanderRadius, moveSpeed);
                NPCThoughtTrigger2D sensor = wander.GetComponentInChildren<NPCThoughtTrigger2D>(true);
                if (sensor == null) sensor = NPCOverthinkingFactory.CreateDetectionZone(wander.transform);
                sensor.Configure(manager, perceptionRadius);
                occupiedSpawns.Add(position);
                spawned++;
            }
            if (spawned < npcCount)
                Debug.LogWarning($"Spawned {spawned}/{npcCount} NPCs. Blocked or out-of-bounds positions were skipped; adjust Spawn Points, Walkable Area or obstacle layers.", this);
        }

        private float PrefabRadius()
        {
            float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            if (npcPrefab == null) return 0.22f * parentScale;
            CircleCollider2D collider = npcPrefab.GetComponent<CircleCollider2D>();
            NPCWander2D movement = npcPrefab.GetComponent<NPCWander2D>();
            Rigidbody2D prefabBody = npcPrefab.GetComponent<Rigidbody2D>();
            if (movement == null || !movement.enabled || collider == null || !collider.enabled
                || prefabBody == null || !prefabBody.simulated || !npcPrefab.activeSelf)
            {
                Debug.LogWarning("NPC Prefab needs an active root, enabled NPCWander2D / CircleCollider2D and a simulated Rigidbody2D. No NPCs were spawned from this prefab.", this);
                return 0f;
            }
            return (collider.radius + collider.offset.magnitude) * parentScale
                * Mathf.Max(Mathf.Abs(npcPrefab.transform.localScale.x), Mathf.Abs(npcPrefab.transform.localScale.y));
        }

        private bool TryFindSpot(int index, float radius, out Vector2 position)
        {
            if (index >= 0 && spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
            {
                position = spawnPoints[index].position;
                if (ValidSpot(position, radius)) return true;
            }
            Bounds bounds = walkableArea.bounds;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                position = new Vector2(Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble()));
                if (ValidSpot(position, radius)) return true;
            }
            position = default;
            return false;
        }

        private bool ValidSpot(Vector2 position, float radius)
        {
            if (!NPCPhysics2D.CanStand(position, radius, walkableArea, obstacleLayers, overlaps)) return false;
            foreach (Vector2 existing in occupiedSpawns)
                if ((existing - position).sqrMagnitude < minimumSpawnSpacing * minimumSpawnSpacing) return false;
            float playerClearance = Mathf.Max(0.8f, radius + 0.5f);
            return player == null || ((Vector2)player.position - position).sqrMagnitude >= playerClearance * playerClearance;
        }

        private void OnValidate()
        {
            npcCount = Mathf.Clamp(npcCount, 0, 30); minimumSpawnSpacing = Mathf.Max(0.3f, minimumSpawnSpacing);
            wanderRadius = Mathf.Max(0.1f, wanderRadius); moveSpeed = Mathf.Max(0.05f, moveSpeed);
            perceptionRadius = Mathf.Max(0.1f, perceptionRadius);
        }
    }
}
