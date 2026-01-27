using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deck editor panel. Edit a single deck: assign elements to each of 16 pieces.
/// Shows an 8x2 grid with chess piece icons tinted by element color.
/// Below the grid: selected piece info, 3 element buttons, bulk actions.
/// </summary>
public class DeckEditorPanel : MonoBehaviour
{
    private MainMenuUI menuUI;
    private DeckSaveData deckData;
    private int currentSlotIndex;
    private DeckSlot workingDeck;
    private int selectedPieceIndex = 0;

    // UI elements
    private GameObject contentRoot;
    private Text headerText;
    private InputField nameInput;
    private Text slotNavText;

    // Grid cells
    private Image[] cellBgs;
    private Image[] cellIcons;
    private Text[] cellLabels;
    private Image[] cellBorders;

    // Element buttons
    private Button[] elementButtons;
    private Image[] elementButtonBgs;

    // Selection info
    private Text selectedInfoText;

    // Sprite cache
    private Sprite[] pieceSprites;

    // Element colors
    private static readonly Color FIRE_COLOR = new Color(1f, 0.4f, 0f);
    private static readonly Color EARTH_COLOR = new Color(0.6f, 0.4f, 0.2f);
    private static readonly Color LIGHTNING_COLOR = new Color(0.2f, 0.6f, 1f);

    // Grid display order: Row 1 = back rank, Row 2 = pawns
    // These map grid position → piece index in the DeckSlot.elements array
    private static readonly int[] GRID_ORDER = {
        8, 10, 12, 14, 15, 13, 11, 9,  // Row 1: Rook1, Knight1, Bishop1, Queen, King, Bishop2, Knight2, Rook2
        0,  1,  2,  3,  4,  5,  6, 7   // Row 2: Pawn 1-8
    };

    public void Init(MainMenuUI menu)
    {
        menuUI = menu;
    }

    public void Open(int slotIndex, DeckSaveData data)
    {
        deckData = data;
        currentSlotIndex = Mathf.Clamp(slotIndex, 0, 8);
        workingDeck = deckData.slots[currentSlotIndex].Clone();
        workingDeck.MigrateNoneToFire();
        selectedPieceIndex = 0;
        LoadSprites();
        BuildUI();
        RefreshUI();
    }

