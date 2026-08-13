using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ติดกับ: Slot GameObject แต่ละจุดบนแผนที่
/// ต้องมี: Collider2D (CircleCollider2D หรือ BoxCollider2D)
/// </summary>
public class TowerSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── State ──
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public GameObject placedTower = null;

    [Header("Visuals")]
    [SerializeField] GameObject slotSprite;       // sprite จุดสีชมพู/marker
    [SerializeField] GameObject highlightRing;    // glow รอบๆ เมื่อ hover/select

    bool _isSelected = false;
    TowerSpot _spot;

    void Awake()
    {
        _spot = GetComponent<TowerSpot>();
    }

    // ────────────────────────────────────────────
    // คลิกที่ slot
    // ────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        // [แก้] มีป้อมแล้ว → เปิดเมนูอัพเกรด/ทำลาย (คลิกซ้ำ = ปิด)
        if (isOccupied)
        {
            if (_isSelected) TowerPlacementManager.Instance.ClosePopup();
            else             TowerPlacementManager.Instance.OpenUpgrade(this);
            return;
        }

        // หมอกยังไม่ถอยพ้นจุดนี้ → วางป้อมไม่ได้
        if (_spot != null && !_spot.IsUnlocked) return;

        if (_isSelected)
            TowerPlacementManager.Instance.ClosePopup();   // คลิกซ้ำ = ปิด popup
        else
            TowerPlacementManager.Instance.OpenPopup(this);
    }

    // [แก้] อ่านป้อมผ่าน interface IUpgradableTower → รองรับทั้งป้อมยิงไกล (Tower) และป้อมประชิด (MeleeTower)
    public IUpgradableTower TowerComponent =>
        placedTower != null ? placedTower.GetComponentInChildren<IUpgradableTower>() : null;

    // [เพิ่ม] แทนป้อมด้วย prefab เลเวลใหม่ (อัพเกรดแบบเปลี่ยน prefab) — slot ยังถือว่ามีป้อมอยู่
    public void ReplaceTower(GameObject prefab)
    {
        var col = GetComponent<CircleCollider2D>();
        Vector3 spawnPos = col != null
            ? transform.TransformPoint(col.offset)
            : transform.position;

        if (placedTower != null) Destroy(placedTower);

        placedTower = Instantiate(prefab, spawnPos, Quaternion.identity);
        placedTower.transform.SetParent(transform);

        isOccupied = true;
        if (slotSprite != null) slotSprite.SetActive(false);
    }

    // [เพิ่ม] ทำลายป้อมแล้วคืน slot ให้ว่าง (ใช้ตอนขาย)
    public void ClearTower()
    {
        if (placedTower != null) Destroy(placedTower);
        placedTower = null;
        isOccupied = false;
        SetSelected(false);
        if (slotSprite != null) slotSprite.SetActive(true);
    }

    // ────────────────────────────────────────────
    // เรียกจาก TowerPlacementManager
    // ────────────────────────────────────────────
    public void SetSelected(bool on)
    {
        _isSelected = on;
        if (highlightRing != null) highlightRing.SetActive(on);
    }

    /// <summary>สร้างป้อมที่ slot นี้</summary>
    public void Build(GameObject prefab)
    {
        if (isOccupied) return;

        // ใช้ center ของ Collider แทน transform.position
        var col = GetComponent<CircleCollider2D>();
        Vector3 spawnPos = col != null
            ? transform.TransformPoint(col.offset)
            : transform.position;

        placedTower = Instantiate(prefab, spawnPos, Quaternion.identity);
        placedTower.transform.SetParent(transform);

        isOccupied = true;
        SetSelected(false);
        if (slotSprite != null) slotSprite.SetActive(false);
    }

    // ────────────────────────────────────────────
    // Hover visual
    // ────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isOccupied || _isSelected || highlightRing == null) return;
        if (_spot != null && !_spot.IsUnlocked) return;   // ยังมีหมอกปกคลุมอยู่ ไม่ต้อง highlight

        highlightRing.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected && highlightRing != null)
            highlightRing.SetActive(false);
    }
}