using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Root menu controller for the MainMenu scene.
/// Manages 6 panels: Title, DeckSelect, DeckEditor, PieceExamine, AIMatch, OnlineMatch.
/// Creates Canvas + EventSystem at runtime.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // Runtime-created UI
    private Canvas canvas;
    private GameObject canvasObj;
    private GameObject eventSystemObj;

    // Panel references
    private GameObject titlePanel;
    private GameObject howToPlayPanel;
    private DeckSelectPanel deckSelectPanel;
    private DeckEditorPanel deckEditorPanel;
    private PieceExaminePanel pieceExaminePanel;
    private AIMatchPanel aiMatchPanel;
    private GameObject aiMatchPanelObj;
    private OnlineMatchPanel onlineMatchPanel;
    private GameObject onlineMatchPanelObj;
    private SettingsUI settingsUI;

    // Shared data
    private DeckSaveData deckData;

    void Start()
    {
        deckData = DeckPersistence.Load();
        CreateCanvas();
        CreateTitlePanel();
        CreateHowToPlayPanel();
        CreateDeckSelectPanel();
        CreateDeckEditorPanel();
        CreatePieceExaminePanel();
        CreateAIMatchPanel();
        CreateOnlineMatchPanel();
        CreateSettingsUI();

        ShowTitlePanel();
    }

    private void CreateCanvas()
    {
        canvasObj = new GameObject("MenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem (uses New Input System's UI input module)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();
        }
    }

    // ========== Panel Show/Hide ==========

    public void ShowTitlePanel()
    {
        titlePanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(false);
        if (aiMatchPanelObj != null) aiMatchPanelObj.SetActive(false);
        if (onlineMatchPanelObj != null) onlineMatchPanelObj.SetActive(false);
        if (settingsUI != null) settingsUI.Hide();
    }

    public void ShowHowToPlayPanel()
    {
        titlePanel.SetActive(false);
        howToPlayPanel.SetActive(true);
    }

    public void ReturnFromHowToPlay()
    {
        ShowTitlePanel();
    }

    public void ShowDeckSelectPanel()
    {
        titlePanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(true);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(false);
        deckSelectPanel.Open(deckData);
    }

    public void ShowDeckEditorPanel(int slotIndex)
    {
        titlePanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(true);
        pieceExaminePanel.gameObject.SetActive(false);
        deckEditorPanel.Open(slotIndex, deckData);
    }

    public void ShowPieceExaminePanel(int preselectedElement = -1, int preselectedPieceType = -1)
    {
        titlePanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(true);
        pieceExaminePanel.Open(preselectedElement, preselectedPieceType);
    }

    public void ShowPieceExamineFromEditor(int preselectedElement, int preselectedPieceType)
    {
        // Show examine panel but allow returning to editor
        pieceExaminePanel.gameObject.SetActive(true);
        pieceExaminePanel.SetReturnToEditor(true);
        pieceExaminePanel.Open(preselectedElement, preselectedPieceType);
    }

    public void ReturnToDeckEditor()
    {
        pieceExaminePanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(true);
    }

    public void ReturnFromDeckEditor()
    {
        // Reload data in case it was saved
        deckData = DeckPersistence.Load();
        ShowTitlePanel();
    }

    public void ReturnFromDeckSelect()
    {
        ShowTitlePanel();
    }

    public void ReturnFromPieceExamine()
    {
        ShowTitlePanel();
    }

    public void ShowAIMatchPanel()
    {
        titlePanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(false);
        aiMatchPanelObj.SetActive(true);
        aiMatchPanel.Open(deckData);
    }

    public void ReturnFromAIMatch()
    {
        ShowTitlePanel();
    }

    public void ShowOnlineMatchPanel()
    {
        titlePanel.SetActive(false);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(false);
        if (aiMatchPanelObj != null) aiMatchPanelObj.SetActive(false);
        onlineMatchPanelObj.SetActive(true);
        onlineMatchPanel.Open(deckData);
    }

    public void ReturnFromOnlineMatch()
    {
        ShowTitlePanel();
    }

    public void ShowSettings()
    {
        titlePanel.SetActive(false);
        if (settingsUI != null) settingsUI.Show();
    }

    public void ReturnFromSettings()
    {
        ShowTitlePanel();
    }

    public DeckSaveData GetDeckData()
    {
        return deckData;
    }

    public void ReloadDeckData()
    {
        deckData = DeckPersistence.Load();
    }

    // ========== Panel Creation ==========

    private void CreateTitlePanel()
    {
        titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = titlePanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Background
        Image bg = titlePanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.14f, 1f);

        // Scale for portrait/mobile
        bool isPortrait = Screen.height > Screen.width;
        int titleSize = isPortrait ? 36 : 48;
        int subtitleSize = isPortrait ? 16 : 20;
        Vector2 buttonSize = isPortrait ? new Vector2(260, 46) : new Vector2(280, 40);
        float buttonY = isPortrait ? 0.62f : 0.56f;
        float buttonStep = isPortrait ? -0.058f : -0.065f;

        // Title text
        GameObject titleObj = CreateText(titlePanel.transform, "TitleText",
            "WIZARD CHESS", titleSize, FontStyle.Bold, Color.white,
            new Vector2(0.5f, isPortrait ? 0.82f : 0.75f), new Vector2(0.5f, 0.5f), new Vector2(600, 80));

        // Subtitle
        GameObject subtitleObj = CreateText(titlePanel.transform, "SubtitleText",
            "Choose Your Elements", subtitleSize, FontStyle.Italic, new Color(0.7f, 0.7f, 0.8f),
            new Vector2(0.5f, isPortrait ? 0.74f : 0.65f), new Vector2(0.5f, 0.5f), new Vector2(400, 40));

        // Buttons
        CreateButton(titlePanel.transform, "PlayButton", "Play Match",
            new Vector2(0.5f, buttonY), buttonSize,
            new Color(0.2f, 0.5f, 0.2f), () => ShowDeckSelectPanel());

        CreateButton(titlePanel.transform, "AIButton", "Play vs AI",
            new Vector2(0.5f, buttonY + buttonStep), buttonSize,
            new Color(0.5f, 0.2f, 0.5f), () => ShowAIMatchPanel());

        CreateButton(titlePanel.transform, "OnlineButton", "Play Online",
            new Vector2(0.5f, buttonY + buttonStep * 2), buttonSize,
            new Color(0.15f, 0.4f, 0.6f), () => ShowOnlineMatchPanel());

        CreateButton(titlePanel.transform, "ManageDecksButton", "Manage Decks",
            new Vector2(0.5f, buttonY + buttonStep * 3), buttonSize,
            new Color(0.3f, 0.3f, 0.6f), () => ShowDeckEditorPanel(0));

        CreateButton(titlePanel.transform, "ExamineButton", "Examine Pieces",
            new Vector2(0.5f, buttonY + buttonStep * 4), buttonSize,
            new Color(0.5f, 0.35f, 0.15f), () => ShowPieceExaminePanel());

        CreateButton(titlePanel.transform, "HowToPlayButton", "How to Play",
            new Vector2(0.5f, buttonY + buttonStep * 5), buttonSize,
            new Color(0.2f, 0.4f, 0.5f), () => ShowHowToPlayPanel());

        CreateButton(titlePanel.transform, "SettingsButton", "Settings",
            new Vector2(0.5f, buttonY + buttonStep * 6), buttonSize,
            new Color(0.4f, 0.4f, 0.45f), () => ShowSettings());

        CreateButton(titlePanel.transform, "QuitButton", "Quit",
            new Vector2(0.5f, buttonY + buttonStep * 7), buttonSize,
            new Color(0.5f, 0.15f, 0.15f), () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
    }

    private void CreateHowToPlayPanel()
    {
        bool isPortrait = Screen.height > Screen.width;

        howToPlayPanel = new GameObject("HowToPlayPanel");
        howToPlayPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = howToPlayPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Background
        Image bg = howToPlayPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.14f, 1f);

        // Scroll view for content
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(howToPlayPanel.transform, false);

        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.12f);
        scrollRt.anchorMax = new Vector2(0.95f, 0.88f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.1f, 0.08f, 0.16f, 0.8f);

        Mask scrollMask = scrollObj.AddComponent<Mask>();
        scrollMask.showMaskGraphic = true;

        // Content container
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);

        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 15f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRt;

        // Title
        CreateHelpText(contentObj.transform, "WIZARD CHESS", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // What is it
        CreateHelpText(contentObj.transform, "WHAT IS IT?", 18, FontStyle.Bold, new Color(0.9f, 0.7f, 0.3f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "Chess with elemental powers.\nEach piece has magical abilities based on its element.", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        // How it works
        CreateHelpText(contentObj.transform, "HOW IT WORKS", 18, FontStyle.Bold, new Color(0.9f, 0.7f, 0.3f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "1. BUILD YOUR DECK\nChoose 3 elements for your army.\nEach element gives different pieces unique powers.", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "2. PLAY MODES\nLocal Match - Pass & play with a friend\nvs AI - Practice against the computer\nOnline - Match against other players", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "3. GAMEPLAY\nStandard chess rules apply\nPassive abilities trigger automatically\nActive abilities: Press Q (or long-press)\nAbilities have cooldowns", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        // Elements
        CreateHelpText(contentObj.transform, "ELEMENTS", 18, FontStyle.Bold, new Color(0.9f, 0.7f, 0.3f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "FIRE - Aggressive, deals damage over time", 14, FontStyle.Normal, new Color(1f, 0.5f, 0.3f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "LIGHTNING - Speed, teleports and swaps", 14, FontStyle.Normal, new Color(1f, 1f, 0.4f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "EARTH - Defense, shields and fortifies", 14, FontStyle.Normal, new Color(0.6f, 0.8f, 0.4f), TextAnchor.MiddleCenter);

        // Tips
        CreateHelpText(contentObj.transform, "TIPS", 18, FontStyle.Bold, new Color(0.9f, 0.7f, 0.3f), TextAnchor.MiddleCenter);
        CreateHelpText(contentObj.transform, "Start with AI matches to learn abilities\nUse 'Examine Pieces' to see all abilities\nHover over squares to see why moves are blocked", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);

        // Footer
        CreateHelpText(contentObj.transform, "Win by checkmate!\nJust like chess, but with magic!", 16, FontStyle.Italic, new Color(0.7f, 0.7f, 0.9f), TextAnchor.MiddleCenter);

        // Back button
        CreateButton(howToPlayPanel.transform, "BackButton", "Back",
            new Vector2(0.5f, 0.05f), new Vector2(200, 45),
            new Color(0.4f, 0.3f, 0.3f), () => ReturnFromHowToPlay());

        howToPlayPanel.SetActive(false);
    }

    private void CreateHelpText(Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement le = textObj.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 10;
    }

    private void CreateDeckSelectPanel()
    {
        GameObject panelObj = new GameObject("DeckSelectPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panelObj.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f, 1f);

        deckSelectPanel = panelObj.AddComponent<DeckSelectPanel>();
        deckSelectPanel.Init(this);
    }

    private void CreateDeckEditorPanel()
    {
        GameObject panelObj = new GameObject("DeckEditorPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panelObj.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f, 1f);

        deckEditorPanel = panelObj.AddComponent<DeckEditorPanel>();
        deckEditorPanel.Init(this);
    }

    private void CreatePieceExaminePanel()
    {
        GameObject panelObj = new GameObject("PieceExaminePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panelObj.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f, 1f);

        pieceExaminePanel = panelObj.AddComponent<PieceExaminePanel>();
        pieceExaminePanel.Init(this);
    }

    private void CreateAIMatchPanel()
    {
        aiMatchPanelObj = new GameObject("AIMatchPanel");
        aiMatchPanelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = aiMatchPanelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        aiMatchPanelObj.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f, 1f);

        aiMatchPanel = aiMatchPanelObj.AddComponent<AIMatchPanel>();
        aiMatchPanel.Init(this);
    }

    private void CreateOnlineMatchPanel()
    {
        onlineMatchPanelObj = new GameObject("OnlineMatchPanel");
        onlineMatchPanelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = onlineMatchPanelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        onlineMatchPanelObj.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f, 1f);

        onlineMatchPanel = onlineMatchPanelObj.AddComponent<OnlineMatchPanel>();
        onlineMatchPanel.Init(this);
    }

    private void CreateSettingsUI()
    {
        settingsUI = gameObject.AddComponent<SettingsUI>();
        settingsUI.Init(canvas, ReturnFromSettings);
    }

    // ========== UI Helpers ==========

    public static GameObject CreateText(Transform parent, string name, string text,
        int fontSize, FontStyle style, Color color, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Text t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;

        return obj;
    }

    public static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        bool isPortrait = Screen.height > Screen.width;

        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Hover/click colors
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        colors.selectedColor = bgColor;
        btn.colors = colors;

        btn.onClick.AddListener(onClick);

        // Label - larger font on portrait for better readability
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);

        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        Text t = labelObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = isPortrait ? 20 : 18;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;

        return btnObj;
    }
}
