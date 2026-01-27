using System.Collections.Generic;

/// <summary>
/// Trail Blazer: After moving, the departure square becomes a Fire Square.
/// </summary>
public class FireRookPassive : IPassiveAbility
{
    private readonly FireRookPassiveParams _params;

    public FireRookPassive() { _params = new FireRookPassiveParams(); }
    public FireRookPassive(FireRookPassiveParams p) { _params = p; }

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
            sem.CreateEffect(fromX, fromY, SquareEffectType.Fire, _params.fireDuration, piece.color);
        }
    }
}
