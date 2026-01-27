using System.Collections.Generic;

/// <summary>
/// Shield Wall: Cannot be captured by pieces worth more than a Pawn
/// if a friendly piece is orthogonally adjacent.
/// </summary>
public class EarthPawnPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        // Only applies when THIS pawn is the defender
        if (defender.elementalPiece == null) return true;
        if (!(defender.elementalPiece.passive is EarthPawnPassive)) return true;

        // Check if attacker is worth more than a pawn
        if (ChessConstants.PieceValue(attacker.piece) <= ChessConstants.PieceValue(ChessConstants.PAWN))
            return true; // Pawns can still capture

        // Check if a friendly piece is orthogonally adjacent to defender
        foreach (var dir in ChessConstants.RookDirections)
        {
            int nx = defender.curx + dir.x;
            int ny = defender.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color == defender.color)
            {
                // Protected! Prevent capture
                return false;
            }
        }
        return true;
    }
}
