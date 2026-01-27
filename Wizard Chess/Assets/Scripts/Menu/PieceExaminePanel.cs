using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Browse all abilities by element and piece type, plus an Effects Glossary tab.
/// Shows passive name+desc, active name+desc+cooldown for the selected combination.
/// Effects tab shows all square effects and status effects with descriptions.
/// </summary>
public class PieceExaminePanel : MonoBehaviour
{
    private MainMenuUI menuUI;
    private bool returnToEditor = false;

    // Current selection
    private int selectedElement = ChessConstants.ELEMENT_FIRE;
    private int selectedPieceType = ChessConstants.PAWN;
    private bool showingEffects = false; // true = effects glossary, false = ability info

    // UI
    private GameObject contentRoot;
    private Button[] elementTabs;
    private Image[] elementTabBgs;
    private Button effectsTab;
    private Image effectsTabBg;
    private Button[] pieceTypeButtons;
    private Image[] pieceTypeBgs;
    private GameObject pieceTypeContainer;

    // Ability info panel
    private GameObject abilityInfoPanel;
    private Text passiveNameText;
    private Text passiveDescText;
    private Text activeNameText;
    private Text activeDescText;
    private Text cooldownText;

    // Effects glossary panel
    private GameObject effectsPanel;

    // Colors
    private static readonly Color FIRE_COLOR = new Color(1f, 0.4f, 0f);
    private static readonly Color EARTH_COLOR = new Color(0.6f, 0.4f, 0.2f);
    private static readonly Color LIGHTNING_COLOR = new Color(0.2f, 0.6f, 1f);
    private static readonly Color EFFECTS_COLOR = new Color(0.6f, 0.3f, 0.6f);

    public void Init(MainMenuUI menu)
    {
        menuUI = menu;
    }

    public void SetReturnToEditor(bool value)
    {
        returnToEditor = value;
    }

    public void Open(int preselectedElement = -1, int preselectedPieceType = -1)
    {
        if (preselectedElement >= ChessConstants.ELEMENT_FIRE && preselectedElement <= ChessConstants.ELEMENT_LIGHTNING)
            selectedElement = preselectedElement;
        else
            selectedElement = ChessConstants.ELEMENT_FIRE;

        if (preselectedPieceType >= ChessConstants.PAWN && preselectedPieceType <= ChessConstants.KING)
            selectedPieceType = preselectedPieceType;
        else
            selectedPieceType = ChessConstants.PAWN;

        showingEffects = false;
        BuildUI();
        RefreshUI();
    }

    private void ClearUI()
    {
        if (contentRoot != null) Destroy(contentRoot);
    }

    private void BuildUI()
    {
        ClearUI();
        contentRoot = new GameObject("ExamineContent");
        contentRoot.transform.SetParent(transform, false);

        RectTransform rt = contentRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Title
        MainMenuUI.CreateText(contentRoot.transform, "Title",
            "Piece & Ability Reference", 28, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.5f), new Vector2(500, 45));

        // Element tabs + Effects tab
        BuildTabs();

        // Piece type buttons
        BuildPieceTypeButtons();

        // Ability info panel
        BuildAbilityInfoPanel();

        // Effects glossary panel
        BuildEffectsPanel();

