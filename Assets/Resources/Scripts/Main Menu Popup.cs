using UnityEngine;
using DG.Tweening;

/// <summary>
/// A main-menu popup panel that hides itself on load and pops in/out with a scale animation.
/// Call ShowPopup / HidePopup from buttons.
/// </summary>
public class MainMenuPopup : MonoBehaviour
{
    [SerializeField] private Vector3 TargetScale = new Vector3(7f, 7f, 7f);   // scale when fully open

    void Awake()
    {
        gameObject.SetActive(false);   // start hidden
    }

    /// <summary>Activates the popup and scales it up from zero.</summary>
    public void ShowPopup()
    {
        transform.localScale = Vector3.zero;
        transform.DOKill();
        transform.DOScale(TargetScale, 0.3f).SetEase(Ease.OutBack);
        gameObject.SetActive(true);
    }

    /// <summary>Scales the popup down to zero, then deactivates it.</summary>
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
