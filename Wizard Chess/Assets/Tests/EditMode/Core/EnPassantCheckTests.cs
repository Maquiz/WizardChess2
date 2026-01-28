using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class EnPassantCheckTests
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
    public void EnPassant_Illegal_WhenExposesOwnKingOnRank()
    {
        // White king at (0,3), white pawn at (3,3), black pawn at (4,3), black rook at (7,3)
        // En passant capture at (4,2) removes black pawn from (4,3), exposing king to rook on rank 3
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 3);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 3);
        wPawn.firstMove = false;
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 3);
        builder.GM.enPassantTarget = builder.GetSquare(4, 2);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertDoesNotContainMove(moves, 4, 2,
            "En passant should be illegal when it exposes king to rook on same rank");
    }

    [Test]
    public void EnPassant_Legal_WhenDoesNotExposeKing()
    {
        // Same setup but king on different rank — ep is safe
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 3);
        wPawn.firstMove = false;
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 3);
        builder.GM.enPassantTarget = builder.GetSquare(4, 2);

        var moves = builder.GenerateMoves(wPawn);
        TestExtensions.AssertContainsMove(moves, 4, 2,
            "En passant should be legal when king is not on the same rank");
    }

    [Test]
    public void EnPassant_ResolvesCheck_ByCapturingChecker()
    {
        // Black pawn at (5,4) just did double move, checks white king at (4,5)?
        // Actually pawns don't check by moving forward. Let's set up a discovered check scenario.
        // White king at (4,4), black pawn at (5,4) (just double-moved from (5,6)),
        // en passant target at (5,5). White pawn at (4,4)? No, king is there.
        // Simpler: White king at (6,3), white pawn at (4,3), black pawn at (3,3) just double-moved.
        // Black pawn at (3,3) attacks (2,4) and (4,4) — doesn't check king at (6,3).
        // We need the black pawn itself to be giving check via diagonal attack.
        // Black pawn at (5,4) attacks (4,5) and (6,5). King at (4,5) is in check.
        // White pawn at (4,4) can en passant capture at (5,5)? No, ep target is behind the pawn.
        // Black pawn moved from (5,6) to (5,4), ep target at (5,5).
        // White pawn at (4,4) is adjacent, can ep capture to (5,5)? direction for white is -1, so (5,3) not (5,5).
        // Actually: white pawn at (4,4) moves -y. En passant for white means capturing to (5, 4-1=3).
        // But ep target would be at (5,5) — the square behind the black pawn (between (5,6) and (5,4)).
        // White pawn attacks (3,3) and (5,3), not (5,5).

        // Let's use black instead for simpler geometry:
        // Black king at (3,5), black pawn at (5,4), white pawn at (4,4) just double-moved from (4,6).
        // En passant target at (4,5). Black pawn at (5,4) can ep capture to (4,5).
        // White pawn at (4,4) attacks (3,3) and (5,3) — doesn't check black king at (3,5).
        // We need something that IS checking the king that ep can resolve.

        // Simplest: use a scenario where capturing ep removes a checking pawn.
        // Black pawn at (4,4) attacks (3,5) and (5,5). Black king at (3,5) is NOT attacked by own pawn.
        // White pawn at (4,4) attacks (3,3) and (5,3).

        // Let's try: White pawn at (3,4) just double-moved from (3,6) to (3,4).
        // White pawn at (3,4) attacks (2,3) and (4,3).
        // Black king at (4,3)? Then it's in check from white pawn.
        // Black pawn at (4,4) can en passant to (3,5)? No, black moves +y, so (3, 4+1=5).
        // ep target at (3,5). But the checking piece is at (3,4), and ep removes it. This works!
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 7, 7);
        var bKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 3);
        var bPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 4);
        bPawn.firstMove = false;
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4);
        builder.GM.enPassantTarget = builder.GetSquare(3, 5);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.BLACK),
            "Black king should be in check from white pawn");

        var moves = builder.GenerateMoves(bPawn);
        TestExtensions.AssertContainsMove(moves, 3, 5,
            "En passant should resolve check by capturing the checking pawn");
    }

    [Test]
    public void EnPassant_Execution_RemovesCapturedPawnFromBoardState()
    {
        // After en passant capture, the captured pawn should be removed from BoardState
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);
        wPawn.firstMove = false;
        var bPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 3);
        builder.GM.enPassantTarget = builder.GetSquare(5, 2);

        // Simulate en passant capture via state update
        // The captured pawn is at (5,3), white pawn moves to (5,2)
        builder.BoardState.MovePiece(4, 3, 5, 2);
        wPawn.curx = 5;
        wPawn.cury = 2;
        // Remove the captured pawn (this is what Bug 2 fix ensures happens in movePiece)
        builder.BoardState.RemovePiece(5, 3);
        builder.BoardState.RecalculateAttacks();

        Assert.IsNull(builder.BoardState.GetPieceAt(5, 3),
            "Captured pawn should be removed from board after en passant");
        Assert.IsNull(builder.BoardState.GetPieceAt(4, 3),
            "Original pawn position should be empty after en passant");
        Assert.AreEqual(wPawn, builder.BoardState.GetPieceAt(5, 2),
            "Capturing pawn should be at en passant target square");

        // Verify captured pawn is not in piece lists
        var blackPieces = builder.BoardState.GetAllPieces(ChessConstants.BLACK);
        Assert.IsFalse(blackPieces.Contains(bPawn),
            "Captured pawn should not be in black piece list");
    }
}
