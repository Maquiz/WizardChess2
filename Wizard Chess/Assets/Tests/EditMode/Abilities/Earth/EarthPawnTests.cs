using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthPawnTests
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

    // ========== EarthPawnPassive (Shield Wall) ==========

    [Test]
    public void ShieldWall_BlocksHighValueCapture_WhenAdjacentFriendlyExists()
    {
        PlaceKings();
        // Place earth pawn at (3,4) with a friendly piece adjacent at (4,4)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);

        // Attack with a rook (value 5 > pawn value 1)
        var attackingRook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 3);

        var passive = earthPawn.elementalPiece.passive as EarthPawnPassive;
        Assert.IsNotNull(passive);

        bool canCapture = passive.OnBeforeCapture(attackingRook, earthPawn, builder.BoardState);
        Assert.IsFalse(canCapture, "High-value attacker should be blocked when earth pawn has adjacent friendly");
    }

    [Test]
    public void ShieldWall_AllowsPawnCapture_EvenWithAdjacentFriendly()
    {
        PlaceKings();
        // Place earth pawn at (3,4) with a friendly piece adjacent at (4,4)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);

        // Attack with another pawn (value 1 <= pawn value 1)
        var attackingPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 3);

        var passive = earthPawn.elementalPiece.passive as EarthPawnPassive;
        bool canCapture = passive.OnBeforeCapture(attackingPawn, earthPawn, builder.BoardState);
        Assert.IsTrue(canCapture, "Pawns should still be able to capture earth pawn even with adjacent friendly");
    }

    [Test]
    public void ShieldWall_AllowsCapture_WhenNoAdjacentFriendly()
    {
        PlaceKings();
        // Place earth pawn at (3,4) with no friendly pieces adjacent
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        // Attack with a queen (value 9 > pawn value 1)
        var attackingQueen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 3, 3);

        var passive = earthPawn.elementalPiece.passive as EarthPawnPassive;
        bool canCapture = passive.OnBeforeCapture(attackingQueen, earthPawn, builder.BoardState);
        Assert.IsTrue(canCapture, "High-value attacker should be allowed when earth pawn has no adjacent friendly");
    }

    [Test]
    public void ShieldWall_ChecksOrthogonalOnly_DiagonalFriendlyDoesNotProtect()
    {
        PlaceKings();
        // Place earth pawn at (3,4) with a friendly piece only at diagonal (4,5)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 5); // diagonal only

        // Attack with a rook (value 5 > pawn value 1)
        var attackingRook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 3);

        var passive = earthPawn.elementalPiece.passive as EarthPawnPassive;
        bool canCapture = passive.OnBeforeCapture(attackingRook, earthPawn, builder.BoardState);
        Assert.IsTrue(canCapture, "Diagonal friendly should not trigger shield wall (only orthogonal)");
    }

    [Test]
    public void ShieldWall_BlocksKnightCapture_WhenAdjacentFriendly()
    {
        PlaceKings();
        // Place earth pawn at (3,4) with adjacent friendly at (2,4)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.BISHOP, ChessConstants.WHITE, 2, 4);

        // Attack with a knight (value 3 > pawn value 1)
        var attackingKnight = builder.PlacePiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 2, 2);

        var passive = earthPawn.elementalPiece.passive as EarthPawnPassive;
        bool canCapture = passive.OnBeforeCapture(attackingKnight, earthPawn, builder.BoardState);
        Assert.IsFalse(canCapture, "Knight (value 3) should be blocked when earth pawn has adjacent friendly");
    }

    // ========== EarthPawnActive (Barricade) ==========

    [Test]
    public void Barricade_CreatesStoneWall_InFrontOfWhitePawn()
    {
        PlaceKings();
        // White pawn at (3,6) - moves in -y direction, so "in front" is (3,5)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Assert.IsNotNull(active);

        Square target = builder.GetSquare(3, 5);
        bool result = active.Execute(earthPawn, target, builder.BoardState, builder.SEM);
        Assert.IsTrue(result);

        var effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect, "Stone wall should be created at (3,5)");
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
    }

    [Test]
    public void Barricade_WallHasCorrectDefaultHP()
    {
        PlaceKings();
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Square target = builder.GetSquare(3, 5);
        active.Execute(earthPawn, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(3, 5);
        // Default wallHP = 2 from EarthPawnActiveParams
        Assert.AreEqual(2, effect.hitPoints, "Stone wall should have default HP of 2");
    }

    [Test]
    public void Barricade_WallHasCorrectDefaultDuration()
    {
        PlaceKings();
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Square target = builder.GetSquare(3, 5);
        active.Execute(earthPawn, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(3, 5);
        // Default wallDuration = 3 from EarthPawnActiveParams
        Assert.AreEqual(3, effect.remainingTurns, "Stone wall should have default duration of 3 turns");
    }

    [Test]
    public void Barricade_CanActivate_WhenSquareInFrontIsEmpty()
    {
        PlaceKings();
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Assert.IsTrue(active.CanActivate(earthPawn, builder.BoardState, builder.SEM));
    }

    [Test]
    public void Barricade_CannotActivate_WhenSquareInFrontIsOccupied()
    {
        PlaceKings();
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 5); // block the front

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Assert.IsFalse(active.CanActivate(earthPawn, builder.BoardState, builder.SEM));
    }

    [Test]
    public void Barricade_BlackPawn_CreatesWallInPlusYDirection()
    {
        PlaceKings();
        // Black pawn at (3,1) - moves in +y direction, so "in front" is (3,2)
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 1, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Square target = builder.GetSquare(3, 2);
        active.Execute(earthPawn, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(3, 2);
        Assert.IsNotNull(effect, "Stone wall should be created at (3,2) for black pawn");
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
    }

    [Test]
    public void Barricade_WallOwnerMatchesPawnColor()
    {
        PlaceKings();
        var earthPawn = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);

        var active = earthPawn.elementalPiece.active as EarthPawnActive;
        Square target = builder.GetSquare(3, 5);
        active.Execute(earthPawn, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(3, 5);
        Assert.AreEqual(ChessConstants.WHITE, effect.ownerColor, "Stone wall owner should match pawn color");
    }
}
