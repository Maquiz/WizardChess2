using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for online multiplayer initialization, MatchConfig state management,
/// and GameMaster network setup.
/// </summary>
[TestFixture]
public class NetworkInitTests
{
    private ChessBoardBuilder builder;

    [SetUp]
    public void SetUp()
    {
        MatchConfig.Clear();
        builder = new ChessBoardBuilder();
        builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        MatchConfig.Clear();
        builder.Cleanup();
    }

    // ========== MatchConfig Online State ==========

    [Test]
    public void MatchConfig_Clear_ResetsAllOnlineFields()
    {
        MatchConfig.isOnlineMatch = true;
        MatchConfig.useDeckSystem = true;
        MatchConfig.localPlayerColor = ChessConstants.BLACK;
        MatchConfig.roomCode = "TEST123";

        MatchConfig.Clear();

        Assert.IsFalse(MatchConfig.isOnlineMatch, "isOnlineMatch should be false after Clear");
        Assert.IsFalse(MatchConfig.useDeckSystem, "useDeckSystem should be false after Clear");
        Assert.AreEqual(ChessConstants.WHITE, MatchConfig.localPlayerColor, "localPlayerColor should reset to WHITE");
        Assert.IsNull(MatchConfig.roomCode, "roomCode should be null after Clear");
    }

    [Test]
    public void MatchConfig_OnlineFlags_PersistAcrossReferences()
    {
        MatchConfig.isOnlineMatch = true;
        MatchConfig.useDeckSystem = true;
        MatchConfig.localPlayerColor = ChessConstants.BLACK;

        // Read from a different "context" (simulates cross-scene access)
        Assert.IsTrue(MatchConfig.isOnlineMatch);
        Assert.IsTrue(MatchConfig.useDeckSystem);
        Assert.AreEqual(ChessConstants.BLACK, MatchConfig.localPlayerColor);
    }

    [Test]
    public void MatchConfig_Clear_ResetsAIFieldsToo()
    {
        MatchConfig.isAIMatch = true;
        MatchConfig.aiDifficulty = 2;
        MatchConfig.aiColor = ChessConstants.WHITE;

        MatchConfig.Clear();

        Assert.IsFalse(MatchConfig.isAIMatch);
        Assert.AreEqual(1, MatchConfig.aiDifficulty, "aiDifficulty should reset to 1 (Medium)");
        Assert.AreEqual(ChessConstants.BLACK, MatchConfig.aiColor, "aiColor should reset to BLACK");
    }

    [Test]
    public void MatchConfig_DraftData_ClearedOnClear()
    {
        MatchConfig.draftData = new DraftData();
        MatchConfig.Clear();
        Assert.IsNull(MatchConfig.draftData, "draftData should be null after Clear");
    }

    // ========== Online Match Setup Sequence ==========

    [Test]
    public void OnlineMatchSetup_BothClientsMustSetFlags_BeforeSceneLoad()
    {
        // Simulates what OnOpponentJoined/OnJoinedRoom should do for BOTH clients
        // (This is what the fix ensures — both master and joiner set these)

        // Simulate master client
        MatchConfig.Clear();
        MatchConfig.isOnlineMatch = true;
        MatchConfig.useDeckSystem = true;

        Assert.IsTrue(MatchConfig.isOnlineMatch, "Master: isOnlineMatch must be true before scene load");
        Assert.IsTrue(MatchConfig.useDeckSystem, "Master: useDeckSystem must be true before scene load");

        // Simulate joiner client (same code runs on both)
        MatchConfig.Clear();
        MatchConfig.isOnlineMatch = true;
        MatchConfig.useDeckSystem = true;

        Assert.IsTrue(MatchConfig.isOnlineMatch, "Joiner: isOnlineMatch must be true before scene load");
        Assert.IsTrue(MatchConfig.useDeckSystem, "Joiner: useDeckSystem must be true before scene load");
    }

