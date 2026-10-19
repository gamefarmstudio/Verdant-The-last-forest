using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD bar for the pollution gauge. Updates a Slider (0..1) and tints its fill
/// with a gradient (e.g. green -> red) whenever GameEvents.OnPollutionChanged fires.
/// </summary>
public class PollutionGaugeUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient dangerColor;   // colour by gauge value, 0 = clean, 1 = full

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
