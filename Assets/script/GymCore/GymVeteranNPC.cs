using FinalProject.Gym.NPC;
using UnityEngine;

namespace FinalProject.Gym.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class GymVeteranNPC : MonoBehaviour
    {
        [SerializeField] private string displayName = "健身老手";
        [SerializeField] private VeteranApproachSettings settings = new VeteranApproachSettings();
        [SerializeField, Min(0.05f)] private float approachSpeed = 0.8f;
        [SerializeField] private Collider2D walkableArea;
        [SerializeField] private LayerMask obstacleLayers = Physics2D.DefaultRaycastLayers;
        private GymGameBridge bridge;
        private VeteranApproachModel model;
        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private NPCWander2D wander;
        private SpriteRenderer visual;
        private Vector2 home;
        private bool originalWanderEnabled;
        private bool ownsMovement;
        private bool previousAvoidActors;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[48];
        public string DisplayName => displayName;
        public VeteranState State => model != null ? model.State : VeteranState.Wandering;
        public float PlayerDistance => bridge != null && bridge.Player != null
            ? Vector2.Distance(transform.position, bridge.Player.transform.position) : float.PositiveInfinity;
        public bool IsAnxietySource => isActiveAndEnabled && (State == VeteranState.Approaching || State == VeteranState.Waiting)
            && PlayerDistance <= settings.detectionRadius;
        public bool CanTalk => isActiveAndEnabled && State == VeteranState.Waiting && PlayerDistance <= settings.stopDistance + 1.2f;

        public void ConfigureApproach(VeteranApproachSettings configuration, float speed)
        {
            if (model != null) return;
            settings = (configuration ?? new VeteranApproachSettings()).NormalizedCopy();
            approachSpeed = Mathf.Max(0.05f, speed);
        }

        public void Initialize(GymGameBridge context)
        {
            if (bridge == context && model != null) return;
            bridge = context;
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            wander = GetComponent<NPCWander2D>();
            visual = GetComponentInChildren<SpriteRenderer>();
            settings = (settings ?? new VeteranApproachSettings()).NormalizedCopy();
            model = new VeteranApproachModel(settings);
            home = body.position;
            originalWanderEnabled = wander != null && wander.enabled;
            if (wander != null)
            {
                previousAvoidActors = wander.AvoidActorsAndEquipment;
                wander.AvoidActorsAndEquipment = true;
                if (walkableArea == null) walkableArea = wander.WalkableArea;
                obstacleLayers = wander.ObstacleLayers;
            }
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            context.RegisterVeteran(this);
        }

        private void Start()
        {
            if (bridge == null && GymGameBridge.Instance != null) Initialize(GymGameBridge.Instance);
        }

        private void FixedUpdate()
        {
            if (bridge == null || !bridge.isActiveAndEnabled || bridge.Player == null || model == null) return;
            if (bridge.ShouldFreezeVeterans)
            {
                TakeMovement();
                body.linearVelocity = Vector2.zero;
                return;
            }
            model.Tick(Time.fixedDeltaTime, PlayerDistance, Vector2.Distance(body.position, home), false);
            if (State == VeteranState.Wandering)
            {
                ReleaseMovement();
                return;
            }
            TakeMovement();
            body.linearVelocity = Vector2.zero;
            Vector2 player = bridge.Player.transform.position;
            Face(player - body.position);
            if (State == VeteranState.Approaching)
            {
                Vector2 away = (body.position - player).normalized;
                if (away.sqrMagnitude < 0.01f) away = Vector2.left;
                MoveTowards(player + away * settings.stopDistance);
            }
            else if (State == VeteranState.Leaving) MoveTowards(home);
        }

        public bool BeginTalk() => CanTalk && model.BeginTalk();
        public void Dismiss() { if (model != null) model.Dismiss(); }
        public void ResetForNewRound() { if (model != null) { model.Reset(); if (body != null) home = body.position; ReleaseMovement(); } }

        public void Unbind()
        {
            if (body != null && ownsMovement) body.linearVelocity = Vector2.zero;
            ReleaseMovement();
            if (wander != null) wander.AvoidActorsAndEquipment = previousAvoidActors;
            if (bridge != null) bridge.UnregisterVeteran(this);
            bridge = null;
            model = null;
        }

        private void TakeMovement()
        {
            if (ownsMovement) return;
            ownsMovement = true;
            if (wander != null) wander.enabled = false;
        }
        private void ReleaseMovement()
        {
            if (!ownsMovement) return;
            ownsMovement = false;
            if (body != null) home = body.position;
            if (wander != null)
            {
                // A timed-out return must not strand a veteran outside its old wander circle.
                wander.RestartWanderingAtCurrentPosition();
                wander.enabled = originalWanderEnabled;
            }
        }

        private void MoveTowards(Vector2 target)
        {
            Vector2 difference = target - body.position;
            if (difference.sqrMagnitude < 0.0025f) return;
            Vector2 direction = difference.normalized;
            float distance = Mathf.Min(approachSpeed * Time.fixedDeltaTime, difference.magnitude);
            float radius = bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            Vector2 offset = (Vector2)bodyCollider.bounds.center - body.position;
            ContactFilter2D filter = NPCPhysics2D.ObstacleFilter(obstacleLayers);
            filter.useTriggers = true;
            int count = Physics2D.CircleCast(body.position + offset, radius, direction, filter, hits, distance + 0.03f);
            if (count == hits.Length) return;
            for (int i = 0; i < count; i++)
                if (Blocks(hits[i].collider)) distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - 0.03f));
            Vector2 next = body.position + direction * distance;
            if (distance <= 0.001f || !NPCPhysics2D.InsideArea(walkableArea, next + offset, radius)) return;
            Face(direction);
            body.MovePosition(next);
        }

        private bool Blocks(Collider2D collider)
        {
            if (collider == null || collider == walkableArea || collider.attachedRigidbody == body) return false;
            if (!collider.isTrigger || NPCPhysics2D.TryGetPlayer(collider, out _)) return true;
            // Sensors are ignored, while actual NPC bodies and equipment interaction volumes are avoided.
            return collider.GetComponent<NPCWander2D>() != null || collider.GetComponent<GymVeteranNPC>() != null
                || collider.GetComponentInParent<GymEquipmentInteractable>() != null;
        }
        private void Face(Vector2 direction) { if (visual != null && Mathf.Abs(direction.x) > 0.01f) visual.flipX = direction.x < 0f; }
        private void OnDisable() { Unbind(); }
        private void OnValidate()
        {
            settings = (settings ?? new VeteranApproachSettings()).NormalizedCopy();
            approachSpeed = Mathf.Max(0.05f, approachSpeed);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, settings != null ? settings.detectionRadius : 4f);
        }
    }
}
