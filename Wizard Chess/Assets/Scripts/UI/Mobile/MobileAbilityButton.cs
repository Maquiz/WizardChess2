using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen ability activation button for mobile devices.
/// Shows near the selected piece when it has an ability ready.
/// Replaces the Q key functionality on mobile.
/// </summary>
public class MobileAbilityButton : MonoBehaviour
{
    private GameObject buttonObj;
    private Text cooldownText;
    private Image buttonBg;
    private Button button;
    private GameMaster gm;
    private Font uiFont;
    private bool isInitialized;

    // Button appearance
    private const float BUTTON_SIZE = 60f;
    private const float MARGIN_FROM_EDGE = 15f;

    // Colors
    private static readonly Color READY_COLOR = new Color(0.3f, 0.6f, 0.9f, 0.9f);
    private static readonly Color COOLDOWN_COLOR = new Color(0.4f, 0.4f, 0.4f, 0.7f);

    void Start()
    {
        // Only show on mobile platforms
        if (!PlatformDetector.IsMobile)
        {
            enabled = false;
            return;
        }

        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        gm = FindFirstObjectByType<GameMaster>();
        if (gm == null)
        {
            Debug.LogWarning("[MobileAbilityButton] GameMaster not found, disabling button");
            enabled = false;
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[MobileAbilityButton] Canvas not found, disabling button");
            enabled = false;
            return;
        }

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateUI(canvas);
        isInitialized = true;

        // Start hidden
        buttonObj.SetActive(false);
    }

    private void CreateUI(Canvas canvas)
    {
        // Button anchored to bottom-right (above the pause menu button)
        buttonObj = new GameObject("MobileAbilityButton");
        buttonObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-MARGIN_FROM_EDGE, MARGIN_FROM_EDGE + 60f); // Above pause button
        rt.sizeDelta = new Vector2(BUTTON_SIZE, BUTTON_SIZE);

        buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = READY_COLOR;

        button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonBg;
        button.onClick.AddListener(OnAbilityButtonClicked);

        // Ability icon/text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        cooldownText = textObj.AddComponent<Text>();
        cooldownText.font = uiFont;
        cooldownText.fontSize = 20;
        cooldownText.color = Color.white;
        cooldownText.alignment = TextAnchor.MiddleCenter;
        cooldownText.fontStyle = FontStyle.Bold;
        cooldownText.text = "Q"; // Q for ability, matches desktop key
    }

    void Update()
    {
        if (!isInitialized || gm == null) return;

        // Only show when a piece is selected and has an ability
        bool shouldShow = false;
        bool isReady = false;
        int cooldownTurns = 0;

        if (gm.isPieceSelected && gm.selectedPiece != null)
        {
            ElementalPiece ep = gm.selectedPiece.elementalPiece;
            if (ep != null && ep.active != null && ep.cooldown != null)
            {
                shouldShow = true;
                isReady = ep.cooldown.IsReady && ep.active.CanActivate(gm.selectedPiece, gm.boardState, gm.squareEffectManager);
                cooldownTurns = ep.cooldown.CurrentCooldown;
            }
        }

        // Update visibility
        if (buttonObj.activeSelf != shouldShow)
        {
            buttonObj.SetActive(shouldShow);
        }

        if (shouldShow)
        {
            // Update button state
            button.interactable = isReady;
            buttonBg.color = isReady ? READY_COLOR : COOLDOWN_COLOR;

            if (isReady)
            {
                cooldownText.text = "Q";
            }
            else
            {
                cooldownText.text = cooldownTurns.ToString();
            }
        }
    }

    private void OnAbilityButtonClicked()
    {
        if (gm == null) return;

        // Trigger the ability activation through GameMaster
        gm.HandleAbilityActivation();
    }

    void OnDestroy()
    {
        if (buttonObj != null)
        {
            Destroy(buttonObj);
        }
    }
}
