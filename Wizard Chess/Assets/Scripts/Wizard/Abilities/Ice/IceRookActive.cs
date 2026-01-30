using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Avalanche: Create a line of Ice in one direction, push enemies back, and chill them.
/// </summary>
public class IceRookActive : IActiveAbility
{
    private readonly IceRookActiveParams _params;

    public IceRookActive() { _params = new IceRookActiveParams(); }
    public IceRookActive(IceRookActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return GetTargetSquares(piece, bs).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        // Can target any empty square in a cardinal direction up to line length
        foreach (var dir in ChessConstants.RookDirections)
        {
            for (int i = 1; i <= _params.lineLength; i++)
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
        // Determine direction from piece to target
        int dx = target.x - piece.curx;
        int dy = target.y - piece.cury;

        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // Create Ice line
        for (int i = 1; i <= _params.lineLength; i++)
        {
            int nx = piece.curx + dirX * i;
            int ny = piece.cury + dirY * i;

            if (!bs.IsInBounds(nx, ny)) break;

            // Check for enemy to push and chill
            PieceMove enemy = bs.GetPieceAt(nx, ny);
            if (enemy != null && enemy.color != piece.color)
            {
                // Push enemy back
                int pushX = nx + dirX * _params.pushDistance;
                int pushY = ny + dirY * _params.pushDistance;

                if (bs.IsInBounds(pushX, pushY) && bs.IsSquareEmpty(pushX, pushY))
                {
                    enemy.movePiece(pushX, pushY, piece.getSquare(pushX, pushY));
                }

                // Chill the enemy
                if (enemy.elementalPiece != null && !enemy.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
                {
                    enemy.elementalPiece.AddStatusEffect(StatusEffectType.Chilled, _params.chillDuration);
                }
            }

            // Create Ice on the square
            if (bs.IsSquareEmpty(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.Ice, _params.iceDuration, piece.color);
            }
        }

        return true;
    }
}
