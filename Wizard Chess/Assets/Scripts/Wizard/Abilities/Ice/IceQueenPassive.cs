using System.Collections.Generic;

/// <summary>
/// Permafrost Aura: Queen is immune to all Ice effects (Ice squares, Frozen, Chilled).
/// </summary>
public class IceQueenPassive : IPassiveAbility
{
    private PieceMove _piece;

    public IceQueenPassive() { }
    public IceQueenPassive(IceQueenPassiveParams p) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        // Ensure immunity is set
        if (_piece == null && piece.elementalPiece != null)
        {
            _piece = piece;
            piece.elementalPiece.AddImmunity(SquareEffectType.Ice);
        }
        return moves;
    }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }

    public void OnTurnStart(int currentTurnColor)
    {
        // Ensure queen is immune to Ice and never has Frozen/Chilled status
        if (_piece != null && _piece.elementalPiece != null)
        {
            _piece.elementalPiece.AddImmunity(SquareEffectType.Ice);
            _piece.elementalPiece.RemoveStatusEffect(StatusEffectType.Frozen);
            _piece.elementalPiece.RemoveStatusEffect(StatusEffectType.Chilled);
        }
    }
}
