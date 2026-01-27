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
    /// Reset to defaults (used when returning to menu or starting fresh).
    /// </summary>
    public static void Clear()
    {
        draftData = null;
        useDeckSystem = false;
    }
}
