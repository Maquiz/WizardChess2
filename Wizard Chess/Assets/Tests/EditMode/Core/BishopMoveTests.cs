using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class BishopMoveTests
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
    public void Bishop_CenterOfBoard_Has13Moves()
    {
        PlaceKings();
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertMoveCount(moves, 13, "Bishop in center should have 13 moves");
    }

    [Test]
    public void Bishop_Corner_Has7Moves()
    {
        PlaceKings();
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 7, 7);
        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertMoveCount(moves, 7);
    }

    [Test]
    public void Bishop_BlockedByFriendly_StopsBefore()
    {
        PlaceKings();
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 6, 6);
        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertContainsMove(moves, 5, 5);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 6, "Cannot land on friendly piece");
        TestExtensions.AssertDoesNotContainMove(moves, 7, 7, "Cannot pass through friendly piece");
    }

    [Test]
    public void Bishop_BlockedByEnemy_IncludesCapture()
    {
        PlaceKings();
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 6, 6);
        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertContainsMove(moves, 6, 6, "Can capture enemy");
        TestExtensions.AssertDoesNotContainMove(moves, 7, 7, "Cannot pass through enemy");
    }

    [Test]
    public void Bishop_PinnedToKing_CanMoveAlongPin()
    {
        // King at (0,0), bishop at (2,2), black queen at (4,4) — bishop pinned on diagonal
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 5);
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 3);

        var moves = builder.GenerateMoves(bishop);
        // Bishop pinned on diagonal from king(0,7) through bishop(2,5) to queen(4,3)
        // Can move along that diagonal: (1,6), (3,4), (4,3)
        foreach (var m in moves)
        {
            Assert.IsFalse(builder.BoardState.WouldMoveLeaveKingInCheck(bishop, m.x, m.y),
                $"Move to ({m.x},{m.y}) would leave king in check");
        }
    }

    [Test]
    public void Bishop_MovesInAllFourDiagonals()
    {
        PlaceKings();
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertContainsMove(moves, 5, 5); // up-right
        TestExtensions.AssertContainsMove(moves, 3, 3); // down-left
        TestExtensions.AssertContainsMove(moves, 5, 3); // down-right
        TestExtensions.AssertContainsMove(moves, 3, 5); // up-left
    }
}
