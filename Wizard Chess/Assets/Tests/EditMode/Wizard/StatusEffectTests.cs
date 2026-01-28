using NUnit.Framework;

[TestFixture]
public class StatusEffectTests
{
    [Test]
    public void Constructor_SetsTypeAndTurns()
    {
        var effect = new StatusEffect(StatusEffectType.Stunned, 3);
        Assert.AreEqual(StatusEffectType.Stunned, effect.Type);
        Assert.AreEqual(3, effect.RemainingTurns);
    }

    [Test]
    public void Constructor_DefaultNotPermanent()
    {
        var effect = new StatusEffect(StatusEffectType.Singed, 2);
        Assert.IsFalse(effect.IsPermanentUntilTriggered);
    }

    [Test]
    public void Tick_DecrementsTurns_ReturnsFalseWhenNotExpired()
    {
        var effect = new StatusEffect(StatusEffectType.Stunned, 3);
        bool expired = effect.Tick();
        Assert.IsFalse(expired);
        Assert.AreEqual(2, effect.RemainingTurns);
    }

    [Test]
    public void Tick_ReturnsTrue_WhenExpired()
    {
        var effect = new StatusEffect(StatusEffectType.Stunned, 1);
        bool expired = effect.Tick();
        Assert.IsTrue(expired);
        Assert.AreEqual(0, effect.RemainingTurns);
    }

    [Test]
    public void Tick_MultipleTicks_ExpiresCorrectly()
    {
        var effect = new StatusEffect(StatusEffectType.Singed, 3);
        Assert.IsFalse(effect.Tick()); // 2
        Assert.IsFalse(effect.Tick()); // 1
        Assert.IsTrue(effect.Tick());  // 0 — expired
    }

    [Test]
    public void IsPermanentUntilTriggered_DoesNotDecrement()
    {
        var effect = new StatusEffect(StatusEffectType.Singed, 1, permanentUntilTriggered: true);
        Assert.IsTrue(effect.IsPermanentUntilTriggered);
        bool expired = effect.Tick();
        Assert.IsFalse(expired, "Permanent effect should not expire from ticking");
        Assert.AreEqual(1, effect.RemainingTurns, "Remaining turns should not change");
    }

    [Test]
    public void Remove_ForceSetsToZero()
    {
        var effect = new StatusEffect(StatusEffectType.Stunned, 5, permanentUntilTriggered: true);
        effect.Remove();
        Assert.AreEqual(0, effect.RemainingTurns);
        Assert.IsFalse(effect.IsPermanentUntilTriggered, "Remove should clear permanent flag");
    }

    [Test]
    public void Remove_AfterRemove_TickReturnsExpired()
    {
        var effect = new StatusEffect(StatusEffectType.Stunned, 5, permanentUntilTriggered: true);
        effect.Remove();
        bool expired = effect.Tick();
        Assert.IsTrue(expired);
    }
}
