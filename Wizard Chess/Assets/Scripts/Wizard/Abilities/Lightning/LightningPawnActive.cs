using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chain Strike: Move forward 1 square, then chain-capture diagonal enemies.
/// </summary>
public class LightningPawnActive : IActiveAbility
{
    private readonly LtPawnActiveParams _params;

    public LightningPawnActive() { _params = new LtPawnActiveParams(); }
    public LightningPawnActive(LtPawnActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int ty = piece.cury + direction;
        if (!bs.IsInBounds(piece.curx, ty)) return false;
        return bs.IsSquareEmpty(piece.curx, ty);
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int ty = piece.cury + direction;

        if (bs.IsInBounds(piece.curx, ty) && bs.IsSquareEmpty(piece.curx, ty))
        {
            targets.Add(piece.getSquare(piece.curx, ty));
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        piece.movePiece(target.x, target.y, target);

        int captures = 0;
        int cx = piece.curx;
        int cy = piece.cury;

        while (captures < _params.maxChainCaptures)
        {
            PieceMove chainTarget = FindDiagonalEnemy(piece, cx, cy, bs);
            if (chainTarget == null) break;

            piece.gm.TryCapture(piece, chainTarget);
            cx = chainTarget.curx;
            cy = chainTarget.cury;
            captures++;
            Debug.Log("Chain Strike captures " + chainTarget.printPieceName() + "! (" + captures + ")");
        }

        return true;
    }

    private PieceMove FindDiagonalEnemy(PieceMove piece, int x, int y, BoardState bs)
    {
        (int dx, int dy)[] diagonals = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        foreach (var d in diagonals)
        {
            int nx = x + d.dx;
            int ny = y + d.dy;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove target = bs.GetPieceAt(nx, ny);
            if (target != null && target.color != piece.color && target.piece != ChessConstants.KING)
            {
                return target;
            }
        }
        return null;
    }
}
