using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Earthquake: All enemy pieces within range (Manhattan distance) are Stunned.
/// </summary>
public class EarthKnightActive : IActiveAbility
{
    private readonly EarthKnightActiveParams _params;

    public EarthKnightActive() { _params = new EarthKnightActiveParams(); }
    public EarthKnightActive(EarthKnightActiveParams p) { _params = p; }

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
        for (int dx = -_params.range; dx <= _params.range; dx++)
        {
            for (int dy = -_params.range; dy <= _params.range; dy++)
            {
                if (System.Math.Abs(dx) + System.Math.Abs(dy) > _params.range) continue;
                if (dx == 0 && dy == 0) continue;

                int nx = piece.curx + dx;
                int ny = piece.cury + dy;
                if (!bs.IsInBounds(nx, ny)) continue;

                PieceMove targetPiece = bs.GetPieceAt(nx, ny);
                if (targetPiece != null && targetPiece.color != piece.color)
                {
                    if (targetPiece.elementalPiece == null)
                    {
                        var ep = targetPiece.gameObject.AddComponent<ElementalPiece>();
                        ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
                    }
                    targetPiece.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.stunDuration);
                    Debug.Log(targetPiece.printPieceName() + " is stunned by Earthquake!");
                }
            }
        }
        return true;
    }
}
