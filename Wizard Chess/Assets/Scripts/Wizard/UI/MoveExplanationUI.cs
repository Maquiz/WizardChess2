using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that displays explanations for why moves are invalid.
/// Shows a tooltip when hovering over squares that the selected piece cannot move to.
/// </summary>
public class MoveExplanationUI : MonoBehaviour
{
    // UI References (created dynamically if not assigned)
    private Canvas tooltipCanvas;
    private GameObject tooltipPanel;
    private Text tooltipText;

    // Settings
    public float showDelay = 0.3f;          // Wait before showing tooltip
    public Vector2 tooltipOffset = new Vector2(15f, -15f); // Offset from mouse cursor

    // State
    private float hoverTimer;
    private (int x, int y) lastHoveredSquare = (-1, -1);
    private bool isShowingTooltip;
    private PieceMove trackedPiece;         // Piece we're tracking rejections for
    private GameMaster gm;

    void Start()
    {
        gm = GetComponent<GameMaster>();
        if (gm == null)
        {
            gm = FindObjectOfType<GameMaster>();
        }

        CreateTooltipUI();
        HideTooltip();
    }

    void Update()
    {
        // Only show tooltips when a piece is selected
        if (gm == null || !gm.isPieceSelected || gm.selectedPiece == null)
        {
            HideTooltip();
            return;
        }

        // Track the currently selected piece
        if (trackedPiece != gm.selectedPiece)
        {
            trackedPiece = gm.selectedPiece;
            HideTooltip();
        }

        // Raycast to find hovered square
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Square hoveredSquare = null;

            if (hit.collider.gameObject.tag == "Board")
            {
                hoveredSquare = hit.collider.gameObject.GetComponent<Square>();
            }
            else if (hit.collider.gameObject.tag == "Piece")
            {
                PieceMove p = hit.collider.gameObject.GetComponent<PieceMove>();
                if (p != null) hoveredSquare = p.curSquare;
            }

            if (hoveredSquare != null)
            {
                HandleSquareHover(hoveredSquare.x, hoveredSquare.y);
            }
            else
            {
                HideTooltip();
                lastHoveredSquare = (-1, -1);
            }
        }
        else
        {
            HideTooltip();
            lastHoveredSquare = (-1, -1);
        }

        // Update tooltip position to follow mouse
        if (isShowingTooltip && tooltipPanel != null)
        {
            UpdateTooltipPosition();
        }
    }

    private void HandleSquareHover(int x, int y)
    {
        // Check if this is a valid move (don't show tooltip for valid moves)
        // Use IsMoveValid() instead of checkMoves() to avoid side effect of showing move indicators
        if (trackedPiece != null && trackedPiece.IsMoveValid(x, y))
        {
            HideTooltip();
            lastHoveredSquare = (-1, -1);
            return;
        }

        // Check if we moved to a new square
        if (lastHoveredSquare.x != x || lastHoveredSquare.y != y)
        {
            lastHoveredSquare = (x, y);
            hoverTimer = 0f;
            HideTooltip();
        }

        // Accumulate hover time
        hoverTimer += Time.deltaTime;

        // Show tooltip after delay
        if (hoverTimer >= showDelay && !isShowingTooltip)
        {
            string explanation = MoveRejectionTracker.GetExplanation(x, y);
            if (!string.IsNullOrEmpty(explanation))
            {
                ShowTooltip(explanation);
            }
        }
    }

    private void ShowTooltip(string text)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        isShowingTooltip = true;

        // Force layout rebuild to get correct size
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.GetComponent<RectTransform>());

        UpdateTooltipPosition();
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        isShowingTooltip = false;
        hoverTimer = 0f;
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipPanel == null) return;

        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        Vector2 mousePos = Input.mousePosition;

        // Position tooltip near the mouse cursor
        Vector2 targetPos = mousePos + tooltipOffset;

        // Keep tooltip on screen
        float panelWidth = rt.rect.width;
        float panelHeight = rt.rect.height;

        if (targetPos.x + panelWidth > Screen.width)
        {
            targetPos.x = mousePos.x - panelWidth - 10f;
        }
        if (targetPos.y - panelHeight < 0)
        {
            targetPos.y = mousePos.y + panelHeight + 10f;
        }

        rt.position = targetPos;
    }

    private void CreateTooltipUI()
    {
        // Create a screen-space overlay canvas for the tooltip
        GameObject canvasObj = new GameObject("MoveExplanationCanvas");
        canvasObj.transform.SetParent(transform);

        tooltipCanvas = canvasObj.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 100; // Render on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create tooltip panel
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(canvasObj.transform);

        RectTransform panelRT = tooltipPanel.AddComponent<RectTransform>();
        panelRT.pivot = new Vector2(0, 1); // Top-left pivot

        // Background image
        Image bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent

        // Horizontal layout for padding
        HorizontalLayoutGroup hlg = tooltipPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = tooltipPanel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;

        tooltipText = textObj.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = 16;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAnchor.MiddleLeft;
        tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tooltipText.verticalOverflow = VerticalWrapMode.Overflow;

        // Set max width for text
        LayoutElement le = textObj.AddComponent<LayoutElement>();
        le.preferredWidth = 300;
    }

    /// <summary>
    /// Force show a tooltip for a specific square (called externally).
    /// </summary>
    public void ShowExplanation(int x, int y)
    {
        string explanation = MoveRejectionTracker.GetExplanation(x, y);
        if (!string.IsNullOrEmpty(explanation))
        {
            ShowTooltip(explanation);
        }
    }
}
