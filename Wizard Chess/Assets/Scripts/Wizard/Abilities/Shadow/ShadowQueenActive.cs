using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Umbral Assault: Move up to N squares in any direction, Mark all adjacent enemies after moving.
/// </summary>
public class ShadowQueenActive : IActiveAbility
{
    private readonly ShadowQueenActiveParams _params;

    public ShadowQueenActive() { _params = new ShadowQueenActiveParams(); }
    public ShadowQueenActive(ShadowQueenActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can move up to moveRange squares in any queen direction
        foreach (var dir in ChessConstants.RookDirections)
        {
            for (int i = 1; i <= _params.moveRange; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;

                if (!bs.IsInBounds(nx, ny)) break;
                if (!bs.IsSquareEmpty(nx, ny)) break;

                targets.Add(piece.getSquare(nx, ny));
            }
        }

        foreach (var dir in ChessConstants.BishopDirections)
        {
            for (int i = 1; i <= _params.moveRange; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;

                if (!bs.IsInBounds(nx, ny)) break;
                if (!bs.IsSquareEmpty(nx, ny)) break;

                targets.Add(piece.getSquare(nx, ny));
            }
        }

        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        // Move the queen
        piece.movePiece(target.x, target.y, target);

        // Mark all adjacent enemies
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = target.x + dir.x;
            int ny = target.y + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adjacentPiece = bs.GetPieceAt(nx, ny);
            if (adjacentPiece != null && adjacentPiece.color != piece.color)
            {
                if (adjacentPiece.elementalPiece != null)
                {
                    adjacentPiece.elementalPiece.AddStatusEffect(StatusEffectType.Marked, _params.markDuration);
                }
            }
        }

        return true;
    }
}
