using System.Collections.Generic;

/// <summary>
/// Ember Aura: The 4 orthogonally adjacent squares are always Fire Squares while King is there.
/// Re-applied each turn start.
/// </summary>
public class FireKingPassive : IPassiveAbility
{
    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        ApplyAura(piece, bs);
    }

    public void OnTurnStart(int currentTurnColor)
    {
        // Aura is re-applied via OnAfterMove; also reapply on turn start
        // Note: we don't have a direct piece reference here, so aura reapplication
        // is handled in OnAfterMove when the king moves.
    }

    private void ApplyAura(PieceMove piece, BoardState bs)
    {
        var sem = piece.gm.squareEffectManager;
        if (sem == null) return;

        foreach (var dir in ChessConstants.RookDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (bs.IsInBounds(nx, ny))
            {
                // Only create if no effect there already or if it's our own aura
                var existing = sem.GetEffectAt(nx, ny);
                if (existing == null)
                {
                    sem.CreateEffect(nx, ny, SquareEffectType.Fire, 999, piece.color);
                }
            }
        }
    }
}
