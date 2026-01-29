using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Backdraft (CD: 8): All Fire Squares on the board capture enemy pieces (not kings)
/// adjacent to them, then all Fire Squares are removed.
/// Uses MultiStepMoveController for sequential animated captures.
/// </summary>
public class FireKingActive : IActiveAbility
{
    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        // Need at least one fire square on the board
        return sem.GetAllEffectsOfType(SquareEffectType.Fire).Count > 0;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        // Self-cast
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        MultiStepMoveController controller = piece.gm.multiStepController;

        List<SquareEffect> fireEffects = sem.GetAllEffectsOfType(SquareEffectType.Fire);
        List<PieceMove> toCaptureList = new List<PieceMove>();

        // Find all enemy pieces adjacent to fire squares
        foreach (var fire in fireEffects)
        {
            Square sq = piece.getSquare(fire.gameObject.GetComponent<Square>().x,
                                         fire.gameObject.GetComponent<Square>().y);
            if (sq == null) continue;

            foreach (var dir in ChessConstants.KingDirections)
            {
                int nx = sq.x + dir.x;
                int ny = sq.y + dir.y;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove adj = bs.GetPieceAt(nx, ny);
                if (adj != null && adj.color != piece.color && adj.piece != ChessConstants.KING)
                {
                    if (!toCaptureList.Contains(adj))
                        toCaptureList.Add(adj);
                }
            }
        }

        // If no controller, fall back to instant execution
        if (controller == null)
        {
            return ExecuteInstant(piece, toCaptureList, sem);
        }

        // Build step sequence
        List<MoveStep> steps = new List<MoveStep>();

        // Capture each enemy with delay between for visual effect
        foreach (var victim in toCaptureList)
        {
            steps.Add(MoveStep.Capture(piece, victim));
        }

        // Final step: remove all fire squares
        steps.Add(MoveStep.Custom(() =>
        {
            sem.RemoveAllEffectsOfType(SquareEffectType.Fire);
            Debug.Log("[Backdraft] Fire consumed - all fire squares removed.");
        }, 0.2f));

        // Execute sequence
        controller.ExecuteSteps(steps, null);

        return true;
    }

    /// <summary>
    /// Fallback instant execution when controller unavailable.
    /// </summary>
    private bool ExecuteInstant(PieceMove piece, List<PieceMove> toCaptureList, SquareEffectManager sem)
    {
        foreach (var p in toCaptureList)
        {
            piece.gm.TryCapture(piece, p);
        }

        sem.RemoveAllEffectsOfType(SquareEffectType.Fire);
        return true;
    }
}
