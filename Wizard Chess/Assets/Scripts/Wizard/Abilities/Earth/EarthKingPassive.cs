using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stone Shield: Once per game, when the Earth King would be captured,
/// the king survives and the attacking piece is destroyed instead.
/// </summary>
public class EarthKingPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;

    /// <summary>
    /// Stone Shield: When Earth King is about to be captured, destroy the attacker instead.
    /// Returns false to PREVENT the capture if shield activates.
    /// </summary>
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        // Only applies when Earth King is the defender
        if (defender.piece != ChessConstants.KING) return true;
        if (defender.elementalPiece == null) return true;
        if (defender.elementalPiece.hasUsedStoneShield) return true;

        // Activate Stone Shield
        defender.elementalPiece.hasUsedStoneShield = true;

        // Destroy the attacker instead
        attacker.pieceTaken();

        Debug.Log("[Earth] Stone Shield activated! " + attacker.printPieceName() + " was destroyed!");
        GameLogUI.Log("<color=#8B4513>Stone Shield! " + attacker.printPieceName() + " shattered against the Earth King!</color>");

        return false; // Prevent the capture
    }

    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }
}
