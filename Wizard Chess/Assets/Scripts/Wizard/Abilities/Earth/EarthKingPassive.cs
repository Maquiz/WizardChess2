using System.Collections.Generic;

/// <summary>
/// Bedrock Throne: Cannot be checked while on starting square (e1/e8).
/// Normal check rules elsewhere.
/// Implementation: Modifies move generation to remove the king from check calculations
/// when on starting square. The king's position makes it "immune" to attacks.
/// </summary>
public class EarthKingPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    /// <summary>
    /// Check if the Earth King is on its starting square.
    /// Starting squares: e1 (4,7) for White, e8 (4,0) for Black.
    /// </summary>
    public static bool IsOnStartingSquare(PieceMove king)
    {
        if (king.color == ChessConstants.WHITE)
            return king.curx == 4 && king.cury == 7;
        else
            return king.curx == 4 && king.cury == 0;
    }
}
