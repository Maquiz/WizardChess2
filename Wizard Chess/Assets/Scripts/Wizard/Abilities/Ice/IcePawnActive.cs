using System.Collections.Generic;

/// <summary>
/// Flash Freeze: Freeze an enemy within 2 squares.
/// </summary>
public class IcePawnActive : IActiveAbility
{
    private readonly IcePawnActiveParams _params;

    public IcePawnActive() { _params = new IcePawnActiveParams(); }
    public IcePawnActive(IcePawnActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        for (int dx = -_params.range; dx <= _params.range; dx++)
        {
            for (int dy = -_params.range; dy <= _params.range; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;

                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove target = bs.GetPieceAt(nx, ny);
                if (target != null && target.color != piece.color)
                {
                    targets.Add(piece.getSquare(nx, ny));
                }
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        PieceMove targetPiece = bs.GetPieceAt(target.x, target.y);
        if (targetPiece == null || targetPiece.color == piece.color) return false;

        if (targetPiece.elementalPiece != null)
        {
            // Check for ice immunity
            if (!targetPiece.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
            {
                targetPiece.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, _params.freezeDuration);
            }
        }

        return true;
    }
}
