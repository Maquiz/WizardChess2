using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningKingTests
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

    // ========== LightningKingPassive (Reactive Blink) ==========

    [Test]
    public void Passive_WhenInCheck_AddsSafeSquaresWithinRange()
    {
        // Reactive Blink: when king is in check, add safe squares within blinkRange=2
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Put king in check with a rook on the same file
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 5);

        var moves = builder.GenerateMoves(king);

        // Without Reactive Blink, king at (4,7) normally has: (3,7),(5,7),(3,6),(5,6)
        // The file column 4 is attacked by the rook, so (4,6) is not safe.
        // With blink (range=2), king can reach squares within Chebyshev distance 2 that are safe.
        // (2,7),(2,6),(2,5),(3,5),(4,5),(5,5),(6,5),(6,6),(6,7) are within range 2
        // But (4,5) has the rook, (4,6) is attacked, etc.
        // Just check that blink adds at least some squares beyond the normal king range
        bool hasBlinkSquare = false;
        foreach (var m in moves)
        {
            int dx = System.Math.Abs(m.x - 4);
            int dy = System.Math.Abs(m.y - 7);
            if (dx > 1 || dy > 1)
            {
                hasBlinkSquare = true;
                break;
            }
        }
        Assert.IsTrue(hasBlinkSquare,
            "Reactive Blink should add safe squares beyond normal king move range when in check");
    }

    [Test]
    public void Passive_WhenNotInCheck_DoesNotAddExtraMoves()
    {
        // Reactive Blink only activates in check
        PlaceKings();

        // Replace white king with elemental king (need to remove first)
        // Actually, since PlaceKings already places a non-elemental king, let's set up differently
        builder.Cleanup();
        builder = new ChessBoardBuilder();
        builder.Build();

        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // No check - just open board
        var moves = builder.GenerateMoves(king);

        // Without check, blink should not activate. King should only have normal moves.
        // Normal king at (4,7): (3,7),(5,7),(3,6),(4,6),(5,6) - 5 squares
        foreach (var m in moves)
        {
            int dx = System.Math.Abs(m.x - 4);
            int dy = System.Math.Abs(m.y - 7);
            Assert.IsTrue(dx <= 1 && dy <= 1,
                $"Move ({m.x},{m.y}) is beyond normal king range - blink should not be active when not in check");
        }
    }

    [Test]
    public void Passive_OncePerGame_DoesNotActivateAfterUsed()
    {
        // Reactive Blink can only be used once. After hasUsedReactiveBlink=true, no extra moves.
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Mark blink as used
        king.elementalPiece.hasUsedReactiveBlink = true;

        // Put king in check
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 5);

        var moves = builder.GenerateMoves(king);

        // Even though in check, blink should not add extra squares
        foreach (var m in moves)
        {
            int dx = System.Math.Abs(m.x - 4);
            int dy = System.Math.Abs(m.y - 7);
            Assert.IsTrue(dx <= 1 && dy <= 1,
                $"Move ({m.x},{m.y}) is beyond king range - blink should not activate after being used");
        }
    }

    [Test]
    public void Passive_BlinkSquaresAreNotAttacked()
    {
        // Reactive Blink only adds squares not attacked by opponents
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Put king in check with rook on column 4
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);

        var moves = builder.GenerateMoves(king);

        int opponentColor = ChessConstants.BLACK;
        foreach (var m in moves)
        {
            // After the king moves there, the square should be safe.
            // Note: this is a simplified check. The real filterIllegalMoves handles
            // full legality. But blink itself filters by IsSquareAttackedBy.
            // We verify blink doesn't add attacked squares within range 2.
            int dx = System.Math.Abs(m.x - 4);
            int dy = System.Math.Abs(m.y - 7);
            if (dx > 1 || dy > 1)
            {
                // This is a blink move - should not be on an attacked square
                // (though filterIllegalMoves may further filter, blink itself checks)
                Assert.IsFalse(builder.BoardState.IsSquareAttackedBy(m.x, m.y, opponentColor),
                    $"Blink move ({m.x},{m.y}) should not be on a square attacked by opponent");
            }
        }
    }

    [Test]
    public void Passive_OnAfterMove_SetsHasUsedBlink_WhenMovedFar()
    {
        // When king moves more than 1 square (a blink), hasUsedReactiveBlink is set to true
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        Assert.IsFalse(king.elementalPiece.hasUsedReactiveBlink, "Blink should not be used initially");

        // Simulate a blink move (more than 1 square in any direction)
        var passive = king.elementalPiece.passive;
        passive.OnAfterMove(king, 4, 7, 4, 5, builder.BoardState);

        Assert.IsTrue(king.elementalPiece.hasUsedReactiveBlink,
            "Blink should be marked as used after moving more than 1 square");
    }

    [Test]
    public void Passive_OnAfterMove_DoesNotSetBlink_WhenMovedOneSquare()
    {
        // Normal 1-square king move should not consume the blink
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        var passive = king.elementalPiece.passive;
        passive.OnAfterMove(king, 4, 7, 4, 6, builder.BoardState);

        Assert.IsFalse(king.elementalPiece.hasUsedReactiveBlink,
            "Blink should not be used for a normal 1-square move");
    }

    [Test]
    public void Passive_OnAfterMove_DoesNotSetBlink_WhenAlreadyUsed()
    {
        // If blink was already used, OnAfterMove should not try to set it again
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        king.elementalPiece.hasUsedReactiveBlink = true;

        // This should not error or change anything
        var passive = king.elementalPiece.passive;
        passive.OnAfterMove(king, 4, 7, 4, 5, builder.BoardState);

        Assert.IsTrue(king.elementalPiece.hasUsedReactiveBlink,
            "Flag should remain true");
    }

    // ========== LightningKingActive (Static Field) ==========

    [Test]
    public void Active_CanActivate_Always()
    {
        // Static Field always returns true
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        Assert.IsTrue(king.elementalPiece.active.CanActivate(king, builder.BoardState, builder.SEM),
            "Static Field should always be activatable");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsSelfSquare()
    {
        // Static Field targets the king's own square
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var targets = king.elementalPiece.active.GetTargetSquares(king, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Should return exactly one target");
        Assert.AreEqual(4, targets[0].x);
        Assert.AreEqual(4, targets[0].y, "Target should be king's own square");
    }

    [Test]
    public void Active_Execute_CreatesLightningFieldsOnAdjacentSquares()
    {
        // Static Field creates LightningField effects on all 8 adjacent squares
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(4, 4);
        king.elementalPiece.active.Execute(king, target, builder.BoardState, builder.SEM);

        // Check all 8 adjacent squares have lightning field effects
        (int x, int y)[] adjacentSquares =
        {
            (4, 3), (4, 5), (5, 4), (5, 5), (5, 3), (3, 4), (3, 5), (3, 3)
        };
        foreach (var sq in adjacentSquares)
        {
            var effect = builder.SEM.GetEffectAt(sq.x, sq.y);
            Assert.IsNotNull(effect, $"Lightning field should exist at ({sq.x},{sq.y})");
            Assert.AreEqual(SquareEffectType.LightningField, effect.effectType,
                $"Effect at ({sq.x},{sq.y}) should be LightningField");
        }
    }

    [Test]
    public void Active_Execute_LightningFieldHasCorrectDuration()
    {
        // Default fieldDuration=2
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(4, 4);
        king.elementalPiece.active.Execute(king, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(4, 3);
        Assert.IsNotNull(effect);
        Assert.AreEqual(2, effect.remainingTurns, "Lightning field should have duration=2 (default)");
    }

    [Test]
    public void Active_Execute_LightningFieldHasCorrectOwner()
    {
        // Lightning field should be owned by the king's color
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(4, 4);
        king.elementalPiece.active.Execute(king, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(5, 4);
        Assert.IsNotNull(effect);
        Assert.AreEqual(ChessConstants.WHITE, effect.ownerColor,
            "Lightning field should be owned by the king's color");
    }

    [Test]
    public void Active_Execute_SkipsOutOfBoundsSquares()
    {
        // King at corner - only valid adjacent squares should get fields
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 0, 0,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(0, 0);
        king.elementalPiece.active.Execute(king, target, builder.BoardState, builder.SEM);

        // At (0,0), only 3 adjacent squares are in bounds: (0,1),(1,0),(1,1)
        var effect01 = builder.SEM.GetEffectAt(0, 1);
        var effect10 = builder.SEM.GetEffectAt(1, 0);
        var effect11 = builder.SEM.GetEffectAt(1, 1);

        Assert.IsNotNull(effect01, "Lightning field should exist at (0,1)");
        Assert.IsNotNull(effect10, "Lightning field should exist at (1,0)");
        Assert.IsNotNull(effect11, "Lightning field should exist at (1,1)");

        // Out of bounds squares should have no effect (no crash)
        // Just verify the above 3 were created without errors
    }

    [Test]
    public void Active_Execute_ReturnsTrue()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        var king = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(4, 4);
        bool result = king.elementalPiece.active.Execute(king, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(result, "Execute should return true");
    }
}
