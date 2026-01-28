using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class FireRookTests
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

    // ========== FireRookPassive (Trail Blazer) ==========

    [Test]
    public void Passive_OnAfterMove_CreatesFireOnDepartureSquare()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7, ChessConstants.ELEMENT_FIRE);

        FireRookPassive passive = (FireRookPassive)fireRook.elementalPiece.passive;
        // Simulate moving from (0,7) to (0,4)
        passive.OnAfterMove(fireRook, 0, 7, 0, 4, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(0, 7);
        Assert.IsNotNull(effect, "Fire should be created on departure square (0,7)");
        Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
    }

    [Test]
    public void Passive_OnAfterMove_FireHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7, ChessConstants.ELEMENT_FIRE);

        FireRookPassive passive = (FireRookPassive)fireRook.elementalPiece.passive;
        passive.OnAfterMove(fireRook, 0, 7, 0, 4, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(0, 7);
        Assert.IsNotNull(effect);
        // Default FireRookPassiveParams.fireDuration = 1
        Assert.AreEqual(1, effect.remainingTurns, "Fire duration should match default of 1");
    }

    [Test]
    public void Passive_OnAfterMove_FireOwnerMatchesRookColor()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 0, ChessConstants.ELEMENT_FIRE);

        FireRookPassive passive = (FireRookPassive)fireRook.elementalPiece.passive;
        passive.OnAfterMove(fireRook, 7, 0, 7, 3, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(7, 0);
        Assert.IsNotNull(effect);
        Assert.AreEqual(ChessConstants.BLACK, effect.ownerColor);
    }

    // ========== FireRookActive (Inferno Line) ==========

    [Test]
    public void Active_Execute_CreatesFireLineInCardinalDirection()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireRookActive active = (FireRookActive)fireRook.elementalPiece.active;
        // Target the last square in the +y direction: (3,4) + 4 squares up = (3,0)
        // Direction is (0,-1), so line goes (3,3), (3,2), (3,1), (3,0)
        // But king is at (4,0), not blocking. Black king at (4,0) is fine.
        // Let's target in +x direction to avoid king.
        // Use target (7,4) which is the end of the line going right (+x)
        Square target = builder.GetSquare(7, 4);

        active.Execute(fireRook, target, builder.BoardState, builder.SEM);

        // Direction dx=+1, dy=0. Default lineLength=4, so fire at (4,4), (5,4), (6,4), (7,4)
        for (int i = 1; i <= 4; i++)
        {
            int nx = 3 + i;
            SquareEffect effect = builder.SEM.GetEffectAt(nx, 4);
            Assert.IsNotNull(effect, $"Fire should be at ({nx}, 4)");
            Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
        }
    }

    [Test]
    public void Active_Execute_FireLineHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireRookActive active = (FireRookActive)fireRook.elementalPiece.active;
        Square target = builder.GetSquare(7, 4);

        active.Execute(fireRook, target, builder.BoardState, builder.SEM);

        // Default FireRookActiveParams.fireDuration = 2
        SquareEffect effect = builder.SEM.GetEffectAt(4, 4);
        Assert.IsNotNull(effect);
        Assert.AreEqual(2, effect.remainingTurns, "Fire line duration should match default of 2");
    }

    [Test]
    public void Active_Execute_CapturesFirstEnemyInLine()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        PieceMove enemyPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4);

        FireRookActive active = (FireRookActive)fireRook.elementalPiece.active;
        Square target = builder.GetSquare(7, 4);

        active.Execute(fireRook, target, builder.BoardState, builder.SEM);

        // The enemy pawn at (5,4) should be captured (removed from board state)
        PieceMove pieceAt5_4 = builder.BoardState.GetPieceAt(5, 4);
        Assert.IsNull(pieceAt5_4, "Enemy pawn at (5,4) should have been captured");
    }

    [Test]
    public void Active_Execute_DoesNotCaptureKings()
    {
        // Place kings such that black king is in the line of fire
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 6, 4);

        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireRookActive active = (FireRookActive)fireRook.elementalPiece.active;
        Square target = builder.GetSquare(7, 4);

        active.Execute(fireRook, target, builder.BoardState, builder.SEM);

        // The black king at (6,4) should NOT be captured
        PieceMove king = builder.BoardState.GetPieceAt(6, 4);
        Assert.IsNotNull(king, "King should not be captured by Inferno Line");
        Assert.AreEqual(ChessConstants.KING, king.piece);
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsEndOfLineInEachDirection()
    {
        PlaceKings();
        PieceMove fireRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireRookActive active = (FireRookActive)fireRook.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(fireRook, builder.BoardState);

        // Default lineLength=4, rook at (3,4)
        // Directions: (0,1) -> (3,8) OOB, last valid = min(4+4,7) -> but board max is 7
        // (0,1): (3,5),(3,6),(3,7) -> last = (3,7) at i=3 (since i=4 would be y=8 OOB)
        // (0,-1): (3,3),(3,2),(3,1),(3,0) -> last = (3,0) at i=4
        // (1,0): (4,4),(5,4),(6,4),(7,4) -> last = (7,4) at i=4
        // (-1,0): (2,4),(1,4),(0,4) -> last = (0,4) at i=3 (i=4 would be x=-1 OOB)
        Assert.AreEqual(4, targets.Count, "Should return one target per cardinal direction");
    }
}
