using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MonoBehaviour attached to each chess piece that has an element assigned.
/// Holds the element type, passive ability, active ability, cooldown, and status effects.
/// Composition: sits alongside PieceMove, does not replace it.
/// </summary>
public class ElementalPiece : MonoBehaviour
{
    public int elementId = ChessConstants.ELEMENT_NONE;
    public IPassiveAbility passive;
    public IActiveAbility active;
    public CooldownTracker cooldown;
    public PieceMove pieceMove;

    private List<StatusEffect> statusEffects = new List<StatusEffect>();

    // Immunity flags (set by specific abilities)
    private HashSet<SquareEffectType> immunities = new HashSet<SquareEffectType>();

    // Per-game flags
    public bool hasUsedReactiveBlink = false; // Lightning King passive (once per game)

    public void Init(int element, IPassiveAbility passiveAbility, IActiveAbility activeAbility, int activeCooldown)
    {
        elementId = element;
        pieceMove = GetComponent<PieceMove>();
        passive = passiveAbility;
        active = activeAbility;
        cooldown = new CooldownTracker(activeCooldown);

        // Set reference on PieceMove
        if (pieceMove != null)
        {
            pieceMove.elementalPiece = this;
        }
    }

    // ========== Status Effects ==========

    public void AddStatusEffect(StatusEffectType type, int turns, bool permanentUntilTriggered = false)
    {
        // Don't stack same effect type, just refresh
        RemoveStatusEffect(type);
        statusEffects.Add(new StatusEffect(type, turns, permanentUntilTriggered));
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        foreach (var effect in statusEffects)
        {
            if (effect.Type == type) return true;
        }
        return false;
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        statusEffects.RemoveAll(e => e.Type == type);
    }

    /// <summary>
    /// Tick all status effects. Called at turn start for this piece's owner.
    /// </summary>
    public void TickStatusEffects()
    {
        List<StatusEffect> expired = new List<StatusEffect>();
        foreach (var effect in statusEffects)
        {
            if (effect.Tick())
                expired.Add(effect);
        }
        foreach (var effect in expired)
        {
            statusEffects.Remove(effect);
        }
    }

    /// <summary>
    /// Whether this piece is stunned (cannot move this turn).
    /// </summary>
    public bool IsStunned()
    {
        return HasStatusEffect(StatusEffectType.Stunned);
    }

    /// <summary>
    /// Whether this piece is singed (captured regardless of normal rules when attacked).
    /// </summary>
    public bool IsSinged()
    {
        return HasStatusEffect(StatusEffectType.Singed);
    }

    // ========== Immunities ==========

    public void AddImmunity(SquareEffectType effectType)
    {
        immunities.Add(effectType);
    }

    public void RemoveImmunity(SquareEffectType effectType)
    {
        immunities.Remove(effectType);
    }

    public bool IsImmuneToEffect(SquareEffectType effectType)
    {
        return immunities.Contains(effectType);
    }

    // ========== Turn Hooks ==========

    /// <summary>
    /// Called at the start of each turn. Cooldowns tick on the piece owner's turn.
    /// Status effects tick on the opponent's turn so that effects like Stun
    /// actually block the affected piece for a full turn before expiring.
    /// </summary>
    public void OnTurnStart(int currentTurnColor)
    {
        if (pieceMove == null) return;

        if (pieceMove.color == currentTurnColor)
        {
            cooldown?.Tick();
        }

        if (pieceMove.color != currentTurnColor)
        {
            TickStatusEffects();
        }

        passive?.OnTurnStart(currentTurnColor);
    }
}
