using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shadowmeld: Teleport to any empty square within range, leave a ShadowVeil on departure.
/// </summary>
public class ShadowRookActive : IActiveAbility
{
    private readonly ShadowRookActiveParams _params;

    public ShadowRookActive() { _params = new ShadowRookActiveParams(); }
    public ShadowRookActive(ShadowRookActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        for (int dx = -_params.teleportRange; dx <= _params.teleportRange; dx++)
        {
            for (int dy = -_params.teleportRange; dy <= _params.teleportRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                // Chebyshev distance
                int distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (distance > _params.teleportRange) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;

                if (!bs.IsInBounds(nx, ny)) continue;
                if (!bs.IsSquareEmpty(nx, ny)) continue;

                targets.Add(piece.getSquare(nx, ny));
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int fromX = piece.curx;
        int fromY = piece.cury;

        // Move the piece
        piece.movePiece(target.x, target.y, target);

        // Leave a ShadowVeil on departure square
        sem.CreateEffect(fromX, fromY, SquareEffectType.ShadowVeil, _params.veilDuration, piece.color);

        return true;
    }
}
