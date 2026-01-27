using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized board state manager - single source of truth for piece positions.
/// Provides check detection, attack calculations, and move simulation.
/// </summary>
public class BoardState
{
    // 8x8 board storing piece references (null = empty)
    private PieceMove[,] board = new PieceMove[ChessConstants.BOARD_SIZE, ChessConstants.BOARD_SIZE];

    // King references for quick check detection
    public PieceMove whiteKing { get; private set; }
    public PieceMove blackKing { get; private set; }

    // Attack maps for O(1) attack queries
    private HashSet<(int x, int y)> whiteAttacks = new HashSet<(int x, int y)>();
    private HashSet<(int x, int y)> blackAttacks = new HashSet<(int x, int y)>();

    // All pieces by color for iteration
    private List<PieceMove> whitePieces = new List<PieceMove>();
    private List<PieceMove> blackPieces = new List<PieceMove>();

    /// <summary>
    /// Get the piece at the specified position.
    /// </summary>
    public PieceMove GetPieceAt(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return board[x, y];
    }

    /// <summary>
    /// Set a piece at the specified position (used during initialization).
    /// </summary>
    public void SetPieceAt(int x, int y, PieceMove piece)
    {
        if (!IsInBounds(x, y)) return;

        // Remove old piece from lists if exists
        PieceMove oldPiece = board[x, y];
        if (oldPiece != null)
        {
            RemovePieceFromLists(oldPiece);
        }

        board[x, y] = piece;

        if (piece != null)
        {
            AddPieceToLists(piece);

            // Track kings
            if (piece.piece == ChessConstants.KING)
            {
                if (piece.color == ChessConstants.WHITE)
                    whiteKing = piece;
                else
                    blackKing = piece;
            }
        }
    }

    /// <summary>
    /// Move a piece from one position to another.
    /// </summary>
    public void MovePiece(int fromX, int fromY, int toX, int toY)
    {
        PieceMove piece = board[fromX, fromY];
        if (piece == null) return;

        // Handle capture
        PieceMove captured = board[toX, toY];
        if (captured != null)
        {
            RemovePieceFromLists(captured);
        }

        // Move piece
        board[fromX, fromY] = null;
        board[toX, toY] = piece;
    }

    /// <summary>
    /// Remove a piece from the board (for captures).
    /// </summary>
    public void RemovePiece(int x, int y)
    {
        PieceMove piece = board[x, y];
        if (piece != null)
        {
            RemovePieceFromLists(piece);
            board[x, y] = null;
        }
    }

    /// <summary>
    /// Check if a square is empty.
    /// </summary>
    public bool IsSquareEmpty(int x, int y)
    {
        return GetPieceAt(x, y) == null;
    }

