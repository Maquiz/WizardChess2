using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthBishopTests
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

    // ========== EarthBishopPassive (Earthen Shield) ==========

    [Test]
    public void EarthenShield_StunsCaptor_WhenBishopIsCaptured()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        var passive = earthBishop.elementalPiece.passive as EarthBishopPassive;
        Assert.IsNotNull(passive);

        // Trigger OnPieceCaptured (bishop was captured, attacker is the rook)
        passive.OnPieceCaptured(earthBishop, attacker, builder.BoardState);

        Assert.IsNotNull(attacker.elementalPiece, "Captor should have elementalPiece after Earthen Shield triggers");
        Assert.IsTrue(attacker.elementalPiece.IsStunned(), "Captor should be stunned after capturing earth bishop");
    }

    [Test]
    public void EarthenShield_StunDuration_DefaultIs1()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        var passive = earthBishop.elementalPiece.passive as EarthBishopPassive;
        passive.OnPieceCaptured(earthBishop, attacker, builder.BoardState);

        Assert.IsTrue(attacker.elementalPiece.IsStunned());

        // Default stunDuration = 1 from EarthBishopPassiveParams
        attacker.elementalPiece.TickStatusEffects();
        Assert.IsFalse(attacker.elementalPiece.IsStunned(), "Stun should expire after 1 tick (default duration 1)");
    }

    [Test]
    public void EarthenShield_StunsCaptor_WhoAlreadyHasElementalPiece()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Attacker already has an elementalPiece
        var attacker = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 3, 2, ChessConstants.ELEMENT_FIRE);

        var passive = earthBishop.elementalPiece.passive as EarthBishopPassive;
        passive.OnPieceCaptured(earthBishop, attacker, builder.BoardState);

        Assert.IsTrue(attacker.elementalPiece.IsStunned(),
            "Captor with existing elementalPiece should still be stunned");
    }

    [Test]
    public void EarthenShield_OnBeforeCapture_AlwaysReturnsTrue()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var attacker = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.BLACK, 3, 2);

        var passive = earthBishop.elementalPiece.passive as EarthBishopPassive;
        bool canCapture = passive.OnBeforeCapture(attacker, earthBishop, builder.BoardState);
        Assert.IsTrue(canCapture, "Earthen Shield should not prevent capture (it retaliates after)");
    }

    // ========== EarthBishopActive (Petrify) ==========

    [Test]
    public void Petrify_StunsTargetEnemy()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Place enemy on diagonal at (5,2)
        var enemy = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 2);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        Assert.IsNotNull(active);

        Square target = builder.GetSquare(5, 2);
        bool result = active.Execute(earthBishop, target, builder.BoardState, builder.SEM);
        Assert.IsTrue(result);

        Assert.IsNotNull(enemy.elementalPiece, "Enemy should have elementalPiece after petrify");
        Assert.IsTrue(enemy.elementalPiece.IsStunned(), "Target enemy should be stunned by Petrify");
    }

    [Test]
    public void Petrify_CreatesStoneWallOnTargetSquare()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var enemy = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 2);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        Square target = builder.GetSquare(5, 2);
        active.Execute(earthBishop, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(5, 2);
        Assert.IsNotNull(effect, "Stone wall should be created on petrified enemy's square");
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
    }

    [Test]
    public void Petrify_WallHasCorrectDefaultProperties()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var enemy = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 2);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        Square target = builder.GetSquare(5, 2);
        active.Execute(earthBishop, target, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(5, 2);
        // Default wallHP = 1, wallDuration = 2 from EarthBishopActiveParams
        Assert.AreEqual(1, effect.hitPoints, "Petrify wall should have default HP of 1");
        Assert.AreEqual(2, effect.remainingTurns, "Petrify wall should have default duration of 2");
    }

    [Test]
    public void Petrify_StunDuration_DefaultIs2()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var enemy = builder.PlaceElementalPiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 2, ChessConstants.ELEMENT_EARTH);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        Square target = builder.GetSquare(5, 2);
        active.Execute(earthBishop, target, builder.BoardState, builder.SEM);

        Assert.IsTrue(enemy.elementalPiece.IsStunned());

        // Default stunDuration = 2
        enemy.elementalPiece.TickStatusEffects(); // tick 1: 2->1
        Assert.IsTrue(enemy.elementalPiece.IsStunned(), "Stun should still be active after 1 tick (duration 2)");

        enemy.elementalPiece.TickStatusEffects(); // tick 2: 1->0
        Assert.IsFalse(enemy.elementalPiece.IsStunned(), "Stun should expire after 2 ticks");
    }

    [Test]
    public void Petrify_ReturnsFalse_WhenNoTargetPieceAtSquare()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        // Target an empty square
        Square emptyTarget = builder.GetSquare(5, 2);
        bool result = active.Execute(earthBishop, emptyTarget, builder.BoardState, builder.SEM);
        Assert.IsFalse(result, "Petrify should return false when no piece at target square");
    }

    [Test]
    public void Petrify_GetTargetSquares_FindsEnemiesOnDiagonals()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Place enemy on a diagonal line
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 5, 2);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        List<Square> targets = active.GetTargetSquares(earthBishop, builder.BoardState);

        bool found = false;
        foreach (var t in targets)
        {
            if (t.x == 5 && t.y == 2) { found = true; break; }
        }
        Assert.IsTrue(found, "Petrify should target enemy on diagonal at (5,2)");
    }

    [Test]
    public void Petrify_DoesNotTargetKing()
    {
        // Kings are already placed -- black king at (4,0)
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // Bishop at (3,1) can see black king at (4,0) diagonally
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 1, ChessConstants.ELEMENT_EARTH);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        List<Square> targets = active.GetTargetSquares(earthBishop, builder.BoardState);

        foreach (var t in targets)
        {
            Assert.IsFalse(t.x == 4 && t.y == 0, "Petrify should not target the king");
        }
    }

    [Test]
    public void Petrify_DoesNotTargetFriendlyPieces()
    {
        PlaceKings();
        var earthBishop = builder.PlaceElementalPiece(ChessConstants.BISHOP, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        // Place friendly on diagonal
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);

        var active = earthBishop.elementalPiece.active as EarthBishopActive;
        List<Square> targets = active.GetTargetSquares(earthBishop, builder.BoardState);

        foreach (var t in targets)
        {
            Assert.IsFalse(t.x == 4 && t.y == 3, "Petrify should not target friendly pieces");
        }
    }
}
