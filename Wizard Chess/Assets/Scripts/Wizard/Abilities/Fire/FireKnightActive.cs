using System.Collections.Generic;

/// <summary>
/// Eruption: Create Fire Squares on all 8 adjacent squares.
/// </summary>
public class FireKnightActive : IActiveAbility
{
    private readonly FireKnightActiveParams _params;

    public FireKnightActive() { _params = new FireKnightActiveParams(); }
    public FireKnightActive(FireKnightActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (bs.IsInBounds(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.Fire, _params.fireDuration, piece.color);
            }
        }
        return true;
    }
}
