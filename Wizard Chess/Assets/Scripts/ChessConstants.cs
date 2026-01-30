/// <summary>
/// Chess game constants to eliminate magic numbers throughout the codebase.
/// </summary>
public static class ChessConstants
{
    // Piece types
    public const int PAWN = 1;
    public const int ROOK = 2;
    public const int KNIGHT = 3;
    public const int BISHOP = 4;
    public const int QUEEN = 5;
    public const int KING = 6;

    // Colors
    public const int BLACK = 1;
    public const int WHITE = 2;

    // Board dimensions
    public const int BOARD_SIZE = 8;

    // Direction arrays for move generation
    public static readonly (int x, int y)[] KingDirections =
    {
        (0, -1), (0, 1), (1, 0), (1, 1), (1, -1), (-1, 0), (-1, 1), (-1, -1)
    };

    public static readonly (int x, int y)[] KnightDirections =
    {
        (1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1)
    };

    public static readonly (int x, int y)[] RookDirections =
    {
        (0, 1), (0, -1), (1, 0), (-1, 0)
    };

    public static readonly (int x, int y)[] BishopDirections =
    {
        (1, 1), (-1, -1), (1, -1), (-1, 1)
    };

    // Queen uses both Rook and Bishop directions

    // Elements
    public const int ELEMENT_NONE = 0;
    public const int ELEMENT_FIRE = 1;
    public const int ELEMENT_EARTH = 2;
    public const int ELEMENT_LIGHTNING = 3;
    public const int ELEMENT_ICE = 4;
    public const int ELEMENT_SHADOW = 5;

    // Piece value for capture-prevention checks (Earth Pawn passive)
    public static int PieceValue(int pieceType)
    {
        switch (pieceType)
        {
            case PAWN: return 1;
            case KNIGHT: return 3;
            case BISHOP: return 3;
            case ROOK: return 5;
            case QUEEN: return 9;
            case KING: return 100;
            default: return 0;
        }
    }
}

/// <summary>
/// Types of effects that can be placed on squares.
/// </summary>
public enum SquareEffectType
{
    None,
    Fire,
    StoneWall,
    LightningField,
    Ice,           // Ice: Causes slide when entered
    ShadowVeil,    // Shadow: Hides piece type while on this square
    ShadowDecoy    // Shadow: Fake piece that disappears when attacked
}

/// <summary>
/// Status effects that can be applied to pieces.
/// </summary>
public enum StatusEffectType
{
    None,
    Stunned,
    Singed,
    Frozen,    // Ice: Cannot move (like Stunned but ice-themed)
    Chilled,   // Ice: Reduced movement range for sliding pieces
    Veiled,    // Shadow: Piece type hidden from opponent
    Marked     // Shadow: Bonus damage on next attack
}
