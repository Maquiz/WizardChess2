using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Pre-game deck picking panel. Player 1 (White) picks, then Player 2 (Black), then Start Match.
/// Both players must select a saved deck — no "Standard Chess" option.
/// </summary>
public class DeckSelectPanel : MonoBehaviour
{
    private MainMenuUI menuUI;
    private DeckSaveData deckData;

    // State
    private int phase = 0; // 0=P1, 1=P2, 2=confirm
    private int selectedSlotP1 = -1;
    private int selectedSlotP2 = -1;

    // UI elements
    private GameObject contentRoot;
    private Text headerText;
    private Text selectedDeckText;
    private GameObject slotContainer;
    private GameObject startButton;
    private Button[] slotButtons;

    public void Init(MainMenuUI menu)
    {
        menuUI = menu;
    }

    public void Open(DeckSaveData data)
    {
        deckData = data;
        phase = 0;
        selectedSlotP1 = -1;
        selectedSlotP2 = -1;
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
        contentRoot = new GameObject("Content");
        contentRoot.transform.SetParent(transform, false);

        RectTransform rt = contentRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Header
        GameObject headerObj = MainMenuUI.CreateText(contentRoot.transform, "Header",
            "Player 1 (White) - Select Deck", 28, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(600, 50));
        headerText = headerObj.GetComponent<Text>();

        // Selected deck info
        GameObject selectedObj = MainMenuUI.CreateText(contentRoot.transform, "SelectedInfo",
            "", 16, FontStyle.Normal, new Color(0.7f, 0.8f, 0.7f),
            new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.5f), new Vector2(500, 30));
        selectedDeckText = selectedObj.GetComponent<Text>();

        // Slot grid (3x3)
        slotContainer = new GameObject("SlotGrid");
        slotContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform gridRt = slotContainer.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f);
        gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = new Vector2(0, 20);
        gridRt.sizeDelta = new Vector2(540, 330);

        GridLayoutGroup grid = slotContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(160, 90);
        grid.spacing = new Vector2(15, 15);
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
            labelText.fontSize = 14;
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
            "Start Match!", new Vector2(0.5f, 0.12f), new Vector2(250, 50),
            new Color(0.2f, 0.5f, 0.2f), () => OnStartMatch());
        startButton.SetActive(false);

        // Back button
        MainMenuUI.CreateButton(contentRoot.transform, "BackButton",
            "Back", new Vector2(0.1f, 0.93f), new Vector2(100, 40),
            new Color(0.4f, 0.2f, 0.2f), () => menuUI.ReturnFromDeckSelect());
    }

    private void RefreshUI()
    {
        switch (phase)
        {
            case 0:
                headerText.text = "Player 1 (White) - Select Deck";
                selectedDeckText.text = "";
                startButton.SetActive(false);
                break;
            case 1:
                headerText.text = "Player 2 (Black) - Select Deck";
                string p1Name = GetSlotName(selectedSlotP1);
                selectedDeckText.text = "White: " + p1Name;
                startButton.SetActive(false);
                break;
            case 2:
                headerText.text = "Ready to Begin!";
                string p1n = GetSlotName(selectedSlotP1);
                string p2n = GetSlotName(selectedSlotP2);
                selectedDeckText.text = "White: " + p1n + "  |  Black: " + p2n;
                startButton.SetActive(true);
                break;
        }
    }

    private void OnSlotClicked(int index)
    {
        DeckSlot slot = deckData.slots[index];
        if (slot.isEmpty)
        {
            Debug.Log("[Menu] Slot " + (index + 1) + " is empty.");
            return;
        }

        if (phase == 0)
        {
            selectedSlotP1 = index;
            phase = 1;
            RefreshUI();
        }
        else if (phase == 1)
        {
            selectedSlotP2 = index;
            phase = 2;
            RefreshUI();
        }
    }

    private void OnStartMatch()
    {
        if (selectedSlotP1 < 0 || selectedSlotP2 < 0) return;

        MatchConfig.useDeckSystem = true;
        DraftData draft = new DraftData();

        DeckSlot p1Deck = deckData.slots[selectedSlotP1];
        for (int i = 0; i < 16; i++)
        {
            int elem = p1Deck.elements[i];
            if (elem == ChessConstants.ELEMENT_NONE) elem = ChessConstants.ELEMENT_FIRE;
            draft.SetElement(ChessConstants.WHITE, i, elem);
        }

        DeckSlot p2Deck = deckData.slots[selectedSlotP2];
        for (int i = 0; i < 16; i++)
        {
            int elem = p2Deck.elements[i];
            if (elem == ChessConstants.ELEMENT_NONE) elem = ChessConstants.ELEMENT_FIRE;
            draft.SetElement(ChessConstants.BLACK, i, elem);
        }

        MatchConfig.draftData = draft;
        SceneManager.LoadScene("Board");
    }

    private string GetSlotName(int index)
    {
        if (index < 0 || index >= 9) return "Unknown";
        DeckSlot slot = deckData.slots[index];
        return string.IsNullOrEmpty(slot.name) ? "Deck " + (index + 1) : slot.name;
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
