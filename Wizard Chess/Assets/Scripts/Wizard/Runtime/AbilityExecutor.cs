using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the "use ability" turn mode. When a player clicks the ability button,
/// this enters ability targeting mode, highlights valid targets, and executes
/// the ability when a target is clicked.
/// </summary>
public class AbilityExecutor : MonoBehaviour
{
    private GameMaster gm;
    private SquareEffectManager sem;

    // Ability mode state
    public bool isInAbilityMode { get; private set; }
    private PieceMove abilityPiece;
    private ElementalPiece abilityElemental;
    private List<Square> targetSquares = new List<Square>();
    private HashSet<(int x, int y)> targetSet = new HashSet<(int x, int y)>();

    public void Init(GameMaster gameMaster, SquareEffectManager squareEffectManager)
    {
        gm = gameMaster;
        sem = squareEffectManager;
        isInAbilityMode = false;
    }

    /// <summary>
    /// Enter ability targeting mode for the given piece.
    /// </summary>
    public bool EnterAbilityMode(PieceMove piece)
    {
        if (piece == null || piece.elementalPiece == null) return false;

        // Cannot use abilities while in check — must resolve with a normal move
        if (gm.boardState != null && gm.boardState.IsKingInCheck(piece.color))
        {
            Debug.Log("[Ability] Cannot use abilities while in check.");
            return false;
        }

        ElementalPiece ep = piece.elementalPiece;
        if (ep.active == null || ep.cooldown == null || !ep.cooldown.IsReady) return false;
        if (!ep.active.CanActivate(piece, gm.boardState, sem)) return false;

        // Get valid targets first — don't enter ability mode if there are none
        List<Square> targets = ep.active.GetTargetSquares(piece, gm.boardState);
        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[Ability] No valid targets available.");
            return false;
        }

        abilityPiece = piece;
        abilityElemental = ep;
        isInAbilityMode = true;

        targetSquares = targets;
        targetSet.Clear();
        foreach (var sq in targetSquares)
        {
            targetSet.Add((sq.x, sq.y));
            sq.showMoveSquare.SetActive(true);
        }

        return true;
    }

    /// <summary>
    /// Try to execute the ability on the given square.
    /// Returns true if the ability was executed (turn is consumed).
    /// </summary>
    public bool TryExecuteOnSquare(int x, int y)
    {
        if (!isInAbilityMode || abilityPiece == null || abilityElemental == null) return false;

        if (!targetSet.Contains((x, y))) return false;

        Square target = abilityPiece.getSquare(x, y);
        if (target == null) return false;

        bool success = abilityElemental.active.Execute(abilityPiece, target, gm.boardState, sem);
        if (success)
        {
            abilityElemental.cooldown.StartCooldown();
            ExitAbilityMode();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Exit ability mode without executing.
    /// </summary>
    public void ExitAbilityMode()
    {
        // Hide target highlights
        foreach (var sq in targetSquares)
        {
            if (sq != null)
                sq.showMoveSquare.SetActive(false);
        }

        targetSquares.Clear();
        targetSet.Clear();
        abilityPiece = null;
        abilityElemental = null;
        isInAbilityMode = false;
    }

    /// <summary>
    /// Check if a square is a valid ability target.
    /// </summary>
    public bool IsValidTarget(int x, int y)
    {
        return targetSet.Contains((x, y));
    }
}
