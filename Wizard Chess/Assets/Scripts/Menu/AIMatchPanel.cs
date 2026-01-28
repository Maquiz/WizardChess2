using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu panel for setting up an AI match.
/// Player selects difficulty level and their deck (White), then starts the match.
/// </summary>
public class AIMatchPanel : MonoBehaviour
{
    private MainMenuUI menuUI;
    private DeckSaveData deckData;

    // State
    private int selectedDifficulty = -1;
    private int selectedSlot = -1;

    // UI elements
    private GameObject contentRoot;
    private Button[] difficultyButtons;
    private Image[] difficultyBgs;
    private Button[] slotButtons;
    private GameObject startButton;
    private Text[] difficultyLabels;

    // Difficulty colors
    private static readonly Color EasyColor = new Color(0.2f, 0.5f, 0.2f);
    private static readonly Color MediumColor = new Color(0.6f, 0.5f, 0.1f);
    private static readonly Color HardColor = new Color(0.6f, 0.15f, 0.15f);

    public void Init(MainMenuUI menu)
    {
        menuUI = menu;
    }

    public void Open(DeckSaveData data)
    {
        deckData = data;
        selectedDifficulty = -1;
        selectedSlot = -1;
        BuildUI();
    }

    private void ClearUI()
    {
        if (contentRoot != null) Destroy(contentRoot);
    }

    private void BuildUI()
    {
        ClearUI();
        contentRoot = new GameObject("Content");
        contentRoot.transform.SetParent(transform, false);

        RectTransform rt = contentRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Back button
        MainMenuUI.CreateButton(contentRoot.transform, "BackButton",
            "Back", new Vector2(0.1f, 0.93f), new Vector2(100, 40),
            new Color(0.4f, 0.2f, 0.2f), () => menuUI.ReturnFromAIMatch());

        // Header
        MainMenuUI.CreateText(contentRoot.transform, "Header",
            "PLAY VS AI", 32, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(400, 50));

        // Difficulty section
        MainMenuUI.CreateText(contentRoot.transform, "DiffLabel",
            "Choose Difficulty:", 20, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(300, 30));

        // Difficulty buttons
        difficultyButtons = new Button[3];
        difficultyBgs = new Image[3];
        difficultyLabels = new Text[3];

        string[] diffNames = { "EASY", "MEDIUM", "HARD" };
        string[] diffDescs = {
            "Beginner\nRandom moves",
            "Intermediate\nCaptures wisely",
            "Expert\nStrong tactics"
        };
        Color[] diffColors = { EasyColor, MediumColor, HardColor };
        float[] diffAnchorsX = { 0.25f, 0.5f, 0.75f };

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            GameObject btnObj = new GameObject("Diff_" + i);
            btnObj.transform.SetParent(contentRoot.transform, false);

            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(diffAnchorsX[i], 0.67f);
            btnRt.anchorMax = new Vector2(diffAnchorsX[i], 0.67f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(180, 80);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = diffColors[i];
            difficultyBgs[i] = bg;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnDifficultyClicked(idx));
            difficultyButtons[i] = btn;

            // Name label
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(btnObj.transform, false);
            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.text = diffNames[i];
            difficultyLabels[i] = nameText;

            // Description label
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(btnObj.transform, false);
            RectTransform descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0, 0);
            descRt.anchorMax = new Vector2(1, 0.5f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;

            Text descText = descObj.AddComponent<Text>();
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 12;
            descText.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            descText.alignment = TextAnchor.MiddleCenter;
            descText.text = diffDescs[i];
        }

        // Deck selection section
        MainMenuUI.CreateText(contentRoot.transform, "DeckLabel",
            "Select Your Deck (White):", 20, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(350, 30));

