using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

[TestFixture]
public class FireKingTests
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

    // ========== FireKingPassive (Ember Aura) ==========

    [Test]
    public void Passive_OnAfterMove_Creates4OrthogonalFires()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKingPassive passive = (FireKingPassive)fireKing.elementalPiece.passive;
        // Simulate king moved to (3,4) from (3,5)
        passive.OnAfterMove(fireKing, 3, 5, 3, 4, builder.BoardState);

        // RookDirections from (3,4): (3,5), (3,3), (4,4), (2,4)
        foreach (var dir in ChessConstants.RookDirections)
        {
            int nx = 3 + dir.x;
            int ny = 4 + dir.y;
            if (builder.BoardState.IsInBounds(nx, ny))
            {
                SquareEffect effect = builder.SEM.GetEffectAt(nx, ny);
                Assert.IsNotNull(effect, $"Fire should be at ({nx},{ny})");
                Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
            }
        }
    }

    [Test]
    public void Passive_OnAfterMove_FiresHaveLongDuration()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKingPassive passive = (FireKingPassive)fireKing.elementalPiece.passive;
        passive.OnAfterMove(fireKing, 3, 5, 3, 4, builder.BoardState);

        // Aura fires have duration of 999 (effectively permanent)
        SquareEffect effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect);
        Assert.AreEqual(999, effect.remainingTurns, "Ember Aura fires should have 999 turn duration");
    }

    [Test]
    public void Passive_OnAfterMove_FireOwnerMatchesKingColor()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKingPassive passive = (FireKingPassive)fireKing.elementalPiece.passive;
        passive.OnAfterMove(fireKing, 3, 5, 3, 4, builder.BoardState);

        SquareEffect effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect);
        Assert.AreEqual(ChessConstants.WHITE, effect.ownerColor, "Fire owner should match king's color");
    }

    [Test]
    public void Passive_OnAfterMove_DoesNotCreateFireWhereEffectAlreadyExists()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        // Pre-create a fire effect at (3,5)
        SquareEffect existingFire = builder.SEM.CreateEffect(3, 5, SquareEffectType.Fire, 1, ChessConstants.BLACK);

        FireKingPassive passive = (FireKingPassive)fireKing.elementalPiece.passive;
        passive.OnAfterMove(fireKing, 3, 5, 3, 4, builder.BoardState);

        // The existing effect at (3,5) should not be replaced since GetEffectAt returns non-null
        SquareEffect effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect);
        // The existing fire had duration 1 and BLACK owner; aura skips if existing != null
        Assert.AreEqual(ChessConstants.BLACK, effect.ownerColor,
            "Existing fire should NOT be overwritten by aura");
        Assert.AreEqual(1, effect.remainingTurns,
            "Existing fire duration should be preserved");
    }

    [Test]
    public void Passive_OnAfterMove_ClipsToBoard_NearEdge()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        // Place king at edge
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 0, 4, ChessConstants.ELEMENT_FIRE);

        FireKingPassive passive = (FireKingPassive)fireKing.elementalPiece.passive;
        passive.OnAfterMove(fireKing, 1, 4, 0, 4, builder.BoardState);

        // From (0,4), orthogonal: (0,5), (0,3), (1,4), (-1,4=OOB)
        // Only 3 fires should be created
        List<SquareEffect> fires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(3, fires.Count, "Only 3 orthogonal squares are in bounds from edge");
    }

    // ========== FireKingActive (Backdraft) ==========

    [Test]
    public void Active_CanActivate_RequiresFireOnBoard()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;

        // No fire on board
        Assert.IsFalse(active.CanActivate(fireKing, builder.BoardState, builder.SEM),
            "Backdraft should not be activatable with no fire on board");

        // Add fire
        builder.SEM.CreateEffect(5, 5, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        Assert.IsTrue(active.CanActivate(fireKing, builder.BoardState, builder.SEM),
            "Backdraft should be activatable when fire exists on board");
    }

    [Test]
    public void Active_Execute_CapturesEnemiesAdjacentToFireSquares()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);

        // Place fire at (5,3)
        builder.SEM.CreateEffect(5, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        // Place enemy adjacent to fire at (5,2)
        PieceMove enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 2);

        // SquareEffect.RemoveEffect() calls Destroy() which logs error in edit mode
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        active.Execute(fireKing, fireKing.curSquare, builder.BoardState, builder.SEM);

        // Enemy at (5,2) is adjacent to fire at (5,3) via KingDirections, so should be captured
        PieceMove pieceAt5_2 = builder.BoardState.GetPieceAt(5, 2);
        Assert.IsNull(pieceAt5_2, "Enemy adjacent to fire should be captured by Backdraft");
    }

    [Test]
    public void Active_Execute_RemovesAllFireAfterCaptures()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);

        // Create multiple fires
        builder.SEM.CreateEffect(2, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        builder.SEM.CreateEffect(5, 5, SquareEffectType.Fire, 3, ChessConstants.WHITE);
        builder.SEM.CreateEffect(6, 2, SquareEffectType.Fire, 1, ChessConstants.BLACK);

        // 3 fire effects → 3 Destroy calls in edit mode
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        active.Execute(fireKing, fireKing.curSquare, builder.BoardState, builder.SEM);

        // All fire should be removed
        List<SquareEffect> remainingFires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(0, remainingFires.Count, "All fire squares should be removed after Backdraft");
    }

    [Test]
    public void Active_Execute_DoesNotCaptureKings()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        // Place black king adjacent to a fire square
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 5, 2);

        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);

        builder.SEM.CreateEffect(5, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);

        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        active.Execute(fireKing, fireKing.curSquare, builder.BoardState, builder.SEM);

        // Black king at (5,2) is adjacent to fire at (5,3) but should NOT be captured
        PieceMove blackKing = builder.BoardState.GetPieceAt(5, 2);
        Assert.IsNotNull(blackKing, "King should not be captured by Backdraft");
        Assert.AreEqual(ChessConstants.KING, blackKing.piece);
    }

    [Test]
    public void Active_Execute_DoesNotCaptureFriendlyPieces()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);

        builder.SEM.CreateEffect(5, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        // Place friendly piece adjacent to fire
        PieceMove friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 2);

        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        active.Execute(fireKing, fireKing.curSquare, builder.BoardState, builder.SEM);

        // Friendly piece should NOT be captured
        PieceMove friendlyStill = builder.BoardState.GetPieceAt(5, 2);
        Assert.IsNotNull(friendlyStill, "Friendly pieces should not be captured by Backdraft");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsSelfSquare()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(fireKing, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Backdraft targets self only");
        Assert.AreEqual(fireKing.curSquare, targets[0]);
    }

    [Test]
    public void Active_Execute_CapturesMultipleEnemiesAdjacentToDifferentFires()
    {
        PlaceKings();
        PieceMove fireKing = builder.PlaceElementalPiece(ChessConstants.KING, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);

        // Fire at (2,3) with enemy at (2,2)
        builder.SEM.CreateEffect(2, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        PieceMove enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 2);

        // Fire at (6,5) with enemy at (7,5)
        builder.SEM.CreateEffect(6, 5, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        PieceMove enemy2 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 7, 5);

        // 2 fire effects → 2 Destroy calls in edit mode
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        FireKingActive active = (FireKingActive)fireKing.elementalPiece.active;
        active.Execute(fireKing, fireKing.curSquare, builder.BoardState, builder.SEM);

        // Both enemies should be captured
        Assert.IsNull(builder.BoardState.GetPieceAt(2, 2), "Enemy 1 should be captured");
        Assert.IsNull(builder.BoardState.GetPieceAt(7, 5), "Enemy 2 should be captured");
    }
}
