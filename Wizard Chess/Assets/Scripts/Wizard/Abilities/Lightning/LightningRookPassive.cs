using System.Collections.Generic;

/// <summary>
/// Overcharge: Can pass through friendly pieces during move.
/// </summary>
public class LightningRookPassive : IPassiveAbility
{
    private readonly LtRookPassiveParams _params;

    public LightningRookPassive() { _params = new LtRookPassiveParams(); }
    public LightningRookPassive(LtRookPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.ROOK && piece.piece != ChessConstants.QUEEN) return moves;

        foreach (var dir in ChessConstants.RookDirections)
        {
            int passCount = 0;
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;

            while (bs.IsInBounds(nx, ny))
            {
                PieceMove occupant = bs.GetPieceAt(nx, ny);
                if (occupant != null)
                {
                    if (occupant.color == piece.color && passCount < _params.maxPassthrough)
                    {
                        passCount++;
                        nx += dir.x;
                        ny += dir.y;
                        continue;
                    }
                    else if (occupant.color != piece.color && passCount > 0)
                    {
                        Square sq = piece.getSquare(nx, ny);
                        if (sq != null && !moves.Contains(sq))
                            moves.Add(sq);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (passCount > 0)
                {
                    Square sq = piece.getSquare(nx, ny);
                    if (sq != null && !moves.Contains(sq))
                        moves.Add(sq);
                }

                nx += dir.x;
                ny += dir.y;
            }
        }

        return moves;
    }
}
