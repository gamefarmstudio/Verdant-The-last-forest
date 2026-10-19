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
    [SerializeField] GameObject slotSprite;     
    [SerializeField] GameObject highlightRing;  
    
    [Header("Knight Settings")]
    [SerializeField] private int ForestKnightHealth;   

    bool _isSelected = false;
    TowerSpot _spot;

    void Awake()
    {
        _spot = GetComponent<TowerSpot>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isOccupied)
        {
            if (_isSelected) TowerPlacementManager.Instance.ClosePopup();
            else             TowerPlacementManager.Instance.OpenUpgrade(this);
            return;
        }

        if (_spot != null && !_spot.IsUnlocked) return;

        if (_isSelected)
            TowerPlacementManager.Instance.ClosePopup();  
        else
            TowerPlacementManager.Instance.OpenPopup(this);
    }

    public IUpgradableTower TowerComponent =>
        placedTower != null ? placedTower.GetComponentInChildren<IUpgradableTower>() : null;

    public void ReplaceTower(GameObject prefab, GameObject ThisPrefab, bool IsForestKnight)
    {
        Debug.Log($"[TowerSlot] ReplaceTower: {ThisPrefab.name} → {prefab.name} (IsForestKnight={IsForestKnight})", this);
        
        if (IsForestKnight)
        {
            if (placedTower != null && placedTower.TryGetComponent<MeleeTower>(out var oldMeleeTower))
            {
                ForestKnightHealth = oldMeleeTower._Health;  
                Debug.Log($"[TowerSlot] ReplaceTower: Saved ForestKnightHealth={ForestKnightHealth}", this);
            }
            else
            {
                Debug.LogWarning("[TowerSlot] ReplaceTower: IsForestKnight is true, but no MeleeTower found on current placedTower.", this);
            }
        }

        var col = GetComponent<CircleCollider2D>();
        Vector3 spawnPos = col != null
            ? transform.TransformPoint(col.offset)
            : transform.position;

        if (placedTower != null) Destroy(placedTower);

        placedTower = Instantiate(prefab, spawnPos, Quaternion.identity);
        placedTower.transform.SetParent(transform);

        if (IsForestKnight)
        {
            if (placedTower.TryGetComponent<MeleeTower>(out var newMeleeTower))
            {
                newMeleeTower._Health = ForestKnightHealth;   
                Debug.Log($"[TowerSlot] ReplaceTower: Restored HP to new MeleeTower. Current HP={newMeleeTower._Health}", this);
            }
        }

        isOccupied = true;
        if (slotSprite != null) slotSprite.SetActive(false);
    }

    public void ClearTower()
    {
        if (placedTower != null) Destroy(placedTower);
        placedTower = null;
        isOccupied = false;
        SetSelected(false);
        if (slotSprite != null) slotSprite.SetActive(true);
    }

    public void SetSelected(bool on)
    {
        _isSelected = on;
        if (highlightRing != null) highlightRing.SetActive(on);
    }
    public void Build(GameObject prefab)
    {
        if (isOccupied) return;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isOccupied || _isSelected || highlightRing == null) return;
        if (_spot != null && !_spot.IsUnlocked) return;  

        highlightRing.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected && highlightRing != null)
            highlightRing.SetActive(false);
    }
}
