using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static Field: Lightning field on adjacent squares, stunning enemies that move adjacent.
/// </summary>
public class LightningKingActive : IActiveAbility
{
    private readonly LtKingActiveParams _params;

    public LightningKingActive() { _params = new LtKingActiveParams(); }
    public LightningKingActive(LtKingActiveParams p) { _params = p; }

    public bool CanActivate(PieceMove piece, BoardState bs, SquareEffectManager sem)
    {
        return true;
    }

    public List<Square> GetTargetSquares(PieceMove piece, BoardState bs)
    {
        List<Square> targets = new List<Square>();
        targets.Add(piece.curSquare);
        return targets;
    }

    public bool Execute(PieceMove piece, Square target, BoardState bs, SquareEffectManager sem)
    {
        foreach (var dir in ChessConstants.KingDirections)
        {
            int nx = piece.curx + dir.x;
            int ny = piece.cury + dir.y;
            if (bs.IsInBounds(nx, ny))
            {
                sem.CreateEffect(nx, ny, SquareEffectType.LightningField, _params.fieldDuration, piece.color);
            }
        }

        Debug.Log("Static Field activated around " + piece.printPieceName() + "!");
        return true;
    }
}
