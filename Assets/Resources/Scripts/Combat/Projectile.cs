using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// Arrow: homes in on its target and deals damage on arrival.
    /// If the target dies on the way, it keeps flying to the last known position and then disappears.
    /// Each arrow has a missChance: a missed arrow flies to a point slightly off the target and deals no damage.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Collision")]
        [Tooltip("Distance that counts as 'reached the target'")]
        [SerializeField] private float hitRadius = 0.15f;
        [Tooltip("Maximum lifetime, so arrows never get stuck in the scene")]
        [SerializeField] private float maxLifetime = 5f;

        [Header("Accuracy")]
        [Tooltip("Chance to miss (0-1), e.g. 0.1 = 10% miss")]
        [Range(0f, 1f)]
        [SerializeField] private float missChance = 0.1f;
        [Tooltip("How far off target a miss lands (world units) — larger = misses land further away")]
        [SerializeField] private float missOffsetRadius = 0.75f;

        private IDamageable target;
        private Vector3 lastKnownPos;
        private Vector3 missOffset;  // offset from the real target position, only used when missing
        private bool hasTarget;   // false = never had a target, or no longer has position data
        private bool isMiss;      // rolled once in Launch — if true it deals no damage even when it arrives
        private float damage;
        private float speed;
        private float life;

        /// <summary>Fires the arrow — called by Tower.Fire().</summary>
        public void Launch(IDamageable target, float damage, float speed)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;

            // Roll the miss once at launch — never re-rolled in flight, so an arrow can't "miss then hit" later
            isMiss = Random.value < missChance;
            if (isMiss)
            {
                Vector2 rand = Random.insideUnitCircle.normalized * missOffsetRadius;
                missOffset = new Vector3(rand.x, rand.y, 0f);
            }
            else
            {
                missOffset = Vector3.zero;
            }

            if (target != null)
            {
                lastKnownPos = target.Transform.position;
                hasTarget = true;
            }
            else
            {
                // Nothing to fly at — destroy immediately so empty arrows don't linger
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (life >= maxLifetime) { Destroy(gameObject); return; }

            if (!hasTarget) return;

            // Target still exists and is alive -> update its last known position
            if (target != null && target.IsAlive)
                lastKnownPos = target.Transform.position;

            Vector3 before = transform.position;
            Vector3 targetPos = lastKnownPos + missOffset; // on a miss, fly to the point offset from the real target
            float step = speed * Time.deltaTime;

            // 2D rotation around the Z axis (same as Tower.AimAtTarget) instead of a 3D LookRotation
            Vector2 dir = targetPos - before;
            if (dir.sqrMagnitude > 0.0001f)
            {
                // -90 because the sprite is assumed to point up (+Y) by default — adjust to match the real sprite
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }

            transform.position = Vector3.MoveTowards(before, targetPos, step);

            if (Vector3.Distance(transform.position, targetPos) <= hitRadius)
                HitTarget();
        }

        private void HitTarget()
        {
            // A miss never deals damage, however close to the target it ends up
            if (!isMiss && target != null && target.IsAlive)
                target.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}