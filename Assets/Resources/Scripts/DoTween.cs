using UnityEngine;
using DG.Tweening;

public class DoTween : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private bool usingMoveTween = false;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float duration = 1f;

    [Header("Scale")]
    [SerializeField] private bool usingScaleTween = false;
    [SerializeField] private Vector3 targetScale;

    [Header("Rotation")]
    [SerializeField] private bool usingRotationTween = false;
    [SerializeField] private Vector3 targetRotation;

    [Header("Ease")]
    [SerializeField] private Ease easeType = Ease.Linear;

    [Header("Loop")]
    [SerializeField] private bool usingLoop = false;
    [Tooltip("If You choose -1 for infinite loops, the tween will loop forever.")]
    [SerializeField] private int loopCount = 1; // -1 for infinite loops
    [SerializeField] private LoopType loopType = LoopType.Restart;

    [Header("Shake")]
    [SerializeField] private bool usingShakeTween = false;
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeStrength = 1f;

    void Awake()
    {
        PlayTween();
    }
    public void ResetTransform()
    {
        targetTransform.localPosition = Vector3.zero;
        targetTransform.localScale = Vector3.one;
        targetTransform.localRotation = Quaternion.identity;
    }
    public void PlayTween()
    {
        if (usingMoveTween)
        {
            targetTransform.DOMove(targetPosition, duration).SetEase(easeType);
        }

        if (usingScaleTween)
        {
            targetTransform.DOScale(targetScale, duration).SetEase(easeType);
        }

        if (usingRotationTween)
        {
            targetTransform.DORotate(targetRotation, duration).SetEase(easeType);
        }

        if (usingLoop)
        {
            targetTransform.DOComplete();
            targetTransform.DOLocalMove(targetPosition, duration).SetLoops(loopCount, loopType).SetEase(easeType);
        }

        if (usingShakeTween)
        {
            targetTransform.DOShakePosition(shakeDuration, shakeStrength).SetEase(easeType);
        }
    }
}