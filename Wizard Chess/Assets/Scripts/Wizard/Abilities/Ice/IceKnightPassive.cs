using System.Collections.Generic;

/// <summary>
/// Frozen Hoof: After landing, freeze one adjacent enemy.
/// </summary>
public class IceKnightPassive : IPassiveAbility
{
    private readonly IceKnightPassiveParams _params;

    public IceKnightPassive() { _params = new IceKnightPassiveParams(); }
    public IceKnightPassive(IceKnightPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        FreezeAdjacentEnemy(attacker, bs);
    }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        FreezeAdjacentEnemy(piece, bs);
    }

    private void FreezeAdjacentEnemy(PieceMove piece, BoardState bs)
    {
        // Find one adjacent enemy and freeze them
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adjacentPiece = bs.GetPieceAt(nx, ny);
            if (adjacentPiece != null && adjacentPiece.color != piece.color)
            {
                if (adjacentPiece.elementalPiece != null &&
                    !adjacentPiece.elementalPiece.IsImmuneToEffect(SquareEffectType.Ice))
                {
                    adjacentPiece.elementalPiece.AddStatusEffect(StatusEffectType.Frozen, _params.freezeDuration);
                    return; // Only freeze one enemy
                }
            }
        }
    }
}
