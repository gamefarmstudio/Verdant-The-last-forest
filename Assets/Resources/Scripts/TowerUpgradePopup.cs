using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Verdant.Combat;

// [เพิ่ม] สคริปต์ใหม่: เมนูอัพเกรด/ทำลาย — เด้งตอนคลิกป้อมที่สร้างแล้ว (คู่ขนานกับ TowerBuildPopup)
//   ปุ่ม Upgrade: ดึงเลเวลถัดไปจาก towers.csv (TowerDatabase.GetNextLevel) — dim ถ้าเงินไม่พอ/เลเวลสูงสุด
//   ปุ่ม Sell: คืนคริสตัลตาม % ของ Cumulative Cost แล้วทำลายป้อม
// วิธีใช้: วางบน Panel ใน Canvas เดียวกับ TowerBuildPopup แล้วต่อปุ่ม/ข้อความ + ลากเข้า TowerPlacementManager
public class TowerUpgradePopup : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button upgradeButton;
    [SerializeField] Button sellButton;
    [SerializeField] TMP_Text upgradeCostText;   // ราคาอัพเกรด หรือ "MAX"
    [SerializeField] TMP_Text sellText;          // เงินคืนตอนขาย เช่น "+120"

    [Header("Panel")]
    [SerializeField] RectTransform panelRect;

    private TowerSlot _slot;   // slot ที่เมนูนี้กำลังเปิดให้

    public void Show(TowerSlot slot)
    {
        Time.timeScale = 0.1f; // หยุดเกมชั่วคราวตอนเมนูเปิด
        _slot = slot;
        gameObject.SetActive(true);
        Refresh();
        PositionNearSlot(slot.transform.position);
    }

    public void Hide()
    {
        Time.timeScale = 1f; // รีเซ็ตเวลาเกมเมื่อเมนูปิด

        _slot = null;
        gameObject.SetActive(false);
    }

    // อัปเดตข้อความ/สถานะปุ่มตามป้อมใน slot ปัจจุบัน
    public void Refresh()
    {
        if (_slot == null) return;

        var tower = _slot.TowerComponent;
        var next = (tower != null && TowerDatabase.Instance != null)
            ? TowerDatabase.Instance.GetNextLevel(tower.TowerId)
            : null;
        bool hasNextPrefab = tower != null && tower.NextLevelPrefab != null;   // [เพิ่ม] ต้องมี prefab เลเวลถัดไปด้วย

        // ── Upgrade ── (ต้องมีทั้งข้อมูล CSV เลเวลถัดไป + prefab ถัดไป + เงินพอ)
        bool canUpgrade = next != null && hasNextPrefab && CurrencyManager.Instance != null &&
                          CurrencyManager.Instance.CanAfford(next.cost);
        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
        if (upgradeCostText != null)
            upgradeCostText.text = (next == null || !hasNextPrefab) ? "MAX" : next.cost.ToString();

        // ── Sell ──
        if (sellText != null)
            sellText.text = "+" + TowerPlacementManager.Instance.SellRefund(tower);
    }

    // ── ต่อกับปุ่ม (OnClick ใน Inspector) ──
    public void OnClickUpgrade() => TowerPlacementManager.Instance.UpgradeActiveTower();
    public void OnClickSell()    => TowerPlacementManager.Instance.SellActiveTower();

    // ── วางเมนูใกล้ slot (เหมือน TowerBuildPopup) ──
    void PositionNearSlot(Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        var canvasRect = transform.parent.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPos);

        panelRect.anchoredPosition = localPos + new Vector2(0, 90f);
        ClampToScreen();
    }

    void ClampToScreen()
    {
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        Vector2 pos = panelRect.anchoredPosition;

        if (corners[2].x > Screen.width) pos.x -= (corners[2].x - Screen.width);
        if (corners[0].x < 0) pos.x -= corners[0].x;
        if (corners[2].y > Screen.height) pos.y -= (corners[2].y - Screen.height);
        if (corners[0].y < 0) pos.y -= corners[0].y;

        panelRect.anchoredPosition = pos;
    }

    // ── คลิกนอกเมนู → ปิด ──
    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                TowerPlacementManager.Instance.ClosePopup();
        }
    }

    // อัปเดตสถานะปุ่มเมื่อคริสตัลเปลี่ยน (dim/undim upgrade)
    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCrystalChanged.AddListener(OnCrystalChanged);
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCrystalChanged.RemoveListener(OnCrystalChanged);
    }

    void OnCrystalChanged(int _) => Refresh();
}
