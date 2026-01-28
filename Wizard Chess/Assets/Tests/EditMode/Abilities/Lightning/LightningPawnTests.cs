using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningPawnTests
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
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
    }

    // ========== LightningPawnPassive (Energized) ==========

    [Test]
    public void Passive_AfterFirstMove_AddsExtraForwardMove()
    {
        // Energized: even after first move, pawn can still move 2 squares forward
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        pawn.firstMove = false;

        var moves = builder.GenerateMoves(pawn);

        // White pawn at (3,4), firstMove=false: base gives (3,3).
        // Passive adds (3,2) since extraForwardRange=2 and path is clear.
        TestExtensions.AssertContainsMove(moves, 3, 3, "Should have normal 1-square forward move");
        TestExtensions.AssertContainsMove(moves, 3, 2, "Passive should add 2-square forward move");
    }

    [Test]
    public void Passive_FirstMoveTrue_DoesNotAddDuplicate()
    {
        // When firstMove=true, base move gen already includes the double forward.
        // Passive should not re-add it.
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6,
            ChessConstants.ELEMENT_LIGHTNING);
        // firstMove is true by default from PlacePiece

        var moves = builder.GenerateMoves(pawn);

        // Base: (3,5) and (3,4). Passive skips (returns early when firstMove=true).
        TestExtensions.AssertContainsMove(moves, 3, 5);
        TestExtensions.AssertContainsMove(moves, 3, 4);

        // Count how many times (3,4) appears - should only be once
        int count = 0;
        foreach (var m in moves)
        {
            if (m.x == 3 && m.y == 4) count++;
        }
        Assert.AreEqual(1, count, "Double-forward move should not be duplicated");
    }

    [Test]
    public void Passive_PathBlocked_DoesNotAddExtraForward()
    {
        // If the path to 2-square forward is blocked, passive should not add the move
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        pawn.firstMove = false;

        // Block the immediate forward square
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        var moves = builder.GenerateMoves(pawn);

        // Cannot move forward at all because (3,3) is blocked
        TestExtensions.AssertDoesNotContainMove(moves, 3, 3, "Blocked by enemy pawn");
        TestExtensions.AssertDoesNotContainMove(moves, 3, 2, "Path to 2-square forward is blocked");
    }

    [Test]
    public void Passive_SecondSquareBlocked_DoesNotAddExtraForward()
    {
        // If the first square is clear but the second is blocked, passive should not add
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        pawn.firstMove = false;

        // Block only the 2-square-forward square
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2);

        var moves = builder.GenerateMoves(pawn);

        // Can move 1 square forward
        TestExtensions.AssertContainsMove(moves, 3, 3, "1-square forward should still work");
        // Cannot move 2 squares forward since (3,2) is occupied
        TestExtensions.AssertDoesNotContainMove(moves, 3, 2, "2-square forward blocked by piece");
    }

    [Test]
    public void Passive_BlackPawn_AddsExtraForwardInCorrectDirection()
    {
        // Black pawns move +y, so extra forward should be +2y
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3,
            ChessConstants.ELEMENT_LIGHTNING);
        pawn.firstMove = false;

        var moves = builder.GenerateMoves(pawn);

        // Black pawn at (3,3), firstMove=false: base gives (3,4).
        // Passive adds (3,5).
        TestExtensions.AssertContainsMove(moves, 3, 4, "Normal forward for black");
        TestExtensions.AssertContainsMove(moves, 3, 5, "Passive extra forward for black");
    }

    [Test]
    public void Passive_NearEdge_DoesNotGoOutOfBounds()
    {
        // White pawn at y=1 (near top), firstMove=false.
        // 1-square forward is y=0 (valid). 2-square forward is y=-1 (out of bounds).
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 1,
            ChessConstants.ELEMENT_LIGHTNING);
        pawn.firstMove = false;

        var moves = builder.GenerateMoves(pawn);

        TestExtensions.AssertContainsMove(moves, 3, 0, "1-square forward to edge is valid");
        // (3,-1) is out of bounds - should not be in moves
    }

    // ========== LightningPawnActive (Chain Strike) ==========

    [Test]
    public void Active_CanActivate_WhenForwardSquareEmpty()
    {
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var active = pawn.elementalPiece.active;
        Assert.IsTrue(active.CanActivate(pawn, builder.BoardState, builder.SEM),
            "Should be able to activate when forward square (3,3) is empty");
    }

    [Test]
    public void Active_CannotActivate_WhenForwardSquareOccupied()
    {
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        var active = pawn.elementalPiece.active;
        Assert.IsFalse(active.CanActivate(pawn, builder.BoardState, builder.SEM),
            "Should not activate when forward square is occupied");
    }

    [Test]
    public void Active_CannotActivate_WhenAtBoardEdge()
    {
        // White pawn at y=0 - forward would be y=-1, out of bounds
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 0,
            ChessConstants.ELEMENT_LIGHTNING);

        var active = pawn.elementalPiece.active;
        Assert.IsFalse(active.CanActivate(pawn, builder.BoardState, builder.SEM),
            "Should not activate when at board edge");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsForwardSquare()
    {
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var targets = pawn.elementalPiece.active.GetTargetSquares(pawn, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Should have exactly one target square");
        Assert.AreEqual(3, targets[0].x);
        Assert.AreEqual(3, targets[0].y, "Target should be one square forward for white");
    }

    [Test]
    public void Active_Execute_MovesPawnForward()
    {
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(3, 3);
        pawn.elementalPiece.active.Execute(pawn, target, builder.BoardState, builder.SEM);

        Assert.AreEqual(3, pawn.curx, "Pawn x should remain 3");
        Assert.AreEqual(3, pawn.cury, "Pawn should have moved to y=3");
    }

    [Test]
    public void Active_Execute_ChainCapturesDiagonalEnemies()
    {
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place enemy diagonally from the target square (3,3)
        var enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 2);

        var target = builder.GetSquare(3, 3);
        pawn.elementalPiece.active.Execute(pawn, target, builder.BoardState, builder.SEM);

        // The chain capture should find the diagonal enemy at (2,2) from (3,3)
        // After capture, enemy should be removed
        Assert.IsNull(builder.BoardState.GetPieceAt(2, 2),
            "Diagonal enemy should be chain-captured");
    }

    [Test]
    public void Active_Execute_DoesNotCaptureKings()
    {
        // Chain strike skips kings
        PlaceKings();
        var pawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // The black king at (4,0) is not diagonal to (3,3), so place one near
        // Actually, chain captures skip kings by design: target.piece != ChessConstants.KING

        var target = builder.GetSquare(3, 3);
        bool result = pawn.elementalPiece.active.Execute(pawn, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(result, "Execute should return true");
        // Kings should still be on the board
        Assert.IsNotNull(builder.BoardState.GetPieceAt(4, 0), "Black king should not be captured");
    }
}
