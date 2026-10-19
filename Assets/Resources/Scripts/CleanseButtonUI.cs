using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// The "Cleanse" button: spends crystals to push the pollution gauge down
/// (calls PollutionManager.TryCleanse). Shows the cost, greys out when the player
/// can't afford it, and plays DOTween hover/click animations.
/// </summary>
[RequireComponent(typeof(Button))]
public class CleanseButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text costText;   // shows the cleanse price

    [Header("Hover / Click Animation")]
    [SerializeField] private float hoverScale = 1.12f;   // how much bigger the button grows on hover
    [SerializeField] private float pressScale = 0.9f;    // how much it shrinks when pressed
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float pressDuration = 0.08f;

    private Vector3 _baseScale;   // the button's original scale, used as the base for all animations
    private bool _hovering;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        _baseScale = transform.localScale;
    }

    void Start()
    {
        button.onClick.AddListener(OnClick);

        if (PollutionManager.Instance != null && costText != null)
            costText.text = PollutionManager.Instance.CleanseCost.ToString();

        // Re-check affordability every time the crystal amount changes.
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

        transform.DOKill();   // stop tweens so no callback runs on a destroyed object
    }

    // Mouse enters -> grow the button (only when it can be clicked).
    public void OnPointerEnter(PointerEventData e)
    {
        _hovering = true;
        if (button.interactable) ScaleTo(_baseScale * hoverScale, hoverDuration, Ease.OutBack);
    }

    // Mouse leaves -> back to the original scale.
    public void OnPointerExit(PointerEventData e)
    {
        _hovering = false;
        ScaleTo(_baseScale, hoverDuration, Ease.OutBack);
    }

    void OnClick()
    {
        // Spend first — this may make the button un-clickable (RefreshInteractable runs right away).
        PollutionManager.Instance?.TryCleanse();

        // Click animation: squash, then bounce back to the scale that matches the current state.
        Vector3 target = (_hovering && button.interactable) ? _baseScale * hoverScale : _baseScale;
        transform.DOKill();
        DOTween.Sequence().SetLink(gameObject).SetUpdate(true)
            .Append(transform.DOScale(_baseScale * pressScale, pressDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(target, pressDuration * 1.6f).SetEase(Ease.OutBack));
    }

    // Enable the button only when the player has enough crystals for one cleanse.
    void RefreshInteractable(int crystal)
    {
        if (PollutionManager.Instance == null) return;
        button.interactable = crystal >= PollutionManager.Instance.CleanseCost;

        // If it just became disabled, shrink back so it doesn't stay enlarged under the cursor.
        if (!button.interactable) ScaleTo(_baseScale, hoverDuration, Ease.OutQuad);
    }

    // Kill the previous tween before scaling, so rapid hover in/out doesn't fight itself.
    void ScaleTo(Vector3 target, float dur, Ease ease)
    {
        transform.DOKill();
        transform.DOScale(target, dur).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }
}
