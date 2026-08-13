using UnityEngine;

// [แก้] เขียนใหม่ทั้งไฟล์ให้ตรงกติกาหมอกของด่านแรก:
//   [เพิ่ม] หมอกไต่ขึ้นเองตามเวลา — +passiveStep ทุก passiveInterval วินาที (ดีฟอลต์ +1% ทุก 2 วิ)
//   [เพิ่ม] TryCleanse() — จ่าย cleanseCost คริสตัล เพื่อลดหมอก cleansePercent (ดีฟอลต์ 10 คริสตัล / -5%)
//   [ลบ]  การลดหมอกตอนฆ่ามอน (defaultCleansePerKill / OnEnemyKilled) — สเปคด่านแรกให้ลดหมอกผ่านปุ่มเท่านั้น
public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float startGauge = 0.7f;
    // [ลบ] gaugePerCrystalDamage — หมอกไม่ผูกกับดาเมจคริสตัลแล้ว (ย้ายไปเป็น "เลือด" ใน GameManager)

    // [เพิ่ม] ค่าการเพิ่มหมอกตามเวลา
    [Header("Passive Rise")]
    [SerializeField] private float passiveInterval = 2f;   // เพิ่มหมอกทุกกี่วินาที
    [SerializeField] private float passiveStep = 0.01f;    // เพิ่มครั้งละเท่าไร (0.01 = 1%)

    // [เพิ่ม] ค่าปุ่มชำระล้าง
    [Header("Cleanse Button")]
    [SerializeField] private int cleanseCost = 10;         // คริสตัลต่อการกด 1 ครั้ง
    [SerializeField] private float cleansePercent = 0.05f; // ลดหมอกต่อครั้ง (0.05 = 5%)

    public float Gauge { get; private set; }
    public int CleanseCost => cleanseCost;   // [เพิ่ม] ให้ UI อ่านไปโชว์ราคา/เช็คปุ่ม

    private bool _ended;
    private float _passiveTimer;             // [เพิ่ม] ตัวจับเวลาสำหรับหมอกเพิ่มเอง

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Gauge = startGauge;
    }

    // [ลบ] OnEnable/OnDisable ที่ subscribe OnCrystalDamaged — หมอกขึ้นจากเวลาอย่างเดียว
    //      (มอนหลุดถึงฐานไปลด "เลือด" ใน GameManager แทน ไม่เพิ่มหมอกอีก)

    void Start()
    {
        GameEvents.RaisePollutionChanged(Gauge);
    }

    // [เพิ่ม] หมอกไต่ขึ้นเองตามเวลา — ทำงานเฉพาะตอนกำลังเล่นและยังไม่จบเกม
    void Update()
    {
        if (_ended) return;
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        _passiveTimer += Time.deltaTime;
        while (_passiveTimer >= passiveInterval)
        {
            _passiveTimer -= passiveInterval;
            ApplyDelta(passiveStep);
        }
    }

    // [เพิ่ม] เรียกจากปุ่มชำระล้าง — คืน true เมื่อจ่ายเงินและลดหมอกสำเร็จ, false ถ้าเงินไม่พอ/จบเกมแล้ว
    public bool TryCleanse()
    {
        if (_ended) return false;
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.CanAfford(cleanseCost))
            return false;

        CurrencyManager.Instance.Spend(cleanseCost);
        ApplyDelta(-cleansePercent);
        return true;
    }

    private void ApplyDelta(float delta)
    {
        if (_ended) return;

        Gauge = Mathf.Clamp01(Gauge + delta);
        GameEvents.RaisePollutionChanged(Gauge);

        // [แก้] เกจ 0% ไม่ใช่เงื่อนไขชนะแล้ว (ชนะจากจบเวฟเท่านั้น) — แค่ clamp ไว้ ปล่อยให้ไต่ขึ้นใหม่ได้
        if (Gauge >= 1f) { _ended = true; GameEvents.RaisePollutionFull(); }   // หมอกเต็ม 100% → แพ้
    }
}
