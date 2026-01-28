using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class FireKnightTests
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

    // ========== FireKnightPassive (Splash Damage) ==========

    [Test]
    public void Passive_OnAfterCapture_SingesAdjacentEnemies()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_FIRE);
        PieceMove defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        // Place enemy adjacent to capture square (orthogonal -- default includeDiagonals=false uses RookDirections)
        PieceMove adjacentEnemy = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 2, ChessConstants.ELEMENT_NONE);

        // Simulate the knight has already captured and is now at (3,3)
        FireKnightPassive passive = (FireKnightPassive)fireKnight.elementalPiece.passive;
        passive.OnAfterCapture(fireKnight, defender, builder.BoardState);

        Assert.IsTrue(adjacentEnemy.elementalPiece.HasStatusEffect(StatusEffectType.Singed),
            "Adjacent enemy should be singed after capture");
    }

    [Test]
    public void Passive_OnAfterCapture_SingesAdjacentEnemy_WithoutElementalPiece()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_FIRE);
        PieceMove defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        // Place non-elemental enemy adjacent orthogonally
        PieceMove adjacentEnemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 3);

        FireKnightPassive passive = (FireKnightPassive)fireKnight.elementalPiece.passive;
        passive.OnAfterCapture(fireKnight, defender, builder.BoardState);

        // The passive adds an ElementalPiece component if none exists
        Assert.IsNotNull(adjacentEnemy.elementalPiece, "ElementalPiece should be added to non-elemental enemy");
        Assert.IsTrue(adjacentEnemy.elementalPiece.HasStatusEffect(StatusEffectType.Singed),
            "Adjacent non-elemental enemy should be singed");
    }

    [Test]
    public void Passive_OnAfterCapture_DoesNotSingeKings()
    {
        // Place black king adjacent to capture square
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 3, 2);

        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_FIRE);
        PieceMove defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);

        FireKnightPassive passive = (FireKnightPassive)fireKnight.elementalPiece.passive;
        passive.OnAfterCapture(fireKnight, defender, builder.BoardState);

        // Black king at (3,2) is adjacent but should NOT be singed
        PieceMove blackKing = builder.BoardState.GetPieceAt(3, 2);
        Assert.IsNotNull(blackKing, "Black king should still be on the board");
        // King either has no elementalPiece or is not singed
        if (blackKing.elementalPiece != null)
        {
            Assert.IsFalse(blackKing.elementalPiece.HasStatusEffect(StatusEffectType.Singed),
                "Kings should not be singed by Splash Damage");
        }
    }

    [Test]
    public void Passive_OnAfterCapture_DoesNotSingeFriendlyPieces()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_FIRE);
        PieceMove defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        // Place friendly piece adjacent
        PieceMove friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 2, 3);

        FireKnightPassive passive = (FireKnightPassive)fireKnight.elementalPiece.passive;
        passive.OnAfterCapture(fireKnight, defender, builder.BoardState);

        // Friendly piece should NOT be singed
        if (friendly.elementalPiece != null)
        {
            Assert.IsFalse(friendly.elementalPiece.HasStatusEffect(StatusEffectType.Singed),
                "Friendly pieces should not be singed by Splash Damage");
        }
        // If no elementalPiece, it's also fine -- means no singe was attempted
    }

    [Test]
    public void Passive_OnAfterCapture_DefaultUsesOrthogonalOnly()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 3, ChessConstants.ELEMENT_FIRE);
        PieceMove defender = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 3);
        // Place enemy diagonally adjacent
        PieceMove diagonalEnemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 2);

        FireKnightPassive passive = (FireKnightPassive)fireKnight.elementalPiece.passive;
        passive.OnAfterCapture(fireKnight, defender, builder.BoardState);

        // Default includeDiagonals=false means only RookDirections are checked
        // Diagonal enemy should NOT be singed
        if (diagonalEnemy.elementalPiece != null)
        {
            Assert.IsFalse(diagonalEnemy.elementalPiece.HasStatusEffect(StatusEffectType.Singed),
                "Diagonal enemies should not be singed when includeDiagonals is false (default)");
        }
    }

    // ========== FireKnightActive (Eruption) ==========

    [Test]
    public void Active_Execute_CreatesFireOnAllAdjacentSquares()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKnightActive active = (FireKnightActive)fireKnight.elementalPiece.active;
        Square target = fireKnight.curSquare;

        active.Execute(fireKnight, target, builder.BoardState, builder.SEM);

        // All 8 KingDirections from (3,4)
        foreach (var dir in ChessConstants.KingDirections)
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
    public void Active_Execute_FireHasCorrectDuration()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKnightActive active = (FireKnightActive)fireKnight.elementalPiece.active;
        active.Execute(fireKnight, fireKnight.curSquare, builder.BoardState, builder.SEM);

        // Default FireKnightActiveParams.fireDuration = 2
        SquareEffect effect = builder.SEM.GetEffectAt(3, 5);
        Assert.IsNotNull(effect);
        Assert.AreEqual(2, effect.remainingTurns, "Fire duration should match default of 2");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsSelfSquare()
    {
        PlaceKings();
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);

        FireKnightActive active = (FireKnightActive)fireKnight.elementalPiece.active;
        List<Square> targets = active.GetTargetSquares(fireKnight, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Eruption targets self only");
        Assert.AreEqual(fireKnight.curSquare, targets[0]);
    }

    [Test]
    public void Active_Execute_DoesNotCreateFireOutOfBounds()
    {
        PlaceKings();
        // Place knight in corner
        PieceMove fireKnight = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.WHITE, 0, 0, ChessConstants.ELEMENT_FIRE);

        FireKnightActive active = (FireKnightActive)fireKnight.elementalPiece.active;
        active.Execute(fireKnight, fireKnight.curSquare, builder.BoardState, builder.SEM);

        // Only in-bounds squares should have fire: (1,0), (0,1), (1,1) from corner (0,0)
        List<SquareEffect> fires = builder.SEM.GetAllEffectsOfType(SquareEffectType.Fire);
        Assert.AreEqual(3, fires.Count, "Only 3 in-bounds adjacent squares from corner");
    }
}
