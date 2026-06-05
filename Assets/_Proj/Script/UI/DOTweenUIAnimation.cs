using DG.Tweening;
using UnityEngine;

public class DOTweenUIAnimation : MonoBehaviour, IUIAnimation
{
    [SerializeField] UIAnimationType animationType = UIAnimationType.Fade;
    [SerializeField] float duration = 0.3f;
    [SerializeField] Ease easeShow = Ease.OutBack;
    [SerializeField] Ease easeHide = Ease.InBack;

    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Vector2 originalPosition;
    Tween currentTween;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void PlayShow(System.Action onComplete = null)
    {
        currentTween?.Kill();
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;

        switch (animationType)
        {
            case UIAnimationType.Fade:
                canvasGroup.alpha = 0;
                currentTween = canvasGroup.DOFade(1, duration)
                    .SetEase(easeShow)
                    .OnComplete(() => onComplete?.Invoke());
                break;

            case UIAnimationType.ScaleUp:
                canvasGroup.alpha = 0;
                rectTransform.localScale = Vector3.zero;
                currentTween = DOTween.Sequence()
                    .Join(canvasGroup.DOFade(1, duration).SetEase(easeShow))
                    .Join(rectTransform.DOScale(1, duration).SetEase(easeShow))
                    .OnComplete(() => onComplete?.Invoke());
                break;

            case UIAnimationType.SlideFromTop:
                SlideShow(new Vector2(0, Screen.height), onComplete); break;
            case UIAnimationType.SlideFromBottom:
                SlideShow(new Vector2(0, -Screen.height), onComplete); break;
            case UIAnimationType.SlideFromLeft:
                SlideShow(new Vector2(-Screen.width, 0), onComplete); break;
            case UIAnimationType.SlideFromRight:
                SlideShow(new Vector2(Screen.width, 0), onComplete); break;
        }
    }

    public void PlayHide(System.Action onComplete = null)
    {
        currentTween?.Kill();
        canvasGroup.blocksRaycasts = false;

        switch (animationType)
        {
            case UIAnimationType.Fade:
                currentTween = canvasGroup.DOFade(0, duration)
                    .SetEase(easeHide)
                    .OnComplete(() => onComplete?.Invoke());
                break;

            case UIAnimationType.ScaleUp:
                currentTween = DOTween.Sequence()
                    .Join(canvasGroup.DOFade(0, duration).SetEase(easeHide))
                    .Join(rectTransform.DOScale(0, duration).SetEase(easeHide))
                    .OnComplete(() => onComplete?.Invoke());
                break;

            case UIAnimationType.SlideFromTop:
                SlideHide(new Vector2(0, Screen.height), onComplete); break;
            case UIAnimationType.SlideFromBottom:
                SlideHide(new Vector2(0, -Screen.height), onComplete); break;
            case UIAnimationType.SlideFromLeft:
                SlideHide(new Vector2(-Screen.width, 0), onComplete); break;
            case UIAnimationType.SlideFromRight:
                SlideHide(new Vector2(Screen.width, 0), onComplete); break;
        }
    }

    void SlideShow(Vector2 offset, System.Action onComplete)
    {
        canvasGroup.alpha = 1;
        rectTransform.anchoredPosition = originalPosition + offset;
        currentTween = rectTransform
            .DOAnchorPos(originalPosition, duration)
            .SetEase(easeShow)
            .OnComplete(() => onComplete?.Invoke());
    }

    void SlideHide(Vector2 offset, System.Action onComplete)
    {
        currentTween = rectTransform
            .DOAnchorPos(originalPosition + offset, duration)
            .SetEase(easeHide)
            .OnComplete(() => onComplete?.Invoke());
    }
}