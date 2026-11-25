using UnityEngine;

namespace FinalProject.Gym.NPC
{
    public static class NPCPhysics2D
    {
        public static bool TryGetPlayer(Collider2D collider, out Transform player)
        {
            player = null;
            if (collider == null) return false;
            Player component = collider.GetComponentInParent<Player>();
            if (component != null) { player = component.transform; return true; }
            for (Transform current = collider.transform; current != null; current = current.parent)
            {
                if (current.CompareTag("Player")) { player = current; return true; }
            }
            return false;
        }

        public static ContactFilter2D ObstacleFilter(LayerMask layers)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(layers);
            filter.useTriggers = false;
            return filter;
        }

        public static bool InsideArea(Collider2D area, Vector2 point, float radius)
        {
            if (area == null) return true;
            if (!area.enabled || !area.gameObject.activeInHierarchy || !area.OverlapPoint(point)) return false;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                if (!area.OverlapPoint(point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius)) return false;
            }
            return true;
        }

        public static bool CanStand(Vector2 position, float radius, Collider2D area, LayerMask obstacles,
            Collider2D[] results, Rigidbody2D self = null)
        {
            if (!InsideArea(area, position, radius)) return false;
            int count = Physics2D.OverlapCircle(position, radius, ObstacleFilter(obstacles), results);
            if (count == results.Length) return false; // A full buffer cannot prove this spot is clear.
            for (int i = 0; i < count; i++)
                if (BlocksMovement(results[i], area, self)) return false;
            return true;
        }

        public static bool BlocksMovement(Collider2D collider, Collider2D area, Rigidbody2D self)
        {
            if (collider == null || collider == area || collider.isTrigger) return false;
            if (self != null && collider.attachedRigidbody == self) return false;
            return !TryGetPlayer(collider, out _);
        }
    }
}
