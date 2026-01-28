using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Menu panel for online matchmaking. Handles deck selection, room creation/joining,
/// random matchmaking, and lobby waiting. Follows AIMatchPanel runtime UI pattern.
/// </summary>
public class OnlineMatchPanel : MonoBehaviour
{
    private MainMenuUI menuUI;
    private DeckSaveData deckData;
    private PhotonConnectionManager connectionManager;

    // State
    private int selectedSlot = -1;
    private bool isWaiting = false;
    private string currentRoomCode = null;

    // UI elements
    private GameObject contentRoot;
    private Button[] slotButtons;
    private Text statusText;
    private GameObject matchmakingSection;
    private GameObject waitingOverlay;
    private Text waitingText;
    private InputField codeInput;
    private GameObject findMatchBtn;
    private GameObject createRoomBtn;
    private GameObject joinRoomBtn;

    // Room code characters (excludes confusing I/O/0/1)
    private static readonly string CODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public void Init(MainMenuUI menu)
    {
        menuUI = menu;
    }

    public void Open(DeckSaveData data)
    {
        deckData = data;
        selectedSlot = -1;
        isWaiting = false;
        currentRoomCode = null;

        EnsureConnectionManager();
        BuildUI();
        SubscribeEvents();

        connectionManager.ConnectToPhoton();
        UpdateStatus("Connecting to server...");
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void EnsureConnectionManager()
    {
        if (PhotonConnectionManager.Instance == null)
        {
            GameObject go = new GameObject("PhotonConnectionManager");
            go.AddComponent<PhotonConnectionManager>();
        }
        connectionManager = PhotonConnectionManager.Instance;
    }

    private void SubscribeEvents()
    {
        if (connectionManager == null) return;
        connectionManager.OnConnectedToMasterEvent += OnConnected;
        connectionManager.OnJoinedRoomEvent += OnJoinedRoom;
        connectionManager.OnOpponentJoinedEvent += OnOpponentJoined;
        connectionManager.OnConnectionErrorEvent += OnError;
        connectionManager.OnOpponentLeftEvent += OnOpponentLeft;
    }

    private void UnsubscribeEvents()
    {
        if (connectionManager == null) return;
        connectionManager.OnConnectedToMasterEvent -= OnConnected;
        connectionManager.OnJoinedRoomEvent -= OnJoinedRoom;
        connectionManager.OnOpponentJoinedEvent -= OnOpponentJoined;
        connectionManager.OnConnectionErrorEvent -= OnError;
        connectionManager.OnOpponentLeftEvent -= OnOpponentLeft;
    }

    // ========== UI Construction ==========

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
            new Color(0.4f, 0.2f, 0.2f), () => OnBackClicked());

        // Header
        MainMenuUI.CreateText(contentRoot.transform, "Header",
            "PLAY ONLINE", 32, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(400, 50));

