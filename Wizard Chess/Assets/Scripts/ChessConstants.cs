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
}
