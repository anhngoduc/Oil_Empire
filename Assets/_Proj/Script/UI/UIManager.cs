using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [System.Serializable]
    public struct UIEntry
    {
        public UIType type;
        public UIPanel panel;
    }

    [SerializeField] List<UIEntry> uiEntries;

    Dictionary<UIType, UIPanel> uiMap = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var entry in uiEntries)
            uiMap[entry.type] = entry.panel;
    }

    public void Show(UIType type)
    {
        if (uiMap.TryGetValue(type, out var panel))
            panel.Show();
    }

    public void Hide(UIType type)
    {
        if (uiMap.TryGetValue(type, out var panel))
            panel.Hide();
    }

    public void ShowOnly(UIType type)
    {
        HideAll();
        Show(type);
    }

    public void HideAll()
    {
        foreach (var panel in uiMap.Values)
            panel.Hide();
    }

    public void Toggle(UIType type)
    {
        if (uiMap.TryGetValue(type, out var panel))
        {
            if (panel.IsVisible) panel.Hide();
            else panel.Show();
        }
    }

    public bool IsVisible(UIType type)
    {
        return uiMap.TryGetValue(type, out var panel) && panel.IsVisible;
    }
}

/*// HUDPanel.cs
public class HUDPanel : UIPanel
{
    // thêm logic riêng của HUD ở đây
}

// PauseMenuPanel.cs
public class PauseMenuPanel : UIPanel
{
    protected override void Awake()
    {
        base.Awake();
        // thêm logic riêng
    }

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;
    }

    public override void Hide()
    {
        base.Hide();
        Time.timeScale = 1f;
    }


UIManager (GameObject)
├── HUDPanel
│   ├── HUDPanel.cs
│   ├── DOTweenUIAnimation.cs  → Fade
│   └── CanvasGroup
├── PauseMenuPanel
│   ├── PauseMenuPanel.cs
│   ├── DOTweenUIAnimation.cs  → ScaleUp
│   └── CanvasGroup
└── GameOverPanel
    ├── GameOverPanel.cs
    ├── DOTweenUIAnimation.cs  → SlideFromBottom
    └── CanvasGroup
}*/