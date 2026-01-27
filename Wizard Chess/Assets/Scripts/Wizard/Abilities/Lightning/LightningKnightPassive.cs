using System.Collections.Generic;

/// <summary>
/// Double Jump: After moving, may move extra squares in cardinal directions.
/// </summary>
public class LightningKnightPassive : IPassiveAbility
{
    private readonly LtKnightPassiveParams _params;

    public LightningKnightPassive() { _params = new LtKnightPassiveParams(); }
    public LightningKnightPassive(LtKnightPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.KNIGHT) return moves;

        List<Square> extraMoves = new List<Square>();
        foreach (Square move in moves)
        {
            foreach (var dir in ChessConstants.RookDirections)
            {
                for (int step = 1; step <= _params.extraMoveRange; step++)
                {
                    int nx = move.x + dir.x * step;
                    int ny = move.y + dir.y * step;
                    if (!bs.IsInBounds(nx, ny)) break;
                    if (!bs.IsSquareEmpty(nx, ny)) break;

                    Square sq = piece.getSquare(nx, ny);
                    if (sq != null && !moves.Contains(sq) && !extraMoves.Contains(sq))
                    {
                        if (nx != piece.curx || ny != piece.cury)
                            extraMoves.Add(sq);
                    }
                }
            }
        }

        moves.AddRange(extraMoves);
        return moves;
    }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
}
