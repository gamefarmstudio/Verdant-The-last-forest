using System.Collections.Generic;
using UnityEngine;

// [เพิ่ม] ป้อม Crystal Spring — โจมตี AoE ทุกตัวในระยะ + ติด slow (และ freeze ที่ L3)
//   Damage / Range / Atk Speed มาจาก towers.csv (T2B) · slow%/freeze ตั้งต่อเลเวลบน prefab
//   อัพเกรด/ขายได้ผ่านระบบเดิม (implement IUpgradableTower)
public class CrystalSpringTower : MonoBehaviour, IUpgradableTower
{
    [Header("อ้างอิง CSV / อัพเกรด")]
    [Tooltip("ID ใน towers.csv เช่น T2B1")]
    [SerializeField] private string towerId = "T2B1";
    [Tooltip("Prefab เลเวลถัดไป (เว้นว่าง = เลเวลสูงสุด)")]
    [SerializeField] private GameObject nextLevelPrefab;

    public string TowerId => towerId;
    public GameObject NextLevelPrefab => nextLevelPrefab;

    [Header("การตรวจจับ")]
    [Tooltip("เลเยอร์ศัตรู (เว้นว่าง = เดา Enemy)")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Slow (ต่อเลเวล)")]
    [Range(0f, 1f)]
    [Tooltip("0.4 = ช้าลง 40%")]
    [SerializeField] private float slowPercent = 0.4f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Freeze (เปิดเฉพาะ L3)")]
    [Range(0f, 1f)]
    [Tooltip("0 = ไม่มี freeze, 0.25 = โอกาส 25%")]
    [SerializeField] private float freezeChance = 0f;
    [SerializeField] private float freezeDuration = 2f;

    [Header("VFX (option)")]
    [SerializeField] private GameObject pulseVfxPrefab;   // เอฟเฟกต์ตอนโจมตี (ปล่อยว่างได้)

    // runtime (จาก CSV)
    private float damage, range, fireCooldown;
    private float cooldownTimer;
    private bool _statsReady, _csvResolved;

    private ContactFilter2D _filter;
    private readonly List<Collider2D> _hits = new List<Collider2D>();

    void Awake()
    {
        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyLayer);
        _filter.useTriggers = true;
    }

    void Start() => TryLoadStats();

    void TryLoadStats()
    {
        if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded) return;
        _csvResolved = true;

        var rec = TowerDatabase.Instance.GetByID(towerId);
        if (rec == null)
        {
            Debug.LogError($"[CrystalSpring] ไม่พบ ID '{towerId}' ใน towers.csv", this);
            return;
        }
        damage = rec.damage;
        range = rec.range;
        fireCooldown = rec.atkSpeed > 0f ? rec.atkSpeed : 1f;
        _statsReady = true;
    }

    void Update()
    {
        if (!_csvResolved) TryLoadStats();
        if (!_statsReady || range <= 0f) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            Fire();
            cooldownTimer = fireCooldown;
        }
    }

    void Fire()
    {
        int count = Physics2D.OverlapCircle(transform.position, range, _filter, _hits);
        bool hitAny = false;

        for (int i = 0; i < count; i++)
        {
            var e = _hits[i].GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead) continue;
            hitAny = true;

            e.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));
            if (e.IsDead) continue;   // ตายจากดาเมจแล้ว ไม่ต้องติดสถานะ

            e.ApplySlow(slowPercent, slowDuration);
            if (freezeChance > 0f && Random.value < freezeChance)
                e.ApplyFreeze(freezeDuration);
        }

        if (hitAny && pulseVfxPrefab != null)
            Destroy(Instantiate(pulseVfxPrefab, transform.position, Quaternion.identity), 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range > 0f ? range : 2.5f);
    }
}
