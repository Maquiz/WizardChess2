using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Chess AI opponent controller. Detects when it's the AI's turn,
/// evaluates candidate moves/abilities, and executes the best one.
/// Supports 3 difficulty levels: Easy (0), Medium (1), Hard (2).
/// </summary>
public class ChessAI : MonoBehaviour
{
    private GameMaster gm;
    private int difficulty;     // 0=Easy, 1=Medium, 2=Hard
    private int aiColor;        // ChessConstants.BLACK by default
    private bool isThinking;    // prevents multiple coroutines
    private float thinkDelay;   // seconds before executing move
    private float thinkStartTime; // watchdog: when thinking started
    private const float THINK_TIMEOUT = 10f; // max seconds before force-reset
    private bool boardReady;      // latches true once all pieces are registered

    private struct AIAction
    {
        public PieceMove piece;
        public Square target;
        public float score;
        public bool isAbility;

        public AIAction(PieceMove piece, Square target, float score, bool isAbility)
        {
            this.piece = piece;
            this.target = target;
            this.score = score;
            this.isAbility = isAbility;
        }
    }

    /// <summary>
    /// Initialize the AI with references and settings.
    /// </summary>
    public void Init(GameMaster gameMaster, int diff, int color)
    {
        gm = gameMaster;
        difficulty = diff;
        aiColor = color;
        isThinking = false;

        switch (difficulty)
        {
            case 0: thinkDelay = 1.0f; break;   // Easy
            case 1: thinkDelay = 0.8f; break;   // Medium
            case 2: thinkDelay = 0.6f; break;   // Hard
            default: thinkDelay = 0.8f; break;
        }

        Debug.Log("[AI] Initialized — Difficulty: " + DifficultyName() + ", Color: " +
                  (aiColor == ChessConstants.BLACK ? "Black" : "White"));
    }

    void Update()
    {
        if (gm == null) return;

        // Watchdog: if thinking for too long, force reset to prevent permanent lock
        if (isThinking)
        {
            if (Time.time - thinkStartTime > THINK_TIMEOUT)
            {
                Debug.LogWarning("[AI] Think timeout — resetting isThinking to prevent deadlock.");
                isThinking = false;
            }
            return;
        }

        // Don't act if game is over
        if (gm.currentGameState == GameState.WhiteWins ||
            gm.currentGameState == GameState.BlackWins ||
            gm.currentGameState == GameState.Stalemate ||
            gm.currentGameState == GameState.Draw)
        {
            return;
        }

        // Don't act during draft phase
        if (gm.isDraftPhase) return;

        // Don't act during ability mode (shouldn't happen, but safety check)
        if (gm.abilityExecutor != null && gm.abilityExecutor.isInAbilityMode) return;

        // Wait for board to be fully set up (pieces fall onto squares via physics).
        // Once ready, latch the flag so captures don't re-trigger the wait.
        if (!boardReady)
        {
            if (gm.boardState == null ||
                gm.boardState.GetAllPieces(ChessConstants.WHITE).Count < 16 ||
                gm.boardState.GetAllPieces(ChessConstants.BLACK).Count < 16)
            {
                return;
            }
            boardReady = true;
        }

        if (gm.currentMove == aiColor)
        {
            thinkStartTime = Time.time;
            StartCoroutine(ThinkAndMove());
        }
    }

    /// <summary>
    /// Returns true if it's currently the AI's turn.
    /// Used by GameMaster to block human input.
    /// </summary>
    public bool IsAITurn()
    {
        return gm != null && gm.currentMove == aiColor;
    }

