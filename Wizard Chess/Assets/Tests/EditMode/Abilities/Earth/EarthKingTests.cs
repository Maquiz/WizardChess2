using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class EarthKingTests
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

    // ========== EarthKingPassive (Bedrock Throne) ==========

    [Test]
    public void BedrockThrone_WhiteKingOnStartingSquare_NotInCheck()
    {
        // White earth king on starting square e1 = (4,7)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place an enemy rook directly attacking the king's square
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.WHITE);
        Assert.IsFalse(inCheck, "Earth king on starting square should not be in check (Bedrock Throne)");
    }

    [Test]
    public void BedrockThrone_WhiteKingOffStartingSquare_CanBeInCheck()
    {
        // White earth king moved to (3,6) -- NOT starting square
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place an enemy rook attacking king at (3,6)
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 2);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.WHITE);
        Assert.IsTrue(inCheck, "Earth king off starting square should be in check normally");
    }

    [Test]
    public void BedrockThrone_BlackKingOnStartingSquare_NotInCheck()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        // Black earth king on starting square e8 = (4,0)
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0, ChessConstants.ELEMENT_EARTH);

        // Place an enemy rook directly attacking the king
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 4, 5);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.BLACK);
        Assert.IsFalse(inCheck, "Black earth king on starting square should not be in check");
    }

    [Test]
    public void BedrockThrone_BlackKingOffStartingSquare_CanBeInCheck()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        // Black earth king moved to (5,1) -- NOT starting square
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.BLACK, 5, 1, ChessConstants.ELEMENT_EARTH);

        // Place an enemy rook attacking king at (5,1)
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 5, 5);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.BLACK);
        Assert.IsTrue(inCheck, "Black earth king off starting square should be in check normally");
    }

    [Test]
    public void BedrockThrone_NonEarthKingOnStartingSquare_CanBeInCheck()
    {
        // Normal (non-earth) white king on starting square
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Attack white king with rook
        builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 4, 3);

        builder.BoardState.RecalculateAttacks();
        bool inCheck = builder.BoardState.IsKingInCheck(ChessConstants.WHITE);
        Assert.IsTrue(inCheck, "Non-earth king on starting square should still be in check");
    }

    [Test]
    public void BedrockThrone_IsOnStartingSquare_ChecksCorrectly()
    {
        // Test the static helper method directly
        var whiteKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        Assert.IsTrue(EarthKingPassive.IsOnStartingSquare(whiteKing), "White king at (4,7) should be on starting square");

        var whiteKingMoved = builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6);
        Assert.IsFalse(EarthKingPassive.IsOnStartingSquare(whiteKingMoved), "White king at (3,6) should NOT be on starting square");

        var blackKing = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        Assert.IsTrue(EarthKingPassive.IsOnStartingSquare(blackKing), "Black king at (4,0) should be on starting square");

        var blackKingMoved = builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 5, 1);
        Assert.IsFalse(EarthKingPassive.IsOnStartingSquare(blackKingMoved), "Black king at (5,1) should NOT be on starting square");
    }

    // ========== EarthKingActive (Sanctuary) ==========

    [Test]
    public void Sanctuary_CreatesAdjacentStoneWalls()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        Assert.IsNotNull(active);

        Square target = earthKing.curSquare;
        active.Execute(earthKing, target, builder.BoardState, builder.SEM);

        // KingDirections: (0,-1),(0,1),(1,0),(1,1),(1,-1),(-1,0),(-1,1),(-1,-1)
        // All 8 adjacent squares from (4,4)
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 3), "Wall at (4,3)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(4, 5), "Wall at (4,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 4), "Wall at (5,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 5), "Wall at (5,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(5, 3), "Wall at (5,3)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 4), "Wall at (3,4)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 5), "Wall at (3,5)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(3, 3), "Wall at (3,3)");
    }

    [Test]
    public void Sanctuary_WallsHaveCorrectDefaultProperties()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        var effect = builder.SEM.GetEffectAt(4, 3);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
        // Default wallHP = 1, wallDuration = 2 from EarthKingActiveParams
        Assert.AreEqual(1, effect.hitPoints, "Sanctuary wall should have default HP of 1");
        Assert.AreEqual(2, effect.remainingTurns, "Sanctuary wall should have default duration of 2");
    }

    [Test]
    public void Sanctuary_StunsKing()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(earthKing.elementalPiece.IsStunned(), "King should be stunned after activating Sanctuary");
    }

    [Test]
    public void Sanctuary_KingSelfStunDuration_DefaultIs2()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(earthKing.elementalPiece.IsStunned());

        // Default selfStunDuration = 2
        earthKing.elementalPiece.TickStatusEffects(); // tick 1
        Assert.IsTrue(earthKing.elementalPiece.IsStunned(), "King should still be stunned after 1 tick (duration 2)");

        earthKing.elementalPiece.TickStatusEffects(); // tick 2
        Assert.IsFalse(earthKing.elementalPiece.IsStunned(), "King stun should expire after 2 ticks");
    }

    [Test]
    public void Sanctuary_StunsAdjacentFriendlyPieces()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place friendly piece adjacent at (5,4)
        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsNotNull(friendly.elementalPiece, "Friendly should get elementalPiece for stun tracking");
        Assert.IsTrue(friendly.elementalPiece.IsStunned(), "Adjacent friendly should be stunned by Sanctuary");
    }

    [Test]
    public void Sanctuary_AllyStunDuration_DefaultIs2()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(friendly.elementalPiece.IsStunned());

        // Default allyStunDuration = 2
        friendly.elementalPiece.TickStatusEffects(); // tick 1
        Assert.IsTrue(friendly.elementalPiece.IsStunned(), "Ally should still be stunned after 1 tick (duration 2)");

        friendly.elementalPiece.TickStatusEffects(); // tick 2
        Assert.IsFalse(friendly.elementalPiece.IsStunned(), "Ally stun should expire after 2 ticks");
    }

    [Test]
    public void Sanctuary_DoesNotStunAdjacentEnemies()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        // Place enemy adjacent at (3,4)
        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 4);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        Assert.IsTrue(enemy.elementalPiece == null || !enemy.elementalPiece.IsStunned(),
            "Adjacent enemy should NOT be stunned by Sanctuary (only allies are stunned)");
    }

    [Test]
    public void Sanctuary_CanAlwaysActivate()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        Assert.IsTrue(active.CanActivate(earthKing, builder.BoardState, builder.SEM),
            "Sanctuary should always be activatable");
    }

    [Test]
    public void Sanctuary_GetTargetSquares_ReturnsKingsOwnSquare()
    {
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 4, 4, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        List<Square> targets = active.GetTargetSquares(earthKing, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Sanctuary should have exactly 1 target (king's own square)");
        Assert.AreEqual(earthKing.curSquare, targets[0], "Target should be the king's own square");
    }

    [Test]
    public void Sanctuary_CreatesWallsAtEdge_HandlesOutOfBounds()
    {
        // King at corner (0,0) -- some adjacent squares are out of bounds
        var earthKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 0, 0, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 7);

        var active = earthKing.elementalPiece.active as EarthKingActive;
        active.Execute(earthKing, earthKing.curSquare, builder.BoardState, builder.SEM);

        // Only in-bounds adjacent squares should have walls
        // From (0,0): valid adjacent = (0,1), (1,0), (1,1)
        Assert.IsNotNull(builder.SEM.GetEffectAt(0, 1), "Wall at (0,1)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(1, 0), "Wall at (1,0)");
        Assert.IsNotNull(builder.SEM.GetEffectAt(1, 1), "Wall at (1,1)");
    }
}
