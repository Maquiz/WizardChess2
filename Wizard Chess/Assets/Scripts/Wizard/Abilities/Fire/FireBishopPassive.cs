using System.Collections.Generic;

/// <summary>
/// Burning Path: After moving, the first square traversed becomes a Fire Square.
/// </summary>
public class FireBishopPassive : IPassiveAbility
{
    private readonly FireBishopPassiveParams _params;

    public FireBishopPassive() { _params = new FireBishopPassiveParams(); }
    public FireBishopPassive(FireBishopPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        int dx = System.Math.Sign(toX - fromX);
        int dy = System.Math.Sign(toY - fromY);

        int firstX = fromX + dx;
        int firstY = fromY + dy;

        if (bs.IsInBounds(firstX, firstY))
        {
            var sem = piece.gm.squareEffectManager;
            if (sem != null)
            {
                sem.CreateEffect(firstX, firstY, SquareEffectType.Fire, _params.fireDuration, piece.color);
            }
        }
    }
}
