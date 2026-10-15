using UnityEngine;
using DG.Tweening;

// [เพิ่ม] สคริปต์ใหม่: ป้อมประชิด — summon อัศวิน 1 ตัว, ตายแล้วเกิดใหม่หลัง respawnDelay
//   วางบน prefab ป้อม Forest Knight (T1A) แทน/เพิ่มจาก Tower ระยะไกล
public class MeleeTower : MonoBehaviour, IUpgradableTower   // [เพิ่ม] อัพเกรด/ขายได้เหมือนป้อมยิงไกล
{
    // [เพิ่ม] ระบบอัพเกรด — สเตตัสอัศวินอ้างจาก towers.csv ด้วย towerId, เปลี่ยน prefab เมื่ออัพ
    [Header("Upgrade")]
    [Tooltip("ID ใน towers.csv เช่น T1A1 — โหลด HP/Damage/Atk Speed/Range ของอัศวินจากแถวนี้")]
    [SerializeField] private string towerId = "T1A1";
    [Tooltip("Prefab ป้อมประชิดเลเวลถัดไป (เว้นว่าง = เลเวลสูงสุด)")]
    [SerializeField] private GameObject nextLevelPrefab;

    public string TowerId => towerId;
    public GameObject NextLevelPrefab => nextLevelPrefab;

    [Header("Summon")]
    [SerializeField] private KnightUnit knightPrefab;
    // [แก้] เปลี่ยนจาก Transform (ลากใส่บน prefab ไม่ได้ → "Type mismatch") เป็น offset จากตัวป้อม
    [Tooltip("ปรับตำแหน่งฐานก่อน snap ลง path (ปกติ (0,0) พอ)")]
    [SerializeField] private Vector2 rallyOffset = Vector2.zero;
    // [เพิ่ม] ให้จุดยืนอัศวิน snap ลง "จุดบน EnemyPath ที่ใกล้ป้อมสุด" อัตโนมัติ
    //        → วางป้อมตรงไหนก็ยืนบน path เอง ไม่ต้องตั้งค่าต่อ slot
    [Tooltip("ยึดจุดยืนอัศวินให้อยู่บนทางเดินมอนที่ใกล้ป้อมสุดอัตโนมัติ")]
    [SerializeField] private bool snapRallyToPath = true;
    // [เพิ่ม] เลื่อนจุดยืนไปตามแนว path: + = ไปทางฐาน (ดักหน้า), - = ไปทางจุดเกิดมอน (หน่วยโลก)
    [Tooltip("เลื่อนอัศวินไปตามแนว path: + ไปทางฐาน / - ไปทางจุดเกิดมอน")]
    [SerializeField] private float pathShift = 0f;
    [SerializeField] private float respawnDelay = 5f;   // เกิดใหม่หลังอัศวินตายกี่วินาที

    private KnightUnit _knight;
    private float _respawnTimer;
    [HideInInspector] public int _Health;   // [เพิ่ม] ให้ TowerSlot/UpgradePopup อ่านค่า HP อัศวินได้ (อ้างจาก towers.csv)

    void Start() => Summon();

    void Update()
    {
        _Health = _knight != null ? _knight._hp : 0;   // [เพิ่ม] ให้ TowerSlot/UpgradePopup อ่านค่า HP อัศวินได้ (อ้างจาก towers.csv)
        if (_knight != null) return;   // ยังมีอัศวินอยู่ (Destroy แล้วจะกลายเป็น null เอง)

        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f) Summon();
    }

    void Summon()
    {
        if (knightPrefab == null)
        {
            Debug.LogError("[MeleeTower] ยังไม่ได้ตั้ง Knight Prefab", this);
            return;
        }

        Vector3 pos = RallyPosition();
        _knight = Instantiate(knightPrefab, pos, Quaternion.identity);

        // [เพิ่ม] โหลดสเตตัสอัศวินจาก towers.csv ตาม towerId ของป้อม (เลเวลนี้)
        var rec = TowerDatabase.Instance != null ? TowerDatabase.Instance.GetByID(towerId) : null;
        if (rec != null)
            _knight.Configure( rec.hp, Mathf.RoundToInt(rec.damage), rec.atkSpeed, rec.range);

        _knight.Init(pos);
        _knight.OnDeath += HandleKnightDeath;
    }

    // [เพิ่ม] คำนวณจุดยืนอัศวิน — offset จากป้อม แล้ว (option) snap ลง path ที่ใกล้สุด
    Vector3 RallyPosition()
    {
        Vector3 basePos = transform.position + (Vector3)rallyOffset;
        if (snapRallyToPath)
        {
            var path = FindAnyObjectByType<EnemyPath>();
            if (path != null && path.IsValid) return path.PointAlongPath(basePos, pathShift);
        }
        return basePos;
    }

    void HandleKnightDeath()
    {
        _knight = null;
        _respawnTimer = respawnDelay;
    }

    // [เพิ่ม] ป้อมถูกทำลาย (เช่นขายป้อม) → ทำลายอัศวินด้วย กันอัศวินค้างเดินต่อ
    void OnDestroy()
    {
        if (_knight != null) Destroy(_knight.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 p = RallyPosition();
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(p, 0.3f);
    }
}
