using System.Collections.Generic;

/// <summary>
/// Mistress of Shadows: Immune to being revealed. All friendly Veil durations get a bonus.
/// </summary>
public class ShadowQueenPassive : IPassiveAbility
{
    private readonly ShadowQueenPassiveParams _params;
    private PieceMove _piece;

    public ShadowQueenPassive() { _params = new ShadowQueenPassiveParams(); }
    public ShadowQueenPassive(ShadowQueenPassiveParams p) { _params = p; }

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
        // Queen always has Veiled status (permanent)
        if (_piece != null && _piece.elementalPiece != null && _piece.color == currentTurnColor)
        {
            // Permanent veil that refreshes every turn
            _piece.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, 999, true);
        }
    }

    /// <summary>
    /// Get the veil bonus provided by this queen (used by other abilities).
    /// </summary>
    public int GetVeilBonus()
    {
        return _params.veilBonus;
    }
}
