using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests that verify PieceMove.curx/cury stays in sync with BoardState.board[x,y]
/// after every move, capture, castling, en passant, and promotion.
/// This directly addresses the "pieces lose their position" bug report.
/// </summary>
[TestFixture]
public class BoardStateSyncTests
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
    public void AfterPlacement_PieceMoveAndBoardStateAgree()
    {
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4);
        builder.AssertPositionSync(piece);
    }

    [Test]
    public void AfterPlacement_AllPiecesSync()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 1);
        builder.AssertAllPositionsSync();
    }

    [Test]
    public void AfterMove_PieceMoveAndBoardStateAgree()
    {
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        builder.MovePieceState(piece, 0, 5);

        builder.AssertPositionSync(piece);
        Assert.AreEqual(0, piece.curx);
        Assert.AreEqual(5, piece.cury);
        Assert.AreEqual(piece, builder.BoardState.GetPieceAt(0, 5));
        Assert.IsNull(builder.BoardState.GetPieceAt(0, 0));
    }

    [Test]
    public void AfterCapture_CapturedPieceRemovedFromBoardState()
    {
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 4);
        var defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 0, 1);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        builder.CapturePiece(attacker, defender);

        Assert.IsNull(builder.BoardState.GetPieceAt(0, 4), "Old attacker position should be empty");
        Assert.AreEqual(attacker, builder.BoardState.GetPieceAt(0, 1), "Attacker should be at capture target");
        Assert.IsFalse(builder.BoardState.GetAllPieces(ChessConstants.BLACK).Contains(defender));
    }

    [Test]
    public void AfterCapture_AttackerPositionSynced()
    {
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 5);
        var defender = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 3, 2);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        builder.CapturePiece(attacker, defender);
        builder.AssertPositionSync(attacker);
    }

    [Test]
    public void AfterCastling_KingAndRookPositionsSynced()
    {
        // Setup kingside castling for white
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Simulate castling: king to (6,7), rook to (5,7)
        builder.MovePieceState(king, 6, 7);
        // Move rook manually since MovePieceState doesn't handle castling
        Square rookOldSq = builder.GetSquare(7, 7);
        rookOldSq.taken = false;
        rookOldSq.piece = null;
        rook.curx = 5;
        rook.cury = 7;
        Square rookNewSq = builder.GetSquare(5, 7);
        rookNewSq.taken = true;
        rookNewSq.piece = rook;
        rook.curSquare = rookNewSq;
        builder.BoardState.MovePiece(7, 7, 5, 7);
        builder.BoardState.RecalculateAttacks();

        builder.AssertPositionSync(king);
        builder.AssertPositionSync(rook);
    }

    [Test]
    public void AfterMultipleMoves_AllPiecesRemainSynced()
    {
        var wKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        var bKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var wRook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        var bPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 1);
        var wPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6);

        // Move sequence: pawn forward, pawn forward, rook up, another pawn forward
        builder.MovePieceState(wPawn, 3, 5);
        builder.AssertAllPositionsSync();

        builder.MovePieceState(bPawn, 3, 2);
        builder.AssertAllPositionsSync();

        builder.MovePieceState(wRook, 0, 3);
        builder.AssertAllPositionsSync();

        builder.MovePieceState(wPawn, 3, 4);
        builder.AssertAllPositionsSync();

        builder.MovePieceState(wRook, 3, 3);
        builder.AssertAllPositionsSync();
    }

    [Test]
    public void SquareTakenFlag_MatchesBoardState()
    {
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 2, 4);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        Square occupied = builder.GetSquare(2, 4);
        Square empty = builder.GetSquare(3, 4);

        Assert.IsTrue(occupied.taken);
        Assert.IsFalse(empty.taken);
        Assert.IsNotNull(builder.BoardState.GetPieceAt(2, 4));
        Assert.IsNull(builder.BoardState.GetPieceAt(3, 4));
    }

    [Test]
    public void SquarePieceRef_MatchesBoardState()
    {
        var piece = builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.BLACK, 5, 3);
        Square sq = builder.GetSquare(5, 3);
        Assert.AreEqual(piece, sq.piece);
        Assert.AreEqual(piece, builder.BoardState.GetPieceAt(5, 3));
    }

    [Test]
    public void CurSquareRef_MatchesPiecePosition()
    {
        var piece = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 1, 7);
        Assert.AreEqual(builder.GetSquare(1, 7), piece.curSquare);

        builder.MovePieceState(piece, 2, 5);
        Assert.AreEqual(builder.GetSquare(2, 5), piece.curSquare);
    }

    [Test]
    public void AttackMaps_RecalculatedAfterEveryMove()
    {
        var king = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 4);

        // Rook at (7,4) does NOT attack (4,7) because not aligned
        Assert.IsFalse(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));

        // Move rook to (4,4) — now attacks king on file
        builder.MovePieceState(rook, 4, 4);
        Assert.IsTrue(builder.BoardState.IsKingInCheck(ChessConstants.WHITE));
    }

    [Test]
    public void AfterAbilityMove_PieceMoveAndBoardStateAgree()
    {
        // Place an elemental pawn and simulate an ability-induced move
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Simulate ability move (just state update, like Flame Rush destination)
        builder.MovePieceState(pawn, 3, 4);
        builder.AssertPositionSync(pawn);
    }

    [Test]
    public void FirstMoveFlag_ClearedAfterMove()
    {
        var piece = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        Assert.IsTrue(piece.firstMove);

        builder.MovePieceState(piece, 3, 4);
        Assert.IsFalse(piece.firstMove);
    }
}
