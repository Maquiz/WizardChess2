using UnityEngine;

/// <summary>
/// Orchestrates the pre-game element selection (draft) phase.
/// Manages draft flow: White picks, then Black picks, then game starts.
/// Sits as a same-scene overlay — no separate scene needed.
/// </summary>
public class DraftManager : MonoBehaviour
{
    private GameMaster gm;
    private DraftUI draftUI;
    public DraftData draftData { get; private set; }

    // Draft state
    public int currentDraftPlayer { get; private set; }
    public bool isDraftComplete { get; private set; }

    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
        draftData = new DraftData();
        isDraftComplete = false;
        currentDraftPlayer = ChessConstants.WHITE;
    }

    /// <summary>
    /// Start the draft phase. Sets GameMaster.isDraftPhase = true.
    /// </summary>
    public void StartDraft()
    {
        gm.isDraftPhase = true;
        isDraftComplete = false;
        currentDraftPlayer = ChessConstants.WHITE;
        draftData = new DraftData();

        // Show draft UI
        if (draftUI != null)
        {
            draftUI.ShowDraft(currentDraftPlayer);
        }
    }

    /// <summary>
    /// Called when a player confirms their draft selections.
    /// </summary>
    public void ConfirmPlayerDraft()
    {
        if (currentDraftPlayer == ChessConstants.WHITE)
        {
            // Switch to black's draft
            currentDraftPlayer = ChessConstants.BLACK;
            if (draftUI != null)
            {
                draftUI.ShowDraft(currentDraftPlayer);
            }
        }
        else
        {
            // Both players have drafted — apply and start game
            CompleteDraft();
        }
    }

    /// <summary>
    /// Skip the draft entirely (standard chess mode).
    /// </summary>
    public void SkipDraft()
    {
        isDraftComplete = true;
        gm.isDraftPhase = false;
    }

    /// <summary>
    /// Complete the draft and apply elements to pieces.
    /// </summary>
    private void CompleteDraft()
    {
        isDraftComplete = true;
        ApplyDraftToGame();

        // Hide draft UI
        if (draftUI != null)
        {
            draftUI.HideDraft();
        }

        gm.isDraftPhase = false;
    }

    /// <summary>
    /// Attach ElementalPiece components to all pieces based on draft selections.
    /// </summary>
    private void ApplyDraftToGame()
    {
        // Apply to white pieces
        for (int i = 0; i < gm.WPieces.Length && i < 16; i++)
        {
            int element = draftData.GetElement(ChessConstants.WHITE, i);
            if (element != ChessConstants.ELEMENT_NONE)
            {
                ApplyElementToPiece(gm.WPieces[i], element);
            }
        }

        // Apply to black pieces
        for (int i = 0; i < gm.BPieces.Length && i < 16; i++)
        {
            int element = draftData.GetElement(ChessConstants.BLACK, i);
            if (element != ChessConstants.ELEMENT_NONE)
            {
                ApplyElementToPiece(gm.BPieces[i], element);
            }
        }

        // Earth Queen passive: set stoneWallBonusHP from config
        if (gm.squareEffectManager != null)
        {
            var cfg = AbilityBalanceConfig.Instance;
            gm.squareEffectManager.stoneWallBonusHP = cfg != null ? cfg.earth.queenPassive.bonusHP : 1;
        }
    }

    /// <summary>
    /// Apply an element to a specific piece GameObject by adding ElementalPiece
    /// and creating the appropriate ability instances.
    /// </summary>
    private void ApplyElementToPiece(GameObject pieceObj, int elementId)
    {
        if (pieceObj == null) return;

        PieceMove pm = pieceObj.GetComponent<PieceMove>();
        if (pm == null) return;

        ElementalPiece ep = pieceObj.AddComponent<ElementalPiece>();

        IPassiveAbility passive = AbilityFactory.CreatePassive(elementId, pm.piece);
        IActiveAbility active = AbilityFactory.CreateActive(elementId, pm.piece);
        int cooldown = AbilityFactory.GetCooldown(elementId, pm.piece);

        ep.Init(elementId, passive, active, cooldown);

        // Set up element-specific immunities
        if (elementId == ChessConstants.ELEMENT_FIRE && pm.piece == ChessConstants.QUEEN)
        {
            // Fire Queen: immune to fire squares
            ep.AddImmunity(SquareEffectType.Fire);
        }
    }

    public void SetDraftUI(DraftUI ui)
    {
        draftUI = ui;
    }
}
