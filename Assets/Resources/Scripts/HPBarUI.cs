using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HP bar for the crystal / base.
/// Subscribes to GameManager.OnBaseHPChanged itself and updates a Slider and optional text.
/// (Works around the fact that a UnityEvent&lt;int&gt; can't be wired directly to the float Slider.value in the Inspector.)
/// Usage: put it on a UI object and drag the Slider into its slot — the label and fillImage are optional.
/// </summary>
public class HPBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;    // optional: show as text, e.g. "18/20"

    [Header("Tint by HP (optional)")]
    [SerializeField] private bool tintFill = false;   // must be ticked to change colour — avoids a black fill from an empty gradient
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient hpColor;        // 1 = green (full), 0 = red (almost dead)

    private int _max = 1;

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[HPBarUI] GameManager.Instance not found — the HP bar will not update", this);
            return;
        }

        _max = Mathf.Max(1, gm.baseMaxHP);
        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = _max;
            slider.interactable = false;   // display only
        }

        gm.OnBaseHPChanged.AddListener(Refresh);

        // Before StartGame() BaseHP is still 0 — show it full so the bar isn't empty for a frame.
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
