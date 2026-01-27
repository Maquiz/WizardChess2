using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a red banner when the current player's king is in check.
/// Attach to the GameMaster GameObject.
/// </summary>
public class CheckBannerUI : MonoBehaviour
{
    private GameMaster gm;
    private GameObject panel;
    private Text checkText;
    private Canvas canvas;
    private bool wasInCheck = false;

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
        panel = new GameObject("CheckBanner");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -65);
        rt.sizeDelta = new Vector2(420, 50);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.7f, 0.1f, 0.1f, 0.85f);

        GameObject textObj = new GameObject("CheckText");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);

        checkText = textObj.AddComponent<Text>();
        checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        checkText.fontSize = 16;
        checkText.color = Color.white;
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.fontStyle = FontStyle.Bold;

        panel.SetActive(false);
    }

    void Update()
    {
        if (gm == null || panel == null) return;

        bool isInCheck = gm.currentGameState == GameState.WhiteInCheck
                      || gm.currentGameState == GameState.BlackInCheck;

        if (isInCheck && !wasInCheck)
        {
            string colorName = gm.currentGameState == GameState.WhiteInCheck ? "White" : "Black";
            checkText.text = "CHECK! " + colorName + " king is in check!";
            panel.SetActive(true);
        }
        else if (!isInCheck && wasInCheck)
        {
            panel.SetActive(false);
        }

        wasInCheck = isInCheck;
    }
}
