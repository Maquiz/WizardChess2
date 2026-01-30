using System.Collections.Generic;

/// <summary>
/// Looming Presence: At the start of your turn, Mark all adjacent enemies.
/// </summary>
public class ShadowRookPassive : IPassiveAbility
{
    private readonly ShadowRookPassiveParams _params;
    private PieceMove _piece;

    public ShadowRookPassive() { _params = new ShadowRookPassiveParams(); }
    public ShadowRookPassive(ShadowRookPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (_piece == null) _piece = piece;
        return moves;
    }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }

    public void OnTurnStart(int currentTurnColor)
    {
        if (_piece == null || _piece.color != currentTurnColor) return;

        var bs = _piece.gm.boardState;

        // Mark all adjacent enemies
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = _piece.curx + dir.x;
            int ny = _piece.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adjacentPiece = bs.GetPieceAt(nx, ny);
            if (adjacentPiece != null && adjacentPiece.color != _piece.color)
            {
                if (adjacentPiece.elementalPiece != null)
                {
                    adjacentPiece.elementalPiece.AddStatusEffect(StatusEffectType.Marked, _params.markDuration);
                }
            }
        }
    }
}