    private void LoadSprites()
    {
        pieceSprites = new Sprite[7]; // index 0 unused, 1-6 = PAWN..KING
        pieceSprites[ChessConstants.PAWN] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.PAWN));
        pieceSprites[ChessConstants.ROOK] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.ROOK));
        pieceSprites[ChessConstants.KNIGHT] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.KNIGHT));
        pieceSprites[ChessConstants.BISHOP] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.BISHOP));
        pieceSprites[ChessConstants.QUEEN] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.QUEEN));
        pieceSprites[ChessConstants.KING] = Resources.Load<Sprite>(PieceIndexHelper.GetIconResourcePath(ChessConstants.KING));
    }

    private void ClearUI()
    {
        if (contentRoot != null) Destroy(contentRoot);
    }

    private void BuildUI()
    {
        ClearUI();
        contentRoot = new GameObject("EditorContent");
        contentRoot.transform.SetParent(transform, false);

        RectTransform rt = contentRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        BuildHeader();
        BuildGrid();
        BuildSelectionPanel();
        BuildBulkActions();
    }

    private void BuildHeader()
    {
        // Back button
        MainMenuUI.CreateButton(contentRoot.transform, "BackButton", "Back",
            new Vector2(0.06f, 0.95f), new Vector2(100, 40),
            new Color(0.4f, 0.2f, 0.2f), () => menuUI.ReturnFromDeckEditor());

        // Prev slot
        MainMenuUI.CreateButton(contentRoot.transform, "PrevSlot", "<",
            new Vector2(0.3f, 0.95f), new Vector2(40, 35),
            new Color(0.3f, 0.3f, 0.4f), () => NavigateSlot(-1));

        // Slot nav text
        GameObject navObj = MainMenuUI.CreateText(contentRoot.transform, "SlotNav",
            "", 20, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(300, 35));
        slotNavText = navObj.GetComponent<Text>();

        // Next slot
        MainMenuUI.CreateButton(contentRoot.transform, "NextSlot", ">",
            new Vector2(0.7f, 0.95f), new Vector2(40, 35),
            new Color(0.3f, 0.3f, 0.4f), () => NavigateSlot(1));

        // Deck name input
        GameObject nameContainer = new GameObject("NameContainer");
        nameContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform nameContRt = nameContainer.AddComponent<RectTransform>();
        nameContRt.anchorMin = new Vector2(0.5f, 0.88f);
        nameContRt.anchorMax = new Vector2(0.5f, 0.88f);
        nameContRt.pivot = new Vector2(0.5f, 0.5f);
        nameContRt.anchoredPosition = Vector2.zero;
        nameContRt.sizeDelta = new Vector2(350, 35);

        Image nameContBg = nameContainer.AddComponent<Image>();
        nameContBg.color = new Color(0.15f, 0.15f, 0.2f);

        nameInput = nameContainer.AddComponent<InputField>();
        nameInput.characterLimit = 30;

        GameObject nameTextObj = new GameObject("Text");
        nameTextObj.transform.SetParent(nameContainer.transform, false);
        RectTransform nameTextRt = nameTextObj.AddComponent<RectTransform>();
        nameTextRt.anchorMin = Vector2.zero;
        nameTextRt.anchorMax = Vector2.one;
        nameTextRt.offsetMin = new Vector2(10, 2);
        nameTextRt.offsetMax = new Vector2(-10, -2);
        Text nameText = nameTextObj.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.supportRichText = false;
        nameInput.textComponent = nameText;

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(nameContainer.transform, false);
        RectTransform phRt = placeholderObj.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10, 2);
        phRt.offsetMax = new Vector2(-10, -2);
        Text phText = placeholderObj.AddComponent<Text>();
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.fontSize = 16;
        phText.fontStyle = FontStyle.Italic;
        phText.color = new Color(0.5f, 0.5f, 0.5f);
        phText.text = "Enter deck name...";
        nameInput.placeholder = phText;

        // Save button
        MainMenuUI.CreateButton(contentRoot.transform, "SaveButton", "Save",
            new Vector2(0.8f, 0.88f), new Vector2(100, 35),
            new Color(0.2f, 0.5f, 0.2f), () => SaveDeck());
    }

    private void BuildGrid()
    {
        // Grid container centered in the upper area
        GameObject gridContainer = new GameObject("GridContainer");
        gridContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform gridRt = gridContainer.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f);
        gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = new Vector2(0, 100);
        // 8 cells wide * 100px + 7 gaps * 6px = 842
        // 2 rows * 110px + 1 gap * 6px = 226
        gridRt.sizeDelta = new Vector2(842, 226);

        GridLayoutGroup grid = gridContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100, 110);
        grid.spacing = new Vector2(6, 6);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        cellBgs = new Image[16];
        cellIcons = new Image[16];
        cellLabels = new Text[16];
        cellBorders = new Image[16];

        for (int g = 0; g < 16; g++)
        {
            int gridIdx = g;
            int pieceIdx = GRID_ORDER[g];

            // Cell container
            GameObject cell = new GameObject("Cell_" + pieceIdx);
            cell.transform.SetParent(gridContainer.transform, false);

            // Cell background (border — slightly larger visual effect via color)
            Image borderImg = cell.AddComponent<Image>();
            borderImg.color = new Color(0.15f, 0.15f, 0.2f);
            cellBorders[g] = borderImg;

            Button cellBtn = cell.AddComponent<Button>();
            cellBtn.targetGraphic = borderImg;
            cellBtn.onClick.AddListener(() => OnPieceSelected(pieceIdx));

            // Inner background for element tint
            GameObject innerBg = new GameObject("InnerBg");
            innerBg.transform.SetParent(cell.transform, false);
            RectTransform innerRt = innerBg.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3, 3);
            innerRt.offsetMax = new Vector2(-3, -3);
            Image bgImg = innerBg.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.18f);
            cellBgs[g] = bgImg;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cell.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.6f);
            iconRt.anchorMax = new Vector2(0.5f, 0.6f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(56, 56);
            Image iconImg = iconObj.AddComponent<Image>();
            int pieceType = PieceIndexHelper.GetPieceType(pieceIdx);
            iconImg.sprite = pieceSprites[pieceType];
            iconImg.preserveAspect = true;
            cellIcons[g] = iconImg;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(cell.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0.25f);
            labelRt.offsetMin = new Vector2(2, 2);
            labelRt.offsetMax = new Vector2(-2, 0);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 11;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.text = PieceIndexHelper.GetPieceLabel(pieceIdx);
            cellLabels[g] = labelText;
        }
    }

    private void BuildSelectionPanel()
    {
        // Selected piece info
        GameObject infoObj = MainMenuUI.CreateText(contentRoot.transform, "SelectedInfo",
            "", 18, FontStyle.Normal, Color.white,
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(500, 30));
        selectedInfoText = infoObj.GetComponent<Text>();

        // Element buttons container
        float btnY = 0.22f;
        float btnSpacing = 0.16f;
        float startX = 0.5f - btnSpacing;

        string[] elemNames = { "FIRE", "EARTH", "LIGHTNING" };
        Color[] elemColors = { FIRE_COLOR, EARTH_COLOR, LIGHTNING_COLOR };
        int[] elemIds = { ChessConstants.ELEMENT_FIRE, ChessConstants.ELEMENT_EARTH, ChessConstants.ELEMENT_LIGHTNING };

        elementButtons = new Button[3];
        elementButtonBgs = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            int elemId = elemIds[i];
            float xPos = startX + i * btnSpacing;

            GameObject btnObj = new GameObject("ElemBtn_" + elemNames[i]);
            btnObj.transform.SetParent(contentRoot.transform, false);

            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(xPos, btnY);
            btnRt.anchorMax = new Vector2(xPos, btnY);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(160, 45);

            Image btnBg = btnObj.AddComponent<Image>();
            elementButtonBgs[i] = btnBg;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(() => OnElementSelected(elemId));
            elementButtons[i] = btn;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            Text label = labelObj.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = elemNames[i];
        }

        // "?" examine button
        MainMenuUI.CreateButton(contentRoot.transform, "ExamineBtn", "?",
            new Vector2(0.5f + btnSpacing + btnSpacing * 0.55f, btnY), new Vector2(45, 45),
            new Color(0.3f, 0.3f, 0.5f), () =>
            {
                int elemId = workingDeck.elements[selectedPieceIndex];
                int pieceType = PieceIndexHelper.GetPieceType(selectedPieceIndex);
                menuUI.ShowPieceExamineFromEditor(elemId, pieceType);
            });
    }

    private void BuildBulkActions()
    {
        float bulkY = 0.12f;

        MainMenuUI.CreateButton(contentRoot.transform, "AllFire", "All Fire",
            new Vector2(0.35f, bulkY), new Vector2(130, 35), FIRE_COLOR * 0.7f,
            () => BulkApply(ChessConstants.ELEMENT_FIRE));

        MainMenuUI.CreateButton(contentRoot.transform, "AllEarth", "All Earth",
            new Vector2(0.5f, bulkY), new Vector2(130, 35), EARTH_COLOR * 0.7f,
            () => BulkApply(ChessConstants.ELEMENT_EARTH));

        MainMenuUI.CreateButton(contentRoot.transform, "AllLightning", "All Lightning",
            new Vector2(0.65f, bulkY), new Vector2(150, 35), LIGHTNING_COLOR * 0.7f,
            () => BulkApply(ChessConstants.ELEMENT_LIGHTNING));
    }

    private void RefreshUI()
    {
        slotNavText.text = "Deck Slot " + (currentSlotIndex + 1) + " / 9";
        nameInput.text = workingDeck.name;

        // Update grid cells
        for (int g = 0; g < 16; g++)
        {
            int pieceIdx = GRID_ORDER[g];
            int elemId = workingDeck.elements[pieceIdx];
            Color elemColor = GetElementColor(elemId);
            bool isSelected = (pieceIdx == selectedPieceIndex);

            // Icon tint by element
            cellIcons[g].color = elemColor;

            // Cell background subtle element tint
            Color bgTint = Color.Lerp(new Color(0.12f, 0.12f, 0.18f), elemColor, 0.15f);
            cellBgs[g].color = bgTint;

            // Border highlight for selected cell
            if (isSelected)
            {
                cellBorders[g].color = Color.Lerp(elemColor, Color.white, 0.5f);
            }
            else
            {
                cellBorders[g].color = new Color(0.15f, 0.15f, 0.2f);
            }
        }

        // Selected piece info
        string pieceName = PieceIndexHelper.GetPieceLabel(selectedPieceIndex);
        int selectedElem = workingDeck.elements[selectedPieceIndex];
        string elemName = AbilityInfo.GetElementName(selectedElem);
        Color selElemColor = GetElementColor(selectedElem);
        string colorHex = ColorUtility.ToHtmlStringRGB(selElemColor);
        selectedInfoText.text = "Selected: " + pieceName + "  |  Element: <color=#" + colorHex + ">" + elemName + "</color>";

        // Element button highlights
        int[] elemIds = { ChessConstants.ELEMENT_FIRE, ChessConstants.ELEMENT_EARTH, ChessConstants.ELEMENT_LIGHTNING };
        Color[] elemColors = { FIRE_COLOR, EARTH_COLOR, LIGHTNING_COLOR };

        for (int i = 0; i < 3; i++)
        {
            if (elemIds[i] == selectedElem)
                elementButtonBgs[i].color = elemColors[i];
            else
                elementButtonBgs[i].color = elemColors[i] * 0.4f;
        }
    }

    private void OnPieceSelected(int index)
    {
        selectedPieceIndex = index;
        RefreshUI();
    }

    private void OnElementSelected(int elementId)
    {
        workingDeck.elements[selectedPieceIndex] = elementId;
        RefreshUI();
    }

    private void BulkApply(int elementId)
    {
        for (int i = 0; i < 16; i++)
        {
            workingDeck.elements[i] = elementId;
        }
        RefreshUI();
    }

    private void NavigateSlot(int direction)
    {
        int newIndex = currentSlotIndex + direction;
        if (newIndex < 0) newIndex = 8;
        if (newIndex > 8) newIndex = 0;

        currentSlotIndex = newIndex;
        workingDeck = deckData.slots[currentSlotIndex].Clone();
        workingDeck.MigrateNoneToFire();
        selectedPieceIndex = 0;
        RefreshUI();
    }

    private void SaveDeck()
    {
        workingDeck.name = nameInput.text;
        workingDeck.isEmpty = false;
        deckData.slots[currentSlotIndex] = workingDeck.Clone();
        DeckPersistence.Save(deckData);
        menuUI.ReloadDeckData();
        Debug.Log("[Menu] Deck saved to slot " + (currentSlotIndex + 1) + ": " + workingDeck.name);
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case ChessConstants.ELEMENT_FIRE: return FIRE_COLOR;
            case ChessConstants.ELEMENT_EARTH: return EARTH_COLOR;
            case ChessConstants.ELEMENT_LIGHTNING: return LIGHTNING_COLOR;
            default: return FIRE_COLOR; // Default to fire since no "None" allowed
        }
    }
}