        // Status text
        GameObject statusObj = MainMenuUI.CreateText(contentRoot.transform, "StatusText",
            "Initializing...", 16, FontStyle.Normal, new Color(0.7f, 0.8f, 1f),
            new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.5f), new Vector2(500, 30));
        statusText = statusObj.GetComponent<Text>();

        // Deck selection section
        MainMenuUI.CreateText(contentRoot.transform, "DeckLabel",
            "Select Your Deck:", 20, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(350, 30));

        // Slot grid (3x3)
        GameObject slotContainer = new GameObject("SlotGrid");
        slotContainer.transform.SetParent(contentRoot.transform, false);

        RectTransform gridRt = slotContainer.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.55f);
        gridRt.anchorMax = new Vector2(0.5f, 0.55f);
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

        // Matchmaking section
        BuildMatchmakingSection();

        // Waiting overlay (hidden initially)
        BuildWaitingOverlay();
    }

    private void BuildMatchmakingSection()
    {
        matchmakingSection = new GameObject("MatchmakingSection");
        matchmakingSection.transform.SetParent(contentRoot.transform, false);

        RectTransform rt = matchmakingSection.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0.35f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Find Match button
        findMatchBtn = MainMenuUI.CreateButton(matchmakingSection.transform, "FindMatchBtn",
            "Find Match", new Vector2(0.25f, 0.65f), new Vector2(200, 45),
            new Color(0.2f, 0.5f, 0.2f), () => OnFindMatchClicked());

        // Create Room button
        createRoomBtn = MainMenuUI.CreateButton(matchmakingSection.transform, "CreateRoomBtn",
            "Create Room", new Vector2(0.75f, 0.65f), new Vector2(200, 45),
            new Color(0.15f, 0.4f, 0.6f), () => OnCreateRoomClicked());

        // Code input field
        GameObject inputObj = new GameObject("CodeInput");
        inputObj.transform.SetParent(matchmakingSection.transform, false);

        RectTransform inputRt = inputObj.AddComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.35f, 0.2f);
        inputRt.anchorMax = new Vector2(0.35f, 0.2f);
        inputRt.pivot = new Vector2(0.5f, 0.5f);
        inputRt.anchoredPosition = Vector2.zero;
        inputRt.sizeDelta = new Vector2(180, 40);

        Image inputBg = inputObj.AddComponent<Image>();
        inputBg.color = new Color(0.15f, 0.15f, 0.25f);

        codeInput = inputObj.AddComponent<InputField>();
        codeInput.characterLimit = 5;

        // Input text child
        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(inputObj.transform, false);
        RectTransform textRt = inputTextObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 2);
        textRt.offsetMax = new Vector2(-10, -2);

        Text inputText = inputTextObj.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 20;
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleCenter;
        inputText.supportRichText = false;
        codeInput.textComponent = inputText;

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        RectTransform phRt = placeholderObj.AddComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10, 2);
        phRt.offsetMax = new Vector2(-10, -2);

        Text placeholder = placeholderObj.AddComponent<Text>();
        placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.fontSize = 18;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAnchor.MiddleCenter;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.text = "CODE";
        codeInput.placeholder = placeholder;

        // Join Room button
        joinRoomBtn = MainMenuUI.CreateButton(matchmakingSection.transform, "JoinRoomBtn",
            "Join Room", new Vector2(0.65f, 0.2f), new Vector2(160, 40),
            new Color(0.4f, 0.35f, 0.15f), () => OnJoinRoomClicked());

        // Initially disable matchmaking until connected
        SetMatchmakingInteractable(false);
    }

    private void BuildWaitingOverlay()
    {
        waitingOverlay = new GameObject("WaitingOverlay");
        waitingOverlay.transform.SetParent(contentRoot.transform, false);

        RectTransform rt = waitingOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = waitingOverlay.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        // Waiting text
        GameObject textObj = MainMenuUI.CreateText(waitingOverlay.transform, "WaitingText",
            "Waiting for opponent...", 24, FontStyle.Bold, Color.white,
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(500, 60));
        waitingText = textObj.GetComponent<Text>();

        // Cancel button
        MainMenuUI.CreateButton(waitingOverlay.transform, "CancelBtn",
            "Cancel", new Vector2(0.5f, 0.35f), new Vector2(180, 45),
            new Color(0.5f, 0.15f, 0.15f), () => OnCancelWaiting());

        waitingOverlay.SetActive(false);
    }

    // ========== Event Handlers ==========

    private void OnConnected()
    {
        UpdateStatus("Connected! Select a deck and choose matchmaking.");
        SetMatchmakingInteractable(true);
    }

    private void OnJoinedRoom(string roomName)
    {
        currentRoomCode = roomName;

        // Set deck as Photon custom property
        SetDeckProperty();

        // Check if room already has 2 players
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            UpdateWaitingText("Opponent found! Starting match...");

            // Both clients must enable scene sync and set MatchConfig
            PhotonNetwork.AutomaticallySyncScene = true;
            MatchConfig.isOnlineMatch = true;
            MatchConfig.useDeckSystem = true;

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke("StartOnlineGame", 1.5f);
            }
        }
        else
        {
            ShowWaiting("Waiting for opponent...\nRoom Code: " + currentRoomCode);
        }
    }

    private void OnOpponentJoined()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            UpdateWaitingText("Opponent found! Starting match...");

            // Both clients must enable scene sync and set MatchConfig
            PhotonNetwork.AutomaticallySyncScene = true;
            MatchConfig.isOnlineMatch = true;
            MatchConfig.useDeckSystem = true;

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke("StartOnlineGame", 1.5f);
            }
        }
    }

    private void OnError(string msg)
    {
        UpdateStatus("<color=#FF4444>" + msg + "</color>");
        HideWaiting();
        SetMatchmakingInteractable(true);
    }

    private void OnOpponentLeft()
    {
        if (isWaiting)
        {
            UpdateStatus("Opponent left the lobby.");
            HideWaiting();
            SetMatchmakingInteractable(true);
        }
    }

    // ========== Button Callbacks ==========

    private void OnBackClicked()
    {
        UnsubscribeEvents();
        if (connectionManager != null)
        {
            connectionManager.Disconnect();
        }
        menuUI.ReturnFromOnlineMatch();
    }

    private void OnSlotClicked(int index)
    {
        DeckSlot slot = deckData.slots[index];
        if (slot.isEmpty)
        {
            Debug.Log("[Online Menu] Slot " + (index + 1) + " is empty.");
            return;
        }

        selectedSlot = index;
        RefreshSlotHighlights();
    }

    private void OnFindMatchClicked()
    {
        if (!ValidateReadyToMatch()) return;
        SetMatchmakingInteractable(false);
        connectionManager.JoinRandomRoom();
        ShowWaiting("Searching for opponent...");
    }

    private void OnCreateRoomClicked()
    {
        if (!ValidateReadyToMatch()) return;
        string code = GenerateRoomCode();
        SetMatchmakingInteractable(false);
        connectionManager.CreateRoom(code);
    }

    private void OnJoinRoomClicked()
    {
        if (!ValidateReadyToMatch()) return;
        string code = codeInput.text.Trim().ToUpper();
        if (code.Length < 3)
        {
            UpdateStatus("<color=#FF4444>Enter a valid room code.</color>");
            return;
        }
        SetMatchmakingInteractable(false);
        connectionManager.JoinRoom(code);
        ShowWaiting("Joining room: " + code + "...");
    }

    private void OnCancelWaiting()
    {
        isWaiting = false;
        HideWaiting();

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        SetMatchmakingInteractable(true);
        UpdateStatus("Matchmaking cancelled.");
    }

    // ========== Match Start ==========

    private void StartOnlineGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

        // Close the room
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Load the Board scene (AutomaticallySyncScene already enabled on both clients)
        PhotonNetwork.LoadLevel("Board");

        Debug.Log("[Online] Starting online match!");
    }

    // ========== Helpers ==========

    private bool ValidateReadyToMatch()
    {
        if (selectedSlot < 0)
        {
            UpdateStatus("<color=#FF4444>Please select a deck first.</color>");
            return false;
        }

        if (connectionManager.State != PhotonConnectionManager.ConnectionState.ConnectedToMaster &&
            connectionManager.State != PhotonConnectionManager.ConnectionState.InRoom)
        {
            UpdateStatus("<color=#FF4444>Not connected to server.</color>");
            return false;
        }

        return true;
    }

    private void SetDeckProperty()
    {
        if (selectedSlot < 0) return;
        DeckSlot slot = deckData.slots[selectedSlot];

        // Serialize elements as comma-separated string
        string deckStr = "";
        for (int i = 0; i < 16; i++)
        {
            if (i > 0) deckStr += ",";
            deckStr += slot.elements[i].ToString();
        }

        Hashtable props = new Hashtable { { "deck", deckStr } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log("[Online] Set deck property: " + deckStr);
    }

    private string GenerateRoomCode()
    {
        char[] code = new char[5];
        for (int i = 0; i < 5; i++)
        {
            code[i] = CODE_CHARS[Random.Range(0, CODE_CHARS.Length)];
        }
        return new string(code);
    }

    private void RefreshSlotHighlights()
    {
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
    }

    private void SetMatchmakingInteractable(bool interactable)
    {
        if (findMatchBtn != null) findMatchBtn.GetComponent<Button>().interactable = interactable;
        if (createRoomBtn != null) createRoomBtn.GetComponent<Button>().interactable = interactable;
        if (joinRoomBtn != null) joinRoomBtn.GetComponent<Button>().interactable = interactable;
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void ShowWaiting(string msg)
    {
        isWaiting = true;
        if (waitingOverlay != null)
        {
            waitingOverlay.SetActive(true);
            if (waitingText != null) waitingText.text = msg;
        }
    }

    private void HideWaiting()
    {
        isWaiting = false;
        if (waitingOverlay != null) waitingOverlay.SetActive(false);
    }

    private void UpdateWaitingText(string msg)
    {
        if (waitingText != null) waitingText.text = msg;
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
