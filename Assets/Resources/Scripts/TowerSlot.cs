using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A place on the map where a tower can be built.
/// Attach to: each slot GameObject on the map.
/// Requires: a Collider2D (CircleCollider2D or BoxCollider2D) so it can be clicked.
///   - Click an empty, unlocked slot -> opens the build menu.
///   - Click a slot with a tower     -> opens the upgrade/sell menu.
///   - Clicking the selected slot again closes the menu.
/// Towers are spawned at the CircleCollider2D's centre (if any) and parented to the slot.
/// </summary>
public class TowerSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ── State ──
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public GameObject placedTower = null;

    [Header("Visuals")]
    [SerializeField] GameObject slotSprite;      // marker shown while the slot is empty
    [SerializeField] GameObject highlightRing;   // glow shown on hover / while selected

    [Header("Knight Settings")]
    [SerializeField] private int ForestKnightHealth;   // knight HP carried over when a Forest Knight tower is upgraded

    bool _isSelected = false;
    TowerSpot _spot;   // optional — locks the slot while it's covered by fog

    void Awake()
    {
        _spot = GetComponent<TowerSpot>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Has a tower -> open the upgrade/sell menu (click again = close)
        if (isOccupied)
        {
            if (_isSelected) TowerPlacementManager.Instance.ClosePopup();
            else             TowerPlacementManager.Instance.OpenUpgrade(this);
            return;
        }

        // Still covered by pollution fog -> can't build here
        if (_spot != null && !_spot.IsUnlocked) return;

        if (_isSelected)
            TowerPlacementManager.Instance.ClosePopup();   // click again = close the popup
        else
            TowerPlacementManager.Instance.OpenPopup(this);
    }

    /// <summary>The placed tower, read through IUpgradableTower so it works for every tower type. Null if empty.</summary>
    public IUpgradableTower TowerComponent =>
        placedTower != null ? placedTower.GetComponentInChildren<IUpgradableTower>() : null;

    /// <summary>
    /// Replaces the current tower with a new-level prefab (upgrade by swapping prefabs). The slot stays occupied.
    /// For Forest Knight towers the knight's current HP is copied over to the new tower.
    /// </summary>
    public void ReplaceTower(GameObject prefab, GameObject ThisPrefab, bool IsForestKnight)
    {
        Debug.Log($"[TowerSlot] ReplaceTower: {ThisPrefab.name} → {prefab.name} (IsForestKnight={IsForestKnight})", this);

        if (IsForestKnight)
        {
            // Save the knight's HP from the old MeleeTower before it is destroyed.
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

        // Spawn position = the collider's centre
        var col = GetComponent<CircleCollider2D>();
        Vector3 spawnPos = col != null
            ? transform.TransformPoint(col.offset)
            : transform.position;

        // Remove the old tower
        if (placedTower != null) Destroy(placedTower);

        // Create the upgraded tower
        placedTower = Instantiate(prefab, spawnPos, Quaternion.identity);
        placedTower.transform.SetParent(transform);

        if (IsForestKnight)
        {
            // Pass the saved HP on to the new tower right after it is created.
            if (placedTower.TryGetComponent<MeleeTower>(out var newMeleeTower))
            {
                newMeleeTower._Health = ForestKnightHealth;
                Debug.Log($"[TowerSlot] ReplaceTower: Restored HP to new MeleeTower. Current HP={newMeleeTower._Health}", this);
            }
        }

        isOccupied = true;
        if (slotSprite != null) slotSprite.SetActive(false);
    }

    /// <summary>Destroys the tower and frees the slot (used when selling).</summary>
    public void ClearTower()
    {
        if (placedTower != null) Destroy(placedTower);
        placedTower = null;
        isOccupied = false;
        SetSelected(false);
        if (slotSprite != null) slotSprite.SetActive(true);
    }

    /// <summary>Shows/hides the highlight ring (called by TowerPlacementManager).</summary>
    public void SetSelected(bool on)
    {
        _isSelected = on;
        if (highlightRing != null) highlightRing.SetActive(on);
    }

    /// <summary>Builds a tower in this slot (does nothing if the slot is already occupied).</summary>
    public void Build(GameObject prefab)
    {
        if (isOccupied) return;

        // Use the collider's centre instead of transform.position
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

    // ── Hover visual ──
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isOccupied || _isSelected || highlightRing == null) return;
        if (_spot != null && !_spot.IsUnlocked) return;   // still covered by fog — no highlight

        highlightRing.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected && highlightRing != null)
            highlightRing.SetActive(false);
    }
}
