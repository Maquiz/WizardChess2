using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// In-game pause menu with Resign, Offer a Draw, Exit to Main Menu, and Resume.
/// Accessible via Escape key or a bottom-right pause button.
/// Blocks board input while open. Attached to GameMaster object.
/// </summary>
public class InGameMenuUI : MonoBehaviour
{
    public bool IsMenuOpen { get; private set; }
    private bool isConfirmOpen;
    private bool isDrawOfferPending;
    private bool isDrawOfferReceived;

    private GameMaster gm;
    private Canvas canvas;

    // UI elements
    private GameObject menuButton;
    private GameObject menuOverlay;
    private GameObject confirmOverlay;
    private GameObject drawOfferOverlay;
    private Button drawButton;
    private Text drawButtonText;
    private SettingsUI settingsUI;

    private Font uiFont;

    void Start()
    {
        gm = GetComponent<GameMaster>();
        canvas = FindFirstObjectByType<Canvas>();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (canvas != null)
        {
            CreateMenuButton();
            CreateMenuOverlay();
            CreateConfirmDialog();
            CreateDrawOfferPopup();

            settingsUI = gameObject.AddComponent<SettingsUI>();
            settingsUI.Init(canvas, OnSettingsClose);
        }
    }

    void Update()
    {
        if (gm == null) return;

        bool isGameOver = gm.currentGameState == GameState.WhiteWins
                       || gm.currentGameState == GameState.BlackWins
                       || gm.currentGameState == GameState.Stalemate
                       || gm.currentGameState == GameState.Draw;

        // Auto-close everything when game ends
        if (isGameOver)
        {
            if (settingsUI != null && settingsUI.IsVisible) settingsUI.Hide();
            if (IsMenuOpen) CloseMenu();
            if (isConfirmOpen) CloseConfirmDialog();
            if (isDrawOfferReceived) CloseDrawOfferPopup();
            if (menuButton != null) menuButton.SetActive(false);
            return;
        }

        // Hide button during draft or before setup complete
        if (menuButton != null)
        {
            menuButton.SetActive(gm.isSetupComplete && !gm.isDraftPhase);
        }

        // Escape key handling
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If draw offer popup is showing, ignore Escape (must accept/decline)
            if (isDrawOfferReceived) return;

            // If settings panel is open, close it and return to menu
            if (settingsUI != null && settingsUI.IsVisible)
            {
                settingsUI.Hide();
                return;
            }

            if (isConfirmOpen)
            {
                // Close confirm dialog only
                CloseConfirmDialog();
            }
            else if (IsMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                // Only open during playable states
                if (gm.currentGameState == GameState.Playing
                    || gm.currentGameState == GameState.WhiteInCheck
                    || gm.currentGameState == GameState.BlackInCheck)
                {
                    OpenMenu();
                }
            }
        }
    }

    // ========== Menu Button (bottom-right) ==========

    private void CreateMenuButton()
    {
        menuButton = new GameObject("PauseMenuButton");
        menuButton.transform.SetParent(canvas.transform, false);

        RectTransform rt = menuButton.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-15, 15);
        rt.sizeDelta = new Vector2(50, 50);

        Image bg = menuButton.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.25f, 0.35f, 0.85f);

        Button btn = menuButton.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(OpenMenu);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(menuButton.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.text = "| |";

        menuButton.SetActive(false);
    }

    // ========== Menu Overlay ==========

    private void CreateMenuOverlay()
    {
        menuOverlay = new GameObject("InGameMenuOverlay");
        menuOverlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = menuOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayBg = menuOverlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.6f);

        // Center panel
        GameObject panel = new GameObject("MenuPanel");
        panel.transform.SetParent(menuOverlay.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(400, 310);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Title
        CreateText(panel.transform, "TitleText", "MENU", 26, FontStyle.Bold, Color.white,
            new Vector2(0f, 0.87f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        // Buttons stacked vertically
        float buttonWidth = 260;
        float buttonHeight = 40;
        float startY = 75;
        float spacing = 44;

        // Resign button (red)
        CreateMenuButton(panel.transform, "ResignButton", "Resign",
            new Color(0.6f, 0.15f, 0.15f), buttonWidth, buttonHeight,
            new Vector2(0, startY), OnResignClicked);

        // Offer a Draw button (amber)
        GameObject drawBtnObj = CreateMenuButton(panel.transform, "DrawButton", "Offer a Draw",
            new Color(0.6f, 0.5f, 0.1f), buttonWidth, buttonHeight,
            new Vector2(0, startY - spacing), OnDrawClicked);
        drawButton = drawBtnObj.GetComponent<Button>();
        drawButtonText = drawBtnObj.GetComponentInChildren<Text>();

        // Settings button (gray)
        CreateMenuButton(panel.transform, "SettingsButton", "Settings",
            new Color(0.4f, 0.4f, 0.45f), buttonWidth, buttonHeight,
            new Vector2(0, startY - spacing * 2), OnSettingsClicked);

        // Exit to Main Menu button (blue-gray)
        CreateMenuButton(panel.transform, "ExitButton", "Exit to Main Menu",
            new Color(0.3f, 0.3f, 0.5f), buttonWidth, buttonHeight,
            new Vector2(0, startY - spacing * 3), OnExitClicked);

        // Resume button (green)
        CreateMenuButton(panel.transform, "ResumeButton", "Resume",
            new Color(0.2f, 0.5f, 0.2f), buttonWidth, buttonHeight,
            new Vector2(0, startY - spacing * 4), CloseMenu);

        menuOverlay.SetActive(false);
    }

    // ========== Resign Confirmation Dialog ==========

    private void CreateConfirmDialog()
    {
        confirmOverlay = new GameObject("ResignConfirmOverlay");
        confirmOverlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = confirmOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayBg = confirmOverlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.5f);

        // Panel
        GameObject panel = new GameObject("ConfirmPanel");
        panel.transform.SetParent(confirmOverlay.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(380, 180);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.15f, 0.12f, 0.12f, 0.98f);

        // Question text
        CreateText(panel.transform, "ConfirmText", "Are you sure you want to resign?", 20, FontStyle.Normal,
            Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(20, 0), new Vector2(-20, -10));

        // Yes, Resign button (red)
        CreateMenuButton(panel.transform, "YesResignButton", "Yes, Resign",
            new Color(0.6f, 0.15f, 0.15f), 150, 45,
            new Vector2(-85, -55), OnConfirmResign);

        // Cancel button (gray)
        CreateMenuButton(panel.transform, "CancelButton", "Cancel",
            new Color(0.4f, 0.4f, 0.4f), 150, 45,
            new Vector2(85, -55), CloseConfirmDialog);

        confirmOverlay.SetActive(false);
    }

    // ========== Draw Offer Popup ==========

    private void CreateDrawOfferPopup()
    {
        drawOfferOverlay = new GameObject("DrawOfferOverlay");
        drawOfferOverlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = drawOfferOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayBg = drawOfferOverlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.5f);

        // Panel
        GameObject panel = new GameObject("DrawOfferPanel");
        panel.transform.SetParent(drawOfferOverlay.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(380, 180);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.15f, 0.12f, 0.98f);

        // Text
        CreateText(panel.transform, "DrawOfferText", "Opponent offers a draw", 20, FontStyle.Normal,
            Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(20, 0), new Vector2(-20, -10));

        // Accept Draw button (green)
        CreateMenuButton(panel.transform, "AcceptDrawButton", "Accept Draw",
            new Color(0.2f, 0.5f, 0.2f), 150, 45,
            new Vector2(-85, -55), OnAcceptDraw);

        // Decline button (red)
        CreateMenuButton(panel.transform, "DeclineButton", "Decline",
            new Color(0.6f, 0.15f, 0.15f), 150, 45,
            new Vector2(85, -55), OnDeclineDraw);

        drawOfferOverlay.SetActive(false);
    }

    // ========== Open / Close ==========

    public void OpenMenu()
    {
        // Exit ability mode and deselect piece if needed
        if (gm.abilityExecutor != null && gm.abilityExecutor.isInAbilityMode)
        {
            gm.abilityExecutor.ExitAbilityMode();
        }
        if (gm.isPieceSelected && gm.selectedPiece != null)
        {
            gm.selectedPiece.hideMovesHelper();
            gm.deSelectPiece();
        }

        IsMenuOpen = true;
        menuOverlay.SetActive(true);

        // Update draw button state
        if (drawButton != null && drawButtonText != null)
        {
            if (isDrawOfferPending)
            {
                drawButtonText.text = "Draw Offered...";
                drawButton.interactable = false;
            }
            else
            {
                drawButtonText.text = "Offer a Draw";
                drawButton.interactable = true;
            }
        }
    }

    public void CloseMenu()
    {
        IsMenuOpen = false;
        if (menuOverlay != null) menuOverlay.SetActive(false);
        if (isConfirmOpen) CloseConfirmDialog();
    }

    private void CloseConfirmDialog()
    {
        isConfirmOpen = false;
        if (confirmOverlay != null) confirmOverlay.SetActive(false);
    }

    private void CloseDrawOfferPopup()
    {
        isDrawOfferReceived = false;
        if (drawOfferOverlay != null) drawOfferOverlay.SetActive(false);
    }

    // ========== Button Handlers ==========

    private void OnSettingsClicked()
    {
        if (settingsUI != null) settingsUI.Show();
    }

    private void OnSettingsClose()
    {
        // Settings closed via Back button — menu stays open underneath
    }

    private void OnResignClicked()
    {
        isConfirmOpen = true;
        confirmOverlay.SetActive(true);
    }

    private void OnConfirmResign()
    {
        CloseConfirmDialog();
        CloseMenu();

        if (gm.networkController != null)
        {
            // Online: local player loses
            int loserColor = gm.networkController.LocalColor;
            if (loserColor == ChessConstants.WHITE)
                gm.currentGameState = GameState.BlackWins;
            else
                gm.currentGameState = GameState.WhiteWins;

            gm.networkController.SendResign();
        }
        else
        {
            // Local/AI: current player loses
            if (gm.currentMove == ChessConstants.WHITE)
                gm.currentGameState = GameState.BlackWins;
            else
                gm.currentGameState = GameState.WhiteWins;
        }

        string winner = gm.currentGameState == GameState.WhiteWins ? "White" : "Black";
        GameLogUI.LogEvent("<color=#FF4444>Resignation. " + winner + " wins.</color>");
    }

    private void OnDrawClicked()
    {
        if (gm.networkController != null)
        {
            // Online: send draw offer
            isDrawOfferPending = true;
            gm.networkController.SendDrawOffer();
            CloseMenu();
            GameLogUI.LogEvent("<color=#FFAA44>Draw offered to opponent.</color>");
        }
        else
        {
            // Local/AI: draw immediately
            CloseMenu();
            gm.currentGameState = GameState.Draw;
            GameLogUI.LogEvent("<color=#FFAA44>Draw agreed.</color>");
        }
    }

    private void OnExitClicked()
    {
        CloseMenu();
        if (PhotonConnectionManager.Instance != null)
        {
            PhotonConnectionManager.Instance.Disconnect();
        }
        MatchConfig.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    // ========== Public Methods (called by NetworkGameController RPCs) ==========

    /// <summary>
    /// Show draw offer popup to receiving player.
    /// </summary>
    public void ShowDrawOffer()
    {
        // Close pause menu first if it's open
        if (IsMenuOpen) CloseMenu();

        isDrawOfferReceived = true;
        drawOfferOverlay.SetActive(true);
    }

    private void OnAcceptDraw()
    {
        CloseDrawOfferPopup();
        gm.currentGameState = GameState.Draw;
        GameLogUI.LogEvent("<color=#FFAA44>Draw accepted.</color>");

        if (gm.networkController != null)
        {
            gm.networkController.SendDrawResponse(true);
        }
    }

    private void OnDeclineDraw()
    {
        CloseDrawOfferPopup();
        GameLogUI.LogEvent("<color=#FFAA44>Draw declined.</color>");

        if (gm.networkController != null)
        {
            gm.networkController.SendDrawResponse(false);
        }
    }

    /// <summary>
    /// Handle opponent's response to our draw offer.
    /// </summary>
    public void OnDrawResponseReceived(bool accepted)
    {
        isDrawOfferPending = false;

        if (accepted)
        {
            gm.currentGameState = GameState.Draw;
            GameLogUI.LogEvent("<color=#FFAA44>Draw accepted by opponent.</color>");
        }
        else
        {
            GameLogUI.LogEvent("<color=#FFAA44>Draw declined by opponent.</color>");
        }
    }

    /// <summary>
    /// Handle opponent resignation. Sets game state to local player's win.
    /// </summary>
    public void OnOpponentResigned()
    {
        // Close all popups
        if (IsMenuOpen) CloseMenu();
        if (isConfirmOpen) CloseConfirmDialog();
        if (isDrawOfferReceived) CloseDrawOfferPopup();

        if (gm.networkController != null)
        {
            int localColor = gm.networkController.LocalColor;
            if (localColor == ChessConstants.WHITE)
                gm.currentGameState = GameState.WhiteWins;
            else
                gm.currentGameState = GameState.BlackWins;
        }

        GameLogUI.LogEvent("<color=#FF4444>Opponent resigned. You win!</color>");
    }

    // ========== UI Helper Methods ==========

    private void CreateText(Transform parent, string name, string content, int fontSize,
        FontStyle style, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text text = obj.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = content;
    }

    private GameObject CreateMenuButton(Transform parent, string name, string label,
        Color bgColor, float width, float height, Vector2 position,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, height);

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

        Text text = textObj.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.text = label;

        return btnObj;
    }
}
