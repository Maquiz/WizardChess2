using System.Collections.Generic;

/// <summary>
/// Glacial Wake: After moving, leave Ice on the departure square.
/// </summary>
public class IceRookPassive : IPassiveAbility
{
    private readonly IceRookPassiveParams _params;

    public IceRookPassive() { _params = new IceRookPassiveParams(); }
    public IceRookPassive(IceRookPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        var sem = piece.gm.squareEffectManager;
        if (sem != null)
        {
            sem.CreateEffect(fromX, fromY, SquareEffectType.Ice, _params.iceDuration, piece.color);
        }
    }
}
