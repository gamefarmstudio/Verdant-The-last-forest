using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;   // [เพิ่ม] ตรวจจับ hover (IPointerEnter/Exit)
using TMPro;
using DG.Tweening;                // [เพิ่ม] ใช้ DOTween ทำ animation ปุ่ม

// ปุ่มชำระล้างหมอก: กด → PollutionManager.TryCleanse(), dim เมื่อคริสตัลไม่พอ
// [เพิ่ม] hover แล้วปุ่มนูน (OutBack) + ยุบ-เด้งตอนคลิก ด้วย DOTween
[RequireComponent(typeof(Button))]
public class CleanseButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text costText;

    // [เพิ่ม] ค่าปรับความรู้สึกของ animation (แก้ใน Inspector ได้)
    [Header("Hover / Click Animation")]
    [SerializeField] private float hoverScale = 1.12f;   // นูนขึ้นกี่เท่าเมื่อเมาส์ค้าง
    [SerializeField] private float pressScale = 0.9f;    // ยุบลงตอนกด
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float pressDuration = 0.08f;

    private Vector3 _baseScale;   // [เพิ่ม] สเกลเดิมของปุ่ม ใช้เป็นฐานคำนวณ
    private bool _hovering;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        _baseScale = transform.localScale;   // [เพิ่ม] จำสเกลตั้งต้นไว้
    }

    void Start()
    {
        button.onClick.AddListener(OnClick);

        if (PollutionManager.Instance != null && costText != null)
            costText.text = PollutionManager.Instance.CleanseCost.ToString();

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCrystalChanged.AddListener(RefreshInteractable);
            RefreshInteractable(CurrencyManager.Instance.Crystal);
        }
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCrystalChanged.RemoveListener(RefreshInteractable);

        transform.DOKill();   // [เพิ่ม] หยุด tween กัน callback ยิงใส่ object ที่ถูกทำลาย
    }

    // [เพิ่ม] เมาส์เข้า → ปุ่มนูน (เฉพาะตอนกดได้)
    public void OnPointerEnter(PointerEventData e)
    {
        _hovering = true;
        if (button.interactable) ScaleTo(_baseScale * hoverScale, hoverDuration, Ease.OutBack);
    }

    // [เพิ่ม] เมาส์ออก → กลับสเกลเดิม
    public void OnPointerExit(PointerEventData e)
    {
        _hovering = false;
        ScaleTo(_baseScale, hoverDuration, Ease.OutBack);
    }

    void OnClick()
    {
        // ใช้เงินก่อน — อาจทำให้ปุ่มเปลี่ยนเป็นกดไม่ได้ (RefreshInteractable จะถูกเรียกทันที)
        PollutionManager.Instance?.TryCleanse();

        // [เพิ่ม] animation คลิก: ยุบลงแล้วเด้งกลับไปสเกลตามสถานะปัจจุบัน (hover/ปกติ)
        Vector3 target = (_hovering && button.interactable) ? _baseScale * hoverScale : _baseScale;
        transform.DOKill();
        DOTween.Sequence().SetLink(gameObject).SetUpdate(true)
            .Append(transform.DOScale(_baseScale * pressScale, pressDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(target, pressDuration * 1.6f).SetEase(Ease.OutBack));
    }

    void RefreshInteractable(int crystal)
    {
        if (PollutionManager.Instance == null) return;
        button.interactable = crystal >= PollutionManager.Instance.CleanseCost;

        // [เพิ่ม] ถ้ากลายเป็นกดไม่ได้ ให้หดกลับสเกลเดิม กันค้างนูนตอน hover อยู่
        if (!button.interactable) ScaleTo(_baseScale, hoverDuration, Ease.OutQuad);
    }

    // [เพิ่ม] helper: kill tween เดิมก่อนแล้วค่อย scale ใหม่ (กัน hover in/out รัวๆ ตีกัน)
    void ScaleTo(Vector3 target, float dur, Ease ease)
    {
        transform.DOKill();
        transform.DOScale(target, dur).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }
}
