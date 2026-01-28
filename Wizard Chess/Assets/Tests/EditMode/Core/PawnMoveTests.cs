using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class PawnMoveTests
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

    private void PlaceKings()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
    }

    [Test]
    public void WhitePawn_InitialPosition_CanMoveOneOrTwoForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        var moves = builder.GenerateMoves(pawn);
        // White moves -y: (4,5) and (4,4)
        TestExtensions.AssertContainsMove(moves, 4, 5);
        TestExtensions.AssertContainsMove(moves, 4, 4);
    }

    [Test]
    public void BlackPawn_InitialPosition_CanMoveOneOrTwoForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 1);
        var moves = builder.GenerateMoves(pawn);
        // Black moves +y: (4,2) and (4,3)
        TestExtensions.AssertContainsMove(moves, 4, 2);
        TestExtensions.AssertContainsMove(moves, 4, 3);
    }

    [Test]
    public void Pawn_AfterFirstMove_CanMoveOnlyOneForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        pawn.firstMove = false;
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 4, 4);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 3);
    }

    [Test]
    public void Pawn_BlockedByFriendly_CannotMoveForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 5);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 4);
    }

    [Test]
    public void Pawn_BlockedByEnemy_CannotMoveForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 5);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 5);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 4);
    }

    [Test]
    public void Pawn_CanCaptureDiagonalLeft()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 5);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 3, 5);
    }

    [Test]
    public void Pawn_CanCaptureDiagonalRight()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 5);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 5, 5);
    }

    [Test]
    public void Pawn_CannotCaptureOwnPiece()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertDoesNotContainMove(moves, 3, 5);
    }

    [Test]
    public void Pawn_AtEdge_DoesNotWrapAround()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 0, 6);
        var moves = builder.GenerateMoves(pawn);
        // Should not have any move at x=-1
        foreach (var m in moves)
        {
            Assert.IsTrue(m.x >= 0, "Move should not wrap around left edge");
        }
    }

    [Test]
    public void Pawn_DoubleMove_BlockedBySingleSquareAhead()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 5); // blocks single ahead
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 4, "Double move should be blocked when single ahead is blocked");
    }

    [Test]
    public void Pawn_NoMoves_WhenCompletelyBlocked()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        // No diagonal enemies either
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertMoveCount(moves, 0);
    }

    [Test]
    public void Pawn_CannotCaptureForward()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        pawn.firstMove = false;
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3);
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 3);
    }

    [Test]
    public void Pawn_FilteredByCheckConstraints()
    {
        // White king at (4,7), black rook at (3,0) — pawn at (3,6) is pinned
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 2, 5);

        var moves = builder.GenerateMoves(pawn);
        // Pawn at (3,6) may be able to capture bishop at (2,5) but that reveals check from bishop diagonal
        // This test verifies illegal moves are filtered
        foreach (var m in moves)
        {
            Assert.IsFalse(builder.BoardState.WouldMoveLeaveKingInCheck(pawn, m.x, m.y),
                $"Move to ({m.x},{m.y}) should not leave king in check");
        }
    }

    [Test]
    public void BlackPawn_MovesCorrectDirection()
    {
        PlaceKings();
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 4);
        pawn.firstMove = false;
        var moves = builder.GenerateMoves(pawn);
        // Black moves +y
        TestExtensions.AssertContainsMove(moves, 4, 5);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 3, "Black pawn should not move backwards");
    }
}
