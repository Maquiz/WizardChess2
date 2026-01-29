using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Tempest: Push enemies within range away. Pieces pushed off-board are captured.
/// Uses MultiStepMoveController for sequential animated pushes.
/// </summary>
public class LightningQueenActive : IActiveAbility
{
    private readonly LtQueenActiveParams _params;

    public LightningQueenActive() { _params = new LtQueenActiveParams(); }
    public LightningQueenActive(LtQueenActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        MultiStepMoveController controller = piece.gm.multiStepController;

        // Gather enemies to push
        List<PieceMove> toPush = new List<PieceMove>();
        for (int dx = -_params.detectionRange; dx <= _params.detectionRange; dx++)
        {
            for (int dy = -_params.detectionRange; dy <= _params.detectionRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove enemy = bs.GetPieceAt(nx, ny);
                // Never push kings - they cannot be captured in chess
                if (enemy != null && enemy.color != piece.color && enemy.piece != ChessConstants.KING)
                {
                    toPush.Add(enemy);
                }
            }
        }

        // If no controller, fall back to instant execution
        if (controller == null)
        {
            return ExecuteInstant(piece, toPush, bs);
        }

        // Pre-calculate all push results
        List<PushResult> pushResults = CalculatePushResults(piece, toPush, bs);

        // Build step sequence
        List<MoveStep> steps = new List<MoveStep>();

        foreach (var result in pushResults)
        {
            if (result.pushedOffBoard)
            {
                // Capture step for pieces pushed off board
                steps.Add(MoveStep.Custom(() =>
                {
                    piece.gm.takePiece(result.enemy);
                    Debug.Log(result.enemy.printPieceName() + " was pushed off the board by Tempest!");
                }, 0.15f));
            }
            else if (result.didMove)
            {
                // Move step for push
                PieceMove enemyRef = result.enemy;
                Square destSquareRef = result.destination;
                int fromX = result.fromX;
                int fromY = result.fromY;

                steps.Add(MoveStep.Custom(() =>
                {
                    // Update state
                    enemyRef.removePieceFromSquare();
                    bs.MovePiece(fromX, fromY, destSquareRef.x, destSquareRef.y);
                    enemyRef.setPieceLocation(destSquareRef.x, destSquareRef.y);
                    enemyRef.curSquare = destSquareRef;
                    destSquareRef.piece = enemyRef;
                    destSquareRef.taken = true;
                    enemyRef.last = destSquareRef.gameObject;

                    // Animate the push
                    Transform t = enemyRef.gameObject.transform;
                    t.DOMove(new Vector3(destSquareRef.transform.position.x, t.position.y, destSquareRef.transform.position.z), 0.3f);
                }, 0.35f));
            }
        }

        // Add final step to recalculate attacks
        steps.Add(MoveStep.Custom(() => bs.RecalculateAttacks()));

        // Execute sequence
        controller.ExecuteSteps(steps, null);

        return true;
    }

    /// <summary>
    /// Pre-calculate push destinations without modifying state.
    /// </summary>
    private List<PushResult> CalculatePushResults(PieceMove piece, List<PieceMove> toPush, BoardState bs)
    {
        var results = new List<PushResult>();

        // Create a temporary map of planned positions to avoid collisions
        Dictionary<(int, int), PieceMove> plannedPositions = new Dictionary<(int, int), PieceMove>();

        foreach (PieceMove enemy in toPush)
        {
            int dirX = System.Math.Sign(enemy.curx - piece.curx);
            int dirY = System.Math.Sign(enemy.cury - piece.cury);

            if (dirX == 0 && dirY == 0) continue;

            int startX = enemy.curx;
            int startY = enemy.cury;
            int finalX = enemy.curx;
            int finalY = enemy.cury;
            bool pushedOff = false;

            for (int push = 1; push <= _params.pushDistance; push++)
            {
                int nextX = finalX + dirX;
                int nextY = finalY + dirY;

                if (!bs.IsInBounds(nextX, nextY))
                {
                    pushedOff = true;
                    break;
                }

                // Check if square is empty (and not planned to be occupied)
                if (!bs.IsSquareEmpty(nextX, nextY) || plannedPositions.ContainsKey((nextX, nextY)))
                {
                    break;
                }

                finalX = nextX;
                finalY = nextY;
            }

            if (pushedOff)
            {
                results.Add(new PushResult(enemy, startX, startY, null, true));
            }
            else if (finalX != startX || finalY != startY)
            {
                Square destSq = piece.getSquare(finalX, finalY);
                if (destSq != null)
                {
                    plannedPositions[(finalX, finalY)] = enemy;
                    results.Add(new PushResult(enemy, startX, startY, destSq, false));
                }
            }
        }

        return results;
    }

    private class PushResult
    {
        public PieceMove enemy;
        public int fromX, fromY;
        public Square destination;
        public bool pushedOffBoard;
        public bool didMove => pushedOffBoard || destination != null;

        public PushResult(PieceMove enemy, int fromX, int fromY, Square dest, bool pushedOff)
        {
            this.enemy = enemy;
            this.fromX = fromX;
            this.fromY = fromY;
            this.destination = dest;
            this.pushedOffBoard = pushedOff;
        }
    }

    /// <summary>
    /// Fallback instant execution when controller unavailable.
    /// </summary>
    private bool ExecuteInstant(PieceMove piece, List<PieceMove> toPush, BoardState bs)
    {
        foreach (PieceMove enemy in toPush)
        {
            int dirX = System.Math.Sign(enemy.curx - piece.curx);
            int dirY = System.Math.Sign(enemy.cury - piece.cury);

            if (dirX == 0 && dirY == 0) continue;

            int finalX = enemy.curx;
            int finalY = enemy.cury;

            for (int push = 1; push <= _params.pushDistance; push++)
            {
                int nextX = finalX + dirX;
                int nextY = finalY + dirY;

                if (!bs.IsInBounds(nextX, nextY))
                {
                    piece.gm.takePiece(enemy);
                    Debug.Log(enemy.printPieceName() + " was pushed off the board by Tempest!");
                    finalX = -1;
                    break;
                }

                if (!bs.IsSquareEmpty(nextX, nextY))
                {
                    break;
                }

                finalX = nextX;
                finalY = nextY;
            }

            if (finalX >= 0 && (finalX != enemy.curx || finalY != enemy.cury))
            {
                Square destSq = piece.getSquare(finalX, finalY);
                if (destSq != null)
                {
                    enemy.removePieceFromSquare();
                    bs.MovePiece(enemy.curx, enemy.cury, finalX, finalY);
                    enemy.setPieceLocation(finalX, finalY);
                    enemy.curSquare = destSq;
                    destSq.piece = enemy;
                    destSq.taken = true;
                    enemy.last = destSq.gameObject;

                    Transform t = enemy.gameObject.transform;
                    t.DOMove(new Vector3(destSq.transform.position.x, t.position.y, destSq.transform.position.z), 0.3f);
                }
            }
        }

        bs.RecalculateAttacks();
        return true;
    }
}
