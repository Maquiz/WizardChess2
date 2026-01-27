using System.Collections.Generic;

/// <summary>
/// Royal Inferno: Immune to Fire Squares; can move through them freely.
/// (Immunity is set in DraftManager.ApplyElementToPiece via ElementalPiece.AddImmunity)
/// This passive has no additional hooks — the immunity flag handles everything.
/// </summary>
public class FireQueenPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }
}
