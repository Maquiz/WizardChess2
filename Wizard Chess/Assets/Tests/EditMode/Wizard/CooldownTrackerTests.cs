using NUnit.Framework;

[TestFixture]
public class CooldownTrackerTests
{
    [Test]
    public void IsReady_WhenNew_ReturnsTrue()
    {
        var tracker = new CooldownTracker(5);
        Assert.IsTrue(tracker.IsReady);
    }

    [Test]
    public void IsReady_AfterStartCooldown_ReturnsFalse()
    {
        var tracker = new CooldownTracker(5);
        tracker.StartCooldown();
        Assert.IsFalse(tracker.IsReady);
    }

    [Test]
    public void CurrentCooldown_AfterStartCooldown_EqualsMax()
    {
        var tracker = new CooldownTracker(5);
        tracker.StartCooldown();
        Assert.AreEqual(5, tracker.CurrentCooldown);
    }

    [Test]
    public void MaxCooldown_ReturnsConstructorValue()
    {
        var tracker = new CooldownTracker(7);
        Assert.AreEqual(7, tracker.MaxCooldown);
    }

    [Test]
    public void Tick_DecrementsByOne()
    {
        var tracker = new CooldownTracker(5);
        tracker.StartCooldown();
        tracker.Tick();
        Assert.AreEqual(4, tracker.CurrentCooldown);
    }

    [Test]
    public void Tick_AtZero_StaysAtZero()
    {
        var tracker = new CooldownTracker(1);
        tracker.StartCooldown();
        tracker.Tick(); // 1 -> 0
        tracker.Tick(); // should stay 0
        Assert.AreEqual(0, tracker.CurrentCooldown);
        Assert.IsTrue(tracker.IsReady);
    }

    [Test]
    public void IsReady_AfterFullTickDown_ReturnsTrue()
    {
        var tracker = new CooldownTracker(3);
        tracker.StartCooldown();
        tracker.Tick(); // 3 -> 2
        tracker.Tick(); // 2 -> 1
        tracker.Tick(); // 1 -> 0
        Assert.IsTrue(tracker.IsReady);
    }

    [Test]
    public void Reset_SetsCooldownToZero()
    {
        var tracker = new CooldownTracker(5);
        tracker.StartCooldown();
        Assert.AreEqual(5, tracker.CurrentCooldown);
        tracker.Reset();
        Assert.AreEqual(0, tracker.CurrentCooldown);
        Assert.IsTrue(tracker.IsReady);
    }

    [Test]
    public void MultipleStartCooldown_ResetsToMax()
    {
        var tracker = new CooldownTracker(5);
        tracker.StartCooldown();
        tracker.Tick(); // 4
        tracker.Tick(); // 3
        tracker.StartCooldown(); // back to 5
        Assert.AreEqual(5, tracker.CurrentCooldown);
    }

    [Test]
    public void ZeroCooldown_AlwaysReady()
    {
        var tracker = new CooldownTracker(0);
        Assert.IsTrue(tracker.IsReady);
        tracker.StartCooldown();
        Assert.IsTrue(tracker.IsReady);
    }
}
