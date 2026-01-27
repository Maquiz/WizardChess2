using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Auto-assigns Fire element to all White pieces and Earth element to all Black pieces.
/// Bypasses the draft system for quick Fire vs Earth gameplay.
/// Also attaches UI components (SquareEffectUI, ElementIndicatorUI) at runtime.
/// </summary>
public class FireVsEarthSetup : MonoBehaviour
{
    private GameMaster gm;
    private bool applied = false;

    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
    }

    void Update()
    {
        // Wait until pieces have been placed on the board (via OnTriggerEnter -> setIntitialPiece)
        if (!applied && gm != null && gm.boardState != null)
        {
            List<PieceMove> whites = gm.boardState.GetAllPieces(ChessConstants.WHITE);
            List<PieceMove> blacks = gm.boardState.GetAllPieces(ChessConstants.BLACK);

            // Both sides should have 16 pieces at game start
            if (whites.Count >= 16 && blacks.Count >= 16)
            {
                ApplyElements(whites, blacks);
                AttachSquareEffectUI();
                AttachElementIndicatorUI();
                AttachElementParticleUI();
                applied = true;
                Debug.Log("[WizardChess] Fire vs Earth setup complete! White=Fire, Black=Earth");
                Debug.Log("[WizardChess] Press Q with a piece selected to use its active ability.");
            }
        }
    }

    private void ApplyElements(List<PieceMove> whites, List<PieceMove> blacks)
    {
        // White = Fire
        foreach (PieceMove pm in whites)
        {
            ApplyElementToPiece(pm, ChessConstants.ELEMENT_FIRE);
        }

        // Black = Earth
        foreach (PieceMove pm in blacks)
        {
            ApplyElementToPiece(pm, ChessConstants.ELEMENT_EARTH);
        }

        // Earth Queen passive: set stoneWallBonusHP from config
        if (gm.squareEffectManager != null)
        {
            var cfg = AbilityBalanceConfig.Instance;
            gm.squareEffectManager.stoneWallBonusHP = cfg != null ? cfg.earth.queenPassive.bonusHP : 1;
        }
    }

    private void ApplyElementToPiece(PieceMove pm, int elementId)
    {
        if (pm == null || pm.gameObject == null) return;

        // Don't double-apply
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

        string elementName = elementId == ChessConstants.ELEMENT_FIRE ? "Fire" : "Earth";
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
