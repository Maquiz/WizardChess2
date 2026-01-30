using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Scrollable in-game log panel that displays moves, captures, abilities,
/// check/checkmate events. Sits on the right side of the screen.
/// Access via the static Log() method from anywhere.
/// Can be collapsed/expanded via toggle button.
/// </summary>
public class GameLogUI : MonoBehaviour
{
    private static GameLogUI instance;

    private Canvas canvas;
    private GameObject panelObj;
    private GameObject contentObj;
    private GameObject scrollAreaObj;
    private GameObject toggleButton;
    private GameObject floatingToggleButton; // Shown when panel is collapsed
    private Text toggleButtonText;
    private Text logText;
    private ScrollRect scrollRect;
    private List<string> entries = new List<string>();
    private bool needsScroll;
    private bool isExpanded = true;

    // Track turn numbers for log formatting
    private GameMaster gm;
    private int lastLoggedTurn = -1;

    // Collapsed/expanded dimensions
    private const float EXPANDED_WIDTH = 220f;
    private const float EXPANDED_WIDTH_PORTRAIT = 160f;

    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
        instance = this;
        CreateUI();

        // Start collapsed on portrait mode (mobile)
        if (Screen.height > Screen.width)
        {
            SetExpanded(false);
        }
    }

    void LateUpdate()
    {
        // Auto-scroll to bottom after new entries
        if (needsScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
            needsScroll = false;
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// Toggle log panel expanded/collapsed state.
    /// </summary>
    public void ToggleExpanded()
    {
        SetExpanded(!isExpanded);
    }

    /// <summary>
    /// Set log panel expanded/collapsed state.
    /// </summary>
    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        // When collapsed, hide entire panel and show only floating toggle button
        if (panelObj != null)
        {
            panelObj.SetActive(isExpanded);
        }

        if (floatingToggleButton != null)
        {
            floatingToggleButton.SetActive(!isExpanded);
        }
    }

    /// <summary>
    /// Static method to toggle log visibility.
    /// </summary>
    public static void Toggle()
    {
        if (instance != null)
        {
            instance.ToggleExpanded();
        }
    }

    /// <summary>
    /// Add an entry to the game log. Call from anywhere.
    /// </summary>
    public static void Log(string message)
    {
        if (instance == null) return;
        instance.AddEntry(message);
    }

    /// <summary>
    /// Add a turn-stamped entry (prefixes with turn number + side).
    /// </summary>
    public static void LogMove(int turnNumber, int color, string message)
    {
        if (instance == null) return;

        int displayTurn = (turnNumber / 2) + 1; // Chess-style turn numbering
        string prefix = displayTurn + ". " + (color == ChessConstants.BLACK ? "..." : "");
        string colorTag = color == ChessConstants.WHITE ? "<color=#DDDDEE>" : "<color=#AABBCC>";
        instance.AddEntry(colorTag + prefix + message + "</color>");
    }

    /// <summary>
    /// Log a piece moving to a square.
    /// </summary>
    public static void LogPieceMove(int turnNumber, int color, PieceMove piece, int toX, int toY)
    {
        string pName = ShortPieceName(piece.piece);
        string square = SquareName(toX, toY);
        LogMove(turnNumber, color, pName + " " + square);
    }

    /// <summary>
    /// Log a piece capturing another piece.
    /// </summary>
    public static void LogCapture(int turnNumber, int color, PieceMove attacker, PieceMove victim, int toX, int toY)
    {
        string aName = ShortPieceName(attacker.piece);
        string vName = ShortPieceName(victim.piece);
        string square = SquareName(toX, toY);
        LogMove(turnNumber, color, aName + " x " + vName + " " + square);
    }

    /// <summary>
    /// Log an ability use.
    /// </summary>
    public static void LogAbility(int turnNumber, int color, PieceMove piece, int toX, int toY)
    {
        string pName = ShortPieceName(piece.piece);
        string square = SquareName(toX, toY);
        LogMove(turnNumber, color, pName + " ability " + square);
    }

    /// <summary>
    /// Log a game event (check, checkmate, etc.) with no turn prefix.
    /// </summary>
    public static void LogEvent(string message)
    {
        if (instance == null) return;
        instance.AddEntry(message);
    }

    // ========== Formatting Helpers ==========

    public static string ShortPieceName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN:   return "Pawn";
            case ChessConstants.ROOK:   return "Rook";
            case ChessConstants.KNIGHT: return "Knight";
            case ChessConstants.BISHOP: return "Bishop";
            case ChessConstants.QUEEN:  return "Queen";
            case ChessConstants.KING:   return "King";
            default: return "?";
        }
    }

    public static string SquareName(int x, int y)
    {
        return "" + (char)(65 + x) + (y + 1);
    }

    private void AddEntry(string message)
    {
        entries.Add(message);
        RefreshText();
        needsScroll = true;
    }

    private void RefreshText()
    {
        if (logText == null) return;

        // Keep last 200 entries to avoid unbounded growth
        if (entries.Count > 200)
        {
            entries.RemoveRange(0, entries.Count - 200);
        }

        logText.text = string.Join("\n", entries.ToArray());
    }

    // ========== UI Creation ==========

    private void CreateUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        bool isPortrait = Screen.height > Screen.width;
        float panelWidth = isPortrait ? EXPANDED_WIDTH_PORTRAIT : EXPANDED_WIDTH;

        // Main panel — right side of screen
        panelObj = new GameObject("GameLogPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        // Anchor to right side, stretch vertically (shorter to leave room for menu button)
        panelRt.anchorMin = new Vector2(1f, 0.12f);
        panelRt.anchorMax = new Vector2(1f, 0.95f);
        panelRt.pivot = new Vector2(1f, 0.5f);
        panelRt.anchoredPosition = new Vector2(-8f, 0f);
        panelRt.sizeDelta = new Vector2(panelWidth, 0f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.06f, 0.1f, 0.75f);

        // Add a VerticalLayoutGroup to the panel for header + scroll area
        VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(3, 3, 2, 3);
        panelLayout.spacing = 1f;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;

        // Header container (horizontal: toggle button + title)
        GameObject headerContainer = new GameObject("HeaderContainer");
        headerContainer.transform.SetParent(panelObj.transform, false);

        RectTransform headerContRt = headerContainer.AddComponent<RectTransform>();
        LayoutElement headerContLayout = headerContainer.AddComponent<LayoutElement>();
        headerContLayout.minHeight = 20f;
        headerContLayout.preferredHeight = 20f;

        HorizontalLayoutGroup headerHlg = headerContainer.AddComponent<HorizontalLayoutGroup>();
        headerHlg.spacing = 2f;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = true;
        headerHlg.childControlWidth = true;
        headerHlg.childControlHeight = true;
        headerHlg.childAlignment = TextAnchor.MiddleCenter;

        // Toggle button - compact square
        toggleButton = new GameObject("ToggleButton");
        toggleButton.transform.SetParent(headerContainer.transform, false);

        RectTransform toggleRt = toggleButton.AddComponent<RectTransform>();
        LayoutElement toggleLayout = toggleButton.AddComponent<LayoutElement>();
        toggleLayout.minWidth = 20f;
        toggleLayout.preferredWidth = 20f;

        Image toggleBg = toggleButton.AddComponent<Image>();
        toggleBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        Button toggleBtn = toggleButton.AddComponent<Button>();
        toggleBtn.targetGraphic = toggleBg;
        toggleBtn.onClick.AddListener(ToggleExpanded);

        // Toggle button text
        GameObject toggleTextObj = new GameObject("Text");
        toggleTextObj.transform.SetParent(toggleButton.transform, false);

        RectTransform toggleTextRt = toggleTextObj.AddComponent<RectTransform>();
        toggleTextRt.anchorMin = Vector2.zero;
        toggleTextRt.anchorMax = Vector2.one;
        toggleTextRt.offsetMin = Vector2.zero;
        toggleTextRt.offsetMax = Vector2.zero;

        toggleButtonText = toggleTextObj.AddComponent<Text>();
        toggleButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        toggleButtonText.fontSize = 11;
        toggleButtonText.fontStyle = FontStyle.Bold;
        toggleButtonText.color = Color.white;
        toggleButtonText.alignment = TextAnchor.MiddleCenter;
        toggleButtonText.text = "-";

        // Header text (flexible width) - compact
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(headerContainer.transform, false);

        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.flexibleWidth = 1f;

        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 9;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(0.7f, 0.7f, 0.8f, 1f);
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.text = "LOG";

        // ScrollRect area (stored for toggle functionality)
        scrollAreaObj = new GameObject("ScrollArea");
        scrollAreaObj.transform.SetParent(panelObj.transform, false);

        RectTransform scrollRt = scrollAreaObj.AddComponent<RectTransform>();
        LayoutElement scrollLayout = scrollAreaObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;

        scrollRect = scrollAreaObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Mask to clip content
        Image scrollBg = scrollAreaObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.01f); // Nearly invisible, needed for Mask
        Mask scrollMask = scrollAreaObj.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;

        // Content container (grows as entries are added)
        contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollAreaObj.transform, false);

        RectTransform contentRt = contentObj.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRt;

        // Log text — single Text component, vertically growing
        GameObject textObj = new GameObject("LogText");
        textObj.transform.SetParent(contentObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 1f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0f, 1f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter textFitter = textObj.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        logText = textObj.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 12;
        logText.color = new Color(0.75f, 0.75f, 0.8f, 1f);
        logText.alignment = TextAnchor.UpperLeft;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.supportRichText = true;
        logText.text = "";

        // Vertical scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(panelObj.transform, false);

        // Place scrollbar on the right edge of the scroll area
        scrollbarObj.transform.SetParent(scrollAreaObj.transform, false);
        RectTransform sbRt = scrollbarObj.AddComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1f, 0f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(1f, 0.5f);
        sbRt.anchoredPosition = new Vector2(8f, 0f);
        sbRt.sizeDelta = new Vector2(6f, 0f);

        Image sbBg = scrollbarObj.AddComponent<Image>();
        sbBg.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        Scrollbar sb = scrollbarObj.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar handle
        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = Vector2.zero;
        haRt.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.AddComponent<RectTransform>();
        hRt.anchorMin = Vector2.zero;
        hRt.anchorMax = Vector2.one;
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;

        Image hImg = handle.AddComponent<Image>();
        hImg.color = new Color(0.4f, 0.4f, 0.5f, 0.7f);

        sb.handleRect = hRt;
        sb.targetGraphic = hImg;
        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Create floating toggle button (shown when panel is collapsed)
        CreateFloatingToggleButton();

        // Initial entry
        AddEntry("<color=#666677>--- Game Started ---</color>");
    }

    private void CreateFloatingToggleButton()
    {
        floatingToggleButton = new GameObject("FloatingLogToggle");
        floatingToggleButton.transform.SetParent(canvas.transform, false);

        RectTransform rt = floatingToggleButton.AddComponent<RectTransform>();
        // Position in middle-right of screen (where expanded panel would be)
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-8f, 0f);
        rt.sizeDelta = new Vector2(32f, 32f);

        Image bg = floatingToggleButton.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        Button btn = floatingToggleButton.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(ToggleExpanded);

        // Plus text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(floatingToggleButton.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "+";

        // Start hidden (panel starts expanded, unless portrait mode)
        floatingToggleButton.SetActive(false);
    }
}
