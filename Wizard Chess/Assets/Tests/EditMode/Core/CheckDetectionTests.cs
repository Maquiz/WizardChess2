using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
public class CheckDetectionTests
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
    public void IsKingInCheck_ByPawn_Detected()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        // Black pawn at (3,3) attacks (4,4)? No — black pawns move +y, attack diag (+y)
        // Black pawn at (5,5) attacks (4,6) and (6,6). Need black pawn at (3,5) or (5,5)
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        builder.BoardState.RecalculateAttacks();
        // Black pawn at (3,3) attacks (2,4) and (4,4)
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_ByRook_Detected()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_ByBishop_Detected()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 7, 4);
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_ByKnight_Detected()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 5, 5); // L-shape to (4,7)? 5+(-1)=4, 5+2=7. Yes!
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_ByQueen_Detected()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 4, 3);
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void IsKingInCheck_NotByBlockedRook()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5); // blocks rook
        builder.BoardState.RecalculateAttacks();
        Assert.IsFalse(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void Checkmate_KingTrapped_NoLegalMoves()
    {
        // Back rank mate: king at h8, rook checks on rank 8, queen covers escape
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0); // checks on rank 0
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 1, 1); // covers rank 1

        // Black king at (7,0) in check from rook at (0,0)
        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.BLACK));

        var moves = builder.GenerateMoves(king);
        // King should have no legal escape (rank 0 attacked by rook, rank 1 attacked by other rook)
        TestExtensions.AssertMoveCount(moves, 0, "Checkmate — king has no legal moves");
    }

    [Test]
    public void Stalemate_KingNotInCheck_NoLegalMoves()
    {
        // King at a8 (0,0), not in check, but all moves are into check
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 2, 1);
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 1, 2); // controls (0,1), (1,1), (1,0)

        builder.BoardState.RecalculateAttacks();
        Assert.IsFalse(builder.BoardState.IsKingInCheck(ChessConstants.BLACK), "King should NOT be in check");

        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertMoveCount(moves, 0, "Stalemate — king has no legal moves but is not in check");
    }

    [Test]
    public void BlockingPiece_CanBlockCheck()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2); // check on file
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 3);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(bishop);
        // Bishop should be able to block at (4,5) or similar — if reachable
        // Bishop at (2,3) can move to (4,5) — blocks check
        TestExtensions.AssertContainsMove(moves, 4, 5, "Bishop should be able to block check");
    }

    [Test]
    public void CapturingChecker_ResolvesCheck()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 5);
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(knight);
        TestExtensions.AssertContainsMove(moves, 4, 5, "Knight should capture rook to resolve check");
    }

    [Test]
    public void CapturingChecker_WithRook_ResolvesCheck()
    {
        // White king at (0,7), black rook checks at (0,3), white rook at (5,3) can capture
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 0, 3);
        var wRook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 5, 3);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(wRook);
        TestExtensions.AssertContainsMove(moves, 0, 3, "Rook should capture checking rook");
    }

    [Test]
    public void CapturingChecker_WithBishop_ResolvesCheck()
    {
        // White king at (4,7), black queen checks diagonally from (7,4), white bishop at (5,2) can capture
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 7, 4);
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 5, 2);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertContainsMove(moves, 7, 4, "Bishop should capture checking queen on diagonal");
    }

    [Test]
    public void CapturingChecker_WithPawn_ResolvesCheck()
    {
        // White king at (4,7), black knight checks from (3,5), white pawn at (4,6) can capture diagonally
        // White pawn at (4,6) moves -y direction, attacks (3,5) and (5,5)
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 3, 5);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        pawn.firstMove = false;

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 3, 5, "Pawn should capture checking knight diagonally");
    }

    [Test]
    public void CapturingChecker_WithKing_ResolvesCheck()
    {
        // White king at (4,7), black knight checks from (5,5), knight is undefended, king can capture
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 5, 5);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertContainsMove(moves, 5, 6, "King should be able to escape check");
    }

    [Test]
    public void DiscoveredCheck_PinnedBishopCannotMoveOffFile()
    {
        // White king at (4,7), white bishop at (4,5), black rook at (4,2)
        // Bishop is pinned on file — moving it off file exposes king
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2);
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 4, 5);

        builder.BoardState.RecalculateAttacks();
        Assert.IsFalse(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var moves = builder.GenerateMoves(bishop);
        TestExtensions.AssertMoveCount(moves, 0, "Bishop pinned on file has no legal moves");
    }

    [Test]
    public void PinnedRook_CanMoveAlongPinRay()
    {
        // White king at (4,7), white rook at (4,5), black rook at (4,2)
        // Rook is pinned on file but can move along the file and capture
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 5);

        builder.BoardState.RecalculateAttacks();

        var moves = builder.GenerateMoves(rook);
        // Rook can move along file: (4,6), (4,4), (4,3), (4,2) capture
        TestExtensions.AssertContainsMove(moves, 4, 6, "Pinned rook can move along pin ray");
        TestExtensions.AssertContainsMove(moves, 4, 4, "Pinned rook can move along pin ray");
        TestExtensions.AssertContainsMove(moves, 4, 3, "Pinned rook can move along pin ray");
        TestExtensions.AssertContainsMove(moves, 4, 2, "Pinned rook can capture pinner");
        // Should NOT be able to move off file
        TestExtensions.AssertDoesNotContainMove(moves, 3, 5, "Pinned rook cannot move off file");
        TestExtensions.AssertDoesNotContainMove(moves, 5, 5, "Pinned rook cannot move off file");
    }

    [Test]
    public void PinnedPawn_OnDiagonal_HasNoMoves()
    {
        // White king at (4,7), white pawn at (3,6), black bishop at (1,4)
        // Pawn is pinned on diagonal — forward move exposes king, no capture along pin
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 1, 4);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6);
        pawn.firstMove = false;

        builder.BoardState.RecalculateAttacks();

        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertMoveCount(moves, 0, "Pawn pinned on diagonal with no capture along pin has no moves");
    }

    [Test]
    public void DoubleCheck_OnlyKingCanMove()
    {
        // White king at (4,7), black rook at (4,2) checks on file, black bishop at (7,4) checks on diagonal
        // Double check — only king can move
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 2);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 7, 4);
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 2, 6);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        // Non-king piece should have 0 moves in double check
        var knightMoves = builder.GenerateMoves(knight);
        TestExtensions.AssertMoveCount(knightMoves, 0, "Non-king pieces have no moves in double check");

        // King should still have some escape squares
        var kingMoves = builder.GenerateMoves(king);
        Assert.IsTrue(kingMoves.Count > 0, "King should have at least one escape square in double check");
    }

    [Test]
    public void Checkmate_SmotheredMate()
    {
        // Black king at (7,0) surrounded by own pieces, white knight delivers mate
        // King at h1, rook at h2, pawn at g1, knight mates from f2
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 1);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 6, 0);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 6, 1);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 5, 1);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.BLACK), "King should be in check from knight");

        var moves = builder.GenerateMoves(king);
        TestExtensions.AssertMoveCount(moves, 0, "Smothered mate — king boxed by own pieces with knight check");
    }

    [Test]
    public void Checkmate_FoolsMate()
    {
        // Fool's mate position: 1.f3 e5 2.g4 Qh4#
        // Full back rank + moved pawns to block all escape
        var wKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // White back rank pieces block d1 and f1
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 7);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 5, 7);
        // White pawns block d2, e2
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 6);
        // Moved pawns: f3, g4
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 5);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 6, 4);
        // Black queen delivers check on diagonal h4-e1
        builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 7, 4);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE), "White king should be in check");

        var kingMoves = builder.GenerateMoves(wKing);
        TestExtensions.AssertMoveCount(kingMoves, 0, "Fool's mate — white king has no legal moves");
    }

    [Test]
    public void Check_MultipleEscapeOptions_BlockAndCapture()
    {
        // White king at (4,7), black rook at (4,3) checks on file
        // White bishop at (2,5) can block at (4,5)
        // White knight at (3,1) can capture at (4,3)
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 0, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);
        var bishop = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 3);
        var knight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 1);

        builder.BoardState.RecalculateAttacks();
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        var bishopMoves = builder.GenerateMoves(bishop);
        TestExtensions.AssertContainsMove(bishopMoves, 4, 5, "Bishop should be able to block check");

        var knightMoves = builder.GenerateMoves(knight);
        TestExtensions.AssertContainsMove(knightMoves, 4, 3, "Knight should be able to capture checker");
    }
}
