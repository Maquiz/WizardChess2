using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Scrollable in-game log panel that displays moves, captures, abilities,
/// check/checkmate events. Sits on the right side of the screen.
/// Access via the static Log() method from anywhere.
/// </summary>
public class GameLogUI : MonoBehaviour
{
    private static GameLogUI instance;

    private Canvas canvas;
    private GameObject panelObj;
    private GameObject contentObj;
    private Text logText;
    private ScrollRect scrollRect;
    private List<string> entries = new List<string>();
    private bool needsScroll;

    // Track turn numbers for log formatting
    private GameMaster gm;
    private int lastLoggedTurn = -1;

    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
        instance = this;
        CreateUI();
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

        // Main panel — right side of screen
        panelObj = new GameObject("GameLogPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        // Anchor to right side, stretch vertically
        panelRt.anchorMin = new Vector2(1f, 0.05f);
        panelRt.anchorMax = new Vector2(1f, 0.95f);
        panelRt.pivot = new Vector2(1f, 0.5f);
        panelRt.anchoredPosition = new Vector2(-10f, 0f);
        panelRt.sizeDelta = new Vector2(260f, 0f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.06f, 0.1f, 0.8f);

        // Add a VerticalLayoutGroup to the panel for header + scroll area
        VerticalLayoutGroup panelLayout = panelObj.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(6, 6, 6, 6);
        panelLayout.spacing = 4f;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;

        // Header
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(panelObj.transform, false);

        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.minHeight = 28f;
        headerLayout.preferredHeight = 28f;

        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 14;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.text = "GAME LOG";

        // Divider line
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(panelObj.transform, false);

        RectTransform divRt = dividerObj.AddComponent<RectTransform>();
        LayoutElement divLayout = dividerObj.AddComponent<LayoutElement>();
        divLayout.minHeight = 1f;
        divLayout.preferredHeight = 1f;

        Image divImg = dividerObj.AddComponent<Image>();
        divImg.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);

        // ScrollRect area
        GameObject scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);

        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        LayoutElement scrollLayout = scrollObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Mask to clip content
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.01f); // Nearly invisible, needed for Mask
        Mask scrollMask = scrollObj.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;

        // Content container (grows as entries are added)
        contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);

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
        scrollbarObj.transform.SetParent(scrollObj.transform, false);
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

        // Initial entry
        AddEntry("<color=#666677>--- Game Started ---</color>");
    }
}
