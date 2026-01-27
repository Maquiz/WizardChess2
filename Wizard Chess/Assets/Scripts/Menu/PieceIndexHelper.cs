/// <summary>
/// Maps piece index (0-15) to piece type and display label.
/// Index ordering matches GameMaster.WPieces/BPieces Inspector arrays:
///   0-7: Pawns, 8-9: Rooks, 10-11: Knights, 12-13: Bishops, 14: Queen, 15: King
/// </summary>
public static class PieceIndexHelper
{
    /// <summary>
    /// Get the ChessConstants piece type for a given piece index.
    /// </summary>
    public static int GetPieceType(int index)
    {
        if (index < 0 || index > 15) return ChessConstants.PAWN;

        if (index <= 7) return ChessConstants.PAWN;
        if (index <= 9) return ChessConstants.ROOK;
        if (index <= 11) return ChessConstants.KNIGHT;
        if (index <= 13) return ChessConstants.BISHOP;
        if (index == 14) return ChessConstants.QUEEN;
        return ChessConstants.KING; // index == 15
    }

    /// <summary>
    /// Get a human-readable label for a piece at the given index.
    /// </summary>
    public static string GetPieceLabel(int index)
    {
        if (index < 0 || index > 15) return "Unknown";

        if (index <= 7) return "Pawn " + (index + 1);
        switch (index)
        {
            case 8: return "Rook 1";
            case 9: return "Rook 2";
            case 10: return "Knight 1";
            case 11: return "Knight 2";
            case 12: return "Bishop 1";
            case 13: return "Bishop 2";
            case 14: return "Queen";
            case 15: return "King";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Get the Resources path for a chess piece icon by piece type constant.
    /// </summary>
    public static string GetIconResourcePath(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN:   return "ChessIcons/pawn";
            case ChessConstants.ROOK:   return "ChessIcons/rook";
            case ChessConstants.KNIGHT: return "ChessIcons/knight";
            case ChessConstants.BISHOP: return "ChessIcons/bishop";
            case ChessConstants.QUEEN:  return "ChessIcons/queen";
            case ChessConstants.KING:   return "ChessIcons/king";
            default: return "ChessIcons/pawn";
        }
    }

    /// <summary>
    /// Get the piece type name (singular) for a given piece type constant.
    /// </summary>
    public static string GetPieceTypeName(int pieceType)
    {
        switch (pieceType)
        {
            case ChessConstants.PAWN: return "Pawn";
            case ChessConstants.ROOK: return "Rook";
            case ChessConstants.KNIGHT: return "Knight";
            case ChessConstants.BISHOP: return "Bishop";
            case ChessConstants.QUEEN: return "Queen";
            case ChessConstants.KING: return "King";
            default: return "Unknown";
        }
    }
}
