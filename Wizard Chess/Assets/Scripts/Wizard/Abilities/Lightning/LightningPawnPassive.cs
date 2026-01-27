using System.Collections.Generic;

/// <summary>
/// Energized: Can always move extra squares forward (not just first move), if squares empty.
/// </summary>
public class LightningPawnPassive : IPassiveAbility
{
    private readonly LtPawnPassiveParams _params;

    public LightningPawnPassive() { _params = new LtPawnPassiveParams(); }
    public LightningPawnPassive(LtPawnPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.PAWN) return moves;
        if (piece.firstMove) return moves;

        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int oneY = piece.cury + direction;
        int twoY = piece.cury + _params.extraForwardRange * direction;

        if (!bs.IsInBounds(piece.curx, oneY) || !bs.IsInBounds(piece.curx, twoY))
            return moves;

        // Check if all squares along the path are empty
        bool pathClear = true;
        for (int i = 1; i <= _params.extraForwardRange; i++)
        {
            int checkY = piece.cury + direction * i;
            if (!bs.IsInBounds(piece.curx, checkY) || !bs.IsSquareEmpty(piece.curx, checkY))
            {
                pathClear = false;
                break;
            }
        }

        if (pathClear)
        {
            bool hasTwoSquare = false;
            foreach (var m in moves)
            {
                if (m.x == piece.curx && m.y == twoY)
                {
                    hasTwoSquare = true;
                    break;
                }
            }

            if (!hasTwoSquare)
            {
                Square twoAhead = piece.getSquare(piece.curx, twoY);
                if (twoAhead != null)
                {
                    moves.Add(twoAhead);
                }
            }
        }

        return moves;
    }
}
