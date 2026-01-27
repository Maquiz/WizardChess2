using System.Collections.Generic;

/// <summary>
/// Thunder Strike (CD: 5): Teleport to any square this Rook could legally move to,
/// ignoring blocking pieces. Cannot capture during teleport.
/// </summary>
public class LightningRookActive : IActiveAbility
{
    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // All empty squares in cardinal directions, ignoring blocking pieces
        foreach (var dir in ChessConstants.RookDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            while (bs.IsInBounds(nx, ny))
            {
                if (bs.IsSquareEmpty(nx, ny))
                {
                    Square sq = piece.getSquare(nx, ny);
                    if (sq != null) targets.Add(sq);
                }
                nx += dir.x;
                ny += dir.y;
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        piece.movePiece(target.x, target.y, target);
        return true;
    }
}
