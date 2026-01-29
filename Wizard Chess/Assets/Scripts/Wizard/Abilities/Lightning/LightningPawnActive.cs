using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chain Strike: Move forward 1 square, then chain-capture diagonal enemies.
/// Uses MultiStepMoveController for animated sequential captures.
/// </summary>
public class LightningPawnActive : IActiveAbility
{
    private readonly LtPawnActiveParams _params;

    public LightningPawnActive() { _params = new LtPawnActiveParams(); }
    public LightningPawnActive(LtPawnActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int ty = piece.cury + direction;
        if (!bs.IsInBounds(piece.curx, ty)) return false;
        return bs.IsSquareEmpty(piece.curx, ty);
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        int direction = piece.color == ChessConstants.WHITE ? -1 : 1;
        int ty = piece.cury + direction;

        if (bs.IsInBounds(piece.curx, ty) && bs.IsSquareEmpty(piece.curx, ty))
        {
            targets.Add(piece.getSquare(piece.curx, ty));
        }
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        MultiStepMoveController controller = piece.gm.multiStepController;

        // If no controller available, fall back to instant execution
        if (controller == null)
        {
            return ExecuteInstant(piece, target, bs);
        }

        // Build move step sequence
        List<MoveStep> steps = new List<MoveStep>();

        // Step 1: Move forward to target square
        steps.Add(MoveStep.MoveTo(piece, target, false));

        // Pre-calculate chain capture sequence
        // We need to simulate the chain to build steps before executing
        List<(PieceMove victim, Square destSquare)> chainSequence = CalculateChainSequence(piece, target, bs);

        // Add capture and move steps for each chain link
        foreach (var link in chainSequence)
        {
            // Capture the enemy
            steps.Add(MoveStep.Capture(piece, link.victim));

            // Move to that square (the captured piece's square)
            steps.Add(MoveStep.MoveTo(piece, link.destSquare, false));
        }

        // Mark last step to record in history
        if (steps.Count > 0)
        {
            steps[steps.Count - 1].RecordInHistory = true;
        }

        // Execute the sequence - no callback needed, ability executor handles turn end
        controller.ExecuteSteps(steps, null);

        return true;
    }

    /// <summary>
    /// Calculate the chain capture sequence ahead of time for building MoveSteps.
    /// </summary>
    private List<(PieceMove victim, Square destSquare)> CalculateChainSequence(PieceMove piece, Square startTarget, BoardState bs)
    {
        var sequence = new List<(PieceMove, Square)>();

        int cx = startTarget.x;
        int cy = startTarget.y;
        int captureCount = 0;

        while (captureCount < _params.maxChainCaptures)
        {
            PieceMove chainTarget = FindDiagonalEnemy(piece, cx, cy, bs);
            if (chainTarget == null) break;

            Square destSquare = piece.getSquare(chainTarget.curx, chainTarget.cury);
            if (destSquare == null) break;

            sequence.Add((chainTarget, destSquare));

            cx = chainTarget.curx;
            cy = chainTarget.cury;
            captureCount++;

            Debug.Log("[Chain Strike] Will capture " + chainTarget.printPieceName() + " at (" + cx + "," + cy + ")");
        }

        return sequence;
    }

    /// <summary>
    /// Fallback instant execution when MultiStepMoveController is unavailable.
    /// </summary>
    private bool ExecuteInstant(PieceMove piece, Square target, BoardState bs)
    {
        piece.movePiece(target.x, target.y, target);

        int captures = 0;
        int cx = piece.curx;
        int cy = piece.cury;

        while (captures < _params.maxChainCaptures)
        {
            PieceMove chainTarget = FindDiagonalEnemy(piece, cx, cy, bs);
            if (chainTarget == null) break;

            piece.gm.TryCapture(piece, chainTarget);
            cx = chainTarget.curx;
            cy = chainTarget.cury;
            captures++;
            Debug.Log("Chain Strike captures " + chainTarget.printPieceName() + "! (" + captures + ")");
        }

        return true;
    }

    private PieceMove FindDiagonalEnemy(PieceMove piece, int x, int y, BoardState bs)
    {
        (int dx, int dy)[] diagonals = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        foreach (var d in diagonals)
        {
            int nx = x + d.dx;
            int ny = y + d.dy;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove target = bs.GetPieceAt(nx, ny);
            if (target != null && target.color != piece.color && target.piece != ChessConstants.KING)
            {
                return target;
            }
        }
        return null;
    }
}
