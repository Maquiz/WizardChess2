using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class RookMoveTests
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
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
    }

    [Test]
    public void Rook_EmptyBoard_Has14Moves()
    {
        PlaceKings();
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(rook);
        TestExtensions.AssertMoveCount(moves, 14, "Rook on open board should have 14 moves");
    }

    [Test]
    public void Rook_Corner_Has14Moves()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 2, 2);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 5, 5);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        var moves = builder.GenerateMoves(rook);
        TestExtensions.AssertMoveCount(moves, 14, "Rook in corner should still have 14 moves on empty board");
    }

    [Test]
    public void Rook_BlockedByFriendly_StopsBefore()
    {
        PlaceKings();
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 2); // blocks downward
        var moves = builder.GenerateMoves(rook);
        TestExtensions.AssertContainsMove(moves, 4, 3);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 2, "Cannot move to friendly piece square");
        TestExtensions.AssertDoesNotContainMove(moves, 4, 1, "Cannot move past friendly piece");
    }

    [Test]
    public void Rook_BlockedByEnemy_IncludesCapture()
    {
        PlaceKings();
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 2);
        var moves = builder.GenerateMoves(rook);
        TestExtensions.AssertContainsMove(moves, 4, 3);
        TestExtensions.AssertContainsMove(moves, 4, 2, "Should be able to capture enemy");
        TestExtensions.AssertDoesNotContainMove(moves, 4, 1, "Cannot move past enemy piece");
    }

    [Test]
    public void Rook_SurroundedByFriendlies_ZeroMoves()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 5, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        var moves = builder.GenerateMoves(rook);
        TestExtensions.AssertMoveCount(moves, 0, "Rook surrounded by friendlies should have 0 moves");
    }

    [Test]
    public void Rook_PinnedToKing_CanOnlyMoveAlongPin()
    {
        // White king at (4,7), white rook at (4,5), black rook at (4,0)
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 5);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 0);

        var moves = builder.GenerateMoves(rook);
        // Can only move along file 4
        foreach (var m in moves)
        {
            Assert.AreEqual(4, m.x, $"Pinned rook should only move along file 4, but has move to ({m.x},{m.y})");
        }
        Assert.IsTrue(moves.Count > 0, "Pinned rook should still have moves along pin line");
    }

    [Test]
    public void Rook_MovesInAllFourDirections()
    {
        PlaceKings();
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(rook);
        // Check all four directions
        TestExtensions.AssertContainsMove(moves, 4, 5); // up
        TestExtensions.AssertContainsMove(moves, 4, 3); // down
        TestExtensions.AssertContainsMove(moves, 5, 4); // right
        TestExtensions.AssertContainsMove(moves, 3, 4); // left
    }
}
