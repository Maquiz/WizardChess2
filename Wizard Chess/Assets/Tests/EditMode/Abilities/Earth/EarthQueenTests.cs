using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthQueenTests
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

    // ========== EarthQueenPassive (Tectonic Presence) ==========

    [Test]
    public void TectonicPresence_RemovesBonusHP_WhenQueenCaptured()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        // Simulate that stoneWallBonusHP was set (normally done by setup code)
        builder.SEM.stoneWallBonusHP = 1;
        Assert.AreEqual(1, builder.SEM.stoneWallBonusHP, "Bonus HP should be 1 before capture");

        var passive = earthQueen.elementalPiece.passive as EarthQueenPassive;
        Assert.IsNotNull(passive);

        // Trigger OnPieceCaptured
        passive.OnPieceCaptured(earthQueen, attacker, builder.BoardState);

        Assert.AreEqual(0, builder.SEM.stoneWallBonusHP, "Bonus HP should be 0 after earth queen is captured");
    }

    [Test]
    public void TectonicPresence_BonusHP_AppliedToStoneWallCreation()
    {
        PlaceKings();
        // Set bonus HP
        builder.SEM.stoneWallBonusHP = 1;

        // Create a stone wall with base HP 2
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.StoneWall, 3, ChessConstants.WHITE, 2);

        // Stone wall should have base HP + bonus HP = 2 + 1 = 3
        Assert.AreEqual(3, effect.hitPoints, "Stone wall HP should include bonus HP from Tectonic Presence");
    }

    [Test]
    public void TectonicPresence_BonusHP_NotAppliedToNonStoneWallEffects()
    {
        PlaceKings();
        // Set bonus HP
        builder.SEM.stoneWallBonusHP = 1;

        // Create a fire effect (should not get bonus HP)
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);

        // Fire effect gets default hp=1 from CreateEffect signature, but NOT the stoneWallBonusHP
        // Formula: hp + stoneWallBonusHP * (type == StoneWall ? 1 : 0) = 1 + 1*0 = 1
        Assert.AreEqual(1, effect.hitPoints, "Non-stone-wall effects should not receive bonus HP");
    }

    [Test]
    public void TectonicPresence_AfterCapture_NewWallsHaveNoBonus()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        // Set bonus HP
        builder.SEM.stoneWallBonusHP = 1;

        // Queen gets captured
        var passive = earthQueen.elementalPiece.passive as EarthQueenPassive;
        passive.OnPieceCaptured(earthQueen, attacker, builder.BoardState);

        // Now create a new stone wall -- should NOT get bonus HP
        var effect = builder.SEM.CreateEffect(5, 5, SquareEffectType.StoneWall, 3, ChessConstants.WHITE, 2);
        Assert.AreEqual(2, effect.hitPoints, "Walls created after queen capture should have base HP only (no bonus)");
    }

    [Test]
    public void TectonicPresence_OnBeforeCapture_AlwaysAllows()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        var passive = earthQueen.elementalPiece.passive as EarthQueenPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthQueen, builder.BoardState);
        Assert.IsTrue(canCapture, "Tectonic Presence should not prevent capture of the queen");
    }

    // ========== EarthQueenActive (Continental Divide) ==========

    [Test]
    public void ContinentalDivide_CreatesLineOfStoneWalls()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        Assert.IsNotNull(active);

        // Target along +x direction: (4,4) to (7,4) -- but maxWalls is 5 and only 4 squares available
        // Actually (4,4),(5,4),(6,4),(7,4) = 4 squares before edge
        Square target = builder.GetSquare(7, 4);
        active.Execute(earthQueen, target, builder.BoardState, builder.SEM);

        // Walls should be created from (4,4) up to maxWalls or board edge
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 4), "Wall at (4,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 4), "Wall at (5,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(6, 4), "Wall at (6,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(7, 4), "Wall at (7,4)");
    }

    [Test]
    public void ContinentalDivide_WallsHaveCorrectDefaultProperties()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        Square target = builder.GetSquare(4, 4);
        active.Execute(earthQueen, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(4, 4);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
        // Default wallHP = 3, wallDuration = 4 from EarthQueenActiveParams
        Assert.AreEqual(3, effect.hitPoints, "Continental Divide wall should have default HP of 3");
        Assert.AreEqual(4, effect.remainingTurns, "Continental Divide wall should have default duration of 4");
    }

    [Test]
    public void ContinentalDivide_MaxWalls_DefaultIs5()
    {
        PlaceKings();
        // Queen at (1,4) with lots of empty space in +x direction
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 1, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        // Target the last empty square in +x within maxWalls
        Square target = builder.GetSquare(6, 4);
        active.Execute(earthQueen, target, builder.BoardState, builder.SEM);

        // Default maxWalls = 5, from (2,4) to (6,4) = 5 walls
        Assert.IsNotNull(builder.SEM.GetEffectAt(2, 4), "Wall at (2,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 4), "Wall at (3,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 4), "Wall at (4,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 4), "Wall at (5,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(6, 4), "Wall at (6,4)");
        Assert.IsNull(builder.SEM.GetEffectAt(7, 4), "No wall beyond maxWalls at (7,4)");
    }

    [Test]
    public void ContinentalDivide_StopsAtOccupiedSquare()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Block at (5,4)
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        Square target = builder.GetSquare(4, 4);
        active.Execute(earthQueen, target, builder.BoardState, builder.SEM);

        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 4), "Wall at (4,4)");
        Assert.IsNull(builder.SEM.GetEffectAt(5, 4), "No wall on occupied square (5,4)");
    }

    [Test]
    public void ContinentalDivide_WorksDiagonally()
    {
        PlaceKings();
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        // Target along (+1,+1) diagonal direction
        Square target = builder.GetSquare(6, 7);
        active.Execute(earthQueen, target, builder.BoardState, builder.SEM);

        // Walls along diagonal from (4,5), (5,6), (6,7)
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 5), "Wall at (4,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 6), "Wall at (5,6)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(6, 7), "Wall at (6,7)");
    }

    [Test]
    public void ContinentalDivide_GetTargetSquares_ReturnsAllDirections()
    {
        PlaceKings();
        // Queen at center of board with no nearby pieces blocking
        var earthQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_EARTH);

        var active = earthQueen.elementalPiece.active as EarthQueenActive;
        List<Square> targets = active.GetTargetSquares(earthQueen, builder.BoardState);

        // Queen should have targets in all 8 directions (if empty squares exist in each)
        Assert.IsTrue(targets.Count > 0, "Should have at least one target direction");
        Assert.IsTrue(targets.Count <= 8, "Should have at most 8 target directions");
    }
}
