using System.Collections.Generic;

/// <summary>
/// Deep Freeze: Freeze a target enemy and all adjacent enemies.
/// </summary>
public class IceBishopActive : IActiveAbility
{
    private readonly IceBishopActiveParams _params;

    public IceBishopActive() { _params = new IceBishopActiveParams(); }
    public IceBishopActive(IceBishopActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can target any visible enemy on the same diagonal
        foreach (var dir in ChessConstants.BishopDirections)
        {
            for (int i = 1; i < ChessConstants.BOARD_SIZE; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;

                if (!bs.IsInBounds(nx, ny)) break;

                PieceMove target = bs.GetPieceAt(nx, ny);
                if (target != null)
                {
                    if (target.color != piece.color)
                    {
                        targets.Add(piece.getSquare(nx, ny));
                    }
                    break; // Stop at first piece
                }
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        PieceMove targetPiece = bs.GetPieceAt(target.x, target.y);
        if (targetPiece == null || targetPiece.color == piece.color) return false;

        // Freeze the target
        if (targetPiece.elementalPiece != null && !targetPiece.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
        {
            targetPiece.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, _params.freezeDuration);
        }

        // Freeze all adjacent enemies
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = target.x + dir.x;
            int ny = target.y + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adjacentPiece = bs.GetPieceAt(nx, ny);
            if (adjacentPiece != null && adjacentPiece.color != piece.color)
            {
                if (adjacentPiece.elementalPiece != null &&
                    !adjacentPiece.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
                {
                    adjacentPiece.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, _params.freezeDuration);
                }
            }
        }

        return true;
    }
}
