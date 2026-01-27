using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays a fullscreen overlay when the game ends (checkmate, stalemate, draw).
/// Includes result text, detail text, and a New Game button.
/// Attach to the GameMaster GameObject.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private GameMaster gm;
    private GameObject overlay;
    private Text resultText;
    private Text detailText;
    private Canvas canvas;
    private bool wasGameOver = false;

    void Start()
    {
        gm = GetComponent<GameMaster>();
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CreateUI();
        }
    }

    private void CreateUI()
    {
        // Fullscreen dark overlay
        overlay = new GameObject("GameOverOverlay");
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.75f);

        // Center panel
        GameObject panel = new GameObject("ResultPanel");
        panel.transform.SetParent(overlay.transform, false);

        RectTransform panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(500, 280);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Result text (top of panel)
        GameObject resultObj = new GameObject("ResultText");
        resultObj.transform.SetParent(panel.transform, false);

        RectTransform resultRt = resultObj.AddComponent<RectTransform>();
        resultRt.anchorMin = new Vector2(0f, 0.6f);
        resultRt.anchorMax = new Vector2(1f, 1f);
        resultRt.offsetMin = new Vector2(20, 0);
        resultRt.offsetMax = new Vector2(-20, -20);

        resultText = resultObj.AddComponent<Text>();
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 32;
        resultText.color = Color.white;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.fontStyle = FontStyle.Bold;

        // Detail text (middle of panel)
        GameObject detailObj = new GameObject("DetailText");
        detailObj.transform.SetParent(panel.transform, false);

        RectTransform detailRt = detailObj.AddComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0f, 0.35f);
        detailRt.anchorMax = new Vector2(1f, 0.6f);
        detailRt.offsetMin = new Vector2(20, 0);
        detailRt.offsetMax = new Vector2(-20, 0);

        detailText = detailObj.AddComponent<Text>();
        detailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detailText.fontSize = 18;
        detailText.color = new Color(0.8f, 0.8f, 0.8f);
        detailText.alignment = TextAnchor.MiddleCenter;

        // Rematch button (bottom-left of panel)
        GameObject rematchObj = new GameObject("RematchButton");
        rematchObj.transform.SetParent(panel.transform, false);

        RectTransform rematchRt = rematchObj.AddComponent<RectTransform>();
        rematchRt.anchorMin = new Vector2(0.5f, 0f);
        rematchRt.anchorMax = new Vector2(0.5f, 0f);
        rematchRt.pivot = new Vector2(0.5f, 0f);
        rematchRt.anchoredPosition = new Vector2(-70, 20);
        rematchRt.sizeDelta = new Vector2(180, 50);

        Image rematchBg = rematchObj.AddComponent<Image>();
        rematchBg.color = new Color(0.2f, 0.5f, 0.2f, 1f);

        Button rematchBtn = rematchObj.AddComponent<Button>();
        rematchBtn.targetGraphic = rematchBg;
        rematchBtn.onClick.AddListener(OnRematchClicked);

        GameObject rematchTextObj = new GameObject("ButtonText");
        rematchTextObj.transform.SetParent(rematchObj.transform, false);

        RectTransform rematchTextRt = rematchTextObj.AddComponent<RectTransform>();
        rematchTextRt.anchorMin = Vector2.zero;
        rematchTextRt.anchorMax = Vector2.one;
        rematchTextRt.offsetMin = Vector2.zero;
        rematchTextRt.offsetMax = Vector2.zero;

        Text rematchText = rematchTextObj.AddComponent<Text>();
        rematchText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rematchText.fontSize = 18;
        rematchText.color = Color.white;
        rematchText.alignment = TextAnchor.MiddleCenter;
        rematchText.fontStyle = FontStyle.Bold;
        rematchText.text = "Rematch";

        // Main Menu button (bottom-right of panel)
        GameObject menuObj = new GameObject("MainMenuButton");
        menuObj.transform.SetParent(panel.transform, false);

        RectTransform menuRt = menuObj.AddComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.5f, 0f);
        menuRt.anchorMax = new Vector2(0.5f, 0f);
        menuRt.pivot = new Vector2(0.5f, 0f);
        menuRt.anchoredPosition = new Vector2(70, 20);
        menuRt.sizeDelta = new Vector2(180, 50);

        Image menuBg = menuObj.AddComponent<Image>();
        menuBg.color = new Color(0.3f, 0.3f, 0.5f, 1f);

        Button menuBtn = menuObj.AddComponent<Button>();
        menuBtn.targetGraphic = menuBg;
        menuBtn.onClick.AddListener(OnMainMenuClicked);

        GameObject menuTextObj = new GameObject("ButtonText");
        menuTextObj.transform.SetParent(menuObj.transform, false);

        RectTransform menuTextRt = menuTextObj.AddComponent<RectTransform>();
        menuTextRt.anchorMin = Vector2.zero;
        menuTextRt.anchorMax = Vector2.one;
        menuTextRt.offsetMin = Vector2.zero;
        menuTextRt.offsetMax = Vector2.zero;

        Text menuText = menuTextObj.AddComponent<Text>();
        menuText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        menuText.fontSize = 18;
        menuText.color = Color.white;
        menuText.alignment = TextAnchor.MiddleCenter;
        menuText.fontStyle = FontStyle.Bold;
        menuText.text = "Main Menu";

        overlay.SetActive(false);
    }

    void Update()
    {
        if (gm == null || overlay == null) return;

        bool isGameOver = gm.currentGameState == GameState.WhiteWins
                       || gm.currentGameState == GameState.BlackWins
                       || gm.currentGameState == GameState.Stalemate
                       || gm.currentGameState == GameState.Draw;

        if (isGameOver && !wasGameOver)
        {
            switch (gm.currentGameState)
            {
                case GameState.WhiteWins:
                    resultText.text = "CHECKMATE!";
                    detailText.text = "White wins the game";
                    break;
                case GameState.BlackWins:
                    resultText.text = "CHECKMATE!";
                    detailText.text = "Black wins the game";
                    break;
                case GameState.Stalemate:
                    resultText.text = "STALEMATE!";
                    detailText.text = "The game is a draw";
                    break;
                case GameState.Draw:
                    resultText.text = "DRAW!";
                    detailText.text = "The game is a draw";
                    break;
            }
            overlay.SetActive(true);
        }

        wasGameOver = isGameOver;
    }

    private void OnRematchClicked()
    {
        // Reload the Board scene with same MatchConfig (keeps deck selections)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMainMenuClicked()
    {
        MatchConfig.Clear();
        SceneManager.LoadScene("MainMenu");
    }
}
