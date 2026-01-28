using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class KnightMoveTests
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
    public void Knight_CenterOfBoard_Has8Moves()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertMoveCount(moves, 8, "Knight in center should have 8 moves");
    }

    [Test]
    public void Knight_Corner_Has2Moves()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 7, 7);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertMoveCount(moves, 2);
        TestExtensions.AssertContainsMove(moves, 6, 5);
        TestExtensions.AssertContainsMove(moves, 5, 6);
    }

    [Test]
    public void Knight_CanJumpOverPieces()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        // Surround with friendly pieces
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        var moves = builder.GenerateMoves(knight);
        Assert.IsTrue(moves.Count > 0, "Knight should jump over adjacent pieces");
        TestExtensions.AssertContainsMove(moves, 5, 6);
        TestExtensions.AssertContainsMove(moves, 3, 6);
    }

    [Test]
    public void Knight_PinnedToKing_NoMoves()
    {
        // White king at (4,7), white knight at (4,5), black rook at (4,0) — knight pinned on file
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 5);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 0);

        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertMoveCount(moves, 0, "Pinned knight should have 0 moves (can't move along pin line)");
    }

    [Test]
    public void Knight_AllEightLShapes_Correct()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertContainsMove(moves, 5, 6);
        TestExtensions.AssertContainsMove(moves, 6, 5);
        TestExtensions.AssertContainsMove(moves, 3, 6);
        TestExtensions.AssertContainsMove(moves, 2, 5);
        TestExtensions.AssertContainsMove(moves, 5, 2);
        TestExtensions.AssertContainsMove(moves, 6, 3);
        TestExtensions.AssertContainsMove(moves, 3, 2);
        TestExtensions.AssertContainsMove(moves, 2, 3);
    }

    [Test]
    public void Knight_CanCaptureEnemy()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 6);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertContainsMove(moves, 5, 6, "Knight should capture enemy");
    }

    [Test]
    public void Knight_CannotCaptureOwnPiece()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 6);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertDoesNotContainMove(moves, 5, 6, "Knight cannot land on friendly piece");
    }

    [Test]
    public void Knight_Edge_Has4Moves()
    {
        PlaceKings();
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 0, 4);
        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertMoveCount(moves, 4);
    }
}
