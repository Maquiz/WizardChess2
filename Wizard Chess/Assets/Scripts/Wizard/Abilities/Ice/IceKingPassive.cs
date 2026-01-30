using System.Collections.Generic;

/// <summary>
/// Frozen Heart: Once per game, when put in check, freeze all checking pieces.
/// </summary>
public class IceKingPassive : IPassiveAbility
{
    private PieceMove _piece;

    public IceKingPassive() { }
    public IceKingPassive(IceKingPassiveParams p) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (_piece == null) _piece = piece;
        return moves;
    }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }

    public void OnTurnStart(int currentTurnColor)
    {
        if (_piece == null || _piece.elementalPiece == null) return;
        if (_piece.color != currentTurnColor) return;
        if (_piece.elementalPiece.hasUsedFrozenHeart) return;

        // Check if king is in check
        var gm = _piece.gm;
        if (gm == null) return;

        bool isInCheck = gm.boardState.IsKingInCheck(_piece.color);
        if (!isInCheck) return;

        // Find and freeze all pieces that are giving check
        _piece.elementalPiece.hasUsedFrozenHeart = true;
        UnityEngine.Debug.Log("[Ice] Frozen Heart activated! Freezing all checking pieces.");

        int kingX = _piece.curx;
        int kingY = _piece.cury;
        int enemyColor = _piece.color == ChessConstants.WHITE ? ChessConstants.BLACK : ChessConstants.WHITE;

        // Check all enemy pieces to see if they're attacking the king position
        List<PieceMove> enemies = gm.boardState.GetAllPieces(enemyColor);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            // Check if this enemy can attack the king square
            if (CanPieceAttackSquare(enemy, kingX, kingY, gm.boardState))
            {
                // This piece is giving check - freeze it
                if (enemy.elementalPiece != null && !enemy.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
                {
                    enemy.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, 2);
                    UnityEngine.Debug.Log("[Ice] " + enemy.printPieceName() + " frozen by Frozen Heart!");
                }
            }
        }
    }

    /// <summary>
    /// Simple check if a piece can attack a given square (without full move generation).
    /// This is a simplified version that checks basic attack patterns.
    /// </summary>
    private bool CanPieceAttackSquare(PieceMove piece, int targetX, int targetY, BoardState bs)
    {
        int dx = targetX - piece.curx;
        int dy = targetY - piece.cury;
        int absDx = System.Math.Abs(dx);
        int absDy = System.Math.Abs(dy);

        switch (piece.piece)
        {
            case ChessConstants.PAWN:
                // Pawns attack diagonally forward
                int pawnDir = piece.color == ChessConstants.WHITE ? -1 : 1;
                return absDx == 1 && dy == pawnDir;

            case ChessConstants.KNIGHT:
                // L-shape
                return (absDx == 1 && absDy == 2) || (absDx == 2 && absDy == 1);

            case ChessConstants.BISHOP:
                // Diagonal
                if (absDx != absDy || absDx == 0) return false;
                return IsPathClear(piece.curx, piece.cury, targetX, targetY, bs);

            case ChessConstants.ROOK:
                // Cardinal
                if (dx != 0 && dy != 0) return false;
                return IsPathClear(piece.curx, piece.cury, targetX, targetY, bs);

            case ChessConstants.QUEEN:
                // Cardinal or diagonal
                if (dx != 0 && dy != 0 && absDx != absDy) return false;
                return IsPathClear(piece.curx, piece.cury, targetX, targetY, bs);

            case ChessConstants.KING:
                // Adjacent
                return absDx <= 1 && absDy <= 1 && (absDx > 0 || absDy > 0);

            default:
                return false;
        }
    }

    private bool IsPathClear(int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        int x = fromX + stepX;
        int y = fromY + stepY;

        while (x != toX || y != toY)
        {
            if (!bs.IsSquareEmpty(x, y)) return false;
            x += stepX;
            y += stepY;
        }
        return true;
    }
}
