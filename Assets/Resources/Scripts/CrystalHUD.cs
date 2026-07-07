using UnityEngine;
using UnityEngine.UI;  // ← Legacy UI ใช้อันนี้ (ไม่ใช่ TMPro)
using TMPro;

/// <summary>
/// ติดกับ: Text GameObject บน HUD
/// ลาก Text component มาใส่ crystalText
/// </summary>
public class CrystalHUD : MonoBehaviour
{
    [SerializeField] TMP_Text crystalText;  // UnityEngine.UI.Text (Legacy)

    void Start()
    {
        // แสดงค่าตั้งต้นทันที
        crystalText.text = CurrencyManager.Instance.Crystal.ToString();

        // Subscribe — อัปเดตอัตโนมัติทุกครั้งที่เงินเปลี่ยน
        CurrencyManager.Instance.OnCrystalChanged.AddListener(OnCrystalChanged);
    }

    void OnCrystalChanged(int newAmount)
    {
        crystalText.text = newAmount.ToString();
    }

    void OnDestroy()
    {
        // Unsubscribe เมื่อ object ถูกทำลาย — ป้องกัน memory leak
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCrystalChanged.RemoveListener(OnCrystalChanged);
    }

    public void UpdateText(int newAmount)
    {
        crystalText.text = newAmount.ToString();
    }
}