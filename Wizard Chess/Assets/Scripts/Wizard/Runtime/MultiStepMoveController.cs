using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Orchestrates sequential animations for multi-step moves and abilities.
/// Handles knight double-jumps, chain captures, pushes, and other multi-part actions
/// with proper delays between steps for visual clarity.
/// </summary>
public class MultiStepMoveController : MonoBehaviour
{
    private GameMaster gm;

    /// <summary>
    /// True while a multi-step sequence is executing.
    /// Used to block input during animation.
    /// </summary>
    public bool IsExecutingMultiStep { get; private set; }

    /// <summary>
    /// Delay between steps for AI moves (seconds).
    /// Allows player to see each step clearly.
    /// </summary>
    public float aiStepDelay = 0.5f;

    /// <summary>
    /// Delay between steps for player moves (seconds).
    /// Can be 0 for instant feedback, or small for visual clarity.
    /// </summary>
    public float playerStepDelay = 0.15f;

    /// <summary>
    /// Default animation duration for move steps.
    /// </summary>
    public float defaultAnimationDuration = 0.4f;

    /// <summary>
    /// Current piece being moved (for UI feedback).
    /// </summary>
    public PieceMove CurrentPiece { get; private set; }

    /// <summary>
    /// Initialize the controller with a reference to GameMaster.
    /// </summary>
    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
    }

    /// <summary>
    /// Execute a sequence of move steps with proper animations and delays.
    /// </summary>
    /// <param name="steps">List of MoveStep actions to execute in order.</param>
    /// <param name="isAIMove">If true, uses longer delays between steps.</param>
    /// <param name="onComplete">Callback when all steps complete.</param>
    public void ExecuteSteps(List<MoveStep> steps, bool isAIMove, Action onComplete = null)
    {
        if (steps == null || steps.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(ExecuteStepsCoroutine(steps, isAIMove, onComplete));
    }

    /// <summary>
    /// Execute steps synchronously (for single player turn actions).
    /// Overload that auto-detects AI vs player based on current turn.
    /// </summary>
    public void ExecuteSteps(List<MoveStep> steps, Action onComplete = null)
    {
        bool isAI = false;
        ChessAI ai = gm?.GetComponent<ChessAI>();
        if (ai != null && ai.IsAITurn())
        {
            isAI = true;
        }
        ExecuteSteps(steps, isAI, onComplete);
    }

    private IEnumerator ExecuteStepsCoroutine(List<MoveStep> steps, bool isAIMove, Action onComplete)
    {
        IsExecutingMultiStep = true;
        float stepDelay = isAIMove ? aiStepDelay : playerStepDelay;

        Debug.Log($"[MultiStep] Starting {steps.Count} step sequence (AI: {isAIMove})");

        for (int i = 0; i < steps.Count; i++)
        {
            MoveStep step = steps[i];
            Debug.Log($"[MultiStep] Step {i + 1}/{steps.Count}: {step}");

            CurrentPiece = step.Piece;

            switch (step.Type)
            {
                case MoveStepType.MoveTo:
                    yield return ExecuteMoveToStep(step);
                    break;

                case MoveStepType.Capture:
                    yield return ExecuteCaptureStep(step);
                    break;

                case MoveStepType.Custom:
                    yield return ExecuteCustomStep(step);
                    break;
            }

            // Add delay between steps (but not after the last step)
            if (i < steps.Count - 1 && stepDelay > 0)
            {
                yield return new WaitForSeconds(stepDelay);
            }
        }

        CurrentPiece = null;
        IsExecutingMultiStep = false;

        Debug.Log("[MultiStep] Sequence complete");
        onComplete?.Invoke();
    }

    private IEnumerator ExecuteMoveToStep(MoveStep step)
    {
        if (step.Piece == null || step.Destination == null)
        {
            Debug.LogWarning("[MultiStep] MoveTo step has null piece or destination");
            yield break;
        }

        float duration = step.Duration > 0 ? step.Duration : defaultAnimationDuration;

        // If there's an intermediate square (e.g., for knight double-jump),
        // animate to intermediate first, then to final destination
        if (step.IntermediateSquare != null)
        {
            // First leg: to intermediate
            step.Piece.UpdateBoardStateOnly(step.IntermediateSquare.x, step.IntermediateSquare.y, step.IntermediateSquare);
            yield return step.Piece.AnimateToSquareCoroutine(step.IntermediateSquare, duration);

            // Brief pause at intermediate
            yield return new WaitForSeconds(aiStepDelay * 0.5f);

            // Second leg: to final destination
            step.Piece.UpdateBoardStateOnly(step.Destination.x, step.Destination.y, step.Destination);
            yield return step.Piece.AnimateToSquareCoroutine(step.Destination, duration);
        }
        else
        {
            // Simple single-leg move
            step.Piece.UpdateBoardStateOnly(step.Destination.x, step.Destination.y, step.Destination);
            yield return step.Piece.AnimateToSquareCoroutine(step.Destination, duration);
        }

        // Record in move history if requested
        if (step.RecordInHistory)
        {
            ChessMove cm = new ChessMove(step.Piece);
            gm.moveHistory.Push(cm);
            step.Piece.createPieceMoves(step.Piece.piece);
        }
    }

    private IEnumerator ExecuteCaptureStep(MoveStep step)
    {
        if (step.CaptureTarget == null)
        {
            Debug.LogWarning("[MultiStep] Capture step has null target");
            yield break;
        }

        // Use GameMaster's TryCapture for proper passive hooks
        if (step.Piece != null)
        {
            gm.TryCapture(step.Piece, step.CaptureTarget);
        }
        else
        {
            // Direct capture without attacker (some effects)
            gm.takePiece(step.CaptureTarget);
        }

        // Brief visual pause after capture
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator ExecuteCustomStep(MoveStep step)
    {
        step.CustomAction?.Invoke();

        // Wait for specified duration if any
        if (step.Duration > 0)
        {
            yield return new WaitForSeconds(step.Duration);
        }
    }

    /// <summary>
    /// Helper to create a simple two-step move sequence (e.g., knight double-jump).
    /// </summary>
    public static List<MoveStep> CreateDoubleMove(PieceMove piece, Square intermediate, Square final)
    {
        var steps = new List<MoveStep>();

        // First step: move to intermediate
        steps.Add(MoveStep.MoveTo(piece, intermediate, false));

        // Second step: move to final destination (record history)
        steps.Add(MoveStep.MoveTo(piece, final, true));

        return steps;
    }

    /// <summary>
    /// Helper to create a single move with animation (wraps in list for controller).
    /// </summary>
    public static List<MoveStep> CreateSingleMove(PieceMove piece, Square destination)
    {
        return new List<MoveStep>
        {
            MoveStep.MoveTo(piece, destination, true)
        };
    }
}
