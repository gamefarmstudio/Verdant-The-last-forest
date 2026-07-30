using System.Collections.Generic;
using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// สมองของป้อม: ตรวจจับศัตรูในระยะ → เลือกเป้า → หมุนหัวเล็ง → ยิงตามอัตราไฟ
    ///
    /// สถานะ (damage / range / atkSpeed) ดึงจาก towers.csv ผ่าน TowerDatabase
    /// โดยอ้างอิงด้วย towerId — ถ้าโหลด CSV ไม่ได้จะตกไปใช้ค่าใน fallbackData
    /// ค่าที่ไม่มีใน CSV (ลูกกระสุน, โหมดเล็ง) ตั้งบน component นี้โดยตรง
    /// </summary>
    [DisallowMultipleComponent]
    public class Tower : MonoBehaviour
    {
        [Header("อ้างอิง CSV")]
        [Tooltip("ID ของป้อมใน towers.csv เช่น T1A1 — ดึง Damage/Range/Atk Speed จากแถวนี้")]
        [SerializeField] private string towerId = "T1A1";

        [Tooltip("ค่าสำรองเมื่อโหลด CSV ไม่ได้ หรือหา ID ไม่เจอ (ปล่อยว่างได้ถ้ามั่นใจว่ามี CSV)")]
        [SerializeField] private TowerData fallbackData;

        [Header("ลูกกระสุน (ไม่มีใน CSV — ตั้งตรงนี้)")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;

        [Header("การเล็ง")]
        [SerializeField] private TargetingMode targeting = TargetingMode.Closest;

        [Header("การอ้างอิงในฉาก")]
        [Tooltip("จุดปล่อยลูกธนู (ปล่อยว่างได้ จะใช้ตำแหน่งป้อมแทน)")]
        [SerializeField] private Transform muzzle;
        [Tooltip("ส่วนหัวป้อมที่จะหมุนตามเป้าหมาย (ปล่อยว่างได้ถ้าไม่ต้องการหมุน)")]
        [SerializeField] private Transform rotatePart;
        [Tooltip("เลเยอร์ของศัตรู — ตั้งเป็น Enemy (ถ้าเว้นว่าง จะเดาเป็น Enemy ให้อัตโนมัติ)")]
        [SerializeField] private LayerMask enemyLayer;

        // ── สถานะ runtime (มาจาก CSV) ──
        private float damage;
        private float range;
        private float fireCooldown;

        private float cooldownTimer;
        private IDamageable currentTarget;

        // ใช้ List + ContactFilter2D แบบ non-alloc (แนะนำสำหรับ Unity 6)
        private readonly List<Collider2D> hits = new List<Collider2D>();
        private ContactFilter2D filter;

        private bool _statsReady;    // มีสถานะพร้อมทำงาน (CSV หรือ fallback)
        private bool _csvResolved;   // ลอง CSV จน DB โหลดเสร็จแล้ว (เลิก retry)

        // ตำแหน่ง rotatePart เทียบกับ muzzle ตอนเริ่มเกม (world space offset + rotation เริ่มต้น)
        // ใช้คำนวณให้ rotatePart "โคจร" รอบ muzzle ซึ่งเป็นจุดหมุนคงที่ เวลาป้อมหมุนเล็ง
        private Vector3 rotatePartOffsetFromMuzzle;
        private Quaternion rotatePartInitialRotation;
        private bool hasRotateOrbit;

        private void Awake()
        {
            if (rotatePart != null && muzzle != null)
            {
                rotatePartOffsetFromMuzzle = rotatePart.position - muzzle.position;
                rotatePartInitialRotation = rotatePart.rotation;
                hasRotateOrbit = true;
            }

            // เว้น enemyLayer ไว้ → เดาเป็น Enemy ให้ (กันลืมตั้งใน Inspector แล้วยิงไม่โดนใคร)
            if (enemyLayer.value == 0)
            {
                enemyLayer = LayerMask.GetMask("Enemy");
                Debug.LogWarning($"[Tower] '{name}' ไม่ได้ตั้ง enemyLayer — ใช้ layer 'Enemy' อัตโนมัติ", this);

                // ถ้าไม่มีเลเยอร์ชื่อ "Enemy" จริง ๆ ใน Project Settings, GetMask จะคืนค่า 0 เงียบ ๆ
                // และ OverlapCircle จะไม่เจอศัตรูเลย — เตือนไว้ตรงนี้ให้ชัดเจน
                if (enemyLayer.value == 0)
                {
                    Debug.LogError($"[Tower] '{name}' หา layer 'Enemy' ไม่เจอเลย — " +
                                   $"ตรวจสอบชื่อ layer ใน Project Settings หรือกำหนด enemyLayer ใน Inspector เอง", this);
                }
            }

            filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = true; // ตรวจจับ trigger collider ของศัตรูด้วย

            ApplyFallback();           // ให้ทำงานได้ก่อน แม้ CSV ยังไม่พร้อม
        }

        private void Start() => TryLoadStatsFromCsv();

        // ── โหลดสถานะจาก CSV ──

        private void ApplyFallback()
        {
            if (fallbackData == null) return;

            damage       = fallbackData.damage;
            range        = fallbackData.range;
            fireCooldown = fallbackData.FireCooldown;
            if (projectilePrefab == null) projectilePrefab = fallbackData.projectilePrefab;
            if (projectileSpeed <= 0f)    projectileSpeed  = fallbackData.projectileSpeed;

            _statsReady = true;
        }

        /// <summary>
        /// พยายามดึงสถานะจาก TowerDatabase — เรียกซ้ำใน Update ได้จนกว่า DB จะโหลดเสร็จ
        /// (ป้อมที่วางตอน runtime ผ่าน TowerSlot จะโหลดทันเสมอ ส่วนป้อมที่วางไว้ในฉาก
        ///  ก่อนเล่นอาจ Start ก่อน TowerDatabase.Start — จึงต้อง retry)
        /// </summary>
        private void TryLoadStatsFromCsv()
        {
            if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded)
                return;   // DB ยังไม่พร้อม — จะลองใหม่ใน Update

            _csvResolved = true;   // DB โหลดแล้ว ไม่ว่าจะเจอแถวหรือไม่ ก็เลิก retry

            var rec = TowerDatabase.Instance.GetByID(towerId);
            if (rec == null)
            {
                Debug.LogError($"[Tower] ไม่พบ tower ID '{towerId}' ใน towers.csv — " +
                               $"'{name}' ใช้ค่า fallback แทน", this);
                return;
            }
            else
            {
                Debug.Log($"[Tower] '{name}' โหลดสถานะจาก CSV ID='{towerId}' สำเร็จ", this);
            }

            damage       = rec.damage;
            range        = rec.range;
            if (rec.atkSpeed > 0f) fireCooldown = rec.atkSpeed;   // atkSpeed = วินาที/นัด อยู่แล้ว
            _statsReady  = true;
        }

        private void Update()
        {
            if (!_csvResolved) TryLoadStatsFromCsv();   // retry จนกว่า DB จะพร้อม
            if (!_statsReady || range <= 0f) return;

            cooldownTimer -= Time.deltaTime;

            AcquireTarget();
            AimAtTarget();

            if (currentTarget != null && cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer = fireCooldown;
            }
        }

        // ---------- เลือกเป้าหมาย ----------
        private void AcquireTarget()
        {
            // ถ้าเป้าเดิมยังอยู่ในระยะและยังไม่ตาย ใช้ต่อ (กันการสลับเป้าถี่เกินไป)
            if (IsValidTarget(currentTarget)) return;
            Debug.Log($"[Tower] '{name}' เป้าหมายเดิมหมดสภาพ → หาเป้าใหม่", this);
            currentTarget = FindBestTarget();
        }

        private bool IsValidTarget(IDamageable t)
        {
            if (t == null || !t.IsAlive) return false;
            float sqr = ((Vector2)(t.Transform.position - transform.position)).sqrMagnitude;
            return sqr <= range * range;
        }

        private IDamageable FindBestTarget()
        {
            int count = Physics2D.OverlapCircle(transform.position, range, filter, hits);

            // Debug ชั่วคราว: ช่วยไล่ปัญหา "No Enemies Detected"
            // ถ้า count เป็น 0 เสมอ → เช็ค enemyLayer / Physics2D collision matrix / collider ของศัตรู
            Debug.Log($"[Tower] '{name}' OverlapCircle: range={range}, enemyLayerMask={enemyLayer.value}, hitCount={count}", this);

            IDamageable best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                var dmg = hits[i].GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;

                float score = ScoreTarget(dmg);
                if (score > bestScore)
                {
                    best = dmg;
                    bestScore = score;
                }
            }

            Debug.Log($"[Tower] '{name}' พบ {count} ศัตรูในระยะ → เลือกเป้า '{best?.Transform.name}'", this);
            return best;
        }

        // แปลงโหมดเล็งเป็น "คะแนน" — ยิ่งมากยิ่งถูกเลือก
        private float ScoreTarget(IDamageable t)
        {
            switch (targeting)
            {
                case TargetingMode.First:     return t.PathProgress;
                case TargetingMode.Last:      return -t.PathProgress;
                case TargetingMode.Strongest: return t.CurrentHealth;
                default: // Closest — ใกล้ = คะแนนสูง
                    return -Vector2.Distance(transform.position, t.Transform.position);
            }
        }

        // ---------- หมุนหัวป้อม ----------
        private void AimAtTarget()
        {
            if (currentTarget == null || rotatePart == null) return;

            Vector2 dir = (Vector2)(currentTarget.Transform.position - rotatePart.position);
            if (dir.sqrMagnitude < 0.0001f) return;

            // -90 เพราะสมมติสไปรต์หันหัวขึ้น (แกน +Y) เป็นค่าเริ่มต้น
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion newRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            rotatePart.rotation = newRotation;

            // ให้ rotatePart โคจรรอบ muzzle (จุดหมุนคงที่) ตามมุมที่หมุนไปจากจุดเริ่มต้น
            // muzzle จะอยู่นิ่งกับที่เสมอ ส่วน rotatePart จะสวิงรอบมันแทน
            if (hasRotateOrbit)
            {
                Quaternion deltaRotation = newRotation * Quaternion.Inverse(rotatePartInitialRotation);
                rotatePart.position = muzzle.position + deltaRotation * rotatePartOffsetFromMuzzle;
            }
        }

        // ---------- ยิง ----------
        private void Fire()
        {
            Debug.Log($"[Tower] '{name}' ยิง {currentTarget.Transform.name} (HP={currentTarget.CurrentHealth})");
            if (projectilePrefab == null) return;

            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
            Projectile arrow = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            arrow.Launch(currentTarget, damage, projectileSpeed);
        }

        // วาดวงระยะยิงใน Scene view ช่วยจัดวางป้อม/ออกแบบด่าน
        private void OnDrawGizmosSelected()
        {
            // ใน edit mode ยังไม่ได้โหลด CSV — โชว์ระยะจาก fallback ให้พอเห็นภาพ
            float r = range > 0f ? range : (fallbackData != null ? fallbackData.range : 0f);
            if (r <= 0f) return;
            Gizmos.color = new Color(0.15f, 1f, 0.55f, 0.35f); // teal-green ตามธีม Verdant
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}