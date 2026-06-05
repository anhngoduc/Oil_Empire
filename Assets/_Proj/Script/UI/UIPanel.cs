using UnityEngine;

public abstract class UIPanel : MonoBehaviour
{
    IUIAnimation uiAnimation;
    bool isVisible = false;

    protected virtual void Awake()
    {
        uiAnimation = GetComponent<IUIAnimation>();
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        isVisible = true;

        if (uiAnimation != null)
            uiAnimation.PlayShow();
    }

    public virtual void Hide()
    {
        isVisible = false;

        if (uiAnimation != null)
            uiAnimation.PlayHide(onComplete: () => gameObject.SetActive(false));
        else
            gameObject.SetActive(false);
    }

    public bool IsVisible => isVisible;
}