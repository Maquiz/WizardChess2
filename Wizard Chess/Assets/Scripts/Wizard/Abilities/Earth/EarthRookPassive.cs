using System.Collections.Generic;

/// <summary>
/// Fortified: Cannot be captured while on its starting square (a1/h1/a8/h8).
/// </summary>
public class EarthRookPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        // Only applies when THIS rook is the defender
        if (defender.elementalPiece == null) return true;
        if (!(defender.elementalPiece.passive is EarthRookPassive)) return true;

        // Check if rook is on its starting square
        if (IsOnStartingSquare(defender))
        {
            return false; // Cannot be captured
        }
        return true;
    }

    private bool IsOnStartingSquare(PieceMove rook)
    {
        // Starting squares: (0,0), (7,0), (0,7), (7,7)
        if (rook.color == ChessConstants.WHITE)
        {
            return (rook.curx == 0 && rook.cury == 7) || (rook.curx == 7 && rook.cury == 7);
        }
        else
        {
            return (rook.curx == 0 && rook.cury == 0) || (rook.curx == 7 && rook.cury == 0);
        }
    }
}
