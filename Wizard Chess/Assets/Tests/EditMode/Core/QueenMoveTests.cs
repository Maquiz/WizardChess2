using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class QueenMoveTests
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
    public void Queen_CenterOfBoard_Has27Moves()
    {
        PlaceKings();
        var queen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(queen);
        // 14 rook-like + 13 bishop-like = 27
        TestExtensions.AssertMoveCount(moves, 27, "Queen in center should have 27 moves");
    }

    [Test]
    public void Queen_CombinesRookAndBishopMoves()
    {
        PlaceKings();
        var queen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(queen);
        // Rook-like: cardinal directions
        TestExtensions.AssertContainsMove(moves, 4, 0); // straight down
        TestExtensions.AssertContainsMove(moves, 4, 7); // straight up (but 7 has king at 0,7 — 4,7 is fine)
        TestExtensions.AssertContainsMove(moves, 7, 4); // straight right
        TestExtensions.AssertContainsMove(moves, 1, 4); // straight left
        // Bishop-like: diagonal directions
        TestExtensions.AssertContainsMove(moves, 5, 5); // up-right
        TestExtensions.AssertContainsMove(moves, 3, 3); // down-left
        TestExtensions.AssertContainsMove(moves, 5, 3); // down-right
        TestExtensions.AssertContainsMove(moves, 3, 5); // up-left
    }

    [Test]
    public void Queen_PinnedToKing_CanMoveAlongPin()
    {
        // King at (0,7), queen at (0,5), black rook at (0,0) — queen pinned on file
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var queen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 0, 5);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 0, 0);

        var moves = builder.GenerateMoves(queen);
        // Can only move along file 0
        foreach (var m in moves)
        {
            Assert.AreEqual(0, m.x, $"Pinned queen should only move along file 0 but has move to ({m.x},{m.y})");
        }
        Assert.IsTrue(moves.Count > 0, "Pinned queen should have moves along pin line");
    }

    [Test]
    public void Queen_BlockedByFriendly_StopsBefore()
    {
        PlaceKings();
        var queen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        var moves = builder.GenerateMoves(queen);
        TestExtensions.AssertContainsMove(moves, 4, 5);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 6);
    }

    [Test]
    public void Queen_Corner_Has20Moves()
    {
        PlaceKings();
        var queen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 7, 7);
        var moves = builder.GenerateMoves(queen);
        // File: 7 moves (7,0)-(7,6), Rank: 6 moves (1,7)-(6,7) — (0,7) blocked by own king
        // Diagonal: 7 moves (0,0)-(6,6). Total = 7 + 6 + 7 = 20
        TestExtensions.AssertMoveCount(moves, 20);
    }
}
