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
        [Tooltip("อายุสูงสุดกันลูกธนูค้างในฉาก")]
        [SerializeField] private float maxLifetime = 5f;

        [Header("ความแม่นยำ")]
        [Tooltip("โอกาสพลาดเป้า (0-1) เช่น 0.1 = พลาด 10%")]
        [Range(0f, 1f)]
        [SerializeField] private float missChance = 0.1f;
        [Tooltip("ระยะเบี่ยงเบนเมื่อพลาด (หน่วยโลก) — ยิ่งมากยิ่งพลาดไกลจากเป้า")]
        [SerializeField] private float missOffsetRadius = 0.75f;

        private IDamageable target;
        private Vector3 lastKnownPos;
        private Vector3 missOffset;  // เบี่ยงจากตำแหน่งเป้าจริง ใช้ตอนพลาดเท่านั้น
        private bool hasTarget;   // false = ยังไม่เคยได้เป้า หรือหมดข้อมูลตำแหน่งแล้ว
        private bool isMiss;      // สุ่มไว้ตอน Launch — ถ้า true จะไม่โดนดาเมจแม้บินไปถึง
        private float damage;
        private float speed;
        private float life;

        /// <summary>ยิงลูกธนู — เรียกจาก Tower.Fire()</summary>
        public void Launch(IDamageable target, float damage, float speed)
        {
            this.target = target;
            this.damage = damage;
            this.speed = speed;

            // สุ่มพลาดตอนยิงครั้งเดียว — ไม่สุ่มซ้ำระหว่างบิน กันลูกธนู "พลาดแล้วโดน" ตอนหลัง
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
                // ไม่มีเป้าหมายให้บินหา — ทำลายตัวเองทันทีกันลูกธนูค้างเปล่า ๆ
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (life >= maxLifetime) { Destroy(gameObject); return; }

            if (!hasTarget) return;

            // เป้ายังอยู่และยังไม่ตาย → อัปเดตตำแหน่งล่าสุดที่รู้
            if (target != null && target.IsAlive)
                lastKnownPos = target.Transform.position;

            Vector3 before = transform.position;
            Vector3 targetPos = lastKnownPos + missOffset; // พลาด → บินไปยังจุดที่เบี่ยงจากเป้าจริง
            float step = speed * Time.deltaTime;

            // หมุนแบบ 2D รอบแกน Z (เหมือนกับ Tower.AimAtTarget) แทน LookRotation แบบ 3D
            Vector2 dir = targetPos - before;
            if (dir.sqrMagnitude > 0.0001f)
            {
                // -90 เพราะสมมติสไปรต์หันหัวขึ้น (แกน +Y) เป็นค่าเริ่มต้น — ปรับให้ตรงกับสไปรต์จริง
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }

            transform.position = Vector3.MoveTowards(before, targetPos, step);

            if (Vector3.Distance(transform.position, targetPos) <= hitRadius)
                HitTarget();
        }

        private void HitTarget()
        {
            // พลาด → ไม่สร้างดาเมจไม่ว่าจะบินไปจบใกล้เป้าแค่ไหนก็ตาม
            if (!isMiss && target != null && target.IsAlive)
                target.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}