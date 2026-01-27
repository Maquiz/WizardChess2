/// <summary>
/// Tracks per-piece ability cooldown state.
/// Cooldown decrements by 1 each turn the owning player takes.
/// </summary>
[System.Serializable]
public class CooldownTracker
{
    private int currentCooldown;
    private int maxCooldown;

    public int CurrentCooldown => currentCooldown;
    public int MaxCooldown => maxCooldown;
    public bool IsReady => currentCooldown <= 0;

    public CooldownTracker(int maxCooldown)
    {
        this.maxCooldown = maxCooldown;
        this.currentCooldown = 0;
    }

    /// <summary>
    /// Start the cooldown (called after ability use).
    /// </summary>
    public void StartCooldown()
    {
        currentCooldown = maxCooldown;
    }

    /// <summary>
    /// Tick the cooldown down by 1 (called at turn start).
    /// </summary>
    public void Tick()
    {
        if (currentCooldown > 0)
            currentCooldown--;
    }

    /// <summary>
    /// Reset cooldown to 0 (ready to use).
    /// </summary>
    public void Reset()
    {
        currentCooldown = 0;
    }
}
