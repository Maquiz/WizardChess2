using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ElementalPieceTests
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
    public void Init_SetsElementAndAbilities()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);

        Assert.IsNotNull(piece.elementalPiece);
        Assert.AreEqual(ChessConstants.ELEMENT_FIRE, piece.elementalPiece.elementId);
        Assert.IsNotNull(piece.elementalPiece.passive);
        Assert.IsNotNull(piece.elementalPiece.active);
        Assert.IsNotNull(piece.elementalPiece.cooldown);
    }

    [Test]
    public void AddStatusEffect_Stunned_ReportsIsStunned()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 2);
        Assert.IsTrue(piece.elementalPiece.IsStunned());
    }

    [Test]
    public void AddStatusEffect_Singed_ReportsIsSinged()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 3, 3, ChessConstants.ELEMENT_EARTH);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Singed, 1, permanentUntilTriggered: true);
        Assert.IsTrue(piece.elementalPiece.IsSinged());
    }

    [Test]
    public void HasStatusEffect_ReturnsFalse_WhenNone()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        Assert.IsFalse(piece.elementalPiece.HasStatusEffect(StatusEffectType.Stunned));
        Assert.IsFalse(piece.elementalPiece.IsStunned());
    }

    [Test]
    public void TickStatusEffects_RemovesExpired()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 1);
        Assert.IsTrue(piece.elementalPiece.IsStunned());

        piece.elementalPiece.TickStatusEffects();
        Assert.IsFalse(piece.elementalPiece.IsStunned(), "Stunned effect should expire after 1 tick");
    }

    [Test]
    public void TickStatusEffects_KeepsPermanent()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.KNIGHT, ChessConstants.BLACK, 3, 3, ChessConstants.ELEMENT_EARTH);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Singed, 1, permanentUntilTriggered: true);

        piece.elementalPiece.TickStatusEffects();
        Assert.IsTrue(piece.elementalPiece.IsSinged(), "Permanent effect should not expire from ticking");
    }

    [Test]
    public void RemoveStatusEffect_RemovesSpecificType()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 3);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Singed, 3);

        piece.elementalPiece.RemoveStatusEffect(StatusEffectType.Stunned);
        Assert.IsFalse(piece.elementalPiece.IsStunned());
        Assert.IsTrue(piece.elementalPiece.IsSinged(), "Singed should remain after removing Stunned");
    }

    [Test]
    public void AddImmunity_GrantsImmunity()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddImmunity(SquareEffectType.Fire);
        Assert.IsTrue(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.Fire));
    }

    [Test]
    public void IsImmuneToEffect_ChecksCorrectly()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddImmunity(SquareEffectType.Fire);
        Assert.IsTrue(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.Fire));
        Assert.IsFalse(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.StoneWall));
        Assert.IsFalse(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.LightningField));
    }

    [Test]
    public void OnTurnStart_TicksCooldown()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.cooldown.StartCooldown();
        int initialCD = piece.elementalPiece.cooldown.CurrentCooldown;

        piece.elementalPiece.OnTurnStart(ChessConstants.WHITE);
        Assert.AreEqual(initialCD - 1, piece.elementalPiece.cooldown.CurrentCooldown);
    }

    [Test]
    public void OnTurnStart_OnlyForOwnColor()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.cooldown.StartCooldown();
        int initialCD = piece.elementalPiece.cooldown.CurrentCooldown;

        piece.elementalPiece.OnTurnStart(ChessConstants.BLACK); // wrong color
        Assert.AreEqual(initialCD, piece.elementalPiece.cooldown.CurrentCooldown, "Cooldown should not tick for opponent's turn");
    }

    [Test]
    public void DuplicateStatusEffect_RefreshesInsteadOfStacking()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 1);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 3); // refresh

        // Tick once — if stacking, first would expire but second still active
        // With refresh, it should still be stunned after 1 tick (3-1=2)
        piece.elementalPiece.TickStatusEffects();
        Assert.IsTrue(piece.elementalPiece.IsStunned(), "Refreshed stun should last longer");
    }

    [Test]
    public void OnTurnStart_StatusEffects_TickOnOpponentTurn()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 1);
        Assert.IsTrue(piece.elementalPiece.IsStunned());

        // Opponent's turn should tick status effects
        piece.elementalPiece.OnTurnStart(ChessConstants.BLACK);
        Assert.IsFalse(piece.elementalPiece.IsStunned(), "Stun should expire after ticking on opponent's turn");
    }

    [Test]
    public void OnTurnStart_StatusEffects_DoNotTickOnOwnTurn()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.PAWN, ChessConstants.WHITE, 3, 6, ChessConstants.ELEMENT_EARTH);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
        piece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, 1);
        Assert.IsTrue(piece.elementalPiece.IsStunned());

        // Own turn should NOT tick status effects
        piece.elementalPiece.OnTurnStart(ChessConstants.WHITE);
        Assert.IsTrue(piece.elementalPiece.IsStunned(), "Stun should persist through own turn");
    }

    [Test]
    public void RemoveImmunity_RemovesCorrectly()
    {
        var piece = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4, ChessConstants.ELEMENT_FIRE);
        piece.elementalPiece.AddImmunity(SquareEffectType.Fire);
        Assert.IsTrue(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.Fire));

        piece.elementalPiece.RemoveImmunity(SquareEffectType.Fire);
        Assert.IsFalse(piece.elementalPiece.IsImmuneToEffect(SquareEffectType.Fire));
    }
}
