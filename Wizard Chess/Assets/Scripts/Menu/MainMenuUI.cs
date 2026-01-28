using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    private DeckSelectPanel deckSelectPanel;
    private DeckEditorPanel deckEditorPanel;
    private PieceExaminePanel pieceExaminePanel;
    private AIMatchPanel aiMatchPanel;
    private GameObject aiMatchPanelObj;
    private OnlineMatchPanel onlineMatchPanel;
    private GameObject onlineMatchPanelObj;

    // Shared data
    private DeckSaveData deckData;

    void Start()
    {
        deckData = DeckPersistence.Load();
        CreateCanvas();
        CreateTitlePanel();
        CreateDeckSelectPanel();
        CreateDeckEditorPanel();
        CreatePieceExaminePanel();
        CreateAIMatchPanel();
        CreateOnlineMatchPanel();

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

        // EventSystem
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ========== Panel Show/Hide ==========

    public void ShowTitlePanel()
    {
        titlePanel.SetActive(true);
        deckSelectPanel.gameObject.SetActive(false);
        deckEditorPanel.gameObject.SetActive(false);
        pieceExaminePanel.gameObject.SetActive(false);
        if (aiMatchPanelObj != null) aiMatchPanelObj.SetActive(false);
        if (onlineMatchPanelObj != null) onlineMatchPanelObj.SetActive(false);
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

        // Title text
        GameObject titleObj = CreateText(titlePanel.transform, "TitleText",
            "WIZARD CHESS", 48, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(600, 80));

        // Subtitle
        GameObject subtitleObj = CreateText(titlePanel.transform, "SubtitleText",
            "Choose Your Elements", 20, FontStyle.Italic, new Color(0.7f, 0.7f, 0.8f),
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(400, 40));

        // Buttons
        float buttonY = 0.48f;
        float buttonStep = -0.1f;

        CreateButton(titlePanel.transform, "PlayButton", "Play Match",
            new Vector2(0.5f, buttonY), new Vector2(280, 50),
            new Color(0.2f, 0.5f, 0.2f), () => ShowDeckSelectPanel());

        CreateButton(titlePanel.transform, "AIButton", "Play vs AI",
            new Vector2(0.5f, buttonY + buttonStep), new Vector2(280, 50),
            new Color(0.5f, 0.2f, 0.5f), () => ShowAIMatchPanel());

        CreateButton(titlePanel.transform, "OnlineButton", "Play Online",
            new Vector2(0.5f, buttonY + buttonStep * 2), new Vector2(280, 50),
            new Color(0.15f, 0.4f, 0.6f), () => ShowOnlineMatchPanel());

        CreateButton(titlePanel.transform, "ManageDecksButton", "Manage Decks",
            new Vector2(0.5f, buttonY + buttonStep * 3), new Vector2(280, 50),
            new Color(0.3f, 0.3f, 0.6f), () => ShowDeckEditorPanel(0));

        CreateButton(titlePanel.transform, "ExamineButton", "Examine Pieces",
            new Vector2(0.5f, buttonY + buttonStep * 4), new Vector2(280, 50),
            new Color(0.5f, 0.35f, 0.15f), () => ShowPieceExaminePanel());

        CreateButton(titlePanel.transform, "QuitButton", "Quit",
            new Vector2(0.5f, buttonY + buttonStep * 5), new Vector2(280, 50),
            new Color(0.5f, 0.15f, 0.15f), () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
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

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);

        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        Text t = labelObj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = label;

        return btnObj;
    }
}
