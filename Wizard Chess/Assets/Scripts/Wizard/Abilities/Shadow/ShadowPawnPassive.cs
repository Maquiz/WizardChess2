using System.Collections.Generic;

/// <summary>
/// Shadow Step: After a non-capture move, become Veiled (piece type hidden from opponent).
/// </summary>
public class ShadowPawnPassive : IPassiveAbility
{
    private readonly ShadowPawnPassiveParams _params;

    public ShadowPawnPassive() { _params = new ShadowPawnPassiveParams(); }
    public ShadowPawnPassive(ShadowPawnPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        // Non-capture move grants Veiled status
        if (piece.elementalPiece != null)
        {
            piece.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, _params.veilDuration);
        }
    }
}
