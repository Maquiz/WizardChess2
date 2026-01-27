using System.Collections.Generic;

/// <summary>
/// Interface for active elemental abilities that cost a turn to use.
/// </summary>
public interface IActiveAbility
{
    /// <summary>
    /// Whether this ability can be activated right now.
    /// Check cooldown, board conditions, etc.
    /// </summary>
    bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem);

    /// <summary>
    /// Get the squares that can be targeted by this ability.
    /// These will be highlighted for the player.
    /// </summary>
    List<Square> GetTargetSquares(PieceMove piece, BoardState bs);

    /// <summary>
    /// Execute the ability on the target square. Returns true if successful.
    /// </summary>
    bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem);
}
