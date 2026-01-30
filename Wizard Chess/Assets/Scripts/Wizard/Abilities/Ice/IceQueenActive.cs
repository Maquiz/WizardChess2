using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Absolute Zero: Create a 3x3 Ice zone on target, freezing all enemies inside.
/// </summary>
public class IceQueenActive : IActiveAbility
{
    private readonly IceQueenActiveParams _params;

    public IceQueenActive() { _params = new IceQueenActiveParams(); }
    public IceQueenActive(IceQueenActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can target any square the queen can move to (any direction)
        foreach (var dir in ChessConstants.RookDirections)
        {
            for (int i = 1; i < ChessConstants.BOARD_SIZE; i++)
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
            for (int i = 1; i < ChessConstants.BOARD_SIZE; i++)
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
        // Create 3x3 Ice zone centered on target
        for (int dx = -_params.aoeRadius; dx <= _params.aoeRadius; dx++)
        {
            for (int dy = -_params.aoeRadius; dy <= _params.aoeRadius; dy++)
            {
                int nx = target.x + dx;
                int ny = target.y + dy;

                if (!bs.IsInBounds(nx, ny)) continue;

                // Check for enemy to freeze
                PieceMove enemy = bs.GetPieceAt(nx, ny);
                if (enemy != null && enemy.color != piece.color)
                {
                    if (enemy.elementalPiece != null && !enemy.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
                    {
                        enemy.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, _params.freezeDuration);
                    }
                }

                // Create Ice on empty squares
                if (bs.IsSquareEmpty(nx, ny))
                {
                    sem.CreateEffect(nx, ny, SquareEffectType.Ice, _params.iceDuration, piece.color);
                }
            }
        }

        return true;
    }
}
