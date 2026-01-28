using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class FireBishopTests
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

    // ========== FireBishopPassive (Burning Path) ==========

    [Test]
    public void Passive_OnAfterMove_CreatesFireOnFirstTraversedSquare()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 5, ChessConstants.ELEMENT_FIRE);

        FireBishopPassive passive = (FireBishopPassive)fireBishop.elementalPiece.passive;
        // Simulate moving from (2,5) to (5,2) -- diagonal: dx=+1, dy=-1
        passive.OnAfterMove(fireBishop, 2, 5, 5, 2, builder.BoardState);

        // First traversed square: fromX+dx, fromY+dy = (3, 4)
        SquareEffect effect = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(effect, "Fire should be created on first traversed square (3,4)");
        Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
    }

    [Test]
    public void Passive_OnAfterMove_FireHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 5, ChessConstants.ELEMENT_FIRE);

        FireBishopPassive passive = (FireBishopPassive)fireBishop.elementalPiece.passive;
        passive.OnAfterMove(fireBishop, 2, 5, 5, 2, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNotNull(effect);
        // Default FireBishopPassiveParams.fireDuration = 1
        Assert.AreEqual(1, effect.remainingTurns, "Fire duration should match default of 1");
    }

    [Test]
    public void Passive_OnAfterMove_OnlyCreatesOneFireSquare()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 1, 6, ChessConstants.ELEMENT_FIRE);

        FireBishopPassive passive = (FireBishopPassive)fireBishop.elementalPiece.passive;
        // Move from (1,6) to (4,3) -- 3 squares diagonally
        passive.OnAfterMove(fireBishop, 1, 6, 4, 3, builder.BoardState);

        // Only the first traversed square should have fire: (2,5)
        SquareEffect firstFire = builder.SEM.GetEffectAt(2, 5);
        Assert.IsNotNull(firstFire, "First traversed square (2,5) should have fire");

        // Other traversed squares should NOT have fire
        SquareEffect secondSquare = builder.SEM.GetEffectAt(3, 4);
        Assert.IsNull(secondSquare, "Second traversed square (3,4) should NOT have fire");
    }

    [Test]
    public void Passive_OnAfterMove_DifferentDiagonalDirections()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 5, 5, ChessConstants.ELEMENT_FIRE);

        FireBishopPassive passive = (FireBishopPassive)fireBishop.elementalPiece.passive;
        // Move from (5,5) to (3,3) -- dx=-1, dy=-1
        passive.OnAfterMove(fireBishop, 5, 5, 3, 3, builder.BoardState);

        // First traversed: (4,4)
        SquareEffect effect = builder.SEM.GetEffectAt(4, 4);
        Assert.IsNotNull(effect, "Fire should be on first traversed square (4,4) in (-1,-1) direction");
    }

    // ========== FireBishopActive (Flame Cross) ==========

    [Test]
    public void Active_Execute_CreatesFireInPlusPattern()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireBishopActive active = (FireBishopActive)fireBishop.elementalPiece.active;
        active.Execute(fireBishop, fireBishop.curSquare, builder.BoardState, builder.SEM);

        // Default armLength=2, so + pattern extends 2 squares in each cardinal direction
        // RookDirections: (0,1), (0,-1), (1,0), (-1,0)
        // From (3,4): (3,5),(3,6), (3,3),(3,2), (4,4),(5,4), (2,4),(1,4)
        int[][] expectedSquares = new int[][]
        {
            new int[] {3, 5}, new int[] {3, 6},  // up (+y)
            new int[] {3, 3}, new int[] {3, 2},  // down (-y)
            new int[] {4, 4}, new int[] {5, 4},  // right (+x)
            new int[] {2, 4}, new int[] {1, 4},  // left (-x)
        };

        foreach (var sq in expectedSquares)
        {
            SquareEffect effect = builder.SEM.GetEffectAt(sq[0], sq[1]);
            Assert.IsNotNull(effect, $"Fire should be at ({sq[0]},{sq[1]})");
            Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
        }
    }

    [Test]
    public void Active_Execute_FireCrossHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireBishopActive active = (FireBishopActive)fireBishop.elementalPiece.active;
        active.Execute(fireBishop, fireBishop.curSquare, builder.BoardState, builder.SEM);

        // Default FireBishopActiveParams.fireDuration = 2
        SquareEffect effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect);
        Assert.AreEqual(2, effect.remainingTurns, "Fire cross duration should match default of 2");
    }

    [Test]
    public void Active_Execute_GrantsFireImmunityToBishop()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireBishopActive active = (FireBishopActive)fireBishop.elementalPiece.active;
        active.Execute(fireBishop, fireBishop.curSquare, builder.BoardState, builder.SEM);

        // Default grantFireImmunity = true
        Assert.IsTrue(fireBishop.elementalPiece.IsImmuneToEffect(SquareEffectType.Fire),
            "Bishop should be immune to fire after using Flame Cross");
    }

    [Test]
    public void Active_Execute_DoesNotCreateFireOutOfBounds()
    {
        PlaceKings();
        // Place bishop near edge
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 0, 4, ChessConstants.ELEMENT_FIRE);

        FireBishopActive active = (FireBishopActive)fireBishop.elementalPiece.active;
        active.Execute(fireBishop, fireBishop.curSquare, builder.BoardState, builder.SEM);

        // From (0,4) with armLength=2:
        // (0,5),(0,6) -- valid
        // (0,3),(0,2) -- valid
        // (1,4),(2,4) -- valid
        // (-1,4),(-2,4) -- OUT OF BOUNDS
        List<SquareEffect> fires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(6, fires.Count, "Should create 6 fire squares (2 OOB in -x direction)");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsSelfSquare()
    {
        PlaceKings();
        PieceMove fireBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireBishopActive active = (FireBishopActive)fireBishop.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(fireBishop, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Flame Cross targets self only");
        Assert.AreEqual(fireBishop.curSquare, targets[0]);
    }
}
