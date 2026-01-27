using System.Collections.Generic;

/// <summary>
/// Flame Rush: Move forward 1-N squares ignoring blocking pieces,
/// create Fire Squares on all passed-through squares.
/// </summary>
public class FirePawnActive : IActiveAbility
{
    private readonly FirePawnActiveParams _params;

    public FirePawnActive() { _params = new FirePawnActiveParams(); }
    public FirePawnActive(FirePawnActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;

        for (int i = 1; i <= _params.maxForwardRange; i++)
        {
            int ty = piece.cury + (direction * i);
            if (!bs.IsInBounds(piece.curx, ty)) break;
            if (bs.IsSquareEmpty(piece.curx, ty))
            {
                targets.Add(piece.getSquare(piece.curx, ty));
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int distance = (target.y - piece.cury) / direction;

        for (int i = 1; i < distance; i++)
        {
            int fy = piece.cury + (direction * i);
            sem.CreateEffect(piece.curx, fy, SquareEffectType.Fire, _params.fireTrailDuration, piece.color);
        }

        piece.movePiece(target.x, target.y, target);
        return true;
    }
}
