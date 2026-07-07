// ═══════════════════════════════════════════════════════════════
//  PollutionGaugeUI.cs — UI Slider แสดงค่าเกจมลพิษ
//  ตั้ง Slider เป็น Interactable = false (แสดงผลอย่างเดียว)
//  Gradient: 0 = เขียว (สะอาด) → 1 = ม่วงพิษ
// ═══════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;

public class PollutionGaugeUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient dangerColor;

    void OnEnable()
    {
        GameEvents.OnPollutionChanged += Refresh;
        if (PollutionManager.Instance != null) Refresh(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= Refresh;

    void Refresh(float gauge)
    {
        slider.value = gauge;
        if (fillImage) fillImage.color = dangerColor.Evaluate(gauge);
    }
}
