using System.Collections.Generic;
using UnityEngine;

namespace Verdant.Combat
{
    // ป้อม Wildfire — จุดไฟเผา (burn DoT) ทุกตัวในระยะทุก BurnSpeedPer10Seconds วินาที
    // ดาเมจรวมตลอด BurnDuration = MaxHP ของเป้า x BurnHpPercent แบ่งจ่ายเป็นก้อนตามจำนวน tick
    public class Wildfire : MonoBehaviour, IUpgradableTower
    {
        [Header("Wildfire Settings")]
        [Range(0.1f, 1.0f)]
        [Tooltip("ดาเมจรวมตลอดการเผา = MaxHP ของเป้า x ค่านี้")]
        [SerializeField] private float BurnHpPercent = 1.0f;
        [Tooltip("เว้นกี่วินาทีต่อ 1 tick ดาเมจ")]
        [SerializeField] private float BurnSpeedPer10Seconds = 1.0f;
        [Tooltip("เลเยอร์ศัตรู (เว้นว่าง = เดา Enemy)")]
        [SerializeField] private LayerMask EnemyLayer;
        [SerializeField] private string towerId;
        [SerializeField] private GameObject NextUpgradePrefab;
        [Range(2.5f, 4.5f)]
        [Tooltip("ค่าเริ่มต้น — จะถูกทับด้วย Range จาก towers.csv ถ้าโหลดได้")]
        [SerializeField] private float AttackRange = 2.5f;

        [Header("VFX Settings")]
        [SerializeField] private GameObject VFXPrefab;
        [SerializeField] private GameObject AttackVFX;

        public string TowerId => towerId;
        public GameObject NextLevelPrefab => NextUpgradePrefab;

        // สถานะการเผาต่อเป้าหมาย — รองรับเผาหลายตัวพร้อมกัน (AoE) แยกเวลากันคนละตัว
        private class BurnState
        {
            public float elapsed;      // เวลาที่ติดไฟมาแล้ว (นับถึง BurnDuration แล้วเลิกเผา)
            public float tickTimer;    // นับถอยหลังถึง tick ถัดไป
            public GameObject vfx;
        }

        private readonly Dictionary<EnemyBase, BurnState> _burning = new Dictionary<EnemyBase, BurnState>();
        private readonly List<EnemyBase> _toRemove = new List<EnemyBase>();

        private ContactFilter2D _filter;
        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private bool _statsReady, _csvResolved;
        private GameObject attackVFX;

        void Awake()
        {
            if (EnemyLayer.value == 0) EnemyLayer = LayerMask.GetMask("Enemy");
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(EnemyLayer);
            _filter.useTriggers = true;
            attackVFX = Instantiate(AttackVFX,transform.position, AttackVFX.transform.rotation, transform); // VFX เผาเป้าหมาย
        }

        void Start() => TryLoadStats();

        // ดึง Range จาก towers.csv ตาม towerId (เหมือน CrystalSpringTower) — เว้นค่า Burn%/Duration/Speed
        // ไว้เป็นค่าปรับต่อเลเวลบน prefab เอง เพราะ towers.csv ไม่มีคอลัมน์พวกนี้
        void TryLoadStats()
        {
            if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded) return;
            _csvResolved = true;

            var rec = TowerDatabase.Instance.GetByID(towerId);
            if (rec == null)
            {
                Debug.LogError($"[Wildfire] ไม่พบ ID '{towerId}' ใน towers.csv — ใช้ค่า Inspector แทน", this);
                _statsReady = true;
                return;
            }
            if (rec.range > 0f) AttackRange = rec.range;
            _statsReady = true;
        }

        void Update()
        {
            if (!_csvResolved) TryLoadStats();

            FindAndIgniteEnemies();
            TickBurningEnemies();
        }

        void FindAndIgniteEnemies()
        {
            int count = Physics2D.OverlapCircle(transform.position, AttackRange, _filter, _hits);
            for (int i = 0; i < count; i++)
            {
                var enemy = _hits[i].GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                if (_burning.ContainsKey(enemy)) continue;   // ติดไฟอยู่แล้ว ไม่ต้องจุดซ้ำ/รีเซ็ตเวลา
                
                attackVFX.SetActive(true); // เปิด VFX เผาเป้าหมายถ้าเจอเป้าในระยะ
                IgniteEnemy(enemy);
            }
            if (_hits.Count == 0) attackVFX.SetActive(false); // ลบ VFX เผาเป้าหมายถ้าไม่มีเป้าในระยะ
            // ล้าง hits list เพื่อเตรียมใช้รอบถัดไป
            _hits.Clear();
        }

        void IgniteEnemy(EnemyBase enemy)
        {
            GameObject vfx = null;
            if (VFXPrefab != null)
            {
                vfx = Instantiate(VFXPrefab, enemy.transform.position, VFXPrefab.transform.rotation);
                vfx.transform.SetParent(enemy.transform);
                Destroy(vfx, BurnSpeedPer10Seconds); // ลบ VFX หลังหมดเวลาเผา
            }

            _burning[enemy] = new BurnState
            {
                elapsed = 0f,
                tickTimer = BurnSpeedPer10Seconds,
                vfx = vfx,
            };
        }

        void TickBurningEnemies()
        {
            _toRemove.Clear();

            foreach (var kv in _burning)
            {
                var enemy = kv.Key;
                var state = kv.Value;

                if (enemy == null || enemy.IsDead)
                {
                    _toRemove.Add(enemy);
                    continue;
                }

                state.elapsed += Time.deltaTime;
                state.tickTimer -= Time.deltaTime;

                if (state.tickTimer <= 0f)
                {
                    state.tickTimer += BurnSpeedPer10Seconds;

                    int ticksTotal = Mathf.Max(1, Mathf.RoundToInt(BurnSpeedPer10Seconds));
                    int tickDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHP * BurnHpPercent / ticksTotal));
                    enemy.TakeDamage(tickDamage);
                }

                if (state.elapsed >= BurnSpeedPer10Seconds || enemy.IsDead)
                    _toRemove.Add(enemy);
            }

            foreach (var enemy in _toRemove)
            {
                if (_burning.TryGetValue(enemy, out var state) && state.vfx != null)
                    Destroy(state.vfx);
                _burning.Remove(enemy);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
