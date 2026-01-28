using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class CastlingTests
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
    public void WhiteKingsideCastle_Available_KingMoves2Right()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 6, 7, "Kingside castle should be available");
    }

    [Test]
    public void WhiteQueensideCastle_Available_KingMoves2Left()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 2, 7, "Queenside castle should be available");
    }

    [Test]
    public void BlackKingsideCastle_Available()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 6, 0, "Black kingside castle should be available");
    }

    [Test]
    public void Castle_KingHasMoved_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        king.firstMove = false;
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 7, "Castle unavailable after king moved");
    }

    [Test]
    public void Castle_RookHasMoved_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        rook.firstMove = false;
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 7, "Castle unavailable after rook moved");
    }

    [Test]
    public void Castle_PathBlocked_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 5, 7); // blocks path
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 7, "Castle unavailable when path blocked");
    }

    [Test]
    public void Castle_KingInCheck_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3); // checks king
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 7, "Cannot castle while in check");
    }

    [Test]
    public void Castle_KingPassesThroughAttack_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 0); // attacks f1 (5,7)
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 6, 7, "Cannot castle through attacked square");
    }

    [Test]
    public void Castle_QueensideExtraSquareBlocked_Unavailable()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 1, 7); // blocks b1
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertDoesNotContainMove(moves, 2, 7, "Queenside castle blocked by b1 piece");
    }

    [Test]
    public void BothCastles_Available()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 6, 7, "Kingside castle");
        TestExtensions.AssertContainsMove(moves, 2, 7, "Queenside castle");
    }
}
