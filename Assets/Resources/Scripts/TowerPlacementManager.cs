using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton ควบคุมระบบวางป้อมทั้งหมด
/// ติดกับ: GameObject ชื่อ "GameManager"
/// </summary>
public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("Reference — ลาก TowerBuildPopup มาใส่")]
    [SerializeField] TowerBuildPopup popup;

    TowerSlot _activeSlot = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── เรียกจาก TowerSlot ──

    public void OpenPopup(TowerSlot slot)
    {
        // ถ้า slot อื่นถูกเลือกอยู่ → ปิดก่อน
        if (_activeSlot != null && _activeSlot != slot)
            _activeSlot.SetSelected(false);

        _activeSlot = slot;
        slot.SetSelected(true);
        popup.Show(slot);
    }

    public void ClosePopup()
    {
        popup.Hide();
        if (_activeSlot != null)
        {
            _activeSlot.SetSelected(false);
            _activeSlot = null;
        }
    }

    /// <summary>
    /// เรียกจาก TowerBuildPopup เมื่อ player เลือกป้อม
    /// </summary>
    public void OnTowerSelected(GameObject towerPrefab, int cost)
    {
        Debug.Log($"OnTowerSelected called | slot={_activeSlot} | prefab={towerPrefab}");

        if (_activeSlot == null)
        {
            Debug.LogError("_activeSlot is NULL!");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(cost))
        {
            Debug.LogWarning("ไม่มีเงินพอ");
            return;
        }

        CurrencyManager.Instance.Spend(cost);
        _activeSlot.Build(towerPrefab);
        _activeSlot = null;
        popup.Hide();
    }

    void Update()
    {
        // กด ESC → ปิด popup (คลิกนอก popup จัดการใน TowerBuildPopup)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePopup();
    }
}