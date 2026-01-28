using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class FireQueenTests
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

    // ========== FireQueenPassive (Royal Inferno) ==========

    [Test]
    public void Passive_QueenIsImmuneToFireSquares_WhenImmunitySet()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        // Fire Queen's immunity is set externally (via DraftManager.ApplyElementToPiece)
        // Simulate by adding immunity directly
        fireQueen.elementalPiece.AddImmunity(SquareEffectType.Fire);

        // Create fire on a square
        builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.BLACK);

        // Queen should not be blocked by fire
        Assert.IsFalse(builder.SEM.IsSquareBlocked(3, 3, fireQueen),
            "Fire Queen with immunity should not be blocked by fire squares");
    }

    [Test]
    public void Passive_NonImmuneQueenIsBlockedByFire()
    {
        PlaceKings();
        // Place a queen WITHOUT fire element (no immunity)
        PieceMove normalQueen = builder.PlacePiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4);

        builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.BLACK);

        // Normal queen should be blocked by fire
        Assert.IsTrue(builder.SEM.IsSquareBlocked(3, 3, normalQueen),
            "Non-immune queen should be blocked by fire squares");
    }

    [Test]
    public void Passive_ImmuneToFireOnly_NotStoneWall()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        fireQueen.elementalPiece.AddImmunity(SquareEffectType.Fire);

        builder.SEM.CreateEffect(3, 3, SquareEffectType.StoneWall, 3, ChessConstants.BLACK, 2);

        // Queen should still be blocked by stone walls
        Assert.IsTrue(builder.SEM.IsSquareBlocked(3, 3, fireQueen),
            "Fire immunity should not grant immunity to stone walls");
    }

    // ========== FireQueenActive (Meteor Strike) ==========

    [Test]
    public void Active_Execute_Creates3x3FireZoneAroundTarget()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        Square target = builder.GetSquare(3, 3);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // Default aoeRadius=1, so 3x3 zone centered on (3,3)
        int fireCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = 3 + dx;
                int ny = 3 + dy;
                if (builder.BoardState.IsInBounds(nx, ny))
                {
                    SquareEffect effect = builder.SEM.GetEffectAt(nx, ny);
                    Assert.IsNotNull(effect, $"Fire should be at ({nx},{ny})");
                    Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
                    fireCount++;
                }
            }
        }
        Assert.AreEqual(9, fireCount, "3x3 zone should have 9 fire squares when fully in bounds");
    }

    [Test]
    public void Active_Execute_FireZoneHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        Square target = builder.GetSquare(3, 3);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // Default FireQueenActiveParams.fireDuration = 3
        SquareEffect effect = builder.SEM.GetEffectAt(3, 3);
        Assert.IsNotNull(effect);
        Assert.AreEqual(3, effect.remainingTurns, "Meteor Strike fire duration should match default of 3");
    }

    [Test]
    public void Active_Execute_CapturesFirstEnemyInZone()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);
        PieceMove enemyPawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        Square target = builder.GetSquare(3, 3);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // Enemy pawn at (3,3) should be captured -- it's within the 3x3 zone
        PieceMove pieceAt3_3 = builder.BoardState.GetPieceAt(3, 3);
        Assert.IsNull(pieceAt3_3, "Enemy pawn at (3,3) should have been captured by Meteor Strike");
    }

    [Test]
    public void Active_Execute_DoesNotCaptureKings()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        // Place black king within meteor strike zone
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 3, 3);

        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        Square target = builder.GetSquare(3, 3);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // Black king should NOT be captured
        PieceMove king = builder.BoardState.GetPieceAt(3, 3);
        Assert.IsNotNull(king, "King should not be captured by Meteor Strike");
        Assert.AreEqual(ChessConstants.KING, king.piece);
    }

    [Test]
    public void Active_Execute_MaxCapturesLimitsCaptures()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);
        // Place two enemy pieces in the zone
        PieceMove enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 2);
        PieceMove enemy2 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        Square target = builder.GetSquare(3, 3);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // Default maxCaptures=1, so only one enemy should be captured
        int capturedCount = 0;
        if (builder.BoardState.GetPieceAt(2, 2) == null) capturedCount++;
        if (builder.BoardState.GetPieceAt(3, 2) == null) capturedCount++;
        Assert.AreEqual(1, capturedCount, "Only 1 enemy should be captured (maxCaptures=1)");
    }

    [Test]
    public void Active_Execute_3x3ZoneNearEdgeClipsToBoard()
    {
        PlaceKings();
        PieceMove fireQueen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireQueenActive active = (FireQueenActive)fireQueen.elementalPiece.active;
        // Target corner square (0,0)
        Square target = builder.GetSquare(0, 0);

        active.Execute(fireQueen, target, builder.BoardState, builder.SEM);

        // 3x3 zone centered on (0,0): only (0,0),(1,0),(0,1),(1,1) are in bounds
        List<SquareEffect> fires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(4, fires.Count, "3x3 zone at corner should clip to 4 in-bounds squares");
    }
}
