/// <summary>
/// Tracks a status effect on a piece (stunned, singed, etc.)
/// </summary>
[System.Serializable]
public class StatusEffect
{
    public StatusEffectType Type { get; private set; }
    public int RemainingTurns { get; private set; }

    /// <summary>
    /// If true, the effect doesn't expire by turns — it's removed by a trigger.
    /// (e.g., Singed is removed when the piece is attacked)
    /// </summary>
    public bool IsPermanentUntilTriggered { get; private set; }

    public StatusEffect(StatusEffectType type, int turns, bool permanentUntilTriggered = false)
    {
        Type = type;
        RemainingTurns = turns;
        IsPermanentUntilTriggered = permanentUntilTriggered;
    }

    /// <summary>
    /// Tick down the duration. Returns true if the effect has expired.
    /// </summary>
    public bool Tick()
    {
        if (IsPermanentUntilTriggered) return false;
        RemainingTurns--;
        return RemainingTurns <= 0;
    }

    /// <summary>
    /// Force-remove this effect.
    /// </summary>
    public void Remove()
    {
        RemainingTurns = 0;
        IsPermanentUntilTriggered = false;
    }
}
