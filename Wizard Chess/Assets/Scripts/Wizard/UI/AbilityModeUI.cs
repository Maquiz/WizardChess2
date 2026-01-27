using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays an on-screen indicator when ability mode is active.
/// Shows the ability name and cancel instructions.
/// Attach to the GameMaster GameObject.
/// </summary>
public class AbilityModeUI : MonoBehaviour
{
    private GameMaster gm;
    private GameObject panel;
    private Text statusText;
    private Canvas canvas;
    private bool wasInAbilityMode = false;

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
        // Create panel at top-center of screen
        panel = new GameObject("AbilityModeIndicator");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(420, 50);

        // Background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0f, 0.85f);

        // Text
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(panel.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);

        statusText = textObj.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16;
        statusText.color = Color.white;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.fontStyle = FontStyle.Bold;
        statusText.supportRichText = true;

        panel.SetActive(false);
    }

    void Update()
    {
        if (gm == null || panel == null) return;

        bool inAbilityMode = gm.abilityExecutor != null && gm.abilityExecutor.isInAbilityMode;

        if (inAbilityMode && !wasInAbilityMode)
        {
            // Just entered ability mode
            string abilityName = "";
            if (gm.selectedPiece != null && gm.selectedPiece.elementalPiece != null)
            {
                abilityName = AbilityInfo.GetActiveName(
                    gm.selectedPiece.elementalPiece.elementId,
                    gm.selectedPiece.piece);
            }
            statusText.text = "ABILITY: " + abilityName +
                "   |   Click target   |   Q or Right-click to cancel";
            panel.SetActive(true);
        }
        else if (!inAbilityMode && wasInAbilityMode)
        {
            // Just exited ability mode
            panel.SetActive(false);
        }

        wasInAbilityMode = inAbilityMode;
    }
}
