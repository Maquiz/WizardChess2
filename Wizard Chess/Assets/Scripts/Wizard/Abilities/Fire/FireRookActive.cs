using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inferno Line: Choose a cardinal direction; create Fire Squares in a line.
/// First enemy piece in the line is captured.
/// Uses MultiStepMoveController for sequential animated captures and fire creation.
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
        MultiStepMoveController controller = piece.gm.multiStepController;

        int dx = System.Math.Sign(target.x - piece.curx);
        int dy = System.Math.Sign(target.y - piece.cury);

        // If no controller, fall back to instant execution
        if (controller == null)
        {
            return ExecuteInstant(piece, dx, dy, bs, sem);
        }

        // Build step sequence: capture then create fire for each square
        List<MoveStep> steps = new List<MoveStep>();
        int captureCount = 0;

        for (int i = 1; i <= _params.lineLength; i++)
        {
            int nx = piece.curx + dx * i;
            int ny = piece.cury + dy * i;
            if (!bs.IsInBounds(nx, ny)) break;

            // Check for capture target
            PieceMove targetPiece = bs.GetPieceAt(nx, ny);
            if (captureCount < _params.maxCaptures && targetPiece != null
                && targetPiece.color != piece.color && targetPiece.piece != ChessConstants.KING)
            {
                // Capture step
                steps.Add(MoveStep.Capture(piece, targetPiece));
                captureCount++;
            }

            // Fire creation step (with delay for visual effect)
            int fireX = nx;
            int fireY = ny;
            int fireDuration = _params.fireDuration;
            int fireOwner = piece.color;
            steps.Add(MoveStep.Custom(() =>
            {
                sem.CreateEffect(fireX, fireY, SquareEffectType.Fire, fireDuration, fireOwner);
            }, 0.1f));
        }

        // Execute sequence
        controller.ExecuteSteps(steps, null);

        return true;
    }

    /// <summary>
    /// Fallback instant execution when controller unavailable.
    /// </summary>
    private bool ExecuteInstant(PieceMove piece, int dx, int dy, BoardState bs, SquareEffectManager sem)
    {
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
