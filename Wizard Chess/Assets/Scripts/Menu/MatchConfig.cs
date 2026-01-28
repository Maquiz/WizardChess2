/// <summary>
/// Static cross-scene data bridge. Populated by the menu, read by the Board scene.
/// Survives scene loads without DontDestroyOnLoad (it's just static data).
/// </summary>
public static class MatchConfig
{
    /// <summary>
    /// Draft data populated by deck selection. Read by DeckBasedSetup in Board scene.
    /// </summary>
    public static DraftData draftData;

    /// <summary>
    /// When true, Board scene uses DeckBasedSetup. When false, falls back to FireVsEarthSetup.
    /// </summary>
    public static bool useDeckSystem = false;

    /// <summary>
    /// Whether this match is against the AI opponent.
    /// </summary>
    public static bool isAIMatch = false;

    /// <summary>
    /// AI difficulty level: 0=Easy, 1=Medium, 2=Hard.
    /// </summary>
    public static int aiDifficulty = 1;

    /// <summary>
    /// AI player color (default: Black).
    /// </summary>
    public static int aiColor = ChessConstants.BLACK;

    /// <summary>
    /// Whether this match is an online multiplayer match.
    /// </summary>
    public static bool isOnlineMatch = false;

    /// <summary>
    /// Local player's color in online mode (WHITE or BLACK).
    /// </summary>
    public static int localPlayerColor = ChessConstants.WHITE;

    /// <summary>
    /// Room code for private online matches (null for random matchmaking).
    /// </summary>
    public static string roomCode = null;

    /// <summary>
    /// Reset to defaults (used when returning to menu or starting fresh).
    /// </summary>
    public static void Clear()
    {
        draftData = null;
        useDeckSystem = false;
        isAIMatch = false;
        aiDifficulty = 1;
        aiColor = ChessConstants.BLACK;
        isOnlineMatch = false;
        localPlayerColor = ChessConstants.WHITE;
        roomCode = null;
    }
}
