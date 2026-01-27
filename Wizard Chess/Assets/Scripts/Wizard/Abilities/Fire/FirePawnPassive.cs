using System.Collections.Generic;

/// <summary>
/// Scorched Earth: When captured, leaves a Fire Square on its position.
/// </summary>
public class FirePawnPassive : IPassiveAbility
{
    private readonly FirePawnPassiveParams _params;

    public FirePawnPassive() { _params = new FirePawnPassiveParams(); }
    public FirePawnPassive(FirePawnPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs)
    {
        var sem = capturedPiece.gm.squareEffectManager;
        if (sem != null)
        {
            sem.CreateEffect(capturedPiece.curx, capturedPiece.cury, SquareEffectType.Fire, _params.fireDuration, capturedPiece.color);
        }
    }
}
