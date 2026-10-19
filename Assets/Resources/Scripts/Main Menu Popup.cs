using UnityEngine;
using DG.Tweening;

public class MainMenuPopup : MonoBehaviour
{
    [SerializeField] private Vector3 TargetScale = new Vector3(7f, 7f, 7f);
    void Awake()
    {
        gameObject.SetActive(false);
    }
    public void ShowPopup()
    {
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(TargetScale, 0.3f).SetEase(Ease.OutBack);
        gameObject.SetActive(true);
    }
    public void HidePopup()
    {
        transform.localScale = TargetScale;
        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
    
}
