using System.Collections.Generic;

/// <summary>
/// Tracks why moves are rejected during move validation.
/// Used for debugging and displaying explanations to players.
/// </summary>
public enum MoveRejectionReason
{
    None,                       // Move is valid
    OutOfBounds,                // Square outside 8x8 board
    BlockedByFriendlyPiece,     // Friendly piece on target square
    BlockedByPiecePath,         // Piece blocks the path (for sliding pieces)
    ElementalPassiveBlocked,    // Piece's elemental passive removed this move
    SquareEffectBlocked,        // Fire/Wall/etc. blocks entry
    CaptureProtected,           // Defender's passive prevents capture
    AttackerCannotCapture,      // Attacker's passive prevents capture
    WouldLeaveKingInCheck,      // Move leaves own king in check
    CastlingKingMoved,          // King has moved, can't castle
    CastlingRookMoved,          // Rook has moved or missing, can't castle
    CastlingPathBlocked,        // Pieces between king and rook
    CastlingThroughCheck,       // King passes through attacked square
    CastlingInCheck,            // King currently in check
    EnPassantNotAvailable,      // No en passant target
    PawnCannotCaptureForward    // Pawn can only capture diagonally
}

/// <summary>
/// Data class for a single rejected move.
/// </summary>
public class MoveRejection
{
    public int X;
    public int Y;
    public MoveRejectionReason Reason;
    public string Details;  // Extra context (e.g., "blocked by Earth Knight passive")

    public MoveRejection(int x, int y, MoveRejectionReason reason, string details = null)
    {
        X = x;
        Y = y;
        Reason = reason;
        Details = details;
    }
}

/// <summary>
/// Static tracker for move rejections during move generation.
/// Call Clear() before generating moves, then AddRejection() during validation.
/// Query with GetRejection() or GetExplanation() after generation completes.
/// </summary>
public static class MoveRejectionTracker
{
    private static Dictionary<(int, int), MoveRejection> rejections = new Dictionary<(int, int), MoveRejection>();

    /// <summary>
    /// Clear all tracked rejections. Call at the start of createPieceMoves().
    /// </summary>
    public static void Clear()
    {
        rejections.Clear();
    }

    /// <summary>
    /// Get all current rejections (read-only copy).
    /// </summary>
    public static Dictionary<(int, int), MoveRejection> CurrentRejections
    {
        get { return new Dictionary<(int, int), MoveRejection>(rejections); }
    }

    /// <summary>
    /// Add a rejection for a specific square. If already rejected, keeps the first reason.
    /// </summary>
    public static void AddRejection(int x, int y, MoveRejectionReason reason, string details = null)
    {
        var key = (x, y);
        if (!rejections.ContainsKey(key))
        {
            rejections[key] = new MoveRejection(x, y, reason, details);
        }
    }

    /// <summary>
    /// Get the rejection for a specific square, or null if not rejected.
    /// </summary>
    public static MoveRejection GetRejection(int x, int y)
    {
        var key = (x, y);
        if (rejections.TryGetValue(key, out MoveRejection rejection))
        {
            return rejection;
        }
        return null;
    }

    /// <summary>
    /// Get a human-readable explanation for why a square is invalid.
    /// Returns null if the square is not rejected.
    /// </summary>
    public static string GetExplanation(int x, int y)
    {
        MoveRejection rejection = GetRejection(x, y);
        if (rejection == null) return null;

        string baseExplanation = GetFriendlyText(rejection.Reason);
        if (!string.IsNullOrEmpty(rejection.Details))
        {
            return baseExplanation + " (" + rejection.Details + ")";
        }
        return baseExplanation;
    }

    /// <summary>
    /// Convert a rejection reason to user-friendly text.
    /// </summary>
    public static string GetFriendlyText(MoveRejectionReason reason)
    {
        switch (reason)
        {
            case MoveRejectionReason.None:
                return "Valid move";
            case MoveRejectionReason.OutOfBounds:
                return "Outside the board";
            case MoveRejectionReason.BlockedByFriendlyPiece:
                return "Your own piece is on this square";
            case MoveRejectionReason.BlockedByPiecePath:
                return "Blocked by a piece in the way";
            case MoveRejectionReason.ElementalPassiveBlocked:
                return "Blocked by elemental passive ability";
            case MoveRejectionReason.SquareEffectBlocked:
                return "Blocked by square effect";
            case MoveRejectionReason.CaptureProtected:
                return "Piece is protected from capture";
            case MoveRejectionReason.AttackerCannotCapture:
                return "Cannot capture this piece";
            case MoveRejectionReason.WouldLeaveKingInCheck:
                return "This move would leave your King in check";
            case MoveRejectionReason.CastlingKingMoved:
                return "Cannot castle - King has already moved";
            case MoveRejectionReason.CastlingRookMoved:
                return "Cannot castle - Rook has moved or is missing";
            case MoveRejectionReason.CastlingPathBlocked:
                return "Cannot castle - pieces in the way";
            case MoveRejectionReason.CastlingThroughCheck:
                return "Cannot castle through an attacked square";
            case MoveRejectionReason.CastlingInCheck:
                return "Cannot castle while in check";
            case MoveRejectionReason.EnPassantNotAvailable:
                return "En passant not available";
            case MoveRejectionReason.PawnCannotCaptureForward:
                return "Pawns can only capture diagonally";
            default:
                return "Invalid move";
        }
    }

    /// <summary>
    /// Check if there are any rejections tracked.
    /// </summary>
    public static bool HasRejections()
    {
        return rejections.Count > 0;
    }

    /// <summary>
    /// Get the count of rejected squares.
    /// </summary>
    public static int RejectionCount
    {
        get { return rejections.Count; }
    }
}
