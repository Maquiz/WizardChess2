using System.Collections.Generic;

/// <summary>
/// Flame Cross: Create Fire Squares in a + pattern centered on the Bishop.
/// Optionally grants fire immunity.
/// </summary>
public class FireBishopActive : IActiveAbility
{
    private readonly FireBishopActiveParams _params;

    public FireBishopActive() { _params = new FireBishopActiveParams(); }
    public FireBishopActive(FireBishopActiveParams p) { _params = p; }

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
        foreach (var dir in ChessConstants.RookDirections)
        {
            for (int i = 1; i <= _params.armLength; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;
                if (bs.IsInBounds(nx, ny))
                {
                    sem.CreateEffect(nx, ny, SquareEffectType.Fire, _params.fireDuration, piece.color);
                }
            }
        }

        if (_params.grantFireImmunity && piece.elementalPiece != null)
        {
            piece.elementalPiece.AddImmunity(SquareEffectType.Fire);
        }

        return true;
    }
}
