/// <summary>
/// Serializable data structure holding draft selections for both players.
/// Maps each piece index to its chosen element.
/// </summary>
[System.Serializable]
public class DraftData
{
    /// <summary>
    /// Element choices for White pieces (16 entries, indexed same as GameMaster.WPieces).
    /// 0 = none, 1 = fire, 2 = earth, 3 = lightning
    /// </summary>
    public int[] whiteElements = new int[16];

    /// <summary>
    /// Element choices for Black pieces (16 entries, indexed same as GameMaster.BPieces).
    /// </summary>
    public int[] blackElements = new int[16];

    public DraftData()
    {
        for (int i = 0; i < 16; i++)
        {
            whiteElements[i] = ChessConstants.ELEMENT_NONE;
            blackElements[i] = ChessConstants.ELEMENT_NONE;
        }
    }

    /// <summary>
    /// Set element for a specific piece.
    /// </summary>
    public void SetElement(int color, int pieceIndex, int elementId)
    {
        if (pieceIndex < 0 || pieceIndex >= 16) return;

        if (color == ChessConstants.WHITE)
            whiteElements[pieceIndex] = elementId;
        else
            blackElements[pieceIndex] = elementId;
    }

    /// <summary>
    /// Get element for a specific piece.
    /// </summary>
    public int GetElement(int color, int pieceIndex)
    {
        if (pieceIndex < 0 || pieceIndex >= 16) return ChessConstants.ELEMENT_NONE;

        return color == ChessConstants.WHITE ? whiteElements[pieceIndex] : blackElements[pieceIndex];
    }
}
