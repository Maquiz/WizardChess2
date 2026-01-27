using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightning Rod: Teleport within range. Stun enemies adjacent to both origin and destination.
/// </summary>
public class LightningKnightActive : IActiveAbility
{
    private readonly LtKnightActiveParams _params;

    public LightningKnightActive() { _params = new LtKnightActiveParams(); }
    public LightningKnightActive(LtKnightActiveParams p) { _params = p; }

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
                if (System.Math.Abs(dx) + System.Math.Abs(dy) > _params.teleportRange) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;
                if (!bs.IsInBounds(nx, ny)) continue;
                if (!bs.IsSquareEmpty(nx, ny)) continue;

                Square sq = piece.getSquare(nx, ny);
                if (sq != null) targets.Add(sq);
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int origX = piece.curx;
        int origY = piece.cury;

        HashSet<PieceMove> originAdj = new HashSet<PieceMove>();
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = origX + dir.x;
            int ny = origY + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;
            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color != piece.color)
                originAdj.Add(adj);
        }

        piece.movePiece(target.x, target.y, target);

        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = target.x + dir.x;
            int ny = target.y + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;
            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color != piece.color && originAdj.Contains(adj))
            {
                if (adj.elementalPiece == null)
                {
                    var ep = adj.gameObject.AddComponent<ElementalPiece>();
                    ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
                }
                adj.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.stunDuration);
                Debug.Log(adj.printPieceName() + " stunned by Lightning Rod!");
            }
        }

        return true;
    }
}