    /// <summary>
    /// Check if coordinates are within board bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < ChessConstants.BOARD_SIZE && y >= 0 && y < ChessConstants.BOARD_SIZE;
    }

    /// <summary>
    /// Get all pieces of a specific color.
    /// </summary>
    public List<PieceMove> GetAllPieces(int color)
    {
        return color == ChessConstants.WHITE ? whitePieces : blackPieces;
    }

    /// <summary>
    /// Check if a square is attacked by any piece of the specified color.
    /// Uses attack map for O(1) lookup after recalculation.
    /// </summary>
    public bool IsSquareAttackedBy(int x, int y, int attackerColor)
    {
        var attacks = attackerColor == ChessConstants.WHITE ? whiteAttacks : blackAttacks;
        return attacks.Contains((x, y));
    }

    /// <summary>
    /// Check if the king of the specified color is in check.
    /// </summary>
    public bool IsKingInCheck(int kingColor)
    {
        PieceMove king = kingColor == ChessConstants.WHITE ? whiteKing : blackKing;
        if (king == null) return false;

        // Bedrock Throne: Earth King on starting square is immune to check
        if (king.elementalPiece != null
            && king.elementalPiece.passive is EarthKingPassive
            && EarthKingPassive.IsOnStartingSquare(king))
        {
            return false;
        }

        int opponentColor = kingColor == ChessConstants.WHITE ? ChessConstants.BLACK : ChessConstants.WHITE;
        return IsSquareAttackedBy(king.curx, king.cury, opponentColor);
    }

    /// <summary>
    /// Recalculate all attack maps. Call after any move.
    /// </summary>
    public void RecalculateAttacks()
    {
        whiteAttacks.Clear();
        blackAttacks.Clear();

        foreach (PieceMove piece in whitePieces)
        {
            AddPieceAttacks(piece, whiteAttacks);
        }

        foreach (PieceMove piece in blackPieces)
        {
            AddPieceAttacks(piece, blackAttacks);
        }
    }

    /// <summary>
    /// Simulate a move and check if it would leave the king in check.
    /// Returns true if the move would leave own king in check (illegal move).
    /// </summary>
    public bool WouldMoveLeaveKingInCheck(PieceMove piece, int toX, int toY)
    {
        int fromX = piece.curx;
        int fromY = piece.cury;

        // Save current state
        PieceMove capturedPiece = board[toX, toY];

        // Simulate move
        board[fromX, fromY] = null;
        board[toX, toY] = piece;

        // Temporarily update piece position for attack calculation
        int savedX = piece.curx;
        int savedY = piece.cury;
        piece.curx = toX;
        piece.cury = toY;

        // Recalculate attacks
        RecalculateAttacks();

        // Check if own king is in check
        bool inCheck = IsKingInCheck(piece.color);

        // Restore state
        board[fromX, fromY] = piece;
        board[toX, toY] = capturedPiece;
        piece.curx = savedX;
        piece.cury = savedY;

        // Recalculate attacks again with restored state
        RecalculateAttacks();

        return inCheck;
    }

    /// <summary>
    /// Create a shallow copy of the board state for move simulation.
    /// </summary>
    public BoardState Clone()
    {
        BoardState clone = new BoardState();

        for (int x = 0; x < ChessConstants.BOARD_SIZE; x++)
        {
            for (int y = 0; y < ChessConstants.BOARD_SIZE; y++)
            {
                clone.board[x, y] = board[x, y];
            }
        }

        clone.whiteKing = whiteKing;
        clone.blackKing = blackKing;
        clone.whitePieces = new List<PieceMove>(whitePieces);
        clone.blackPieces = new List<PieceMove>(blackPieces);

        return clone;
    }

    /// <summary>
    /// Check if a square is blocked by a square effect (delegates to SquareEffectManager).
    /// This is a convenience method; the actual logic lives in SquareEffectManager.
    /// </summary>
    public bool IsSquareBlockedByEffect(int x, int y, PieceMove piece, SquareEffectManager sem)
    {
        if (sem == null) return false;
        return sem.IsSquareBlocked(x, y, piece);
    }

    // ========== Private Helper Methods ==========

    private void AddPieceToLists(PieceMove piece)
    {
        if (piece.color == ChessConstants.WHITE)
        {
            if (!whitePieces.Contains(piece))
                whitePieces.Add(piece);
        }
        else
        {
            if (!blackPieces.Contains(piece))
                blackPieces.Add(piece);
        }
    }

    private void RemovePieceFromLists(PieceMove piece)
    {
        if (piece.color == ChessConstants.WHITE)
            whitePieces.Remove(piece);
        else
            blackPieces.Remove(piece);
    }

    /// <summary>
    /// Calculate all squares a piece attacks and add to the attack set.
    /// Note: This calculates ATTACKS, not legal moves (pawns attack diagonally even if empty).
    /// </summary>
    private void AddPieceAttacks(PieceMove piece, HashSet<(int x, int y)> attacks)
    {
        int x = piece.curx;
        int y = piece.cury;

        switch (piece.piece)
        {
            case ChessConstants.PAWN:
                AddPawnAttacks(piece, attacks);
                break;

            case ChessConstants.KNIGHT:
                AddKnightAttacks(x, y, attacks);
                break;

            case ChessConstants.BISHOP:
                AddSlidingAttacks(x, y, ChessConstants.BishopDirections, attacks);
                break;

            case ChessConstants.ROOK:
                AddSlidingAttacks(x, y, ChessConstants.RookDirections, attacks);
                break;

            case ChessConstants.QUEEN:
                AddSlidingAttacks(x, y, ChessConstants.BishopDirections, attacks);
                AddSlidingAttacks(x, y, ChessConstants.RookDirections, attacks);
                break;

            case ChessConstants.KING:
                AddKingAttacks(x, y, attacks);
                break;
        }
    }

    private void AddPawnAttacks(PieceMove pawn, HashSet<(int x, int y)> attacks)
    {
        int direction = pawn.color == ChessConstants.WHITE ? -1 : 1;
        int x = pawn.curx;
        int y = pawn.cury;

        // Pawns attack diagonally
        if (IsInBounds(x - 1, y + direction))
            attacks.Add((x - 1, y + direction));
        if (IsInBounds(x + 1, y + direction))
            attacks.Add((x + 1, y + direction));
    }

    private void AddKnightAttacks(int x, int y, HashSet<(int x, int y)> attacks)
    {
        foreach (var dir in ChessConstants.KnightDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInBounds(nx, ny))
                attacks.Add((nx, ny));
        }
    }

    private void AddKingAttacks(int x, int y, HashSet<(int x, int y)> attacks)
    {
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (IsInBounds(nx, ny))
                attacks.Add((nx, ny));
        }
    }

    private void AddSlidingAttacks(int x, int y, (int x, int y)[] directions, HashSet<(int x, int y)> attacks)
    {
        foreach (var dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            while (IsInBounds(nx, ny))
            {
                attacks.Add((nx, ny));

                // Stop if we hit a piece (but we still attack that square)
                if (board[nx, ny] != null)
                    break;

                nx += dir.x;
                ny += dir.y;
            }
        }
    }

    /// <summary>
    /// Debug: Print the board state to console.
    /// </summary>
    public void DebugPrintBoard()
    {
        string output = "Board State:\n";
        for (int y = ChessConstants.BOARD_SIZE - 1; y >= 0; y--)
        {
            output += (y + 1) + " ";
            for (int x = 0; x < ChessConstants.BOARD_SIZE; x++)
            {
                PieceMove p = board[x, y];
                if (p == null)
                    output += ". ";
                else
                    output += GetPieceChar(p) + " ";
            }
            output += "\n";
        }
        output += "  a b c d e f g h";
        Debug.Log(output);
    }

    private string GetPieceChar(PieceMove p)
    {
        string c = "";
        switch (p.piece)
        {
            case ChessConstants.PAWN: c = "p"; break;
            case ChessConstants.ROOK: c = "r"; break;
            case ChessConstants.KNIGHT: c = "n"; break;
            case ChessConstants.BISHOP: c = "b"; break;
            case ChessConstants.QUEEN: c = "q"; break;
            case ChessConstants.KING: c = "k"; break;
        }
        return p.color == ChessConstants.WHITE ? c.ToUpper() : c;
    }
}
