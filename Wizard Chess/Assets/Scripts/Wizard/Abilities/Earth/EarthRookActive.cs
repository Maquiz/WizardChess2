using System.Collections.Generic;

/// <summary>
/// Rampart: Create Stone Walls on consecutive empty squares in one cardinal direction.
/// </summary>
public class EarthRookActive : IActiveAbility
{
    private readonly EarthRookActiveParams _params;

    public EarthRookActive() { _params = new EarthRookActiveParams(); }
    public EarthRookActive(EarthRookActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        foreach (var dir in ChessConstants.RookDirections)
        {
            Square lastEmpty = null;
            for (int i = 1; i <= _params.maxWalls; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;
                if (!bs.IsInBounds(nx, ny)) break;
                if (!bs.IsSquareEmpty(nx, ny)) break;

                lastEmpty = piece.getSquare(nx, ny);
            }
            if (lastEmpty != null)
                targets.Add(lastEmpty);
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int dx = System.Math.Sign(target.x - piece.curx);
        int dy = System.Math.Sign(target.y - piece.cury);

        for (int i = 1; i <= _params.maxWalls; i++)
        {
            int nx = piece.curx + dx * i;
            int ny = piece.cury + dy * i;
            if (!bs.IsInBounds(nx, ny)) break;
            if (!bs.IsSquareEmpty(nx, ny)) break;

            sem.CreateEffect(nx, ny, SquareEffectType.StoneWall, _params.wallDuration, piece.color, _params.wallHP);
        }
        return true;
    }
}
