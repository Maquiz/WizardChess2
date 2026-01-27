using UnityEngine;

/// <summary>
/// MonoBehaviour placed on a Square to represent a temporary elemental effect.
/// Manages effect lifetime, HP (for stone walls), and visual indicators.
/// </summary>
public class SquareEffect : MonoBehaviour
{
    public SquareEffectType effectType = SquareEffectType.None;
    public int remainingTurns;
    public int ownerColor; // which player created this effect
    public int hitPoints = 1; // for stone walls

    private Square square;

    /// <summary>
    /// Initialize this effect on a square.
    /// </summary>
    public void Init(Square sq, SquareEffectType type, int turns, int owner, int hp = 1)
    {
        square = sq;
        effectType = type;
        remainingTurns = turns;
        ownerColor = owner;
        hitPoints = hp;
        sq.activeEffect = this;
    }

    /// <summary>
    /// Tick the effect duration. Returns true if expired.
    /// </summary>
    public bool Tick()
    {
        remainingTurns--;
        return remainingTurns <= 0;
    }

    /// <summary>
    /// Deal damage to this effect (stone walls). Returns true if destroyed.
    /// </summary>
    public bool TakeDamage(int damage = 1)
    {
        hitPoints -= damage;
        return hitPoints <= 0;
    }

    /// <summary>
    /// Whether this effect blocks piece movement through the square.
    /// </summary>
    public bool BlocksMovement(PieceMove piece)
    {
        switch (effectType)
        {
            case SquareEffectType.Fire:
                // Fire blocks all movement unless piece is immune
                return true;
            case SquareEffectType.StoneWall:
                // Stone walls block all movement
                return true;
            case SquareEffectType.LightningField:
                // Lightning fields don't block movement
                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Remove this effect from the square and destroy the component.
    /// </summary>
    public void RemoveEffect()
    {
        if (square != null)
        {
            square.activeEffect = null;
        }
        effectType = SquareEffectType.None;
        Destroy(this);
    }
}
