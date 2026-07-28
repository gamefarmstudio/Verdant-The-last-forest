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
        // ถ้ามีป้อมแล้ว → ไม่ทำอะไร (วันหลังค่อยทำ upgrade/sell)
        if (isOccupied) return;

        // หมอกยังไม่ถอยพ้นจุดนี้ → วางป้อมไม่ได้
        if (_spot != null && !_spot.IsUnlocked) return;

        if (_isSelected)
        {
            // คลิกซ้ำ = ปิด popup
            TowerPlacementManager.Instance.ClosePopup();
        }
        else
        {
            TowerPlacementManager.Instance.OpenPopup(this);
        }
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