        // Slot grid (3x3)
        GameObject slotContainer = new GameObject("SlotGrid");
        slotContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform gridRt = slotContainer.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.35f);
        gridRt.anchorMax = new Vector2(0.5f, 0.35f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = Vector2.zero;
        gridRt.sizeDelta = new Vector2(540, 270);

        GridLayoutGroup grid = slotContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 75);
        grid.spacing = new Vector2(15, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        slotButtons = new Button[9];
        for (int i = 0; i < 9; i++)
        {
            int idx = i;
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(slotContainer.transform, false);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.2f, 0.2f, 0.3f);

            Button slotBtn = slotObj.AddComponent<Button>();
            slotBtn.targetGraphic = slotBg;
            slotBtn.onClick.AddListener(() => OnSlotClicked(idx));
            slotButtons[i] = slotBtn;

            // Slot label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slotObj.transform, false);

            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(5, 5);
            labelRt.offsetMax = new Vector2(-5, -5);

            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 13;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            DeckSlot slot = deckData.slots[i];
            if (slot.isEmpty)
            {
                labelText.text = "Slot " + (i + 1) + "\n<color=#888>(Empty)</color>";
            }
            else
            {
                string deckName = string.IsNullOrEmpty(slot.name) ? "Deck " + (i + 1) : slot.name;
                labelText.text = deckName + "\n" + GetDeckSummary(slot);
            }
        }

        // Start Match button
        startButton = MainMenuUI.CreateButton(contentRoot.transform, "StartMatch",
            "Start Match!", new Vector2(0.5f, 0.1f), new Vector2(250, 50),
            new Color(0.2f, 0.5f, 0.2f), () => OnStartMatch());
        startButton.SetActive(false);
    }

    private void OnDifficultyClicked(int diff)
    {
        selectedDifficulty = diff;
        RefreshHighlights();
    }

    private void OnSlotClicked(int index)
    {
        DeckSlot slot = deckData.slots[index];
        if (slot.isEmpty)
        {
            Debug.Log("[AI Menu] Slot " + (index + 1) + " is empty.");
            return;
        }

        selectedSlot = index;
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        // Highlight selected difficulty
        Color[] baseColors = { EasyColor, MediumColor, HardColor };
        for (int i = 0; i < 3; i++)
        {
            if (i == selectedDifficulty)
            {
                difficultyBgs[i].color = baseColors[i] * 1.5f;
            }
            else
            {
                difficultyBgs[i].color = baseColors[i];
            }
        }

        // Highlight selected slot
        for (int i = 0; i < 9; i++)
        {
            Image bg = slotButtons[i].GetComponent<Image>();
            if (i == selectedSlot)
            {
                bg.color = new Color(0.3f, 0.5f, 0.3f);
            }
            else
            {
                bg.color = new Color(0.2f, 0.2f, 0.3f);
            }
        }

        // Enable start button only when both are selected
        startButton.SetActive(selectedDifficulty >= 0 && selectedSlot >= 0);
    }

    private void OnStartMatch()
    {
        if (selectedDifficulty < 0 || selectedSlot < 0) return;

        // Build DraftData: White = player's deck, Black = random AI deck
        DraftData draft = new DraftData();

        // Player's deck (White)
        DeckSlot playerDeck = deckData.slots[selectedSlot];
        for (int i = 0; i < 16; i++)
        {
            int elem = playerDeck.elements[i];
            if (elem == ChessConstants.ELEMENT_NONE) elem = ChessConstants.ELEMENT_FIRE;
            draft.SetElement(ChessConstants.WHITE, i, elem);
        }

        // AI deck (Black): random elements
        for (int i = 0; i < 16; i++)
        {
            int randomElem = Random.Range(1, 4); // 1=Fire, 2=Earth, 3=Lightning
            draft.SetElement(ChessConstants.BLACK, i, randomElem);
        }

        // Set MatchConfig
        MatchConfig.isAIMatch = true;
        MatchConfig.aiDifficulty = selectedDifficulty;
        MatchConfig.aiColor = ChessConstants.BLACK;
        MatchConfig.useDeckSystem = true;
        MatchConfig.draftData = draft;

        SceneManager.LoadScene("Board");
    }

    private string GetDeckSummary(DeckSlot slot)
    {
        int fire = 0, earth = 0, lightning = 0;
        for (int i = 0; i < 16; i++)
        {
            switch (slot.elements[i])
            {
                case ChessConstants.ELEMENT_FIRE: fire++; break;
                case ChessConstants.ELEMENT_EARTH: earth++; break;
                case ChessConstants.ELEMENT_LIGHTNING: lightning++; break;
            }
        }
        string summary = "";
        if (fire > 0) summary += "<color=#FF6600>F:" + fire + "</color> ";
        if (earth > 0) summary += "<color=#996633>E:" + earth + "</color> ";
        if (lightning > 0) summary += "<color=#3399FF>L:" + lightning + "</color> ";
        if (summary == "") summary = "<color=#888>No elements</color>";
        return summary.Trim();
    }
}
