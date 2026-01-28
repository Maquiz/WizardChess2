using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Applies element assignments from DraftData (populated by menu deck selection).
/// Modeled on FireVsEarthSetup but uses per-piece element mapping from deck data
/// rather than uniform element per team.
/// </summary>
public class DeckBasedSetup : MonoBehaviour
{
    private GameMaster gm;
    private DraftData draftData;
    private bool applied = false;

    public void Init(GameMaster gameMaster, DraftData data)
    {
        gm = gameMaster;
        draftData = data;
    }

    void Update()
    {
        if (!applied && gm != null && gm.boardState != null && draftData != null)
        {
            List<PieceMove> whites = gm.boardState.GetAllPieces(ChessConstants.WHITE);
            List<PieceMove> blacks = gm.boardState.GetAllPieces(ChessConstants.BLACK);

            if (whites.Count >= 16 && blacks.Count >= 16)
            {
                ApplyElements();
                AttachSquareEffectUI();
                AttachElementIndicatorUI();
                AttachElementParticleUI();
                applied = true;
                gm.isSetupComplete = true;
                Debug.Log("[WizardChess] Deck-based setup complete!");
                Debug.Log("[WizardChess] Press Q with a piece selected to use its active ability.");
            }
        }
    }

    private void ApplyElements()
    {
        bool hasEarthQueen = false;

        // Use boardState (like FireVsEarthSetup) — reliable regardless of Inspector arrays.
        // Derive each piece's deck-slot index from its starting position.
        List<PieceMove> whites = gm.boardState.GetAllPieces(ChessConstants.WHITE);
        List<PieceMove> blacks = gm.boardState.GetAllPieces(ChessConstants.BLACK);

        Debug.Log("[WizardChess] Applying deck elements: " + whites.Count + " white, " + blacks.Count + " black pieces");

        foreach (PieceMove pm in whites)
        {
            int idx = GetPieceIndex(pm);
            if (idx < 0) { Debug.LogWarning("[WizardChess] Could not map white " + pm.printPieceName() + " to deck index"); continue; }
            int elementId = draftData.GetElement(ChessConstants.WHITE, idx);
            ApplyElementToPiece(pm, elementId);
            if (elementId == ChessConstants.ELEMENT_EARTH && pm.piece == ChessConstants.QUEEN)
                hasEarthQueen = true;
        }

        foreach (PieceMove pm in blacks)
        {
            int idx = GetPieceIndex(pm);
            if (idx < 0) { Debug.LogWarning("[WizardChess] Could not map black " + pm.printPieceName() + " to deck index"); continue; }
            int elementId = draftData.GetElement(ChessConstants.BLACK, idx);
            ApplyElementToPiece(pm, elementId);
            if (elementId == ChessConstants.ELEMENT_EARTH && pm.piece == ChessConstants.QUEEN)
                hasEarthQueen = true;
        }

        // Earth Queen passive: set stoneWallBonusHP from config
        if (hasEarthQueen && gm.squareEffectManager != null)
        {
            var cfg = AbilityBalanceConfig.Instance;
            gm.squareEffectManager.stoneWallBonusHP = cfg != null ? cfg.earth.queenPassive.bonusHP : 1;
        }
    }

    /// <summary>
    /// Derive the deck-slot index (0-15) from a piece's type and starting column.
    /// Runs before any moves, so curx reflects the initial file.
    /// 0-7 = Pawns (by column), 8-9 = Rooks, 10-11 = Knights, 12-13 = Bishops, 14 = Queen, 15 = King.
    /// </summary>
    private int GetPieceIndex(PieceMove pm)
    {
        switch (pm.piece)
        {
            case ChessConstants.PAWN:
                return pm.curx; // 0-7
            case ChessConstants.ROOK:
                return pm.curx <= 3 ? 8 : 9; // a-file=8, h-file=9
            case ChessConstants.KNIGHT:
                return pm.curx <= 3 ? 10 : 11; // b-file=10, g-file=11
            case ChessConstants.BISHOP:
                return pm.curx <= 3 ? 12 : 13; // c-file=12, f-file=13
            case ChessConstants.QUEEN:
                return 14;
            case ChessConstants.KING:
                return 15;
            default:
                return -1;
        }
    }

    private void ApplyElementToPiece(PieceMove pm, int elementId)
    {
        if (pm == null || pm.gameObject == null) return;
        if (pm.elementalPiece != null) return;

        ElementalPiece ep = pm.gameObject.AddComponent<ElementalPiece>();

        IPassiveAbility passive = AbilityFactory.CreatePassive(elementId, pm.piece);
        IActiveAbility active = AbilityFactory.CreateActive(elementId, pm.piece);
        int cooldown = AbilityFactory.GetCooldown(elementId, pm.piece);

        ep.Init(elementId, passive, active, cooldown);

        // Fire Queen: immune to fire squares
        if (elementId == ChessConstants.ELEMENT_FIRE && pm.piece == ChessConstants.QUEEN)
        {
            ep.AddImmunity(SquareEffectType.Fire);
        }

        string elementName = AbilityInfo.GetElementName(elementId);
        Debug.Log("[WizardChess] " + pm.printPieceName() + " assigned " + elementName);
    }

    private void AttachSquareEffectUI()
    {
        if (gm.boardRows == null) return;

        foreach (GameObject row in gm.boardRows)
        {
            if (row == null) continue;
            for (int i = 0; i < row.transform.childCount; i++)
            {
                GameObject squareObj = row.transform.GetChild(i).gameObject;
                if (squareObj.GetComponent<SquareEffectUI>() == null)
                {
                    squareObj.AddComponent<SquareEffectUI>();
                }
            }
        }
    }

    private void AttachElementIndicatorUI()
    {
        List<PieceMove> allPieces = new List<PieceMove>();
        allPieces.AddRange(gm.boardState.GetAllPieces(ChessConstants.WHITE));
        allPieces.AddRange(gm.boardState.GetAllPieces(ChessConstants.BLACK));

        foreach (PieceMove pm in allPieces)
        {
            if (pm.gameObject.GetComponent<ElementIndicatorUI>() == null)
            {
                pm.gameObject.AddComponent<ElementIndicatorUI>();
            }
        }
    }

    private void AttachElementParticleUI()
    {
        List<PieceMove> allPieces = new List<PieceMove>();
        allPieces.AddRange(gm.boardState.GetAllPieces(ChessConstants.WHITE));
        allPieces.AddRange(gm.boardState.GetAllPieces(ChessConstants.BLACK));

        foreach (PieceMove pm in allPieces)
        {
            if (pm.gameObject.GetComponent<ElementParticleUI>() == null)
            {
                pm.gameObject.AddComponent<ElementParticleUI>();
            }
        }
    }
}
