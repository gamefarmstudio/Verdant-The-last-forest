using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// The build menu that pops up next to an empty TowerSlot.
/// Shows up to 4 tower options with icons and prices; prices come from towers.csv (via towerId),
/// falling back to the typed-in cost if the CSV isn't available. Options the player can't afford are dimmed.
/// Slows the game to 0.1x while open and restores the chosen speed when closed.
/// Clicking outside the menu closes it.
/// </summary>
public class TowerBuildPopup : MonoBehaviour
{
    /// <summary>One buildable tower in this level's menu.</summary>
    [System.Serializable]
    public class TowerOption
    {
        public string towerName;
        public string towerNameTH;
        public Sprite icon;
        public GameObject prefab;
        public int cost;           // fallback only, used if the CSV can't be loaded / the ID isn't found
        public string towerId;     // ID in towers.csv (e.g. T1B1) — the real price is read from this row
    }

    [Header("Towers buildable in this level")]
    public TowerOption[] towerOptions;

    [Header("UI Buttons (4 slots)")]
    [SerializeField] GameObject[] optionButtons;
    [SerializeField] Image[] btnIcons;
    [SerializeField] TMP_Text[] btnCosts;
    [SerializeField] GameObject cancelButton;

    [Header("Panel")]
    [SerializeField] RectTransform panelRect;

    /// <summary>Opens the menu next to the given slot.</summary>
    public void Show(TowerSlot slot)
    {
        Time.timeScale = 0.1f; // nearly pause the game while the menu is open
        gameObject.SetActive(true);
        SetupButtons();
        PositionNearSlot(slot.transform.position);

        panelRect.localScale = Vector3.zero;
        panelRect.DOKill();
        panelRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    /// <summary>Closes the menu with a shrink animation.</summary>
    public void Hide()
    {
        GameSpeed.RestoreCurrent(); // back to the speed the player chose (1x/2x) instead of always resetting to 1x
        //gameObject.SetActive(false);
        panelRect.DOKill();
        panelRect.DOScale(0f, 0.25f).SetEase(Ease.OutBack).SetUpdate(false);

    }

    // Price from towers.csv via towerId — falls back to opt.cost if the CSV isn't loaded.
    int ResolveCost(TowerOption opt)
    {
        if (!string.IsNullOrEmpty(opt.towerId) &&
            TowerDatabase.Instance != null && TowerDatabase.Instance.IsLoaded)
        {
            var rec = TowerDatabase.Instance.GetByID(opt.towerId);
            if (rec != null) return rec.cost;
        }
        return opt.cost;
    }

    // Fills in each button (icon, price) and dims the ones the player can't afford.
    void SetupButtons()
    {
        foreach (var btn in optionButtons) btn.SetActive(false);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i >= towerOptions.Length) break;

            var opt = towerOptions[i];
            optionButtons[i].SetActive(true);

            if (btnIcons != null && i < btnIcons.Length && opt.icon != null)
                btnIcons[i].sprite = opt.icon;

            int cost = ResolveCost(opt);   // read from the CSV first

            if (btnCosts != null && i < btnCosts.Length)
                btnCosts[i].text = cost.ToString();

            // Null-check CurrencyManager, in case the popup appears before the manager is ready.
            bool canAfford = CurrencyManager.Instance != null &&
                             CurrencyManager.Instance.CanAfford(cost);

            var btnComp = optionButtons[i].GetComponent<Button>();
            if (btnComp != null) btnComp.interactable = canAfford;

            var cg = optionButtons[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = canAfford ? 1f : 0.45f;
        }
    }

    // ── Wired to the option buttons' OnClick in the Inspector ──
    public void OnClickOption0() => SelectTower(0);
    public void OnClickOption1() => SelectTower(1);
    public void OnClickOption2() => SelectTower(2);
    public void OnClickOption3() => SelectTower(3);
    public void OnClickCancel() => TowerPlacementManager.Instance.ClosePopup();

    void SelectTower(int index)
    {
        if (index >= towerOptions.Length) return;
        var opt = towerOptions[index];
        // Charge the CSV price rather than the typed-in one.
        TowerPlacementManager.Instance.OnTowerSelected(opt.prefab, ResolveCost(opt));
    }

    // Places the panel next to the slot on screen.
    void PositionNearSlot(Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        var canvasRect = transform.parent.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPos);

        panelRect.anchoredPosition = localPos + new Vector2(0, -10f);
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

    // Click outside the menu -> close it.
    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                TowerPlacementManager.Instance.ClosePopup();
        }
    }

    // Re-check affordability whenever crystals change while the menu is open.
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

    void OnCrystalChanged(int _) => SetupButtons();
}
