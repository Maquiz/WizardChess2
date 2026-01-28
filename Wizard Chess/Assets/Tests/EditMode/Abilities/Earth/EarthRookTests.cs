using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthRookTests
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

    // ========== EarthRookPassive (Fortified) ==========

    [Test]
    public void Fortified_BlocksCapture_WhenWhiteRookOnStartingSquare_A1()
    {
        PlaceKings();
        // White rook starting square at (0,7) = a1
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 0, 3);

        var passive = earthRook.elementalPiece.passive as EarthRookPassive;
        Assert.IsNotNull(passive);

        bool canCapture = passive.OnBeforeCapture(attacker, earthRook, builder.BoardState);
        Assert.IsFalse(canCapture, "Rook on starting square (0,7) should not be capturable");
    }

    [Test]
    public void Fortified_BlocksCapture_WhenWhiteRookOnStartingSquare_H1()
    {
        PlaceKings();
        // White rook starting square at (7,7) = h1
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 7, 7, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 7, 3);

        var passive = earthRook.elementalPiece.passive as EarthRookPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthRook, builder.BoardState);
        Assert.IsFalse(canCapture, "Rook on starting square (7,7) should not be capturable");
    }

    [Test]
    public void Fortified_BlocksCapture_WhenBlackRookOnStartingSquare_A8()
    {
        PlaceKings();
        // Black rook starting square at (0,0) = a8
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.BLACK, 0, 0, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 0, 3);

        var passive = earthRook.elementalPiece.passive as EarthRookPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthRook, builder.BoardState);
        Assert.IsFalse(canCapture, "Black rook on starting square (0,0) should not be capturable");
    }

    [Test]
    public void Fortified_BlocksCapture_WhenBlackRookOnStartingSquare_H8()
    {
        PlaceKings();
        // Black rook starting square at (7,0) = h8
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.BLACK, 7, 0, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 7, 3);

        var passive = earthRook.elementalPiece.passive as EarthRookPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthRook, builder.BoardState);
        Assert.IsFalse(canCapture, "Black rook on starting square (7,0) should not be capturable");
    }

    [Test]
    public void Fortified_AllowsCapture_WhenRookNotOnStartingSquare()
    {
        PlaceKings();
        // White rook moved to (3,4) - not a starting square
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 3, 2);

        var passive = earthRook.elementalPiece.passive as EarthRookPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthRook, builder.BoardState);
        Assert.IsTrue(canCapture, "Rook off starting square should be capturable");
    }

    [Test]
    public void Fortified_AllowsCapture_WhenNonEarthRookOnStartingSquare()
    {
        PlaceKings();
        // Non-earth rook on starting square
        var normalRook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 7);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 0, 3);

        var earthPassive = new EarthRookPassive();
        bool canCapture = earthPassive.OnBeforeCapture(attacker, normalRook, builder.BoardState);
        Assert.IsTrue(canCapture, "Non-earth rook should not benefit from Fortified passive");
    }

    // ========== EarthRookActive (Rampart) ==========

    [Test]
    public void Rampart_CreatesLineOfStoneWalls()
    {
        PlaceKings();
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthRook.elementalPiece.active as EarthRookActive;
        Assert.IsNotNull(active);

        // Target along +x direction: last empty square at distance up to maxWalls (default 3)
        // From (3,4) going +x: (4,4), (5,4), (6,4) are empty
        Square target = builder.GetSquare(6, 4); // last empty in +x direction within 3 squares
        active.Execute(earthRook, target, builder.BoardState, builder.SEM);

        // Should create walls at (4,4), (5,4), (6,4) - 3 walls in +x direction
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 4), "Wall should exist at (4,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 4), "Wall should exist at (5,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(6, 4), "Wall should exist at (6,4)");
    }

    [Test]
    public void Rampart_WallsHaveCorrectDefaultProperties()
    {
        PlaceKings();
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthRook.elementalPiece.active as EarthRookActive;
        Square target = builder.GetSquare(4, 4);
        active.Execute(earthRook, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(4, 4);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
        // Default wallHP = 2, wallDuration = 3 from EarthRookActiveParams
        Assert.AreEqual(2, effect.hitPoints, "Wall should have default HP of 2");
        Assert.AreEqual(3, effect.remainingTurns, "Wall should have default duration of 3");
    }

    [Test]
    public void Rampart_StopsAtOccupiedSquare()
    {
        PlaceKings();
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Place a blocking piece at (5,4) -- walls should stop before it
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4);

        var active = earthRook.elementalPiece.active as EarthRookActive;
        // Execute toward +x direction with target at (4,4) -- the only empty square before blocker
        Square target = builder.GetSquare(4, 4);
        active.Execute(earthRook, target, builder.BoardState, builder.SEM);

        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 4), "Wall should exist at (4,4)");
        Assert.IsNull(builder.SEM.GetEffectAt(5, 4), "No wall should be placed on occupied square (5,4)");
    }

    [Test]
    public void Rampart_MaxWallsDefault_Is3()
    {
        PlaceKings();
        // Rook at (0,4) with lots of empty space in +x direction
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 0, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthRook.elementalPiece.active as EarthRookActive;
        // Target at (3,4) -- 3 squares away in +x
        Square target = builder.GetSquare(3, 4);
        active.Execute(earthRook, target, builder.BoardState, builder.SEM);

        // Default maxWalls = 3, so only 3 walls should be created
        Assert.IsNotNull(builder.SEM.GetEffectAt(1, 4), "Wall at (1,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(2, 4), "Wall at (2,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 4), "Wall at (3,4)");
        Assert.IsNull(builder.SEM.GetEffectAt(4, 4), "No wall beyond maxWalls at (4,4)");
    }

    [Test]
    public void Rampart_GetTargetSquares_ReturnsLastEmptyInEachDirection()
    {
        PlaceKings();
        // Rook at center (3,3) with open space
        var earthRook = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_EARTH);

        var active = earthRook.elementalPiece.active as EarthRookActive;
        List<Square> targets = active.GetTargetSquares(earthRook, builder.BoardState);

        // Should have targets in all 4 cardinal directions (since board is open)
        Assert.IsTrue(targets.Count > 0, "Should have at least one target direction");
        Assert.IsTrue(targets.Count <= 4, "Should have at most 4 target directions (cardinal)");
    }
}
