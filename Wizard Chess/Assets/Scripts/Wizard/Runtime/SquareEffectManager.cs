using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton MonoBehaviour that manages all active square effects on the board.
/// Handles ticking durations, creating/removing effects, and querying blocked squares.
/// </summary>
public class SquareEffectManager : MonoBehaviour
{
    private List<SquareEffect> activeEffects = new List<SquareEffect>();
    private GameMaster gm;

    /// <summary>
    /// Earth Queen bonus: +1 HP to all stone walls while an Earth Queen is alive.
    /// Set by ElementalPiece when checking queen status.
    /// </summary>
    public int stoneWallBonusHP = 0;

    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;
    }

    /// <summary>
    /// Create a new square effect at the given coordinates.
    /// </summary>
    public SquareEffect CreateEffect(int x, int y, SquareEffectType type, int turns, int ownerColor, int hp = 1)
    {
        Square sq = GetSquare(x, y);
        if (sq == null) return null;

        // Remove existing effect on this square
        if (sq.activeEffect != null)
        {
            RemoveEffect(sq.activeEffect);
        }

        SquareEffect effect = sq.gameObject.AddComponent<SquareEffect>();
        effect.Init(sq, type, turns, ownerColor, hp + stoneWallBonusHP * (type == SquareEffectType.StoneWall ? 1 : 0));
        activeEffects.Add(effect);
        return effect;
    }

    /// <summary>
    /// Tick all effects, removing expired ones. Called at turn start.
    /// </summary>
    public void TickAllEffects()
    {
        List<SquareEffect> expired = new List<SquareEffect>();
        foreach (var effect in activeEffects)
        {
            if (effect == null || effect.Tick())
            {
                expired.Add(effect);
            }
        }
        foreach (var effect in expired)
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Remove a specific effect.
    /// </summary>
    public void RemoveEffect(SquareEffect effect)
    {
        if (effect != null)
        {
            activeEffects.Remove(effect);
            effect.RemoveEffect();
        }
    }

    /// <summary>
    /// Remove all effects of a given type.
    /// </summary>
    public void RemoveAllEffectsOfType(SquareEffectType type)
    {
        List<SquareEffect> toRemove = new List<SquareEffect>();
        foreach (var effect in activeEffects)
        {
            if (effect != null && effect.effectType == type)
                toRemove.Add(effect);
        }
        foreach (var effect in toRemove)
        {
            RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Check if a square is blocked by an effect for a given piece.
    /// </summary>
    public bool IsSquareBlocked(int x, int y, PieceMove piece)
    {
        Square sq = GetSquare(x, y);
        if (sq == null || sq.activeEffect == null) return false;

        // Check if this piece has immunity to the effect
        if (piece != null && piece.elementalPiece != null)
        {
            if (piece.elementalPiece.IsImmuneToEffect(sq.activeEffect.effectType))
                return false;
        }

        return sq.activeEffect.BlocksMovement(piece);
    }

    /// <summary>
    /// Get the effect on a square, if any.
    /// </summary>
    public SquareEffect GetEffectAt(int x, int y)
    {
        Square sq = GetSquare(x, y);
        if (sq == null) return null;
        return sq.activeEffect;
    }

    /// <summary>
    /// Get the name of the effect blocking a square, for UI display.
    /// Returns a human-readable name like "Fire", "Stone Wall", etc.
    /// </summary>
    public string GetBlockingEffectName(int x, int y)
    {
        SquareEffect effect = GetEffectAt(x, y);
        if (effect == null) return "Unknown effect";

        switch (effect.effectType)
        {
            case SquareEffectType.Fire:
                return "Fire";
            case SquareEffectType.StoneWall:
                return "Stone Wall";
            case SquareEffectType.LightningField:
                return "Lightning Field";
            default:
                return effect.effectType.ToString();
        }
    }

    /// <summary>
    /// Get all active effects of a specific type.
    /// </summary>
    public List<SquareEffect> GetAllEffectsOfType(SquareEffectType type)
    {
        List<SquareEffect> results = new List<SquareEffect>();
        foreach (var effect in activeEffects)
        {
            if (effect != null && effect.effectType == type)
                results.Add(effect);
        }
        return results;
    }

    /// <summary>
    /// Get all active effects.
    /// </summary>
    public List<SquareEffect> GetAllEffects()
    {
        return new List<SquareEffect>(activeEffects);
    }

    private Square GetSquare(int x, int y)
    {
        if (gm == null || !gm.boardState.IsInBounds(x, y)) return null;
        return gm.boardRows[y].transform.GetChild(x).GetComponent<Square>();
    }
}
