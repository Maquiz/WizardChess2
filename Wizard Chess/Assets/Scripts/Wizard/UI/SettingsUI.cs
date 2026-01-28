using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Reusable settings panel with resolution selector and fullscreen/windowed toggle.
/// Created at runtime and shown as a fullscreen overlay. Used by both MainMenuUI and InGameMenuUI.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    private GameObject overlay;
    private Text resolutionText;
    private Image fullscreenBtnBg;
    private Image windowedBtnBg;
    private Font uiFont;
    private System.Action onClose;

    private List<Resolution> uniqueResolutions;
    private int currentResolutionIndex;

    private static readonly Color ACTIVE_COLOR = new Color(0.2f, 0.5f, 0.2f);
    private static readonly Color INACTIVE_COLOR = new Color(0.3f, 0.3f, 0.35f);

    public bool IsVisible => overlay != null && overlay.activeSelf;

    public void Init(Canvas canvas, System.Action closeCallback)
    {
        onClose = closeCallback;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CollectResolutions();
        CreateUI(canvas);
        overlay.SetActive(false);
    }

    public void Show()
    {
        RefreshState();
        overlay.SetActive(true);
    }

    public void Hide()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    // ========== Resolution Collection ==========

    private void CollectResolutions()
    {
        uniqueResolutions = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        Resolution[] all = Screen.resolutions;
        for (int i = 0; i < all.Length; i++)
        {
            string key = all[i].width + "x" + all[i].height;
            if (!seen.Contains(key))
            {
                seen.Add(key);
                uniqueResolutions.Add(all[i]);
            }
        }

        // Find current resolution
        currentResolutionIndex = uniqueResolutions.Count - 1;
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            if (uniqueResolutions[i].width == Screen.width
                && uniqueResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }
    }

    // ========== UI Creation ==========

    private void CreateUI(Canvas canvas)
    {
        overlay = new GameObject("SettingsOverlay");
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.7f);

        // Center panel
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(overlay.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(420, 280);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Title
        CreateLabel(panel.transform, "Title", "SETTINGS", 26, FontStyle.Bold,
            Color.white, new Vector2(0, 110));

        // Resolution section
        CreateLabel(panel.transform, "ResLabel", "Resolution", 16, FontStyle.Normal,
            new Color(0.7f, 0.7f, 0.8f), new Vector2(0, 65));

        // [<] 1920 x 1080 [>]
        CreateArrowButton(panel.transform, "ResLeft", "<",
            new Vector2(-140, 35), OnResolutionLeft);

        GameObject resTextObj = new GameObject("ResolutionText");
        resTextObj.transform.SetParent(panel.transform, false);

        RectTransform resTextRt = resTextObj.AddComponent<RectTransform>();
        resTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        resTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        resTextRt.pivot = new Vector2(0.5f, 0.5f);
        resTextRt.anchoredPosition = new Vector2(0, 35);
        resTextRt.sizeDelta = new Vector2(220, 35);

        resolutionText = resTextObj.AddComponent<Text>();
        resolutionText.font = uiFont;
        resolutionText.fontSize = 18;
        resolutionText.color = Color.white;
        resolutionText.alignment = TextAnchor.MiddleCenter;

        CreateArrowButton(panel.transform, "ResRight", ">",
            new Vector2(140, 35), OnResolutionRight);

        // Display Mode section
        CreateLabel(panel.transform, "ModeLabel", "Display Mode", 16, FontStyle.Normal,
            new Color(0.7f, 0.7f, 0.8f), new Vector2(0, -10));

        GameObject fsBtnObj = CreateModeButton(panel.transform, "FullscreenBtn",
            "Fullscreen", new Vector2(-80, -42));
        fsBtnObj.GetComponent<Button>().onClick.AddListener(OnFullscreenClicked);
        fullscreenBtnBg = fsBtnObj.GetComponent<Image>();

        GameObject winBtnObj = CreateModeButton(panel.transform, "WindowedBtn",
            "Windowed", new Vector2(80, -42));
        winBtnObj.GetComponent<Button>().onClick.AddListener(OnWindowedClicked);
        windowedBtnBg = winBtnObj.GetComponent<Image>();

        // Back button
        CreateActionButton(panel.transform, "BackButton", "Back",
            new Color(0.4f, 0.4f, 0.4f), new Vector2(0, -105), OnBackClicked);
    }

    // ========== State ==========

    private void RefreshState()
    {
        // Update resolution text
        if (uniqueResolutions.Count > 0)
        {
            Resolution r = uniqueResolutions[currentResolutionIndex];
            resolutionText.text = r.width + " x " + r.height;
        }
        else
        {
            resolutionText.text = Screen.width + " x " + Screen.height;
        }

        // Update fullscreen/windowed highlight
        UpdateModeButtons();
    }

    private void UpdateModeButtons()
    {
        bool isFs = Screen.fullScreen;
        fullscreenBtnBg.color = isFs ? ACTIVE_COLOR : INACTIVE_COLOR;
        windowedBtnBg.color = isFs ? INACTIVE_COLOR : ACTIVE_COLOR;
    }

    // ========== Event Handlers ==========

    private void OnResolutionLeft()
    {
        if (uniqueResolutions.Count == 0) return;
        currentResolutionIndex--;
        if (currentResolutionIndex < 0)
            currentResolutionIndex = uniqueResolutions.Count - 1;
        ApplyResolution();
    }

    private void OnResolutionRight()
    {
        if (uniqueResolutions.Count == 0) return;
        currentResolutionIndex++;
        if (currentResolutionIndex >= uniqueResolutions.Count)
            currentResolutionIndex = 0;
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        Resolution r = uniqueResolutions[currentResolutionIndex];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        RefreshState();
    }

    private void OnFullscreenClicked()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
        UpdateModeButtons();
    }

    private void OnWindowedClicked()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
        UpdateModeButtons();
    }

    private void OnBackClicked()
    {
        Hide();
        if (onClose != null) onClose();
    }

    // ========== UI Helpers ==========

    private void CreateLabel(Transform parent, string name, string text,
        int fontSize, FontStyle style, Color color, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(380, 30);

        Text t = obj.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;
    }

    private void CreateArrowButton(Transform parent, string name, string label,
        Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(40, 35);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.4f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = textObj.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = 20;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
    }

    private GameObject CreateModeButton(Transform parent, string name, string label,
        Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(140, 38);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = INACTIVE_COLOR;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = textObj.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = 16;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;

        return btnObj;
    }

    private void CreateActionButton(Transform parent, string name, string label,
        Color bgColor, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(160, 40);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = textObj.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = 18;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;
    }
}
