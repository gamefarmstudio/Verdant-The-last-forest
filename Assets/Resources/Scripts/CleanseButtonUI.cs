using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;   
using TMPro;
using DG.Tweening;                

[RequireComponent(typeof(Button))]
public class CleanseButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text costText;

    [Header("Hover / Click Animation")]
    [SerializeField] private float hoverScale = 1.12f;  
    [SerializeField] private float pressScale = 0.9f;   
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private float pressDuration = 0.08f;

    private Vector3 _baseScale;
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

        transform.DOKill();  
    }

    public void OnPointerEnter(PointerEventData e)
    {
        _hovering = true;
        if (button.interactable) ScaleTo(_baseScale * hoverScale, hoverDuration, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovering = false;
        ScaleTo(_baseScale, hoverDuration, Ease.OutBack);
    }

    void OnClick()
    {
    
        PollutionManager.Instance?.TryCleanse();

        
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

        if (!button.interactable) ScaleTo(_baseScale, hoverDuration, Ease.OutQuad);
    }

    void ScaleTo(Vector3 target, float dur, Ease ease)
    {
        transform.DOKill();
        transform.DOScale(target, dur).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }
}
