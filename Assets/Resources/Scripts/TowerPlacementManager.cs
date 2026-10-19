using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using Verdant.Combat;

/// <summary>
/// Singleton that controls building, upgrading and selling towers. Lives on the "GameManager" GameObject.
///   - Clicking an empty TowerSlot opens the build menu (TowerBuildPopup).
///   - Clicking a built tower opens the upgrade/sell menu (TowerUpgradePopup).
///   - Upgrading swaps in the next-level prefab and charges that level's cost from towers.csv.
///   - Selling refunds sellRefundPercent of the tower's cumulative cost.
/// Escape closes any open menu.
/// </summary>
public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] TowerBuildPopup popup;              // build menu (empty slot)
    [SerializeField] TowerUpgradePopup upgradePopup;     // upgrade/sell menu (built tower)

    [Header("Sell")]
    [Range(0f, 1f)]
    [SerializeField] float sellRefundPercent = 0.6f;     // % of the Cumulative Cost refunded on sale

    TowerSlot _activeSlot = null;   // the slot whose menu is currently open

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Build menu (empty slot) ──
    public void OpenPopup(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        popup.Show(slot);
    }

    // ── Upgrade/sell menu (built tower) ──
    public void OpenUpgrade(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        upgradePopup.Show(slot);
    }

    /// <summary>Closes both menus and deselects the active slot.</summary>
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

    /// <summary>Builds a tower on the active slot (called by TowerBuildPopup) if the player can afford it.</summary>
    public void OnTowerSelected(GameObject towerPrefab, int cost)
    {
        if (_activeSlot == null) { Debug.LogError("[TowerPlacement] _activeSlot is NULL"); return; }
        if (!CurrencyManager.Instance.CanAfford(cost)) { Debug.LogWarning("Not enough crystals"); return; }

        CurrencyManager.Instance.Spend(cost);
        _activeSlot.Build(towerPrefab);
        _activeSlot = null;
        popup.Hide();
    }

    /// <summary>Upgrades the tower in the selected slot to its next level, if one exists and is affordable.</summary>
    public void UpgradeActiveTower()
    {
        if (_activeSlot == null) return;

        var tower = _activeSlot.TowerComponent;
        if (tower == null) { Debug.LogWarning("[Upgrade] No Tower found in the slot"); return; }

        // Needs both the next level's CSV row (for the price) and the next-level prefab set on the tower.
        var next = TowerDatabase.Instance != null ? TowerDatabase.Instance.GetNextLevel(tower.TowerId) : null;
        GameObject nextPrefab = tower.NextLevelPrefab;
        if (next == null || nextPrefab == null)
        {
            Debug.Log($"[Upgrade] '{tower.TowerId}' is at max level, or no Next Level Prefab is set");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(next.cost)) { Debug.LogWarning("[Upgrade] Not enough crystals"); return; }

        CurrencyManager.Instance.Spend(next.cost);
        _activeSlot.ReplaceTower(nextPrefab,(this.gameObject),true);   // destroy the old tower and place the new-level prefab

        // Feedback: give the new tower a small bounce
        if (_activeSlot.placedTower != null)
        {
            _activeSlot.placedTower.transform.DOKill();
            _activeSlot.placedTower.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f).SetLink(_activeSlot.placedTower);
        }

        upgradePopup.Refresh();   // update the next price / button state
        Debug.Log($"[Upgrade] → {next.id} (paid {next.cost})");
    }

    /// <summary>Sells the tower in the selected slot and refunds part of what was spent on it.</summary>
    public void SellActiveTower()
    {
        if (_activeSlot == null) return;

        int refund = SellRefund(_activeSlot.TowerComponent);
        if (refund > 0) CurrencyManager.Instance.Earn(refund);

        _activeSlot.ClearTower();
        _activeSlot = null;
        upgradePopup.Hide();
        Debug.Log($"[Sell] Refunded {refund} crystal");
    }

    /// <summary>Refund for selling a tower: sellRefundPercent of its Cumulative Cost from towers.csv. Works for any IUpgradableTower.</summary>
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
            ClosePopup();   // Esc closes the menu
    }
}
