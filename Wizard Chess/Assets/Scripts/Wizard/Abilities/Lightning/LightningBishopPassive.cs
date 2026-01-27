using System.Collections.Generic;

/// <summary>
/// Voltage Burst: After moving 3+ squares, adjacent enemies become Singed.
/// </summary>
public class LightningBishopPassive : IPassiveAbility
{
    private readonly LtBishopPassiveParams _params;

    public LightningBishopPassive() { _params = new LtBishopPassiveParams(); }
    public LightningBishopPassive(LtBishopPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        int dist = System.Math.Max(System.Math.Abs(toX - fromX), System.Math.Abs(toY - fromY));
        if (dist < _params.minMoveDistance) return;

        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = toX + dir.x;
            int ny = toY + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color != piece.color && adj.piece != ChessConstants.KING)
            {
                if (adj.elementalPiece == null)
                {
                    var ep = adj.gameObject.AddComponent<ElementalPiece>();
                    ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
                }
                adj.elementalPiece.AddStatusEffect(StatusEffectType.Singed, 0, true);
            }
        }
    }
}
