using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class EnPassantTests
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
    public void EnPassant_WhiteCaptures_WhenTargetSet()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // White pawn on rank 3 (0-indexed) = en passant rank for white
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        wPawn.firstMove = false;
        // Black pawn just did double move — set en passant target
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 3);
        builder.GM.enPassantTarget = builder.GetSquare(5, 2);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertContainsMove(moves, 5, 2, "White should capture en passant");
    }

    [Test]
    public void EnPassant_BlackCaptures_WhenTargetSet()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // Black pawn on rank 4 (0-indexed) = en passant rank for black
        var bPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 4);
        bPawn.firstMove = false;
        // White pawn just did double move
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        builder.GM.enPassantTarget = builder.GetSquare(4, 5);

        var moves = builder.GenerateMoves(bPawn);
        TestExtensions.AssertContainsMove(moves, 4, 5, "Black should capture en passant");
    }

    [Test]
    public void EnPassant_NotAvailable_WhenNoTarget()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        wPawn.firstMove = false;
        builder.GM.enPassantTarget = null;

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertMoveCount(moves, 1, "Only forward move, no en passant");
        TestExtensions.AssertContainsMove(moves, 4, 2);
    }

    [Test]
    public void EnPassant_NotAvailable_WrongRank()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // White pawn NOT on rank 3
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5);
        wPawn.firstMove = false;
        builder.GM.enPassantTarget = builder.GetSquare(5, 4);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertDoesNotContainMove(moves, 5, 4, "En passant not available on wrong rank");
    }

    [Test]
    public void EnPassant_OnlyAvailableFromAdjacentFile()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 2, 3);
        wPawn.firstMove = false;
        // Target at file 5 — too far from file 2
        builder.GM.enPassantTarget = builder.GetSquare(5, 2);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertDoesNotContainMove(moves, 5, 2, "En passant only from adjacent file");
    }

    [Test]
    public void EnPassant_CaptureIsAtTargetSquare()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        wPawn.firstMove = false;
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        builder.GM.enPassantTarget = builder.GetSquare(3, 2);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertContainsMove(moves, 3, 2, "En passant captures to the target square behind the enemy pawn");
    }
}
