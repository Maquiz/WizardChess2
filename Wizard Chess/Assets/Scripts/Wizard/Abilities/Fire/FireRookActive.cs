using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inferno Line: Choose a cardinal direction; create Fire Squares in a line.
/// First enemy piece in the line is captured.
/// </summary>
public class FireRookActive : IActiveAbility
{
    private readonly FireRookActiveParams _params;

    public FireRookActive() { _params = new FireRookActiveParams(); }
    public FireRookActive(FireRookActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();

        foreach (var dir in ChessConstants.RookDirections)
        {
            int lastX = piece.curx;
            int lastY = piece.cury;
            for (int i = 1; i <= _params.lineLength; i++)
            {
                int nx = piece.curx + dir.x * i;
                int ny = piece.cury + dir.y * i;
                if (!bs.IsInBounds(nx, ny)) break;
                lastX = nx;
                lastY = ny;
            }
            if (lastX != piece.curx || lastY != piece.cury)
            {
                Square sq = piece.getSquare(lastX, lastY);
                if (sq != null) targets.Add(sq);
            }
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        int dx = System.Math.Sign(target.x - piece.curx);
        int dy = System.Math.Sign(target.y - piece.cury);

        int captureCount = 0;
        for (int i = 1; i <= _params.lineLength; i++)
        {
            int nx = piece.curx + dx * i;
            int ny = piece.cury + dy * i;
            if (!bs.IsInBounds(nx, ny)) break;

            PieceMove targetPiece = bs.GetPieceAt(nx, ny);
            if (captureCount < _params.maxCaptures && targetPiece != null && targetPiece.color != piece.color
                && targetPiece.piece != ChessConstants.KING)
            {
                if (piece.gm.TryCapture(piece, targetPiece))
                    captureCount++;
            }

            sem.CreateEffect(nx, ny, SquareEffectType.Fire, _params.fireDuration, piece.color);
        }
        return true;
    }
}
