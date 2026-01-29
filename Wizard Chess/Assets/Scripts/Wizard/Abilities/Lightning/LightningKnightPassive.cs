using System.Collections.Generic;

/// <summary>
/// Data about a knight move: whether it's a double-jump and its intermediate square.
/// </summary>
public class KnightMoveData
{
    public Square FinalDestination;
    public Square IntermediateSquare;  // null for standard L-jumps
    public bool IsDoubleJump => IntermediateSquare != null;

    public KnightMoveData(Square finalDest, Square intermediate = null)
    {
        FinalDestination = finalDest;
        IntermediateSquare = intermediate;
    }
}

/// <summary>
/// Double Jump: After moving, may move extra squares in cardinal directions.
/// Tracks move metadata so the animation system can show two distinct jumps.
/// </summary>
public class LightningKnightPassive : IPassiveAbility
{
    private readonly LtKnightPassiveParams _params;

    /// <summary>
    /// Static dictionary tracking move metadata for the current move generation.
    /// Key: (pieceInstanceId, destX, destY) -> KnightMoveData
    /// Cleared at start of each ModifyMoveGeneration call.
    /// </summary>
    private static Dictionary<(int, int, int), KnightMoveData> _moveDataCache
        = new Dictionary<(int, int, int), KnightMoveData>();

    public LightningKnightPassive() { _params = new LtKnightPassiveParams(); }
    public LightningKnightPassive(LtKnightPassiveParams p) { _params = p; }

    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    /// <summary>
    /// Get move data for a specific knight move. Returns null if not a Lightning Knight move.
    /// </summary>
    public static KnightMoveData GetMoveData(PieceMove piece, int destX, int destY)
    {
        if (piece == null) return null;
        int pieceId = piece.gameObject.GetInstanceID();
        var key = (pieceId, destX, destY);

        if (_moveDataCache.TryGetValue(key, out KnightMoveData data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// Check if a move is a double-jump (extended move through an intermediate L-jump square).
    /// </summary>
    public static bool IsDoubleJump(PieceMove piece, int destX, int destY)
    {
        var data = GetMoveData(piece, destX, destY);
        return data != null && data.IsDoubleJump;
    }

    /// <summary>
    /// Clear the move data cache. Called internally at start of move generation.
    /// </summary>
    public static void ClearMoveDataCache()
    {
        _moveDataCache.Clear();
    }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs)
    {
        if (piece.piece != ChessConstants.KNIGHT) return moves;

        int pieceId = piece.gameObject.GetInstanceID();

        // Clear cache for this piece's previous data
        // We'll rebuild it fresh each time moves are generated
        List<(int, int, int)> keysToRemove = new List<(int, int, int)>();
        foreach (var key in _moveDataCache.Keys)
        {
            if (key.Item1 == pieceId)
                keysToRemove.Add(key);
        }
        foreach (var key in keysToRemove)
        {
            _moveDataCache.Remove(key);
        }

        // Register standard L-jump moves (no intermediate)
        foreach (Square move in moves)
        {
            var key = (pieceId, move.x, move.y);
            if (!_moveDataCache.ContainsKey(key))
            {
                _moveDataCache[key] = new KnightMoveData(move, null);
            }
        }

        // Generate extended moves (double-jumps)
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
                        {
                            extraMoves.Add(sq);

                            // Register this as a double-jump with intermediate square
                            var key = (pieceId, nx, ny);
                            _moveDataCache[key] = new KnightMoveData(sq, move);
                        }
                    }
                }
            }
        }

        moves.AddRange(extraMoves);
        return moves;
    }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
}
