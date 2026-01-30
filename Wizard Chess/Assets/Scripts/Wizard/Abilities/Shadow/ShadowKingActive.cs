using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shadow Swap: Swap positions with any friendly piece within range, both become Veiled.
/// </summary>
public class ShadowKingActive : IActiveAbility
{
    private readonly ShadowKingActiveParams _params;

    public ShadowKingActive() { _params = new ShadowKingActiveParams(); }
    public ShadowKingActive(ShadowKingActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        for (int dx = -_params.swapRange; dx <= _params.swapRange; dx++)
        {
            for (int dy = -_params.swapRange; dy <= _params.swapRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                // Chebyshev distance
                int distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (distance > _params.swapRange) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;

                if (!bs.IsInBounds(nx, ny)) continue;

                // Must have a friendly piece (not the king itself)
                PieceMove target = bs.GetPieceAt(nx, ny);
                if (target != null && target.color == piece.color && target != piece)
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
        if (targetPiece == null || targetPiece.color != piece.color) return false;

        // Store original positions
        int kingX = piece.curx;
        int kingY = piece.cury;
        int allyX = targetPiece.curx;
        int allyY = targetPiece.cury;

        // Get squares
        Square kingSquare = piece.getSquare(kingX, kingY);
        Square allySquare = piece.getSquare(allyX, allyY);

        // Swap positions (move ally first, then king)
        targetPiece.movePiece(kingX, kingY, kingSquare);
        piece.movePiece(allyX, allyY, allySquare);

        // Both become Veiled
        if (piece.elementalPiece != null)
        {
            piece.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, _params.veilDuration);
        }
        if (targetPiece.elementalPiece != null)
        {
            targetPiece.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, _params.veilDuration);
        }

        return true;
    }
}
