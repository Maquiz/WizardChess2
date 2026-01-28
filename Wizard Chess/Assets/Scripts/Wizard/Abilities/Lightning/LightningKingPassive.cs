using System.Collections.Generic;

/// <summary>
/// Reactive Blink: Once per game, when checked, blink to a safe square within range.
/// </summary>
public class LightningKingPassive : IPassiveAbility
{
    private readonly LtKingPassiveParams _params;

    public LightningKingPassive() { _params = new LtKingPassiveParams(); }
    public LightningKingPassive(LtKingPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        // Detect if king used the blink (moved more than 1 square in any direction).
        // Can't castle while in check, and blink only activates in check, so any >1 move is a blink.
        if (piece.piece != ChessConstants.KING) return;
        if (piece.elementalPiece == null || piece.elementalPiece.hasUsedReactiveBlink) return;

        int dx = System.Math.Abs(toX - fromX);
        int dy = System.Math.Abs(toY - fromY);
        if (dx > 1 || dy > 1)
        {
            piece.elementalPiece.hasUsedReactiveBlink = true;
            UnityEngine.Debug.Log("[Lightning] " + piece.printPieceName() + " used Reactive Blink! (once per game)");
        }
    }

    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.KING) return moves;
        if (piece.elementalPiece == null || piece.elementalPiece.hasUsedReactiveBlink) return moves;

        // Reactive Blink only activates when the king is in check
        if (!bs.IsKingInCheck(piece.color)) return moves;

        int opponentColor = piece.color == ChessConstants.WHITE ? ChessConstants.BLACK : ChessConstants.WHITE;
        for (int dx = -_params.blinkRange; dx <= _params.blinkRange; dx++)
        {
            for (int dy = -_params.blinkRange; dy <= _params.blinkRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;
                if (!bs.IsInBounds(nx, ny)) continue;
                if (!bs.IsSquareEmpty(nx, ny)) continue;
                if (bs.IsSquareAttackedBy(nx, ny, opponentColor)) continue;

                Square sq = piece.getSquare(nx, ny);
                if (sq != null && !moves.Contains(sq))
                {
                    moves.Add(sq);
                }
            }
        }
        return moves;
    }
}
