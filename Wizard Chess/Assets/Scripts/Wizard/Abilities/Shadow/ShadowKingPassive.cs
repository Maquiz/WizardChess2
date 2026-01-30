using System.Collections.Generic;

/// <summary>
/// Cloak of Shadows: King is always Veiled (type hidden), but check status is still revealed.
/// </summary>
public class ShadowKingPassive : IPassiveAbility
{
    private PieceMove _piece;

    public ShadowKingPassive() { }
    public ShadowKingPassive(ShadowKingPassiveParams p) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (_piece == null) _piece = piece;
        return moves;
    }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }

    public void OnTurnStart(int currentTurnColor)
    {
        // King is permanently Veiled
        if (_piece != null && _piece.elementalPiece != null && _piece.color == currentTurnColor)
        {
            // Permanent veil (but note: check is still visible for game rules)
            _piece.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, 999, true);
        }
    }
}
