using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;            // [เพิ่ม] เด้ง feedback ตอนอัพเกรด
using Verdant.Combat;

// [แก้] เขียนใหม่ (ล้างคอมเมนต์เดิม) — [เพิ่ม] ระบบอัพเกรด/ขายป้อม
// Singleton ควบคุมการวาง/อัพเกรด/ขายป้อม — ติดกับ GameObject "GameManager"
public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] TowerBuildPopup popup;                    // เมนูสร้างป้อม (ช่องว่าง)
    [SerializeField] TowerUpgradePopup upgradePopup;           // [เพิ่ม] เมนูอัพเกรด/ทำลาย (ป้อมที่วางแล้ว)

    [Header("Sell")]
    [Range(0f, 1f)]
    [SerializeField] float sellRefundPercent = 0.6f;           // [เพิ่ม] คืนเงินกี่ % ของ Cumulative Cost

    TowerSlot _activeSlot = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── เมนูสร้างป้อม (ช่องว่าง) ──
    public void OpenPopup(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        popup.Show(slot);
    }

    // [เพิ่ม] เมนูอัพเกรด/ทำลาย (ป้อมที่วางแล้ว)
    public void OpenUpgrade(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        upgradePopup.Show(slot);
    }

    // [แก้] ปิดได้ทั้งสองเมนู
    public void ClosePopup()
    {
        if (popup != null) popup.Hide();
        if (upgradePopup != null) upgradePopup.Hide();
        if (_activeSlot != null)
        {
            _activeSlot.SetSelected(false);
            _activeSlot = null;
        }
    }

    // ── สร้างป้อม (เรียกจาก TowerBuildPopup) ──
    public void OnTowerSelected(GameObject towerPrefab, int cost)
    {
        if (_activeSlot == null) { Debug.LogError("[TowerPlacement] _activeSlot is NULL"); return; }
        if (!CurrencyManager.Instance.CanAfford(cost)) { Debug.LogWarning("ไม่มีเงินพอ"); return; }

        CurrencyManager.Instance.Spend(cost);
        _activeSlot.Build(towerPrefab);
        _activeSlot = null;
        popup.Hide();
    }

    // ── [เพิ่ม] อัพเกรดป้อมใน slot ที่เลือกอยู่ ──
    public void UpgradeActiveTower()
    {
        if (_activeSlot == null) return;

        var tower = _activeSlot.TowerComponent;
        if (tower == null) { Debug.LogWarning("[Upgrade] ไม่พบ Tower ใน slot"); return; }

        var next = TowerDatabase.Instance != null ? TowerDatabase.Instance.GetNextLevel(tower.TowerId) : null;
        GameObject nextPrefab = tower.NextLevelPrefab;   // [เพิ่ม] prefab เลเวลถัดไป (ตั้งบน Tower)
        if (next == null || nextPrefab == null)
        {
            Debug.Log($"[Upgrade] '{tower.TowerId}' เลเวลสูงสุด หรือไม่ได้ตั้ง Next Level Prefab");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(next.cost)) { Debug.LogWarning("[Upgrade] เงินไม่พอ"); return; }

        CurrencyManager.Instance.Spend(next.cost);
        _activeSlot.ReplaceTower(nextPrefab);   // [แก้] ทำลายตัวเก่า + วาง prefab เลเวลใหม่ (เดิม Reconfigure ค่า)

        // feedback: เด้งป้อมใหม่นิดๆ
        if (_activeSlot.placedTower != null)
        {
            _activeSlot.placedTower.transform.DOKill();
            _activeSlot.placedTower.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f).SetLink(_activeSlot.placedTower);
        }

        upgradePopup.Refresh();       // อัปเดตราคาถัดไป/สถานะปุ่ม
        Debug.Log($"[Upgrade] → {next.id} (จ่าย {next.cost})");
    }

    // ── [เพิ่ม] ขายป้อมใน slot ที่เลือกอยู่ ──
    public void SellActiveTower()
    {
        if (_activeSlot == null) return;

        int refund = SellRefund(_activeSlot.TowerComponent);
        if (refund > 0) CurrencyManager.Instance.Earn(refund);

        _activeSlot.ClearTower();
        _activeSlot = null;
        upgradePopup.Hide();
        Debug.Log($"[Sell] คืน {refund} crystal");
    }

    // [แก้] รับ IUpgradableTower → คิดเงินคืนได้ทั้งป้อมยิงไกลและป้อมประชิด
    public int SellRefund(IUpgradableTower tower)
    {
        if (tower == null || TowerDatabase.Instance == null) return 0;
        var rec = TowerDatabase.Instance.GetByID(tower.TowerId);
        if (rec == null) return 0;
        return Mathf.RoundToInt(rec.cumulativeCost * sellRefundPercent);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePopup();   // ESC → ปิดเมนู
    }
}
