using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [เพิ่ม] สคริปต์ใหม่: แถบเลือดของคริสตัล/ฐาน
//   subscribe GameManager.OnBaseHPChanged เองแล้วอัปเดต Slider / ข้อความ
//   (แก้ปัญหา UnityEvent<int> ต่อ Slider.value ที่เป็น float ตรงๆ ใน Inspector ไม่ได้)
// วิธีใช้: วางบน UI แล้วลาก Slider เข้า slot — ที่เหลือ (label / fillImage) เป็น option
public class HPBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;    // option: โชว์เป็นข้อความ เช่น "18/20"

    [Header("เปลี่ยนสีตามเลือด (option)")]
    [SerializeField] private bool tintFill = false;   // เปิดถึงจะแตะสี — กันเผลอทำ fill ดำเพราะ gradient ว่าง
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient hpColor;        // 1 = เขียว (เต็ม), 0 = แดง (ใกล้ตาย)

    private int _max = 1;

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[HPBarUI] ไม่พบ GameManager.Instance — แถบเลือดจะไม่อัปเดต", this);
            return;
        }

        _max = Mathf.Max(1, gm.baseMaxHP);
        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = _max;
            slider.interactable = false;   // โชว์อย่างเดียว
        }

        gm.OnBaseHPChanged.AddListener(Refresh);

        // ก่อน StartGame() ค่า BaseHP ยังเป็น 0 → โชว์เต็มไว้ก่อน กันเห็นแถบว่าง 1 เฟรม
        Refresh(gm.BaseHP > 0 ? gm.BaseHP : _max);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnBaseHPChanged.RemoveListener(Refresh);
    }

    void Refresh(int hp)
    {
        float t = _max > 0 ? (float)hp / _max : 0f;

        if (slider != null) slider.value = hp;
        if (label != null) label.text = $"{hp}/{_max}";
        if (tintFill && fillImage != null) fillImage.color = hpColor.Evaluate(t);
    }
}