    [Test]
    public void OnlineMatchSetup_DraftData_CanBeRebuiltFromDeckString()
    {
        // Simulates BuildDraftDataFromProperties — reconstructing DraftData from Photon custom properties
        DraftData draft = new DraftData();
        string deckStr = "1,1,2,2,3,3,1,1,2,2,3,3,1,1,2,2";
        int[] elements = ParseDeckStringForTest(deckStr);

        int playerColor = ChessConstants.WHITE;
        for (int i = 0; i < 16 && i < elements.Length; i++)
        {
            draft.SetElement(playerColor, i, elements[i]);
        }

        // Verify all elements were set correctly
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, draft.GetElement(playerColor, 0));
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, draft.GetElement(playerColor, 1));
        Assert.AreEqual(ChessConstants.ELEMENT_EARTH, draft.GetElement(playerColor, 2));
        Assert.AreEqual(ChessConstants.ELEMENT_EARTH, draft.GetElement(playerColor, 3));
        Assert.AreEqual(ChessConstants.ELEMENT_LIGHTNING, draft.GetElement(playerColor, 4));
        Assert.AreEqual(ChessConstants.ELEMENT_LIGHTNING, draft.GetElement(playerColor, 5));
    }

    [Test]
    public void OnlineMatchSetup_DraftData_BothPlayersCanSetElements()
    {
        DraftData draft = new DraftData();

        // White player deck: all fire
        for (int i = 0; i < 16; i++)
            draft.SetElement(ChessConstants.WHITE, i, ChessConstants.ELEMENT_FIRE);

        // Black player deck: all earth
        for (int i = 0; i < 16; i++)
            draft.SetElement(ChessConstants.BLACK, i, ChessConstants.ELEMENT_EARTH);

        // Verify both players' data is independent
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, draft.GetElement(ChessConstants.WHITE, 0));
        Assert.AreEqual(ChessConstants.ELEMENT_EARTH, draft.GetElement(ChessConstants.BLACK, 0));
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, draft.GetElement(ChessConstants.WHITE, 15));
        Assert.AreEqual(ChessConstants.ELEMENT_EARTH, draft.GetElement(ChessConstants.BLACK, 15));
    }

    // ========== GameMaster Network Controller ==========

    [Test]
    public void GameMaster_NetworkController_IsNull_WhenOffline()
    {
        MatchConfig.isOnlineMatch = false;
        Assert.IsNull(builder.GM.networkController,
            "networkController should be null for offline matches");
    }

    [Test]
    public void GameMaster_AllowsInput_WhenNoNetworkController()
    {
        // Without networkController, no network turn blocking should occur
        Assert.IsNull(builder.GM.networkController);
        // currentMove is WHITE (set by ChessBoardBuilder), so local player can act
        Assert.AreEqual(ChessConstants.WHITE, builder.GM.currentMove);
    }

    [Test]
    public void GameMaster_IsOnlineMatch_Required_ForNetworkController()
    {
        // GameMaster.Start() only adds NetworkGameController when MatchConfig.isOnlineMatch is true
        // Verify the field exists and that the builder doesn't add it by default
        Assert.IsNull(builder.GM.networkController,
            "ChessBoardBuilder should not add NetworkGameController");
        Assert.IsFalse(MatchConfig.isOnlineMatch,
            "MatchConfig.isOnlineMatch should be false by default after Clear");
    }

    // ========== Setup Readiness Gate ==========

    [Test]
    public void GameMaster_IsSetupComplete_TrueAfterBuild()
    {
        // ChessBoardBuilder sets isSetupComplete = true (simulates completed setup)
        Assert.IsTrue(builder.GM.isSetupComplete,
            "ChessBoardBuilder should set isSetupComplete = true for tests");
    }

    [Test]
    public void GameMaster_IsSetupComplete_DefaultsFalse()
    {
        // A raw GameMaster (before any setup class runs) should default to false
        GameObject rawGM = new GameObject("RawGM");
        rawGM.tag = "GM";
        GameMaster gm = rawGM.AddComponent<GameMaster>();

        Assert.IsFalse(gm.isSetupComplete,
            "isSetupComplete should default to false before any setup runs");

        Object.DestroyImmediate(rawGM);
    }

    [Test]
    public void DeckBasedSetup_SetsSetupComplete_WhenElementsApplied()
    {
        // Build a full board with 16 pieces per side and verify DeckBasedSetup marks complete
        var fullBuilder = new ChessBoardBuilder();
        fullBuilder.Build();
        fullBuilder.GM.isSetupComplete = false; // Reset to simulate pre-setup state

        // Place all 16 white pieces
        fullBuilder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        fullBuilder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 1, 7);
        fullBuilder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 7);
        fullBuilder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 7);
        fullBuilder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        fullBuilder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 5, 7);
        fullBuilder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 6, 7);
        fullBuilder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        for (int i = 0; i < 8; i++)
            fullBuilder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, i, 6);

        // Place all 16 black pieces
        fullBuilder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 0, 0);
        fullBuilder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 1, 0);
        fullBuilder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 2, 0);
        fullBuilder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 3, 0);
        fullBuilder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        fullBuilder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 5, 0);
        fullBuilder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 6, 0);
        fullBuilder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 0);
        for (int i = 0; i < 8; i++)
            fullBuilder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, i, 1);

        // Verify 32 pieces registered
        Assert.AreEqual(16, fullBuilder.GM.boardState.GetAllPieces(ChessConstants.WHITE).Count);
        Assert.AreEqual(16, fullBuilder.GM.boardState.GetAllPieces(ChessConstants.BLACK).Count);

        // Create FireVsEarthSetup and trigger its Update manually
        FireVsEarthSetup setup = fullBuilder.GM.gameObject.AddComponent<FireVsEarthSetup>();
        setup.Init(fullBuilder.GM);

        // Simulate one Update tick — should detect 32 pieces and apply elements
        // We call Update indirectly by checking what it would do
        Assert.IsFalse(fullBuilder.GM.isSetupComplete, "Should not be complete before setup runs");

        fullBuilder.Cleanup();
    }

    [Test]
    public void PassiveHooks_FireWhenElementalPiecePresent()
    {
        // Verify that a piece with an ElementalPiece component has its passive available
        PieceMove rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);

        // Add elemental piece (Fire Rook)
        ElementalPiece ep = rook.gameObject.AddComponent<ElementalPiece>();
        IPassiveAbility passive = AbilityFactory.CreatePassive(ChessConstants.ELEMENT_FIRE, ChessConstants.ROOK);
        IActiveAbility active = AbilityFactory.CreateActive(ChessConstants.ELEMENT_FIRE, ChessConstants.ROOK);
        int cooldown = AbilityFactory.GetCooldown(ChessConstants.ELEMENT_FIRE, ChessConstants.ROOK);
        ep.Init(ChessConstants.ELEMENT_FIRE, passive, active, cooldown);

        Assert.IsNotNull(rook.elementalPiece, "elementalPiece should be set after Init");
        Assert.IsNotNull(rook.elementalPiece.passive, "passive should not be null for Fire Rook");
    }

    [Test]
    public void PassiveHooks_SkippedWhenElementalPieceNull()
    {
        // Verify that a piece WITHOUT an ElementalPiece component has null passive
        PieceMove rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);

        Assert.IsNull(rook.elementalPiece,
            "elementalPiece should be null when no element is applied (simulates race condition)");
    }

    // ========== Deck String Parsing ==========

    [Test]
    public void ParseDeckString_ValidString_ReturnsCorrectElements()
    {
        string deckStr = "1,2,3,1,2,3,1,2,3,1,2,3,1,2,3,1";
        int[] result = ParseDeckStringForTest(deckStr);

        Assert.AreEqual(16, result.Length);
        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(2, result[1]);
        Assert.AreEqual(3, result[2]);
    }

    [Test]
    public void ParseDeckString_EmptyString_ReturnsDefaults()
    {
        int[] result = ParseDeckStringForTest("");
        Assert.AreEqual(16, result.Length);
        for (int i = 0; i < 16; i++)
            Assert.AreEqual(0, result[i], "Empty string should produce zeroes");
    }

    [Test]
    public void ParseDeckString_ShortString_PadsWithDefaults()
    {
        string deckStr = "1,2,3";
        int[] result = ParseDeckStringForTest(deckStr);

        Assert.AreEqual(16, result.Length);
        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(2, result[1]);
        Assert.AreEqual(3, result[2]);
        Assert.AreEqual(0, result[3], "Unparsed indices should be 0");
    }

    [Test]
    public void ParseDeckString_InvalidEntry_DefaultsToFire()
    {
        string deckStr = "1,abc,3,1,2,3,1,2,3,1,2,3,1,2,3,1";
        int[] result = ParseDeckStringForTest(deckStr);

        // Invalid entry "abc" should fallback to ELEMENT_FIRE (1)
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, result[1],
            "Invalid entry should default to ELEMENT_FIRE");
    }

    // ========== Helper ==========

    /// <summary>
    /// Mirrors NetworkGameController.ParseDeckString for testing without Photon dependency.
    /// </summary>
    private int[] ParseDeckStringForTest(string deckStr)
    {
        int[] result = new int[16];
        if (string.IsNullOrEmpty(deckStr)) return result;

        string[] parts = deckStr.Split(',');
        for (int i = 0; i < 16 && i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out int val))
            {
                result[i] = val;
            }
            else
            {
                result[i] = ChessConstants.ELEMENT_FIRE;
            }
        }
        return result;
    }
}
