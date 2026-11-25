using UnityEngine;

namespace FinalProject.Gym.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NPCWander2D : MonoBehaviour
    {
        [SerializeField] private OverthinkingManager manager;
        [SerializeField] private Collider2D walkableArea;
        [SerializeField] private LayerMask obstacleLayers = Physics2D.DefaultRaycastLayers;
        [SerializeField] private Transform areaCenter;
        [SerializeField, Min(0.1f)] private float wanderRadius = 1.6f;
        [SerializeField, Min(0.05f)] private float moveSpeed = 0.65f;
        [SerializeField, Min(0f)] private float minimumPause = 0.7f;
        [SerializeField, Min(0f)] private float maximumPause = 2.2f;
        [SerializeField] private Vector2 initialFacing = Vector2.right;
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private readonly System.Random random = new System.Random(System.Guid.NewGuid().GetHashCode());
        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private Vector2 origin;
        private Vector2 target;
        private float waitRemaining;
        private bool hasTarget;
        private bool hasSpeedParameter;
        private Sprite generatedSprite;
        private const float Skin = 0.03f;
        private Vector2 Center => areaCenter != null ? (Vector2)areaCenter.position : origin;
        private float Radius => bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        private Vector2 BodyOffset => (Vector2)bodyCollider.bounds.center - body.position;

        public void Configure(OverthinkingManager value, Collider2D area, LayerMask obstacles,
            float radius, float speed, SpriteRenderer visual = null, Sprite ownedPlaceholder = null)
        {
            manager = value; walkableArea = area; obstacleLayers = obstacles;
            wanderRadius = Mathf.Max(0.1f, radius); moveSpeed = Mathf.Max(0.05f, speed);
            if (visual != null) characterRenderer = visual;
            if (ownedPlaceholder != null) generatedSprite = ownedPlaceholder;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            // Movement casts stop at walls, while the NPC body never pushes or traps the player.
            bodyCollider.isTrigger = true;
            origin = body.position;
            if (characterRenderer == null) characterRenderer = GetComponentInChildren<SpriteRenderer>();
            if (characterRenderer != null && characterRenderer.sprite == null)
            {
                generatedSprite = NPCOverthinkingFactory.CreatePlaceholderSprite();
                characterRenderer.sprite = generatedSprite;
            }
            if (characterRenderer != null && Mathf.Abs(initialFacing.x) > 0.01f) characterRenderer.flipX = initialFacing.x < 0f;
            if (animator != null)
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                    if (parameter.name == speedParameter && parameter.type == AnimatorControllerParameterType.Float) hasSpeedParameter = true;
            Pause();
        }

        private void FixedUpdate()
        {
            body.linearVelocity = Vector2.zero;
            if (manager != null && manager.IsSuppressed) { Animate(0f); return; }
            if (waitRemaining > 0f) { waitRemaining -= Time.fixedDeltaTime; Animate(0f); return; }
            if (!hasTarget && !ChooseTarget()) { Pause(); return; }

            Vector2 difference = target - body.position;
            if (difference.sqrMagnitude < 0.0025f) { Pause(); return; }
            Vector2 direction = difference.normalized;
            float distance = Mathf.Min(moveSpeed * Time.fixedDeltaTime, difference.magnitude);
            int count = Physics2D.CircleCast(body.position + BodyOffset, Radius, direction,
                NPCPhysics2D.ObstacleFilter(obstacleLayers), hits, distance + Skin);
            if (count == hits.Length) { Pause(); return; }
            for (int i = 0; i < count; i++)
                if (NPCPhysics2D.BlocksMovement(hits[i].collider, walkableArea, body))
                    distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - Skin));

            Vector2 next = body.position + direction * distance;
            if (distance < 0.001f || !WithinWanderArea(next + BodyOffset)
                || !NPCPhysics2D.CanStand(next + BodyOffset, Radius, walkableArea, obstacleLayers, overlaps, body))
            { Pause(); return; }
            body.MovePosition(next);
            if (characterRenderer != null && Mathf.Abs(direction.x) > 0.01f) characterRenderer.flipX = direction.x < 0f;
            Animate(moveSpeed);
        }

        private bool ChooseTarget()
        {
            float travelRadius = Mathf.Max(0f, wanderRadius - Radius);
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = Mathf.Sqrt((float)random.NextDouble()) * travelRadius;
                Vector2 candidate = Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if ((candidate - (body.position + BodyOffset)).sqrMagnitude < 0.04f) continue;
                if (!NPCPhysics2D.CanStand(candidate, Radius + Skin, walkableArea, obstacleLayers, overlaps, body)) continue;
                target = candidate - BodyOffset;
                hasTarget = true;
                return true;
            }
            return false;
        }

        private bool WithinWanderArea(Vector2 point)
        {
            float allowedRadius = Mathf.Max(0f, wanderRadius - Radius);
            return (point - Center).sqrMagnitude <= allowedRadius * allowedRadius;
        }

        private void Pause()
        {
            hasTarget = false;
            waitRemaining = Mathf.Max(0.05f, minimumPause + (maximumPause - minimumPause) * (float)random.NextDouble());
            Animate(0f);
        }
        private void Animate(float speed) { if (animator != null && hasSpeedParameter) animator.SetFloat(speedParameter, speed); }
        private void OnDisable() { if (body != null) body.linearVelocity = Vector2.zero; Animate(0f); }
        private void OnDestroy() { if (generatedSprite != null) Destroy(generatedSprite); }
        private void OnValidate()
        {
            wanderRadius = Mathf.Max(0.1f, wanderRadius); moveSpeed = Mathf.Max(0.05f, moveSpeed);
            minimumPause = Mathf.Max(0f, minimumPause); maximumPause = Mathf.Max(minimumPause, maximumPause);
        }
        private void OnDrawGizmosSelected()
        {
            Vector3 center = areaCenter != null ? areaCenter.position : Application.isPlaying ? (Vector3)origin : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, wanderRadius);
        }
    }
}
