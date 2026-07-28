// ═══════════════════════════════════════════════════════════════
//  TowerSpot.cs — จุดวางป้อมที่ปลดล็อกเมื่อหมอกถอยพ้น
//  แต่ละจุดคำนวณเกณฑ์เองจากตำแหน่ง X — วางตรงไหนก็รู้เองว่า
//  หมอกต้องถอยถึงเท่าไหร่ตัวเองถึงเปิด (และล็อกคืนถ้าหมอกลามกลับ)
//  วางบน prefab จุดวางป้อม
// ═══════════════════════════════════════════════════════════════
using UnityEngine;

public class TowerSpot : MonoBehaviour
{
    [SerializeField] private float clearMargin = 0.5f;  // หมอกต้องถอยพ้นเลยไปอีกเท่านี้ (world unit)
    [SerializeField] private GameObject lockedVisual;   // เช่น icon แม่กุญแจ / พื้นสีหม่น
    [SerializeField] private GameObject unlockedVisual;

    public bool IsUnlocked { get; private set; }
    private float _requiredGauge;
    private TowerSlot _slot;
    private bool _initialized;   // บังคับ apply visual ครั้งแรกเสมอ แม้สถานะจะยัง "ล็อก" เหมือนค่า default

    void Start()
    {
        _slot = GetComponent<TowerSlot>();

        var fog = FindAnyObjectByType<PollutionFogController>();
        if (fog == null)
        {
            Debug.LogError($"[TowerSpot] ไม่พบ PollutionFogController ในฉาก — '{name}' จะถูกล็อกถาวร", this);
            return;   // ไม่ subscribe — spot ค้างสถานะล็อกไว้ ดีกว่าพังทั้งเกม
        }

        // จุดนี้พ้นหมอกเมื่อขอบหมอก (edgeX) อยู่ซ้ายกว่าตำแหน่งเรา - margin
        _requiredGauge = fog.GaugeAtX(transform.position.x - clearMargin);

        GameEvents.OnPollutionChanged += Check;

        if (PollutionManager.Instance != null)
            Check(PollutionManager.Instance.Gauge);   // sync สถานะเริ่มต้นทันที
        else
            Debug.LogWarning($"[TowerSpot] ไม่พบ PollutionManager — '{name}' จะรอ sync จาก event แรก", this);
    }

    void OnDestroy() => GameEvents.OnPollutionChanged -= Check;

    void Check(float gauge)
    {
        // มีป้อมตั้งอยู่แล้ว → ไม่ล็อกคืนแม้หมอกจะลามกลับมา
        if (_slot != null && _slot.isOccupied) return;

        bool unlocked = gauge < _requiredGauge;

        // เปลี่ยนจริงค่อยทำงาน — ยกเว้นรอบแรกที่ต้อง apply visual ให้ตรงสถานะเสมอ
        // (กันเคสเริ่มมาแบบ "ล็อก" ซึ่ง unlocked==IsUnlocked==false แล้ว return ทิ้งไปโดยไม่เซ็ตภาพ)
        if (_initialized && unlocked == IsUnlocked) return;
        _initialized = true;

        IsUnlocked = unlocked;
        if (lockedVisual)   lockedVisual.SetActive(!unlocked);
        if (unlockedVisual) unlockedVisual.SetActive(unlocked);
    }
}
