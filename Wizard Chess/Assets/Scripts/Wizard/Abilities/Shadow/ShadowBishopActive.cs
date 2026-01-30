using System.Collections.Generic;

/// <summary>
/// Eclipse: Veil all friendly pieces in a 2x2 area.
/// </summary>
public class ShadowBishopActive : IActiveAbility
{
    private readonly ShadowBishopActiveParams _params;

    public ShadowBishopActive() { _params = new ShadowBishopActiveParams(); }
    public ShadowBishopActive(ShadowBishopActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can target any square on the board that has at least one friendly piece in the 2x2 area
        for (int x = 0; x < ChessConstants.BOARD_SIZE; x++)
        {
            for (int y = 0; y < ChessConstants.BOARD_SIZE; y++)
            {
                // Check if there's at least one friendly piece in the 2x2 area starting at (x,y)
                bool hasFriendly = false;
                for (int dx = 0; dx <= _params.aoeRadius && !hasFriendly; dx++)
                {
                    for (int dy = 0; dy <= _params.aoeRadius && !hasFriendly; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (!bs.IsInBounds(nx, ny)) continue;

                        PieceMove p = bs.GetPieceAt(nx, ny);
                        if (p != null && p.color == piece.color)
                        {
                            hasFriendly = true;
                        }
                    }
                }

                if (hasFriendly)
                {
                    targets.Add(piece.getSquare(x, y));
                }
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        // Veil all friendly pieces in the 2x2 area
        for (int dx = 0; dx <= _params.aoeRadius; dx++)
        {
            for (int dy = 0; dy <= _params.aoeRadius; dy++)
            {
                int nx = target.x + dx;
                int ny = target.y + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove p = bs.GetPieceAt(nx, ny);
                if (p != null && p.color == piece.color && p.elementalPiece != null)
                {
                    p.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, _params.veilDuration);
                }
            }
        }

        return true;
    }
}
