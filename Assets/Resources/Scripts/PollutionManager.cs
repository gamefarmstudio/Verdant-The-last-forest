// ═══════════════════════════════════════════════════════════════
//  PollutionManager.cs — สมองกลางของระบบมลพิษ
//  กติกาทั้งหมดอยู่ที่นี่ที่เดียว: ฆ่ามอน = เกจลด, คริสตัลโดนตี = เกจเพิ่ม
//  เกจ 0% → ชนะ / เกจ 100% → เกมโอเวอร์
//  วางบน GameObject เปล่าในฉาก (1 ตัวต่อด่าน)
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float startGauge = 0.7f;            // เริ่มด่าน หมอก 70%
    [SerializeField] private float defaultCleansePerKill = 0.02f; // fallback ถ้า CSV ไม่มีค่า
    [SerializeField] private float gaugePerCrystalDamage = 0.005f;// ดาเมจ 1 หน่วย = เกจ +0.5%

    public float Gauge { get; private set; }
    private bool _ended;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // ตั้งค่าเกจใน Awake (ไม่ใช่ Start) — สคริปต์อื่น (เช่น TowerSpot) ต้องอ่าน
        // Gauge ที่ถูกต้องได้ตั้งแต่ Start() ของตัวเอง ซึ่ง Unity รับประกันว่า
        // Awake ของทุกสคริปต์จะรันเสร็จก่อน Start ของสคริปต์ใดๆ เสมอ
        Gauge = startGauge;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled    += HandleEnemyKilled;
        GameEvents.OnCrystalDamaged += HandleCrystalDamaged;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled    -= HandleEnemyKilled;
        GameEvents.OnCrystalDamaged -= HandleCrystalDamaged;
    }

    void Start()
    {
        GameEvents.RaisePollutionChanged(Gauge);   // แจ้งค่าเริ่มต้นให้ทุกระบบ sync กัน
    }

    void HandleEnemyKilled(float cleanse)
        => ApplyDelta(-(cleanse > 0f ? cleanse : defaultCleansePerKill));

    void HandleCrystalDamaged(float damage)
        => ApplyDelta(damage * gaugePerCrystalDamage);

    private void ApplyDelta(float delta)
    {
        if (_ended) return;                        // จบเกมแล้ว ไม่รับอะไรอีก

        Gauge = Mathf.Clamp01(Gauge + delta);
        GameEvents.RaisePollutionChanged(Gauge);

        if (Gauge <= 0f)      { _ended = true; GameEvents.RaisePollutionCleared(); }
        else if (Gauge >= 1f) { _ended = true; GameEvents.RaisePollutionFull(); }
    }
}
