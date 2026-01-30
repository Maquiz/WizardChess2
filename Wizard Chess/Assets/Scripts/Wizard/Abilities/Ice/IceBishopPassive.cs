using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rime Trail: After moving 3+ squares, create Ice on all traversed squares.
/// </summary>
public class IceBishopPassive : IPassiveAbility
{
    private readonly IceBishopPassiveParams _params;

    public IceBishopPassive() { _params = new IceBishopPassiveParams(); }
    public IceBishopPassive(IceBishopPassiveParams p) { _params = p; }

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
        if (sem == null) return;

        // Determine direction
        int dirX = toX > fromX ? 1 : (toX < fromX ? -1 : 0);
        int dirY = toY > fromY ? 1 : (toY < fromY ? -1 : 0);

        // Create Ice on all traversed squares (excluding destination)
        for (int i = 1; i < distance; i++)
        {
            int nx = fromX + dirX * i;
            int ny = fromY + dirY * i;

            if (bs.IsInBounds(nx, ny) && bs.IsSquareEmpty(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.Ice, _params.iceDuration, piece.color);
            }
        }
    }
}
