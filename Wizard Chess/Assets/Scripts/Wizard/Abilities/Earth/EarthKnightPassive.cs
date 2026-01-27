using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tremor Hop: After moving, enemy pieces adjacent to landing square are Stunned.
/// </summary>
public class EarthKnightPassive : IPassiveAbility
{
    private readonly EarthKnightPassiveParams _params;

    public EarthKnightPassive() { _params = new EarthKnightPassiveParams(); }
    public EarthKnightPassive(EarthKnightPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs)
    {
        int stunned = 0;
        foreach (var dir in ChessConstants.KingDirections)
        {
            if (stunned >= _params.maxTargets) break;

            int nx = toX + dir.x;
            int ny = toY + dir.y;
            if (!bs.IsInBounds(nx, ny)) continue;

            PieceMove adj = bs.GetPieceAt(nx, ny);
            if (adj != null && adj.color != piece.color)
            {
                if (adj.elementalPiece == null)
                {
                    var ep = adj.gameObject.AddComponent<ElementalPiece>();
                    ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
                }
                adj.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.stunDuration);
                Debug.Log(adj.printPieceName() + " is stunned by Tremor Hop!");
                stunned++;
            }
        }
    }
}
