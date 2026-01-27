using System.Collections.Generic;

/// <summary>
/// Splash Damage: When capturing, enemy pieces adjacent to the capture square become Singed.
/// </summary>
public class FireKnightPassive : IPassiveAbility
{
    private readonly FireKnightPassiveParams _params;

    public FireKnightPassive() { _params = new FireKnightPassiveParams(); }
    public FireKnightPassive(FireKnightPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs)
    {
        int cx = attacker.curx;
        int cy = attacker.cury;

        var directions = _params.includeDiagonals ? ChessConstants.KingDirections : ChessConstants.RookDirections;
        foreach (var dir in directions)
        {
            int nx = cx + dir.x;
            int ny = cy + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color != attacker.color && adj.piece != ChessConstants.KING)
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