    /// <summary>
    /// Main AI coroutine: gather candidates, score them, pick best, execute.
    /// Wrapped in try-finally to guarantee isThinking is reset even on exceptions.
    /// </summary>
    private IEnumerator ThinkAndMove()
    {
        isThinking = true;

        // Natural thinking delay
        yield return new WaitForSeconds(thinkDelay);

        try
        {
            // Safety: re-check game state after delay
            if (gm.currentGameState == GameState.WhiteWins ||
                gm.currentGameState == GameState.BlackWins ||
                gm.currentGameState == GameState.Stalemate ||
                gm.currentGameState == GameState.Draw ||
                gm.currentMove != aiColor)
            {
                yield break;
            }

            List<AIAction> candidates = new List<AIAction>();

            // 1. Collect all normal moves
            List<PieceMove> aiPieces = gm.boardState.GetAllPieces(aiColor);
            // Copy list to avoid modification during iteration
            List<PieceMove> piecesCopy = new List<PieceMove>(aiPieces);

            foreach (PieceMove piece in piecesCopy)
            {
                if (piece == null || piece.gameObject == null) continue;

                // Skip stunned pieces
                if (piece.elementalPiece != null && piece.elementalPiece.IsStunned())
                    continue;

                piece.createPieceMoves(piece.piece);

                // Copy moves list since it may be modified
                List<Square> moveCopy = new List<Square>(piece.moves);
                foreach (Square sq in moveCopy)
                {
                    if (sq == null) continue;
                    float score = EvaluateMove(piece, sq);
                    candidates.Add(new AIAction(piece, sq, score, false));
                }

                piece.hideMovesHelper();
            }

            // 2. Collect ability candidates (Medium and Hard only)
            if (difficulty >= 1)
            {
                CollectAbilityCandidates(piecesCopy, candidates);
            }

            // 3. Pick and execute — try multiple candidates if first fails
            if (candidates.Count > 0)
            {
                // Sort by score descending so we can try the best moves first
                candidates.Sort((a, b) => b.score.CompareTo(a.score));

                bool executed = false;

                // First try ability if best is ability
                AIAction best = PickBest(candidates);
                if (best.isAbility)
                {
                    executed = ExecuteAbility(best.piece, best.target);
                }

                // Try normal moves in order of score until one succeeds
                if (!executed)
                {
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        AIAction action = candidates[i];
                        if (action.isAbility) continue;
                        if (action.piece == null || action.target == null) continue;

                        if (ExecuteMove(action.piece, action.target))
                        {
                            executed = true;
                            break;
                        }
                    }
                }

                if (!executed)
                {
                    Debug.LogWarning("[AI] All candidate moves failed. Ending turn to avoid deadlock.");
                    gm.EndTurn();
                }
            }
            else
            {
                Debug.Log("[AI] No legal moves available.");
            }
        }
        finally
        {
            isThinking = false;
        }
    }

    // ========== Difficulty-Specific Evaluation ==========

    private float EvaluateMove(PieceMove piece, Square target)
    {
        switch (difficulty)
        {
            case 0: return EvaluateEasy(piece, target);
            case 1: return EvaluateMedium(piece, target);
            case 2: return EvaluateHard(piece, target);
            default: return EvaluateMedium(piece, target);
        }
    }

    /// <summary>
    /// Easy: Intentionally bad moves - tries to lose by giving away pieces.
    /// Prefers moving valuable pieces into danger and avoids capturing.
    /// </summary>
    private float EvaluateEasy(PieceMove piece, Square target)
    {
        float score = 0f;
        int opponentColor = (piece.color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;

        // PENALTY for capturing (we don't want to take pieces)
        PieceMove victim = gm.boardState.GetPieceAt(target.x, target.y);
        if (victim != null && victim.color != piece.color)
        {
            score -= AIEvaluation.GetPieceValue(victim.piece) * 2f;
        }

        // BONUS for moving into attacked squares (give away our pieces!)
        if (gm.boardState.IsSquareAttackedBy(target.x, target.y, opponentColor))
        {
            // Higher bonus for more valuable pieces being sacrificed
            score += AIEvaluation.GetPieceValue(piece.piece) * 3f;
        }

        // BONUS for moving pieces away from safe squares into the open
        // Prefer moving valuable pieces (Queen, Rook) over pawns
        score += AIEvaluation.GetPieceValue(piece.piece) * 0.5f;

        // Avoid moving King into check (still need legal moves)
        if (piece.piece == ChessConstants.KING)
        {
            if (gm.boardState.IsSquareAttackedBy(target.x, target.y, opponentColor))
            {
                score -= 1000f; // Can't actually move into check
            }
        }

        // Some randomness to vary the bad moves
        score += Random.Range(0f, 50f);

        return score;
    }

    /// <summary>
    /// Medium: full positional evaluation with slight randomness.
    /// </summary>
    private float EvaluateMedium(PieceMove piece, Square target)
    {
        float score = AIEvaluation.ScoreMove(piece, target, gm.boardState,
                                              gm.squareEffectManager, gm.turnNumber);
        score += Random.Range(0f, 20f);
        return score;
    }

    /// <summary>
    /// Hard: full evaluation with hanging piece analysis, minimal randomness.
    /// </summary>
    private float EvaluateHard(PieceMove piece, Square target)
    {
        float score = AIEvaluation.ScoreMoveWithHangingAnalysis(piece, target, gm.boardState,
                                                                 gm.squareEffectManager, gm.turnNumber);
        score += Random.Range(0f, 5f);

        // Bonus for attacking undefended enemy pieces
        int opponentColor = (piece.color == ChessConstants.WHITE) ? ChessConstants.BLACK : ChessConstants.WHITE;
        int friendlyColor = piece.color;
        PieceMove targetPiece = gm.boardState.GetPieceAt(target.x, target.y);
        if (targetPiece != null && targetPiece.color == opponentColor)
        {
            if (!gm.boardState.IsSquareAttackedBy(target.x, target.y, opponentColor))
            {
                // Undefended enemy piece — free capture
                score += 20f;
            }
        }

        return score;
    }

    // ========== Ability Collection ==========

    private void CollectAbilityCandidates(List<PieceMove> pieces, List<AIAction> candidates)
    {
        // Can't use abilities while in check
        if (gm.boardState.IsKingInCheck(aiColor)) return;

        foreach (PieceMove piece in pieces)
        {
            if (piece == null) continue;
            if (piece.elementalPiece == null) continue;
            if (piece.elementalPiece.IsStunned()) continue;
            if (piece.elementalPiece.active == null) continue;
            if (piece.elementalPiece.cooldown == null || !piece.elementalPiece.cooldown.IsReady) continue;

            if (!piece.elementalPiece.active.CanActivate(piece, gm.boardState, gm.squareEffectManager))
                continue;

            List<Square> targets = piece.elementalPiece.active.GetTargetSquares(piece, gm.boardState);
            if (targets == null || targets.Count == 0) continue;

            foreach (Square t in targets)
            {
                float score = AIEvaluation.ScoreAbilityUse(piece, t, gm.boardState, gm.squareEffectManager);

                // Medium: penalty to avoid overusing abilities
                if (difficulty == 1)
                {
                    score -= 30f;
                }

                candidates.Add(new AIAction(piece, t, score, true));
            }
        }
    }

    // ========== Move Selection ==========

    private AIAction PickBest(List<AIAction> candidates)
    {
        if (candidates.Count == 0)
            return default;

        if (difficulty == 0)
        {
            // Easy: picks highest score (which is now the WORST move due to inverted evaluation)
            // Random from top 3 worst moves for variety
            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            int topN = Mathf.Min(3, candidates.Count);
            return candidates[Random.Range(0, topN)];
        }
        else
        {
            // Medium/Hard: best scoring
            AIAction best = candidates[0];
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].score > best.score)
                {
                    best = candidates[i];
                }
            }
            return best;
        }
    }

    private AIAction PickBestNormalMove(List<AIAction> candidates)
    {
        AIAction best = default;
        bool found = false;

        foreach (AIAction action in candidates)
        {
            if (action.isAbility) continue;
            if (!found || action.score > best.score)
            {
                best = action;
                found = true;
            }
        }

        return best;
    }

    // ========== Move Execution ==========

    /// <summary>
    /// Execute a normal move. Returns true if successful, false if blocked (e.g. by passive).
    /// For multi-step moves (Lightning Knight double-jump), uses coroutine with delays.
    /// </summary>
    private bool ExecuteMove(PieceMove piece, Square target)
    {
        if (piece == null || target == null) return false;
        if (piece.gameObject == null) return false;

        // Check for capture
        PieceMove victim = gm.boardState.GetPieceAt(target.x, target.y);
        bool isCapture = victim != null && victim.color != piece.color;

        // Regenerate moves (needed for internal state)
        piece.createPieceMoves(piece.piece);

        if (isCapture)
        {
            if (!gm.TryCapture(piece, victim))
            {
                // Capture blocked by passive — caller should try next candidate
                Debug.Log("[AI] Capture blocked by passive ability, trying next move.");
                piece.hideMovesHelper();
                return false;
            }
        }

        // Log before move (capture or normal)
        if (isCapture)
        {
            GameLogUI.LogCapture(gm.turnNumber, aiColor, piece, victim, target.x, target.y);
        }
        else
        {
            GameLogUI.LogPieceMove(gm.turnNumber, aiColor, piece, target.x, target.y);
        }

        // Check for Lightning Knight double-jump (multi-step move)
        KnightMoveData moveData = LightningKnightPassive.GetMoveData(piece, target.x, target.y);
        if (moveData != null && moveData.IsDoubleJump && gm.multiStepController != null)
        {
            piece.hideMovesHelper();
            ExecuteDoubleJumpAI(piece, moveData, target);
            return true;  // Coroutine handles EndTurn
        }

        // Standard single move
        piece.movePiece(target.x, target.y, target);

        // Clean up selection state
        gm.isPieceSelected = false;
        gm.selectedPiece = null;

        gm.EndTurn();

        Debug.Log("[AI] " + DifficultyName() + " moved " + piece.printPieceName() +
                  " to " + piece.printSquare(target.x, target.y));
        return true;
    }

    /// <summary>
    /// Execute a Lightning Knight double-jump with visible animation between steps.
    /// </summary>
    private void ExecuteDoubleJumpAI(PieceMove piece, KnightMoveData moveData, Square finalDest)
    {
        var steps = new List<MoveStep>();

        // Step 1: Move to intermediate L-jump square
        steps.Add(MoveStep.MoveTo(piece, moveData.IntermediateSquare, false));

        // Step 2: Move to final destination (record in history)
        steps.Add(MoveStep.MoveTo(piece, finalDest, true));

        // Execute with AI delays (0.5s between steps)
        gm.multiStepController.ExecuteSteps(steps, true, () =>
        {
            gm.isPieceSelected = false;
            gm.selectedPiece = null;
            gm.EndTurn();

            Debug.Log("[AI] " + DifficultyName() + " double-jumped " + piece.printPieceName() +
                      " via " + piece.printSquare(moveData.IntermediateSquare.x, moveData.IntermediateSquare.y) +
                      " to " + piece.printSquare(finalDest.x, finalDest.y));
        });
    }

    private bool ExecuteAbility(PieceMove piece, Square target)
    {
        if (piece == null || target == null) return false;
        if (gm.abilityExecutor == null) return false;

        if (gm.abilityExecutor.EnterAbilityMode(piece))
        {
            if (gm.abilityExecutor.TryExecuteOnSquare(target.x, target.y))
            {
                GameLogUI.LogAbility(gm.turnNumber, aiColor, piece, target.x, target.y);

                gm.isPieceSelected = false;
                gm.selectedPiece = null;
                gm.deSelectPiece();
                gm.EndTurn();

                Debug.Log("[AI] " + DifficultyName() + " used ability: " +
                          piece.printPieceName() + " on " + piece.printSquare(target.x, target.y));
                return true;
            }
            else
            {
                gm.abilityExecutor.ExitAbilityMode();
                Debug.Log("[AI] Ability execution failed, falling back to normal move.");
                return false;
            }
        }

        return false;
    }

    // ========== Utility ==========

    private string DifficultyName()
    {
        switch (difficulty)
        {
            case 0: return "Easy";
            case 1: return "Medium";
            case 2: return "Hard";
            default: return "Unknown";
        }
    }
}
