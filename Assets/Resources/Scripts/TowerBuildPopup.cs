using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Popup UI แสดงป้อมที่สร้างได้
/// ติดกับ: Canvas > TowerBuildPopup (Panel)
///
/// รองรับ 1–4 ป้อม — วันนี้ใส่แค่ป้อมธนู (Greenshot) ตัวเดียว
/// เพิ่มป้อมทีหลังแค่ drag prefab ใส่ towerOptions เพิ่ม
/// </summary>
public class TowerBuildPopup : MonoBehaviour
{
    [System.Serializable]
    public class TowerOption
    {
        public string towerName;      // ชื่อแสดงS
        public string towerNameTH;    // ชื่อไทย
        public Sprite icon;           // icon ใน popup
        public GameObject prefab;         // Prefab ที่จะ Instantiate
        public int cost;           // ราคา Crystal
    }

    [Header("ป้อมที่สร้างได้ในด่านนี้ (drag prefab มาใส่)")]
    public TowerOption[] towerOptions;    // วันนี้ใส่แค่ 1 ตัว

    [Header("UI Buttons (4 ช่อง — ไม่ใช้ก็ SetActive false)")]
    [SerializeField] GameObject[] optionButtons;   // Button TL, TR, BL, BR
    [SerializeField] Image[] btnIcons;         // Image icon แต่ละปุ่ม
    //[SerializeField] TMP_Text[] btnNames;         // ชื่อป้อม
    [SerializeField] TMP_Text[] btnCosts;         // ราคา
    [SerializeField] GameObject cancelButton;     // ปุ่ม X กลาง

    [Header("Panel")]
    [SerializeField] RectTransform panelRect;

    // ── แสดง popup ──
    public void Show(TowerSlot slot)
    {
        gameObject.SetActive(true);
        SetupButtons();
        PositionNearSlot(slot.transform.position);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── ตั้งค่าปุ่มตาม towerOptions ──
    void SetupButtons()
    {
        // ซ่อนทุกปุ่มก่อน
        foreach (var btn in optionButtons) btn.SetActive(false);

        // เปิดเฉพาะปุ่มที่มี option
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i >= towerOptions.Length) break;

            var opt = towerOptions[i];
            optionButtons[i].SetActive(true);

            // ใส่ข้อมูล
            if (btnIcons != null && i < btnIcons.Length && opt.icon != null)
                btnIcons[i].sprite = opt.icon;

            //if (btnNames != null && i < btnNames.Length)
                //btnNames[i].text = opt.towerNameTH != "" ? opt.towerNameTH : opt.towerName;

            if (btnCosts != null && i < btnCosts.Length)
                btnCosts[i].text = opt.cost.ToString();

            // Dim ปุ่มถ้าเงินไม่พอ
            bool canAfford = CurrencyManager.Instance.CanAfford(opt.cost);
            var btnComp = optionButtons[i].GetComponent<Button>();
            if (btnComp != null) btnComp.interactable = canAfford;

            var cg = optionButtons[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = canAfford ? 1f : 0.45f;
        }
    }

    // ── เรียกจากปุ่มแต่ละอัน (OnClick ใน Inspector) ──
    public void OnClickOption0() => SelectTower(0);
    public void OnClickOption1() => SelectTower(1);
    public void OnClickOption2() => SelectTower(2);
    public void OnClickOption3() => SelectTower(3);
    public void OnClickCancel() => TowerPlacementManager.Instance.ClosePopup();

    void SelectTower(int index)
    {
        if (index >= towerOptions.Length) return;
        var opt = towerOptions[index];
        TowerPlacementManager.Instance.OnTowerSelected(opt.prefab, opt.cost);
    }

    // ── วาง popup ใกล้ slot ──
    void PositionNearSlot(Vector3 worldPos)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // แปลงจาก screen → local position บน Canvas
        var canvasRect = transform.parent.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPos);

        panelRect.anchoredPosition = localPos + new Vector2(0, 90f);
        ClampToScreen();
    }

    void ClampToScreen()
    {
        // ป้องกัน popup ออกนอกขอบจอ
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        Vector2 pos = panelRect.anchoredPosition;

        if (corners[2].x > Screen.width) pos.x -= (corners[2].x - Screen.width);
        if (corners[0].x < 0) pos.x -= corners[0].x;
        if (corners[2].y > Screen.height) pos.y -= (corners[2].y - Screen.height);
        if (corners[0].y < 0) pos.y -= corners[0].y;

        panelRect.anchoredPosition = pos;
    }

    // ── คลิกนอก popup → ปิด ──
    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
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