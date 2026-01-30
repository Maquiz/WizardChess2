using System.Collections.Generic;

/// <summary>
/// Frostbite: When this pawn captures an enemy, adjacent enemies become Chilled.
/// </summary>
public class IcePawnPassive : IPassiveAbility
{
    private readonly IcePawnPassiveParams _params;

    public IcePawnPassive() { _params = new IcePawnPassiveParams(); }
    public IcePawnPassive(IcePawnPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        // Chill all adjacent enemies
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = attacker.curx + dir.x;
            int ny = attacker.cury + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adjacentPiece = bs.GetPieceAt(nx, ny);
            if (adjacentPiece != null && adjacentPiece.color != attacker.color)
            {
                if (adjacentPiece.elementalPiece != null)
                {
                    adjacentPiece.elementalPiece.AddStatusEffect(StatusEffectType.Chilled, _params.chillDuration);
                }
            }
        }
    }
}
