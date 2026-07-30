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
        if (targetTransform == null)
        {
            targetTransform = this.transform;
        }

        // สร้าง tween แค่ครั้งเดียวต่อชนิด แล้วใช้ SetLoops จัดการวนซ้ำ
        // แทนการสร้างซ้ำในลูป for ซึ่งจะได้ tween หลายตัวมาแย่งกันขยับ property เดียวกัน
        // และทำให้ loopCount = -1 (infinite) ไม่ทำงานเลยเพราะ for-loop ไม่รันแม้แต่รอบเดียว
        if (usingMoveTween)
        {
            Tween moveTween = targetTransform.DOMove(targetPosition, duration).SetEase(easeType);
            if (usingLoop) moveTween.SetLoops(loopCount, loopType);
        }

        if (usingScaleTween)
        {
            Tween scaleTween = targetTransform.DOScale(targetScale, duration).SetEase(easeType);
            if (usingLoop) scaleTween.SetLoops(loopCount, loopType);
        }

        if (usingRotationTween)
        {
            Tween rotateTween = targetTransform.DORotate(targetRotation, duration).SetEase(easeType);
            if (usingLoop) rotateTween.SetLoops(loopCount, loopType);
        }

        if (usingShakeTween)
        {
            Tween shakeTween = targetTransform.DOShakePosition(shakeDuration, shakeStrength).SetEase(easeType);
            if (usingLoop) shakeTween.SetLoops(loopCount, loopType);
        }
    }
}