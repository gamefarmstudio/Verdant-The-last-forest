using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using DG.Tweening;

// [แก้] เขียนใหม่ทั้งไฟล์ (ล้างคอมเมนต์เดิม) — ประเด็นหลัก: ราคาป้อมอ้างอิงจาก towers.csv
public class TowerBuildPopup : MonoBehaviour
{
    [System.Serializable]
    public class TowerOption
    {
        public string towerName;
        public string towerNameTH;
        public Sprite icon;
        public GameObject prefab;
        public int cost;          // [แก้] เหลือไว้เป็น fallback เมื่อ CSV โหลดไม่ได้/หา ID ไม่เจอ
        public string towerId;     // [เพิ่ม] ID ใน towers.csv (เช่น T1B1) — ราคาจริงดึงจากแถวนี้
    }

    [Header("ป้อมที่สร้างได้ในด่านนี้")]
    public TowerOption[] towerOptions;

    [Header("UI Buttons (4 ช่อง)")]
    [SerializeField] GameObject[] optionButtons;
    [SerializeField] Image[] btnIcons;
    [SerializeField] TMP_Text[] btnCosts;
    [SerializeField] GameObject cancelButton;

    [Header("Panel")]
    [SerializeField] RectTransform panelRect;

    public void Show(TowerSlot slot)
    {
        gameObject.SetActive(true);
        SetupButtons();
        PositionNearSlot(slot.transform.position);

        panelRect.localScale = Vector3.zero;
        panelRect.DOKill();
        panelRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Hide()
    {
        //gameObject.SetActive(false);
        panelRect.DOKill();
        panelRect.DOScale(0f, 0.25f).SetEase(Ease.OutBack).SetUpdate(false);

    }

    // [เพิ่ม] คืนราคาจาก towers.csv ผ่าน towerId — ถ้าโหลด CSV ไม่ได้ค่อย fallback เป็น opt.cost
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

            int cost = ResolveCost(opt);   // [แก้] เดิมใช้ opt.cost ตรงๆ ตอนนี้ดึงจาก CSV ก่อน

            if (btnCosts != null && i < btnCosts.Length)
                btnCosts[i].text = cost.ToString();

            // [แก้] เช็ค null ก่อนใช้ CurrencyManager.Instance กัน NRE ตอน popup โผล่ก่อน manager พร้อม
            bool canAfford = CurrencyManager.Instance != null &&
                             CurrencyManager.Instance.CanAfford(cost);

            var btnComp = optionButtons[i].GetComponent<Button>();
            if (btnComp != null) btnComp.interactable = canAfford;

            var cg = optionButtons[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = canAfford ? 1f : 0.45f;
        }
    }

    public void OnClickOption0() => SelectTower(0);
    public void OnClickOption1() => SelectTower(1);
    public void OnClickOption2() => SelectTower(2);
    public void OnClickOption3() => SelectTower(3);
    public void OnClickCancel() => TowerPlacementManager.Instance.ClosePopup();

    void SelectTower(int index)
    {
        if (index >= towerOptions.Length) return;
        var opt = towerOptions[index];
        // [แก้] หักเงินตามราคาจาก CSV แทนราคาที่กรอกมือ
        TowerPlacementManager.Instance.OnTowerSelected(opt.prefab, ResolveCost(opt));
    }

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

    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                TowerPlacementManager.Instance.ClosePopup();
        }
    }

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
