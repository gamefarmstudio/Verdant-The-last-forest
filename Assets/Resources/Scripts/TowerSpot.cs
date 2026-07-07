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

    void Start()
    {
        _slot = GetComponent<TowerSlot>();

        var fog = FindAnyObjectByType<PollutionFogController>();
        // จุดนี้พ้นหมอกเมื่อขอบหมอก (edgeX) อยู่ซ้ายกว่าตำแหน่งเรา - margin
        _requiredGauge = fog.GaugeAtX(transform.position.x - clearMargin);

        GameEvents.OnPollutionChanged += Check;
        Check(PollutionManager.Instance.Gauge);   // sync สถานะเริ่มต้นทันที
    }

    void OnDestroy() => GameEvents.OnPollutionChanged -= Check;

    void Check(float gauge)
    {
        // มีป้อมตั้งอยู่แล้ว → ไม่ล็อกคืนแม้หมอกจะลามกลับมา
        if (_slot != null && _slot.isOccupied) return;

        bool unlocked = gauge < _requiredGauge;
        if (unlocked == IsUnlocked) return;      // เปลี่ยนจริงค่อยทำงาน

        IsUnlocked = unlocked;
        if (lockedVisual)   lockedVisual.SetActive(!unlocked);
        if (unlockedVisual) unlockedVisual.SetActive(unlocked);
    }
}