        // Back button
        MainMenuUI.CreateButton(contentRoot.transform, "BackButton",
            "Back", new Vector2(0.1f, 0.93f), new Vector2(100, 40),
            new Color(0.4f, 0.2f, 0.2f), () => OnBack());
    }

    private void BuildTabs()
    {
        string[] elemNames = { "Fire", "Earth", "Lightning" };
        Color[] elemColors = { FIRE_COLOR, EARTH_COLOR, LIGHTNING_COLOR };
        int[] elemIds = { ChessConstants.ELEMENT_FIRE, ChessConstants.ELEMENT_EARTH, ChessConstants.ELEMENT_LIGHTNING };

        elementTabs = new Button[3];
        elementTabBgs = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            int elemId = elemIds[i];
            float xPos = 0.25f + i * 0.16f;

            GameObject tabObj = new GameObject("ElemTab_" + elemNames[i]);
            tabObj.transform.SetParent(contentRoot.transform, false);

            RectTransform tabRt = tabObj.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(xPos, 0.84f);
            tabRt.anchorMax = new Vector2(xPos, 0.84f);
            tabRt.pivot = new Vector2(0.5f, 0.5f);
            tabRt.anchoredPosition = Vector2.zero;
            tabRt.sizeDelta = new Vector2(140, 42);

            Image tabBg = tabObj.AddComponent<Image>();
            elementTabBgs[i] = tabBg;

            Button tabBtn = tabObj.AddComponent<Button>();
            tabBtn.targetGraphic = tabBg;
            tabBtn.onClick.AddListener(() => { selectedElement = elemId; showingEffects = false; RefreshUI(); });
            elementTabs[i] = tabBtn;

            GameObject tabLabel = new GameObject("Label");
            tabLabel.transform.SetParent(tabObj.transform, false);
            RectTransform tabLabelRt = tabLabel.AddComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;
            Text tabText = tabLabel.AddComponent<Text>();
            tabText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tabText.fontSize = 18;
            tabText.fontStyle = FontStyle.Bold;
            tabText.color = Color.white;
            tabText.alignment = TextAnchor.MiddleCenter;
            tabText.text = elemNames[i];
        }

        // Effects tab
        float effectsX = 0.25f + 3 * 0.16f;
        GameObject effectsObj = new GameObject("EffectsTab");
        effectsObj.transform.SetParent(contentRoot.transform, false);

        RectTransform effectsRt = effectsObj.AddComponent<RectTransform>();
        effectsRt.anchorMin = new Vector2(effectsX, 0.84f);
        effectsRt.anchorMax = new Vector2(effectsX, 0.84f);
        effectsRt.pivot = new Vector2(0.5f, 0.5f);
        effectsRt.anchoredPosition = Vector2.zero;
        effectsRt.sizeDelta = new Vector2(140, 42);

        effectsTabBg = effectsObj.AddComponent<Image>();

        effectsTab = effectsObj.AddComponent<Button>();
        effectsTab.targetGraphic = effectsTabBg;
        effectsTab.onClick.AddListener(() => { showingEffects = true; RefreshUI(); });

        GameObject effectsLabel = new GameObject("Label");
        effectsLabel.transform.SetParent(effectsObj.transform, false);
        RectTransform effectsLabelRt = effectsLabel.AddComponent<RectTransform>();
        effectsLabelRt.anchorMin = Vector2.zero;
        effectsLabelRt.anchorMax = Vector2.one;
        effectsLabelRt.offsetMin = Vector2.zero;
        effectsLabelRt.offsetMax = Vector2.zero;
        Text effectsText = effectsLabel.AddComponent<Text>();
        effectsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        effectsText.fontSize = 18;
        effectsText.fontStyle = FontStyle.Bold;
        effectsText.color = Color.white;
        effectsText.alignment = TextAnchor.MiddleCenter;
        effectsText.text = "Effects";
    }

    private void BuildPieceTypeButtons()
    {
        pieceTypeContainer = new GameObject("PieceTypeContainer");
        pieceTypeContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform containerRt = pieceTypeContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        string[] pieceNames = { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };
        int[] pieceTypes = { ChessConstants.PAWN, ChessConstants.ROOK, ChessConstants.KNIGHT,
                             ChessConstants.BISHOP, ChessConstants.QUEEN, ChessConstants.KING };

        pieceTypeButtons = new Button[6];
        pieceTypeBgs = new Image[6];

        for (int i = 0; i < 6; i++)
        {
            int pt = pieceTypes[i];
            float xPos = 0.12f + i * 0.135f;

            GameObject ptObj = new GameObject("PieceBtn_" + pieceNames[i]);
            ptObj.transform.SetParent(pieceTypeContainer.transform, false);

            RectTransform ptRt = ptObj.AddComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(xPos, 0.74f);
            ptRt.anchorMax = new Vector2(xPos, 0.74f);
            ptRt.pivot = new Vector2(0.5f, 0.5f);
            ptRt.anchoredPosition = Vector2.zero;
            ptRt.sizeDelta = new Vector2(120, 38);

            Image ptBg = ptObj.AddComponent<Image>();
            pieceTypeBgs[i] = ptBg;

            Button ptBtn = ptObj.AddComponent<Button>();
            ptBtn.targetGraphic = ptBg;
            ptBtn.onClick.AddListener(() => { selectedPieceType = pt; RefreshUI(); });
            pieceTypeButtons[i] = ptBtn;

            GameObject ptLabel = new GameObject("Label");
            ptLabel.transform.SetParent(ptObj.transform, false);
            RectTransform ptLabelRt = ptLabel.AddComponent<RectTransform>();
            ptLabelRt.anchorMin = Vector2.zero;
            ptLabelRt.anchorMax = Vector2.one;
            ptLabelRt.offsetMin = Vector2.zero;
            ptLabelRt.offsetMax = Vector2.zero;
            Text ptText = ptLabel.AddComponent<Text>();
            ptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ptText.fontSize = 16;
            ptText.fontStyle = FontStyle.Bold;
            ptText.color = Color.white;
            ptText.alignment = TextAnchor.MiddleCenter;
            ptText.text = pieceNames[i];
        }
    }

    private void BuildAbilityInfoPanel()
    {
        abilityInfoPanel = new GameObject("AbilityInfoPanel");
        abilityInfoPanel.transform.SetParent(contentRoot.transform, false);

        RectTransform infoPanelRt = abilityInfoPanel.AddComponent<RectTransform>();
        infoPanelRt.anchorMin = new Vector2(0.1f, 0.15f);
        infoPanelRt.anchorMax = new Vector2(0.9f, 0.68f);
        infoPanelRt.offsetMin = Vector2.zero;
        infoPanelRt.offsetMax = Vector2.zero;

        Image infoPanelBg = abilityInfoPanel.AddComponent<Image>();
        infoPanelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        // Passive section
        MainMenuUI.CreateText(abilityInfoPanel.transform, "PassiveHeader",
            "PASSIVE ABILITY", 14, FontStyle.Bold, new Color(0.6f, 0.8f, 0.6f),
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(400, 25));

        GameObject passNameObj = MainMenuUI.CreateText(abilityInfoPanel.transform, "PassiveName",
            "", 22, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), new Vector2(600, 30));
        passiveNameText = passNameObj.GetComponent<Text>();

        GameObject passDescObj = new GameObject("PassiveDesc");
        passDescObj.transform.SetParent(abilityInfoPanel.transform, false);
        RectTransform passDescRt = passDescObj.AddComponent<RectTransform>();
        passDescRt.anchorMin = new Vector2(0.05f, 0.62f);
        passDescRt.anchorMax = new Vector2(0.95f, 0.78f);
        passDescRt.offsetMin = Vector2.zero;
        passDescRt.offsetMax = Vector2.zero;
        passiveDescText = passDescObj.AddComponent<Text>();
        passiveDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        passiveDescText.fontSize = 16;
        passiveDescText.color = new Color(0.85f, 0.85f, 0.85f);
        passiveDescText.alignment = TextAnchor.UpperCenter;

        // Divider
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(abilityInfoPanel.transform, false);
        RectTransform divRt = divider.AddComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.1f, 0.55f);
        divRt.anchorMax = new Vector2(0.9f, 0.55f);
        divRt.sizeDelta = new Vector2(0, 2);
        divider.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

        // Active section
        MainMenuUI.CreateText(abilityInfoPanel.transform, "ActiveHeader",
            "ACTIVE ABILITY", 14, FontStyle.Bold, new Color(0.8f, 0.6f, 0.6f),
            new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), new Vector2(400, 25));

        GameObject actNameObj = MainMenuUI.CreateText(abilityInfoPanel.transform, "ActiveName",
            "", 22, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), new Vector2(600, 30));
        activeNameText = actNameObj.GetComponent<Text>();

        GameObject actDescObj = new GameObject("ActiveDesc");
        actDescObj.transform.SetParent(abilityInfoPanel.transform, false);
        RectTransform actDescRt = actDescObj.AddComponent<RectTransform>();
        actDescRt.anchorMin = new Vector2(0.05f, 0.18f);
        actDescRt.anchorMax = new Vector2(0.95f, 0.34f);
        actDescRt.offsetMin = Vector2.zero;
        actDescRt.offsetMax = Vector2.zero;
        activeDescText = actDescObj.AddComponent<Text>();
        activeDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        activeDescText.fontSize = 16;
        activeDescText.color = new Color(0.85f, 0.85f, 0.85f);
        activeDescText.alignment = TextAnchor.UpperCenter;

        // Cooldown
        GameObject cdObj = MainMenuUI.CreateText(abilityInfoPanel.transform, "Cooldown",
            "", 16, FontStyle.Normal, new Color(0.7f, 0.7f, 0.5f),
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.5f), new Vector2(300, 25));
        cooldownText = cdObj.GetComponent<Text>();
    }

    private void BuildEffectsPanel()
    {
        effectsPanel = new GameObject("EffectsPanel");
        effectsPanel.transform.SetParent(contentRoot.transform, false);

        RectTransform panelRt = effectsPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.15f);
        panelRt.anchorMax = new Vector2(0.9f, 0.78f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image panelBg = effectsPanel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);

        // Scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(effectsPanel.transform, false);
        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(10, 10);
        scrollRt.offsetMax = new Vector2(-10, -10);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scrollObj.AddComponent<Image>().color = Color.clear;
        scrollObj.AddComponent<Mask>().showMaskGraphic = false;

        // Content: a single Text element with all glossary content as rich text.
        // No VerticalLayoutGroup, no ContentSizeFitter, no nested layouts.
        // Fixed large height ensures all text is visible and scrollable.
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contRt = contentObj.AddComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0, 1);
        contRt.anchorMax = new Vector2(1, 1);
        contRt.pivot = new Vector2(0.5f, 1);
        contRt.anchoredPosition = Vector2.zero;
        contRt.sizeDelta = new Vector2(0, 800);

        scroll.content = contRt;

        Text glossaryText = contentObj.AddComponent<Text>();
        glossaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        glossaryText.fontSize = 14;
        glossaryText.color = new Color(0.85f, 0.85f, 0.85f);
        glossaryText.alignment = TextAnchor.UpperLeft;
        glossaryText.supportRichText = true;
        glossaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        glossaryText.verticalOverflow = VerticalWrapMode.Overflow;

        glossaryText.text = BuildGlossaryText();
    }

    private string BuildGlossaryText()
    {
        string t = "";

        // Square Effects header
        t += "<color=#CC9966><size=20><b>SQUARE EFFECTS</b></size></color>\n\n";

        // Fire Square
        t += "<color=#" + AbilityInfo.GetSquareEffectColor(SquareEffectType.Fire) + ">"
            + "<size=17><b>" + AbilityInfo.GetSquareEffectName(SquareEffectType.Fire) + "</b></size></color>\n";
        t += AbilityInfo.GetSquareEffectDescription(SquareEffectType.Fire) + "\n\n";

        // Stone Wall
        t += "<color=#" + AbilityInfo.GetSquareEffectColor(SquareEffectType.StoneWall) + ">"
            + "<size=17><b>" + AbilityInfo.GetSquareEffectName(SquareEffectType.StoneWall) + "</b></size></color>\n";
        t += AbilityInfo.GetSquareEffectDescription(SquareEffectType.StoneWall) + "\n\n";

        // Lightning Field
        t += "<color=#" + AbilityInfo.GetSquareEffectColor(SquareEffectType.LightningField) + ">"
            + "<size=17><b>" + AbilityInfo.GetSquareEffectName(SquareEffectType.LightningField) + "</b></size></color>\n";
        t += AbilityInfo.GetSquareEffectDescription(SquareEffectType.LightningField) + "\n\n\n";

        // Status Effects header
        t += "<color=#CC8080><size=20><b>STATUS EFFECTS</b></size></color>\n\n";

        // Stunned
        t += "<color=#" + AbilityInfo.GetStatusEffectColor(StatusEffectType.Stunned) + ">"
            + "<size=17><b>" + AbilityInfo.GetStatusEffectName(StatusEffectType.Stunned) + "</b></size></color>\n";
        t += AbilityInfo.GetStatusEffectDescription(StatusEffectType.Stunned) + "\n\n";

        // Singed
        t += "<color=#" + AbilityInfo.GetStatusEffectColor(StatusEffectType.Singed) + ">"
            + "<size=17><b>" + AbilityInfo.GetStatusEffectName(StatusEffectType.Singed) + "</b></size></color>\n";
        t += AbilityInfo.GetStatusEffectDescription(StatusEffectType.Singed);

        return t;
    }

    private void RefreshUI()
    {
        // Tab highlights
        int[] elemIds = { ChessConstants.ELEMENT_FIRE, ChessConstants.ELEMENT_EARTH, ChessConstants.ELEMENT_LIGHTNING };
        Color[] elemColors = { FIRE_COLOR, EARTH_COLOR, LIGHTNING_COLOR };

        for (int i = 0; i < 3; i++)
        {
            if (!showingEffects && elemIds[i] == selectedElement)
                elementTabBgs[i].color = elemColors[i];
            else
                elementTabBgs[i].color = elemColors[i] * 0.35f;
        }

        // Effects tab highlight
        effectsTabBg.color = showingEffects ? EFFECTS_COLOR : EFFECTS_COLOR * 0.35f;

        // Toggle panels
        if (showingEffects)
        {
            abilityInfoPanel.SetActive(false);
            effectsPanel.SetActive(true);
            pieceTypeContainer.SetActive(false);
        }
        else
        {
            abilityInfoPanel.SetActive(true);
            effectsPanel.SetActive(false);
            pieceTypeContainer.SetActive(true);

            // Piece type buttons highlight
            int[] pieceTypes = { ChessConstants.PAWN, ChessConstants.ROOK, ChessConstants.KNIGHT,
                                 ChessConstants.BISHOP, ChessConstants.QUEEN, ChessConstants.KING };

            for (int i = 0; i < 6; i++)
            {
                if (pieceTypes[i] == selectedPieceType)
                    pieceTypeBgs[i].color = new Color(0.35f, 0.35f, 0.5f);
                else
                    pieceTypeBgs[i].color = new Color(0.2f, 0.2f, 0.3f);
            }

            // Ability info
            passiveNameText.text = AbilityInfo.GetPassiveName(selectedElement, selectedPieceType);
            passiveDescText.text = AbilityInfo.GetPassiveDescription(selectedElement, selectedPieceType);
            activeNameText.text = AbilityInfo.GetActiveName(selectedElement, selectedPieceType);
            activeDescText.text = AbilityInfo.GetActiveDescription(selectedElement, selectedPieceType);

            int cd = AbilityFactory.GetCooldown(selectedElement, selectedPieceType);
            cooldownText.text = "Cooldown: " + cd + " turns";
        }
    }

    private void OnBack()
    {
        if (returnToEditor)
        {
            returnToEditor = false;
            menuUI.ReturnToDeckEditor();
        }
        else
        {
            menuUI.ReturnFromPieceExamine();
        }
    }
}
