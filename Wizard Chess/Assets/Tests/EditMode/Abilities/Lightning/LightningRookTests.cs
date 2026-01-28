using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningRookTests
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

    // ========== LightningRookPassive (Overcharge) ==========

    [Test]
    public void Passive_AddsMovesThroughFriendlyPiece()
    {
        // Overcharge: rook can pass through one friendly piece on cardinal lines
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place a friendly piece blocking the path upward
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 3);

        var moves = builder.GenerateMoves(rook);

        // Base rook moves stop at (3,3) since it's friendly.
        // Passive should add squares beyond the friendly piece: (3,2), (3,1)
        // but (3,0) has the black king which is an enemy - should also be added as capture
        TestExtensions.AssertContainsMove(moves, 3, 2, "Should be able to move past friendly piece to (3,2)");
        TestExtensions.AssertContainsMove(moves, 3, 1, "Should be able to move past friendly piece to (3,1)");
    }

    [Test]
    public void Passive_CanCaptureEnemyAfterPassthrough()
    {
        // Overcharge: after passing through a friendly piece, can capture enemy beyond
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 5,
            ChessConstants.ELEMENT_LIGHTNING);

        // Friendly piece on the path
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4);
        // Enemy piece beyond
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        var moves = builder.GenerateMoves(rook);

        // Passive should add enemy square (3,3) as a capture after passing through friendly at (3,4)
        TestExtensions.AssertContainsMove(moves, 3, 3,
            "Should be able to capture enemy beyond friendly piece");
    }

    [Test]
    public void Passive_StopsAfterMaxPassthrough()
    {
        // Default maxPassthrough=1, so cannot pass through 2 friendly pieces
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 6,
            ChessConstants.ELEMENT_LIGHTNING);

        // Two friendly pieces on the upward path
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4);

        var moves = builder.GenerateMoves(rook);

        // Base moves stop at (3,5). Passive passes through (3,5) but hits another friendly at (3,4).
        // Since maxPassthrough=1, it stops. Should NOT reach (3,3).
        TestExtensions.AssertDoesNotContainMove(moves, 3, 3,
            "Should not pass through 2 friendly pieces when maxPassthrough=1");
    }

    [Test]
    public void Passive_NoEffectWithoutFriendlyBlocker()
    {
        // When no friendly piece blocks the path, passive doesn't add anything extra
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(rook);

        // Standard rook moves in all 4 cardinal directions (no passthrough needed)
        TestExtensions.AssertContainsMove(moves, 3, 3);
        TestExtensions.AssertContainsMove(moves, 3, 5);
        TestExtensions.AssertContainsMove(moves, 0, 4);
        TestExtensions.AssertContainsMove(moves, 7, 4);
    }

    [Test]
    public void Passive_EmptySquaresBeyondFriendlyAreAdded()
    {
        // After passing through a friendly, all empty squares beyond are added until blocked
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Friendly piece at (1,4)
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 1, 4);

        var moves = builder.GenerateMoves(rook);

        // Beyond friendly at (1,4), empty squares (2,4), (3,4), etc. should be added
        TestExtensions.AssertContainsMove(moves, 2, 4, "Empty square beyond friendly");
        TestExtensions.AssertContainsMove(moves, 3, 4, "Empty square beyond friendly");
    }

    // ========== LightningRookActive (Thunder Strike) ==========

    [Test]
    public void Active_CanActivate_WhenEmptySquaresExist()
    {
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        Assert.IsTrue(rook.elementalPiece.active.CanActivate(rook, builder.BoardState, builder.SEM),
            "Should be able to activate when empty cardinal squares exist");
    }

    [Test]
    public void Active_GetTargetSquares_IgnoresBlockers()
    {
        // Thunder Strike: teleport to any empty square in cardinal directions, ignoring blockers
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place blockers on cardinal lines
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5);

        var targets = rook.elementalPiece.active.GetTargetSquares(rook, builder.BoardState);

        // Should include empty squares beyond blockers
        bool hasBeyondBlackPawn = false;
        bool hasBeyondWhitePawn = false;
        foreach (var t in targets)
        {
            if (t.x == 3 && t.y == 2) hasBeyondBlackPawn = true;
            if (t.x == 3 && t.y == 6) hasBeyondWhitePawn = true;
        }
        Assert.IsTrue(hasBeyondBlackPawn, "Should teleport past enemy blocker to (3,2)");
        Assert.IsTrue(hasBeyondWhitePawn, "Should teleport past friendly blocker to (3,6)");
    }

    [Test]
    public void Active_GetTargetSquares_DoesNotIncludeOccupiedSquares()
    {
        // Thunder Strike cannot capture (no capture during teleport)
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2);

        var targets = rook.elementalPiece.active.GetTargetSquares(rook, builder.BoardState);

        // (3,2) is occupied by enemy - should NOT be a target
        bool hasOccupied = false;
        foreach (var t in targets)
        {
            if (t.x == 3 && t.y == 2) hasOccupied = true;
        }
        Assert.IsFalse(hasOccupied, "Should not include occupied squares as teleport targets");
    }

    [Test]
    public void Active_Execute_TeleportsRook()
    {
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place blocker between rook and target
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        // Teleport to (3,1) - beyond the blocker
        var target = builder.GetSquare(3, 1);
        bool result = rook.elementalPiece.active.Execute(rook, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(result, "Execute should return true");
        Assert.AreEqual(3, rook.curx);
        Assert.AreEqual(1, rook.cury, "Rook should have teleported to (3,1)");
    }

    [Test]
    public void Active_Execute_DoesNotCapture()
    {
        // Thunder Strike is movement only, no capture
        PlaceKings();
        var rook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Enemy still there after teleport past it
        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        var target = builder.GetSquare(3, 1);
        rook.elementalPiece.active.Execute(rook, target, builder.BoardState, builder.SEM);

        Assert.IsNotNull(builder.BoardState.GetPieceAt(3, 3),
            "Enemy should not be captured by Thunder Strike");
    }
}
