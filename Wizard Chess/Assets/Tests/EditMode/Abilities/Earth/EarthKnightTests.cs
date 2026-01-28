using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthKnightTests
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

    // ========== EarthKnightPassive (Tremor Hop) ==========

    [Test]
    public void TremorHop_StunsAdjacentEnemy_AfterMove()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        // Place an enemy adjacent to the landing square (4,4)
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4, ChessConstants.ELEMENT_EARTH);

        // Simulate OnAfterMove: knight moves to (4,4)
        var passive = earthKnight.elementalPiece.passive as EarthKnightPassive;
        Assert.IsNotNull(passive);

        // Move knight state first so bs.GetPieceAt works correctly for adjacency
        builder.MovePieceState(earthKnight, 4, 4);
        passive.OnAfterMove(earthKnight, 3, 4, 4, 4, builder.BoardState);

        Assert.IsTrue(enemy.elementalPiece.IsStunned(), "Adjacent enemy should be stunned after Tremor Hop");
    }

    [Test]
    public void TremorHop_DoesNotStunFriendlyPieces()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        // Place a friendly piece adjacent to landing square (4,4)
        var friendly = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4, ChessConstants.ELEMENT_EARTH);

        builder.MovePieceState(earthKnight, 4, 4);
        var passive = earthKnight.elementalPiece.passive as EarthKnightPassive;
        passive.OnAfterMove(earthKnight, 3, 4, 4, 4, builder.BoardState);

        Assert.IsFalse(friendly.elementalPiece.IsStunned(), "Friendly piece should not be stunned by Tremor Hop");
    }

    [Test]
    public void TremorHop_MaxTargetsLimit_DefaultIs1()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        // Place multiple enemies adjacent to landing square (4,4)
        // KingDirections: (0,-1),(0,1),(1,0),(1,1),(1,-1),(-1,0),(-1,1),(-1,-1)
        var enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3);  // (0,-1) from (4,4)
        var enemy2 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 5);  // (0,+1) from (4,4)
        var enemy3 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4);  // (+1,0) from (4,4)

        builder.MovePieceState(earthKnight, 4, 4);
        var passive = earthKnight.elementalPiece.passive as EarthKnightPassive;
        passive.OnAfterMove(earthKnight, 3, 4, 4, 4, builder.BoardState);

        // Default maxTargets = 1, so only 1 enemy should be stunned
        int stunnedCount = 0;
        if (enemy1.elementalPiece != null && enemy1.elementalPiece.IsStunned()) stunnedCount++;
        if (enemy2.elementalPiece != null && enemy2.elementalPiece.IsStunned()) stunnedCount++;
        if (enemy3.elementalPiece != null && enemy3.elementalPiece.IsStunned()) stunnedCount++;

        Assert.AreEqual(1, stunnedCount, "Only 1 enemy should be stunned (maxTargets default = 1)");
    }

    [Test]
    public void TremorHop_StunDuration_DefaultIs1()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);
        var enemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4, ChessConstants.ELEMENT_EARTH);

        builder.MovePieceState(earthKnight, 4, 4);
        var passive = earthKnight.elementalPiece.passive as EarthKnightPassive;
        passive.OnAfterMove(earthKnight, 3, 4, 4, 4, builder.BoardState);

        Assert.IsTrue(enemy.elementalPiece.IsStunned());

        // Default stunDuration = 1, so after one tick it should expire
        enemy.elementalPiece.TickStatusEffects();
        Assert.IsFalse(enemy.elementalPiece.IsStunned(), "Stun with default duration 1 should expire after 1 tick");
    }

    [Test]
    public void TremorHop_DoesNotStunNonAdjacentEnemies()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_EARTH);

        // Place an enemy 2 squares away from landing (4,4)
        var farEnemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 6, 4);

        builder.MovePieceState(earthKnight, 4, 4);
        var passive = earthKnight.elementalPiece.passive as EarthKnightPassive;
        passive.OnAfterMove(earthKnight, 3, 4, 4, 4, builder.BoardState);

        // farEnemy should not have elementalPiece added (no stun)
        Assert.IsTrue(farEnemy.elementalPiece == null || !farEnemy.elementalPiece.IsStunned(),
            "Enemy 2 squares away should not be stunned by Tremor Hop");
    }

    // ========== EarthKnightActive (Earthquake) ==========

    [Test]
    public void Earthquake_StunsEnemiesWithinRange()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);

        // Place enemy at Manhattan distance 2 from (4,4): (4,2) -> |0|+|2|=2
        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 2);

        var active = earthKnight.elementalPiece.active as EarthKnightActive;
        Assert.IsNotNull(active);

        Square target = earthKnight.curSquare;
        active.Execute(earthKnight, target, builder.BoardState, builder.SEM);

        Assert.IsNotNull(enemy.elementalPiece, "Enemy should have elementalPiece added by Earthquake");
        Assert.IsTrue(enemy.elementalPiece.IsStunned(), "Enemy within Manhattan distance 2 should be stunned");
    }

    [Test]
    public void Earthquake_DoesNotStunFriendlies()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);

        // Place a friendly within range
        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 3);

        var active = earthKnight.elementalPiece.active as EarthKnightActive;
        active.Execute(earthKnight, earthKnight.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(friendly.elementalPiece == null || !friendly.elementalPiece.IsStunned(),
            "Friendly piece should not be stunned by Earthquake");
    }

    [Test]
    public void Earthquake_DoesNotStunEnemiesBeyondRange()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);

        // Place enemy at Manhattan distance 3: (4,1) -> |0|+|3|=3 (default range is 2)
        var farEnemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 1);

        var active = earthKnight.elementalPiece.active as EarthKnightActive;
        active.Execute(earthKnight, earthKnight.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(farEnemy.elementalPiece == null || !farEnemy.elementalPiece.IsStunned(),
            "Enemy beyond Manhattan distance 2 should not be stunned");
    }

    [Test]
    public void Earthquake_StunsMultipleEnemiesInRange()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);

        // Place two enemies within range
        var enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3); // distance 1
        var enemy2 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 3); // distance 2

        var active = earthKnight.elementalPiece.active as EarthKnightActive;
        active.Execute(earthKnight, earthKnight.curSquare, builder.BoardState, builder.SEM);

        Assert.IsNotNull(enemy1.elementalPiece);
        Assert.IsTrue(enemy1.elementalPiece.IsStunned(), "First enemy should be stunned");
        Assert.IsNotNull(enemy2.elementalPiece);
        Assert.IsTrue(enemy2.elementalPiece.IsStunned(), "Second enemy should be stunned");
    }

    [Test]
    public void Earthquake_CanAlwaysActivate()
    {
        PlaceKings();
        var earthKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);

        var active = earthKnight.elementalPiece.active as EarthKnightActive;
        Assert.IsTrue(active.CanActivate(earthKnight, builder.BoardState, builder.SEM),
            "Earthquake should always be activatable");
    }
}
