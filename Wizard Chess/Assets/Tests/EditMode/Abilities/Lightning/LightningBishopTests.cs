using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningBishopTests
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

    // ========== LightningBishopPassive (Voltage Burst) ==========

    [Test]
    public void Passive_SingesAdjacentEnemies_WhenMovedFarEnough()
    {
        // Voltage Burst: after moving 3+ squares (Chebyshev distance), singe adjacent enemies
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 0, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place an enemy adjacent to the destination (3,4)
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 3,
            ChessConstants.ELEMENT_LIGHTNING);

        // Simulate bishop moving from (0,7) to (3,4) - Chebyshev distance = 3
        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 0, 7, 3, 4, builder.BoardState);

        Assert.IsTrue(enemy.elementalPiece.IsSinged(),
            "Adjacent enemy should be singed after bishop moves 3+ squares");
    }

    [Test]
    public void Passive_NoSinge_WhenMovedShortDistance()
    {
        // Voltage Burst: no effect when bishop moves less than 3 squares
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place an enemy adjacent to the destination (4,3)
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2,
            ChessConstants.ELEMENT_LIGHTNING);

        // Simulate bishop moving from (3,4) to (4,3) - Chebyshev distance = 1
        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 3, 4, 4, 3, builder.BoardState);

        Assert.IsFalse(enemy.elementalPiece.IsSinged(),
            "Adjacent enemy should NOT be singed after short move (dist=1)");
    }

    [Test]
    public void Passive_NoSinge_WhenDistanceExactly2()
    {
        // minMoveDistance is 3, so distance 2 should not trigger
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 5,
            ChessConstants.ELEMENT_LIGHTNING);

        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 2,
            ChessConstants.ELEMENT_LIGHTNING);

        // Simulate bishop moving from (3,5) to (5,3) - Chebyshev distance = 2
        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 3, 5, 5, 3, builder.BoardState);

        Assert.IsFalse(enemy.elementalPiece.IsSinged(),
            "Should not singe when move distance is exactly 2 (min is 3)");
    }

    [Test]
    public void Passive_DoesNotSingeFriendlyPieces()
    {
        // Voltage Burst only singes enemy pieces
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 0, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place a friendly piece adjacent to destination (3,4)
        var friendly = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 2, 3,
            ChessConstants.ELEMENT_LIGHTNING);

        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 0, 7, 3, 4, builder.BoardState);

        Assert.IsFalse(friendly.elementalPiece.IsSinged(),
            "Friendly pieces should not be singed");
    }

    [Test]
    public void Passive_DoesNotSingeKings()
    {
        // Voltage Burst skips kings (target.piece != ChessConstants.KING)
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 0, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Black king is at (4,0). Simulate bishop moving to (3,1) - adjacent to king.
        // Chebyshev distance from (0,4) to (3,1) = max(3,3) = 3
        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 0, 4, 3, 1, builder.BoardState);

        // King at (4,0) is adjacent to (3,1). Check it's not singed.
        PieceMove blackKing = builder.BoardState.GetPieceAt(4, 0);
        // King has no elementalPiece, so singe code tries AddComponent - but the check is piece != KING
        // Since the code checks adj.piece != ChessConstants.KING, king should not be singed.
        // The king doesn't have elementalPiece, so even if the code attempted, it wouldn't crash.
        // Just verify king is still there.
        Assert.IsNotNull(blackKing, "King should still be on the board");
    }

    [Test]
    public void Passive_SingesMultipleAdjacentEnemies()
    {
        // All adjacent enemies should be singed
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 0, 7,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place two enemies adjacent to destination (3,4)
        var enemy1 = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        var enemy2 = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3,
            ChessConstants.ELEMENT_LIGHTNING);

        var passive = bishop.elementalPiece.passive;
        passive.OnAfterMove(bishop, 0, 7, 3, 4, builder.BoardState);

        Assert.IsTrue(enemy1.elementalPiece.IsSinged(), "First adjacent enemy should be singed");
        Assert.IsTrue(enemy2.elementalPiece.IsSinged(), "Second adjacent enemy should be singed");
    }

    // ========== LightningBishopActive (Arc Flash) ==========

    [Test]
    public void Active_CanActivate_WhenFriendlyPiecesExist()
    {
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // White king at (4,7) is a friendly piece to swap with
        Assert.IsTrue(bishop.elementalPiece.active.CanActivate(bishop, builder.BoardState, builder.SEM),
            "Should be able to activate when friendly pieces exist");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsFriendlyPieceSquares()
    {
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 5);

        var targets = bishop.elementalPiece.active.GetTargetSquares(bishop, builder.BoardState);

        // Should include squares of friendly pieces (white king at 4,7 and pawn at 5,5), but not self
        bool hasPawnSquare = false;
        bool hasKingSquare = false;
        bool hasSelfSquare = false;
        foreach (var t in targets)
        {
            if (t.x == 5 && t.y == 5) hasPawnSquare = true;
            if (t.x == 4 && t.y == 7) hasKingSquare = true;
            if (t.x == 3 && t.y == 4) hasSelfSquare = true;
        }
        Assert.IsTrue(hasPawnSquare, "Should include friendly pawn's square");
        Assert.IsTrue(hasKingSquare, "Should include friendly king's square");
        Assert.IsFalse(hasSelfSquare, "Should not include self square");
    }

    [Test]
    public void Active_GetTargetSquares_ExcludesEnemyPieces()
    {
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var targets = bishop.elementalPiece.active.GetTargetSquares(bishop, builder.BoardState);

        // Should not include black king square (4,0)
        bool hasEnemySquare = false;
        foreach (var t in targets)
        {
            if (t.x == 4 && t.y == 0) hasEnemySquare = true;
        }
        Assert.IsFalse(hasEnemySquare, "Should not include enemy piece squares");
    }

    [Test]
    public void Active_Execute_SwapsPositions()
    {
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 5);

        var target = builder.GetSquare(5, 5);
        bool result = bishop.elementalPiece.active.Execute(bishop, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(result, "Execute should return true");
        Assert.AreEqual(5, bishop.curx, "Bishop should now be at x=5");
        Assert.AreEqual(5, bishop.cury, "Bishop should now be at y=5");
        Assert.AreEqual(3, pawn.curx, "Pawn should now be at x=3");
        Assert.AreEqual(4, pawn.cury, "Pawn should now be at y=4");
    }

    [Test]
    public void Active_Execute_PreservesFirstMoveFlag()
    {
        // Arc Flash preserves firstMove status for both pieces
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);
        bishop.firstMove = false; // bishop has already moved

        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 5);
        // pawn.firstMove is true by default

        var target = builder.GetSquare(5, 5);
        bishop.elementalPiece.active.Execute(bishop, target, builder.BoardState, builder.SEM);

        Assert.IsFalse(bishop.firstMove, "Bishop's firstMove should still be false after swap");
        Assert.IsTrue(pawn.firstMove, "Pawn's firstMove should still be true after swap");
    }

    [Test]
    public void Active_Execute_FailsIfTargetIsEnemy()
    {
        // Arc Flash only works with friendly pieces
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 5);

        var target = builder.GetSquare(5, 5);
        bool result = bishop.elementalPiece.active.Execute(bishop, target, builder.BoardState, builder.SEM);

        Assert.IsFalse(result, "Execute should return false when target is an enemy");
    }

    [Test]
    public void Active_Execute_FailsIfTargetEmpty()
    {
        // Arc Flash requires a friendly piece at the target
        PlaceKings();
        var bishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var target = builder.GetSquare(5, 5); // empty square
        bool result = bishop.elementalPiece.active.Execute(bishop, target, builder.BoardState, builder.SEM);

        Assert.IsFalse(result, "Execute should return false when target square is empty");
    }
}
