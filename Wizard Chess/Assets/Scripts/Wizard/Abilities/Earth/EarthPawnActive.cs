using System.Collections.Generic;

/// <summary>
/// Barricade: Create a Stone Wall in front of pawn.
/// </summary>
public class EarthPawnActive : IActiveAbility
{
    private readonly EarthPawnActiveParams _params;

    public EarthPawnActive() { _params = new EarthPawnActiveParams(); }
    public EarthPawnActive(EarthPawnActiveParams p) { _params = p; }

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
            Square sq = piece.getSquare(piece.curx, ty);
            if (sq != null) targets.Add(sq);
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        sem.CreateEffect(target.x, target.y, SquareEffectType.StoneWall, _params.wallDuration, piece.color, _params.wallHP);
        return true;
    }
}
