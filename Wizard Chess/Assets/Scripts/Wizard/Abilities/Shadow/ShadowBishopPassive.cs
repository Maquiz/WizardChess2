using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dark Corners: After moving 3+ squares, leave a ShadowVeil on the departure square.
/// </summary>
public class ShadowBishopPassive : IPassiveAbility
{
    private readonly ShadowBishopPassiveParams _params;

    public ShadowBishopPassive() { _params = new ShadowBishopPassiveParams(); }
    public ShadowBishopPassive(ShadowBishopPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        // Calculate Chebyshev distance
        int distance = Mathf.Max(Mathf.Abs(toX - fromX), Mathf.Abs(toY - fromY));
        if (distance < _params.minMoveDistance) return;

        var sem = piece.gm.squareEffectManager;
        if (sem != null)
        {
            sem.CreateEffect(fromX, fromY, SquareEffectType.ShadowVeil, _params.veilDuration, piece.color);
        }
    }
}
