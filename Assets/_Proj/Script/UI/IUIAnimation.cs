public interface IUIAnimation
{
    void PlayShow(System.Action onComplete = null);
    void PlayHide(System.Action onComplete = null);
}