using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class SquareEffectTests
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

    [Test]
    public void CreateEffect_Fire_SetsCorrectProperties()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.Fire, effect.effectType);
        Assert.AreEqual(2, effect.remainingTurns);
        Assert.AreEqual(ChessConstants.WHITE, effect.ownerColor);
    }

    [Test]
    public void CreateEffect_StoneWall_SetsHP()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.StoneWall, 3, ChessConstants.WHITE, 2);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.StoneWall, effect.effectType);
        Assert.AreEqual(2, effect.hitPoints);
    }

    [Test]
    public void CreateEffect_LightningField_SetsCorrectType()
    {
        var effect = builder.SEM.CreateEffect(5, 5, SquareEffectType.LightningField, 2, ChessConstants.BLACK);
        Assert.IsNotNull(effect);
        Assert.AreEqual(SquareEffectType.LightningField, effect.effectType);
    }

    [Test]
    public void Tick_Decrements_ReturnsTrue_WhenExpired()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 1, ChessConstants.WHITE);
        bool expired = effect.Tick();
        Assert.IsTrue(expired, "Effect with 1 turn remaining should expire after tick");
    }

    [Test]
    public void Tick_ReturnsFalse_WhenNotExpired()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 3, ChessConstants.WHITE);
        bool expired = effect.Tick();
        Assert.IsFalse(expired);
        Assert.AreEqual(2, effect.remainingTurns);
    }

    [Test]
    public void BlocksMovement_Fire_ReturnsTrue()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        var piece = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 4);
        Assert.IsTrue(effect.BlocksMovement(piece));
    }

    [Test]
    public void BlocksMovement_StoneWall_ReturnsTrue()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.StoneWall, 3, ChessConstants.WHITE, 2);
        var piece = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.BLACK, 3, 5);
        Assert.IsTrue(effect.BlocksMovement(piece));
    }

    [Test]
    public void BlocksMovement_LightningField_ReturnsFalse()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.LightningField, 2, ChessConstants.WHITE);
        var piece = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 3, 4);
        Assert.IsFalse(effect.BlocksMovement(piece));
    }

    [Test]
    public void TakeDamage_DestroysWhenHPZero()
    {
        var effect = builder.SEM.CreateEffect(3, 3, SquareEffectType.StoneWall, 5, ChessConstants.WHITE, 2);
        Assert.IsFalse(effect.TakeDamage(1)); // 2 -> 1
        Assert.IsTrue(effect.TakeDamage(1));  // 1 -> 0 (destroyed)
    }

    [Test]
    public void IsSquareBlocked_WithImmunity_ReturnsFalse()
    {
        builder.SEM.CreateEffect(3, 3, SquareEffectType.Fire, 2, ChessConstants.WHITE);
        var piece = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 5, ChessConstants.ELEMENT_FIRE);
        // Fire Queen passive grants fire immunity
        piece.elementalPiece.AddImmunity(SquareEffectType.Fire);
        Assert.IsFalse(builder.SEM.IsSquareBlocked(3, 3, piece));
    }

    [Test]
    public void GetEffectAt_ReturnsCorrectEffect()
    {
        var effect = builder.SEM.CreateEffect(2, 5, SquareEffectType.Fire, 3, ChessConstants.BLACK);
        Assert.AreEqual(effect, builder.SEM.GetEffectAt(2, 5));
    }

    [Test]
    public void GetEffectAt_ReturnsNull_WhenNoEffect()
    {
        Assert.IsNull(builder.SEM.GetEffectAt(4, 4));
    }
}
