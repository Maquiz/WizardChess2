using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class FirePawnTests
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

    // ========== FirePawnPassive (Scorched Earth) ==========

    [Test]
    public void Passive_OnPieceCaptured_CreatesFireOnCapturedSquare()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        PieceMove attacker = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 3);

        // Simulate the pawn being captured: trigger OnPieceCaptured
        FirePawnPassive passive = (FirePawnPassive)firePawn.elementalPiece.passive;
        passive.OnPieceCaptured(firePawn, attacker, builder.BoardState);

        // Fire should be created on the captured pawn's square (3,4)
        SquareEffect effect = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(effect, "Fire effect should be created on captured pawn's square");
        Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
    }

    [Test]
    public void Passive_OnPieceCaptured_FireHasCorrectDuration()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        PieceMove attacker = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 3);

        FirePawnPassive passive = (FirePawnPassive)firePawn.elementalPiece.passive;
        passive.OnPieceCaptured(firePawn, attacker, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(effect);
        Assert.IsTrue(effect.remainingTurns > 0, "Fire should have positive duration");
    }

    [Test]
    public void Passive_OnPieceCaptured_FireOwnerMatchesCapturedPieceColor()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        PieceMove attacker = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 3);

        FirePawnPassive passive = (FirePawnPassive)firePawn.elementalPiece.passive;
        passive.OnPieceCaptured(firePawn, attacker, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(effect);
        Assert.AreEqual(ChessConstants.WHITE, effect.ownerColor, "Fire owner should be the captured pawn's color");
    }

    // ========== FirePawnActive (Flame Rush) ==========

    [Test]
    public void Active_GetTargetSquares_ReturnsForwardEmptySquares_White()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);

        FirePawnActive active = (FirePawnActive)firePawn.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(firePawn, builder.BoardState);

        // White pawn at (3,5) moves -y direction: (3,4), (3,3), (3,2)
        // Default maxForwardRange = 3
        Assert.AreEqual(3, targets.Count, "Should find 3 forward empty squares");
        Assert.AreEqual(3, targets[0].x);
        Assert.AreEqual(4, targets[0].y);
        Assert.AreEqual(3, targets[1].x);
        Assert.AreEqual(3, targets[1].y);
        Assert.AreEqual(3, targets[2].x);
        Assert.AreEqual(2, targets[2].y);
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsForwardEmptySquares_Black()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2, ChessConstants.ELEMENT_FIRE);

        FirePawnActive active = (FirePawnActive)firePawn.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(firePawn, builder.BoardState);

        // Black pawn at (3,2) moves +y direction: (3,3), (3,4), (3,5)
        Assert.AreEqual(3, targets.Count, "Should find 3 forward empty squares");
        Assert.AreEqual(3, targets[0].x);
        Assert.AreEqual(3, targets[0].y);
        Assert.AreEqual(3, targets[1].x);
        Assert.AreEqual(4, targets[1].y);
        Assert.AreEqual(3, targets[2].x);
        Assert.AreEqual(5, targets[2].y);
    }

    [Test]
    public void Active_GetTargetSquares_SkipsOccupiedSquares()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);
        // Place blocking piece at (3,3) -- two squares ahead
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        FirePawnActive active = (FirePawnActive)firePawn.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(firePawn, builder.BoardState);

        // Flame Rush ignores blocking pieces: (3,4) empty, (3,3) occupied (skip), (3,2) empty
        Assert.AreEqual(2, targets.Count, "Should find 2 empty squares, skipping the occupied one");
        Assert.AreEqual(4, targets[0].y);
        Assert.AreEqual(2, targets[1].y);
    }

    [Test]
    public void Active_Execute_CreatesFireTrailOnTraversedSquares()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);

        FirePawnActive active = (FirePawnActive)firePawn.elementalPiece.active;
        // Target square is (3,2) -- 3 squares ahead for white (-y)
        Square target = builder.GetSquare(3, 2);

        active.Execute(firePawn, target, builder.BoardState, builder.SEM);

        // Fire trail should be on traversed squares (3,4) and (3,3) -- NOT on (3,2) which is landing square
        SquareEffect fire4 = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(fire4, "Fire should be created on traversed square (3,4)");
        Assert.AreEqual(SquareEffectType.Fire, fire4.effectType);

        SquareEffect fire3 = builder.SEM.GetEffectAt(3, 3);
        Assert.IsNotNull(fire3, "Fire should be created on traversed square (3,3)");
        Assert.AreEqual(SquareEffectType.Fire, fire3.effectType);

        // Default fireTrailDuration = 2
        Assert.AreEqual(2, fire4.remainingTurns);
        Assert.AreEqual(2, fire3.remainingTurns);
    }

    [Test]
    public void Active_Execute_NoFireTrailWhenMovingOneSquare()
    {
        PlaceKings();
        PieceMove firePawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);

        FirePawnActive active = (FirePawnActive)firePawn.elementalPiece.active;
        // Target square is (3,4) -- 1 square ahead, no traversed squares
        Square target = builder.GetSquare(3, 4);

        active.Execute(firePawn, target, builder.BoardState, builder.SEM);

        // No fire trail when distance is 1 (loop runs for i=1 to i<1, so 0 iterations)
        // The landing square (3,4) should have no fire trail on it
        // (fire trail is only on passed-through squares, not destination)
        List<SquareEffect> fires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(0, fires.Count, "No fire trail when moving only 1 square");
    }
}
