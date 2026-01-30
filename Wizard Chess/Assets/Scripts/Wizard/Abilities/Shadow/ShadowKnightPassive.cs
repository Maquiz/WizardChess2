using System.Collections.Generic;

/// <summary>
/// Phantom Rider: After capturing an enemy, become Veiled.
/// </summary>
public class ShadowKnightPassive : IPassiveAbility
{
    private readonly ShadowKnightPassiveParams _params;

    public ShadowKnightPassive() { _params = new ShadowKnightPassiveParams(); }
    public ShadowKnightPassive(ShadowKnightPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        // After capturing, become Veiled
        if (attacker.elementalPiece != null)
        {
            attacker.elementalPiece.AddStatusEffect(StatusEffectType.Veiled, _params.veilDuration);
        }
    }
}
