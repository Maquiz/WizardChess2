using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class KingMoveTests
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

    [Test]
    public void King_CenterOfBoard_Has8Moves()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertMoveCount(moves, 8);
    }

    [Test]
    public void King_Corner_Has3Moves()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 7);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertMoveCount(moves, 3);
        TestExtensions.AssertContainsMove(moves, 0, 1);
        TestExtensions.AssertContainsMove(moves, 1, 0);
        TestExtensions.AssertContainsMove(moves, 1, 1);
    }

    [Test]
    public void King_CannotMoveIntoCheck()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 0);
        var moves = builder.GenerateMoves(king);
        // Rook on file 5 attacks all of column 5
        TestExtensions.AssertDoesNotContainMove(moves, 5, 7, "King cannot move into check from rook");
        TestExtensions.AssertDoesNotContainMove(moves, 5, 6, "King cannot move into check from rook");
    }

    [Test]
    public void King_CannotCaptureDefendedEnemy()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // Black pawn at (5,6) defended by another black piece
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 5); // defends (5,6) diagonally
        var moves = builder.GenerateMoves(king);
        // (5,6) is attacked by black pawn at (4,5)
        TestExtensions.AssertDoesNotContainMove(moves, 5, 6, "King cannot capture defended enemy");
    }

    [Test]
    public void King_InCheck_MustEscape()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2); // check on file 4

        var moves = builder.GenerateMoves(king);
        // King should not be able to stay on file 4
        foreach (var m in moves)
        {
            Assert.AreNotEqual(4, m.x,
                $"King in check on file 4 should escape, but has move to ({m.x},{m.y})");
        }
        Assert.IsTrue(moves.Count > 0, "King should have escape moves");
    }

    [Test]
    public void King_DoubleCheck_MustMoveKing()
    {
        // Double check: both a knight and bishop give check. Only king can move.
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 5, 5); // checks (4,7)? No, (5,5) to king(4,7) is not L-shape
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 0);   // checks on file
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 7, 4); // checks on diagonal

        // Place a white piece — in double check, only king moves are legal
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 2, 5);
        var rookMoves = builder.GenerateMoves(rook);
        TestExtensions.AssertMoveCount(rookMoves, 0, "Non-king piece should have 0 moves in double check");
    }

    [Test]
    public void King_CanCaptureUndefendedEnemy()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 6); // undefended
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 5, 6, "King can capture undefended enemy");
    }

    [Test]
    public void King_AllEightDirections()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 4, 3);
        TestExtensions.AssertContainsMove(moves, 4, 5);
        TestExtensions.AssertContainsMove(moves, 3, 4);
        TestExtensions.AssertContainsMove(moves, 5, 4);
        TestExtensions.AssertContainsMove(moves, 3, 3);
        TestExtensions.AssertContainsMove(moves, 5, 5);
        TestExtensions.AssertContainsMove(moves, 3, 5);
        TestExtensions.AssertContainsMove(moves, 5, 3);
    }
}
