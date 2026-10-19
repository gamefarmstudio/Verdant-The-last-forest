using UnityEngine;
using DG.Tweening;

/// <summary>
/// General-purpose "play a tween on start" component for decoration and UI.
/// Tick the effects you want in the Inspector (move, scale, rotate, loop, shake);
/// they play once in Awake, or again whenever PlayTween() is called.
/// </summary>
public class DoTween : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private bool usingMoveTween = false;
    [SerializeField] private Transform targetTransform;   // the object that gets animated
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
    [SerializeField] private int loopCount = 1;
    [SerializeField] private LoopType loopType = LoopType.Restart;

    [Header("Shake")]
    [SerializeField] private bool usingShakeTween = false;
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeStrength = 1f;

    void Awake()
    {
        PlayTween();
    }

    /// <summary>Stops running tweens and resets the target to local position 0, scale 1, no rotation.</summary>
    public void ResetTransform()
    {
        transform.DOKill();
        targetTransform.localPosition = Vector3.zero;
        targetTransform.localScale = Vector3.one;
        targetTransform.localRotation = Quaternion.identity;
    }

    /// <summary>Resets the target, then plays every enabled effect.</summary>
    public void PlayTween()
    {
        ResetTransform();
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

        // Loop replaces the one-shot move: finish it instantly, then loop a local move.
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