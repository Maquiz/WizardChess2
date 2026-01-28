using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BoardStateTests
{
    private ChessBoardBuilder builder;

    [SetUp]
    public void SetUp()
    {
        builder = new ChessBoardBuilder();
        builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        builder.Cleanup();
    }

    // ========== SetPieceAt / GetPieceAt ==========

    [Test]
    public void SetPieceAt_PlacesPieceCorrectly()
    {
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4);
        Assert.AreEqual(piece, builder.BoardState.GetPieceAt(3, 4));
    }

    [Test]
    public void GetPieceAt_ReturnsNull_WhenEmpty()
    {
        Assert.IsNull(builder.BoardState.GetPieceAt(3, 3));
    }

    [Test]
    public void GetPieceAt_ReturnsNull_ForOutOfBounds()
    {
        Assert.IsNull(builder.BoardState.GetPieceAt(-1, 0));
        Assert.IsNull(builder.BoardState.GetPieceAt(8, 0));
        Assert.IsNull(builder.BoardState.GetPieceAt(0, -1));
        Assert.IsNull(builder.BoardState.GetPieceAt(0, 8));
    }

    // ========== IsInBounds ==========

    [Test]
    public void IsInBounds_ReturnsTrue_ForValidCoords()
    {
        Assert.IsTrue(builder.BoardState.IsInBounds(0, 0));
        Assert.IsTrue(builder.BoardState.IsInBounds(7, 7));
        Assert.IsTrue(builder.BoardState.IsInBounds(4, 4));
    }

    [Test]
    public void IsInBounds_ReturnsFalse_ForNegativeCoords()
    {
        Assert.IsFalse(builder.BoardState.IsInBounds(-1, 0));
        Assert.IsFalse(builder.BoardState.IsInBounds(0, -1));
        Assert.IsFalse(builder.BoardState.IsInBounds(-1, -1));
    }

    [Test]
    public void IsInBounds_ReturnsFalse_ForTooLargeCoords()
    {
        Assert.IsFalse(builder.BoardState.IsInBounds(8, 0));
        Assert.IsFalse(builder.BoardState.IsInBounds(0, 8));
        Assert.IsFalse(builder.BoardState.IsInBounds(8, 8));
    }

    // ========== IsSquareEmpty ==========

    [Test]
    public void IsSquareEmpty_ReturnsTrue_WhenEmpty()
    {
        Assert.IsTrue(builder.BoardState.IsSquareEmpty(4, 4));
    }

    [Test]
    public void IsSquareEmpty_ReturnsFalse_WhenOccupied()
    {
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        Assert.IsFalse(builder.BoardState.IsSquareEmpty(4, 4));
    }

    // ========== MovePiece ==========

    [Test]
    public void MovePiece_UpdatesPosition()
    {
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0);
        builder.BoardState.MovePiece(0, 0, 3, 3);
        Assert.AreEqual(piece, builder.BoardState.GetPieceAt(3, 3));
    }

    [Test]
    public void MovePiece_ClearsOldPosition()
    {
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0);
        builder.BoardState.MovePiece(0, 0, 3, 3);
        Assert.IsNull(builder.BoardState.GetPieceAt(0, 0));
    }

    [Test]
    public void MovePiece_HandlesCapture()
    {
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0);
        var defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 0, 4);
        builder.BoardState.MovePiece(0, 0, 0, 4);
        Assert.AreEqual(attacker, builder.BoardState.GetPieceAt(0, 4));
        Assert.IsFalse(builder.BoardState.GetAllPieces(ChessConstants.BLACK).Contains(defender));
    }

    // ========== RemovePiece ==========

    [Test]
    public void RemovePiece_ClearsSquare()
    {
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        builder.BoardState.RemovePiece(3, 3);
        Assert.IsNull(builder.BoardState.GetPieceAt(3, 3));
    }

    [Test]
    public void RemovePiece_RemovesFromPieceList()
    {
        var piece = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        builder.BoardState.RemovePiece(3, 3);
        Assert.IsFalse(builder.BoardState.GetAllPieces(ChessConstants.BLACK).Contains(piece));
    }

    // ========== GetAllPieces ==========

    [Test]
    public void GetAllPieces_ReturnsCorrectColor()
    {
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 0, 0);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 1, 0);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 7);

        Assert.AreEqual(2, builder.BoardState.GetAllPieces(ChessConstants.WHITE).Count);
        Assert.AreEqual(1, builder.BoardState.GetAllPieces(ChessConstants.BLACK).Count);
    }

    // ========== RecalculateAttacks ==========

    [Test]
    public void RecalculateAttacks_PawnAttacksDiagonal()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        builder.BoardState.RecalculateAttacks();
        // White pawn at (4,4) attacks (3,3) and (5,3) (white moves -y)
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(3, 3, ChessConstants.WHITE));
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(5, 3, ChessConstants.WHITE));
        Assert.IsFalse(builder.BoardState.IsSquareAttackedBy(4, 3, ChessConstants.WHITE));
    }

    [Test]
    public void RecalculateAttacks_KnightAttacksLShape()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(5, 6, ChessConstants.WHITE));
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(6, 5, ChessConstants.WHITE));
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(3, 2, ChessConstants.WHITE));
        Assert.IsFalse(builder.BoardState.IsSquareAttackedBy(4, 5, ChessConstants.WHITE));
    }

    [Test]
    public void RecalculateAttacks_SlidingPiece_StopsAtBlocker()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 0, 2); // blocker
        builder.BoardState.RecalculateAttacks();
        // Rook attacks 0,3 but is blocked at 0,2 (friendly piece)
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(0, 3, ChessConstants.WHITE));
        Assert.IsTrue(builder.BoardState.IsSquareAttackedBy(0, 2, ChessConstants.WHITE)); // attacks the blocker square
        Assert.IsFalse(builder.BoardState.IsSquareAttackedBy(0, 1, ChessConstants.WHITE)); // can't see past blocker
    }

    // ========== IsKingInCheck ==========

    [Test]
    public void IsKingInCheck_WhenAttacked_ReturnsTrue()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 5);
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_WhenSafe_ReturnsFalse()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 5);
        builder.BoardState.RecalculateAttacks();
        Assert.IsFalse(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    // ========== WouldMoveLeaveKingInCheck ==========

    [Test]
    public void WouldMoveLeaveKingInCheck_DetectsPinnedPiece()
    {
        // White king at e1 (4,7), white rook at d1 (3,7), black rook at a1 (0,7)
        // Moving white rook off file would expose king
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 0, 7);
        builder.BoardState.RecalculateAttacks();

        // Moving rook away from rank 7 would expose king on that rank
        Assert.IsTrue(builder.BoardState.WouldMoveLeaveKingInCheck(rook, 3, 5));
        // Moving rook along rank 7 is fine
        Assert.IsFalse(builder.BoardState.WouldMoveLeaveKingInCheck(rook, 2, 7));
    }

    // ========== Clone ==========

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        BoardState clone = builder.BoardState.Clone();
        Assert.AreEqual(piece, clone.GetPieceAt(0, 0));
        Assert.AreEqual(2, clone.GetAllPieces(ChessConstants.WHITE).Count);
    }
}
