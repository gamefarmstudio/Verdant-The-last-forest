using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// ลูกธนู: บินโฮมมิ่งเข้าหาเป้าหมาย ถึงตัวแล้วสร้างดาเมจ
    /// ถ้าเป้าตายกลางทาง จะบินต่อไปยังตำแหน่งสุดท้ายที่รู้แล้วหายไป
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("การชน")]
        [Tooltip("ระยะที่ถือว่า 'ถึงเป้า'")]
        [SerializeField] private float hitRadius = 0.15f;
        [Tooltip("เอฟเฟกต์ตอนโดน (ต่อ VFX Graph ได้)")]
        [SerializeField] private GameObject hitVfxPrefab;
        [Tooltip("อายุสูงสุดกันลูกธนูค้างในฉาก")]
        [SerializeField] private float maxLifetime = 5f;

        private IDamageable target;
        private Vector3 lastKnownPos;
        private float damage;
        private float speed;
        private float life;

        /// <summary>ยิงลูกธนู — เรียกจาก Tower.Fire()</summary>
        public void Launch(IDamageable target, float damage, float speed)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;
            if (target != null) lastKnownPos = target.Transform.position;
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (life >= maxLifetime) { Destroy(gameObject); return; }

            // โฮมมิ่ง: อัปเดตตำแหน่งเป้าล่าสุดถ้ายังมีชีวิต
            if (target != null && target.IsAlive)
                lastKnownPos = target.Transform.position;

            Vector3 toTarget = lastKnownPos - transform.position;
            float step = speed * Time.deltaTime;

            // หันหัวลูกธนูไปทางที่บิน
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // ถึงเป้าแล้ว → สร้างดาเมจ
            if (toTarget.magnitude <= step + hitRadius)
            {
                transform.position = lastKnownPos;
                HitTarget();
                return;
            }

            transform.position += toTarget.normalized * step;
        }

        private void HitTarget()
        {
            if (target != null && target.IsAlive)
                target.TakeDamage(damage);

            if (hitVfxPrefab != null)
                Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
