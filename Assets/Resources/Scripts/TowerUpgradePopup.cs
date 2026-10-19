using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Verdant.Combat;

/// <summary>
/// Upgrade / sell menu — pops up when clicking a built tower (the counterpart of TowerBuildPopup).
///   Upgrade button: next level from towers.csv (TowerDatabase.GetNextLevel) — dimmed if unaffordable or at max level.
///   Sell button: refunds a % of the tower's Cumulative Cost and destroys the tower.
/// Usage: put it on a Panel in the same Canvas as TowerBuildPopup, wire up the buttons/texts, and drag it into TowerPlacementManager.
/// Slows the game to 0.1x while open and restores the chosen speed when closed.
/// </summary>
public class TowerUpgradePopup : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button upgradeButton;
    [SerializeField] Button sellButton;
    [SerializeField] TMP_Text upgradeCostText;   // upgrade price, or "MAX"
    [SerializeField] TMP_Text sellText;          // refund when selling, e.g. "+120"

    [Header("Panel")]
    [SerializeField] RectTransform panelRect;

    private TowerSlot _slot;   // the slot this menu is open for

    /// <summary>Opens the menu for the given slot.</summary>
    public void Show(TowerSlot slot)
    {
        Time.timeScale = 0.1f; // nearly pause the game while the menu is open
        _slot = slot;
        gameObject.SetActive(true);
        Refresh();
        PositionNearSlot(slot.transform.position);
    }

    public void Hide()
    {
        GameSpeed.RestoreCurrent(); // back to the speed the player chose (1x/2x) instead of always resetting to 1x

        _slot = null;
        gameObject.SetActive(false);
    }

    /// <summary>Updates the texts and button states for the tower in the current slot.</summary>
    public void Refresh()
    {
        if (_slot == null) return;

        var tower = _slot.TowerComponent;
        var next = (tower != null && TowerDatabase.Instance != null)
            ? TowerDatabase.Instance.GetNextLevel(tower.TowerId)
            : null;
        bool hasNextPrefab = tower != null && tower.NextLevelPrefab != null;   // a next-level prefab is also required

        // ── Upgrade ── (needs the next level's CSV row + a next-level prefab + enough crystals)
        bool canUpgrade = next != null && hasNextPrefab && CurrencyManager.Instance != null &&
                          CurrencyManager.Instance.CanAfford(next.cost);
        if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
        if (upgradeCostText != null)
            upgradeCostText.text = (next == null || !hasNextPrefab) ? "MAX" : next.cost.ToString();

        // ── Sell ──
        if (sellText != null)
            sellText.text = "+" + TowerPlacementManager.Instance.SellRefund(tower);
    }

    // ── Wired to the buttons (OnClick in the Inspector) ──
    public void OnClickUpgrade() => TowerPlacementManager.Instance.UpgradeActiveTower();
    public void OnClickSell()    => TowerPlacementManager.Instance.SellActiveTower();

    // ── Place the menu next to the slot (same as TowerBuildPopup) ──
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

    // Pushes the panel back inside the screen if it would stick out.
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

    // ── Click outside the menu -> close ──
    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                TowerPlacementManager.Instance.ClosePopup();
        }
    }

    // Refresh the buttons whenever crystals change (dim/undim upgrade)
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
