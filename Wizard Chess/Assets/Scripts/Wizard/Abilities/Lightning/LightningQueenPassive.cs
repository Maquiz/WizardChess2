using System.Collections.Generic;

/// <summary>
/// Swiftness: Can also move like a Knight (L-shape). Capture on knight moves configurable.
/// </summary>
public class LightningQueenPassive : IPassiveAbility
{
    private readonly LtQueenPassiveParams _params;

    public LightningQueenPassive() { _params = new LtQueenPassiveParams(); }
    public LightningQueenPassive(LtQueenPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.QUEEN) return moves;

        foreach (var dir in ChessConstants.KnightDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            if (_params.allowKnightCapture)
            {
                // Allow both empty and enemy-occupied squares
                PieceMove occupant = bs.GetPieceAt(nx, ny);
                if (occupant != null && occupant.color == piece.color) continue;
            }
            else
            {
                if (!bs.IsSquareEmpty(nx, ny)) continue;
            }

            Square sq = piece.getSquare(nx, ny);
            if (sq != null && !moves.Contains(sq))
            {
                moves.Add(sq);
            }
        }
        return moves;
    }
}
