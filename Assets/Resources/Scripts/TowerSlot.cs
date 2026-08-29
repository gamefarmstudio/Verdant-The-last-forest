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
    
    [Header("Knight Settings")]
    [SerializeField] private int ForestKnightHealth;   // เก็บค่า HP ของอัศวิน (อ้างจาก towers.csv) ให้ TowerUpgradePopup อ่านได้

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
        // มีป้อมแล้ว → เปิดเมนูอัพเกรด/ทำลาย (คลิกซ้ำ = ปิด)
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

    // อ่านป้อมผ่าน interface IUpgradableTower → รองรับทั้งป้อมยิงไกล (Tower) และป้อมประชิด (MeleeTower)
    public IUpgradableTower TowerComponent =>
        placedTower != null ? placedTower.GetComponentInChildren<IUpgradableTower>() : null;

    // [แก้ไขแล้ว] แทนป้อมด้วย prefab เลเวลใหม่ (อัพเกรดแบบเปลี่ยน prefab) — slot ยังถือว่ามีป้อมอยู่
    public void ReplaceTower(GameObject prefab, GameObject ThisPrefab, bool IsForestKnight)
    {
        Debug.Log($"[TowerSlot] ReplaceTower: {ThisPrefab.name} → {prefab.name} (IsForestKnight={IsForestKnight})", this);
        
        if (IsForestKnight)
        {
            // ดึงค่า HP จากป้อม MeleeTower ตัวเก่าก่อนที่จะถูกทำลาย
            if (placedTower != null && placedTower.TryGetComponent<MeleeTower>(out var oldMeleeTower))
            {
                ForestKnightHealth = oldMeleeTower._Health;   // บันทึกค่า HP ของอัศวินปัจจุบันไว้
                Debug.Log($"[TowerSlot] ReplaceTower: Saved ForestKnightHealth={ForestKnightHealth}", this);
            }
            else
            {
                Debug.LogWarning("[TowerSlot] ReplaceTower: IsForestKnight is true, but no MeleeTower found on current placedTower.", this);
            }
        }

        // คำนวณตำแหน่งเกิดจาก Collider
        var col = GetComponent<CircleCollider2D>();
        Vector3 spawnPos = col != null
            ? transform.TransformPoint(col.offset)
            : transform.position;

        // ทำลายป้อมเก่าทิ้ง
        if (placedTower != null) Destroy(placedTower);

        // สร้างป้อมใหม่ในร่างอัพเกรด
        placedTower = Instantiate(prefab, spawnPos, Quaternion.identity);
        placedTower.transform.SetParent(transform);

        if (IsForestKnight)
        {
            // ส่งค่า HP เดิมที่บันทึกไว้ ไปให้ป้อมใหม่ทันทีหลังสร้างเสร็จ
            if (placedTower.TryGetComponent<MeleeTower>(out var newMeleeTower))
            {
                newMeleeTower._Health = ForestKnightHealth;   
                Debug.Log($"[TowerSlot] ReplaceTower: Restored HP to new MeleeTower. Current HP={newMeleeTower._Health}", this);
            }
        }

        isOccupied = true;
        if (slotSprite != null) slotSprite.SetActive(false);
    }

    // ทำลายป้อมแล้วคืน slot ให้ว่าง (ใช้ตอนขาย)
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
