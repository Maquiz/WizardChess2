using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Earthen Shield: When this Bishop is captured, the capturing piece is Stunned.
/// </summary>
public class EarthBishopPassive : IPassiveAbility
{
    private readonly EarthBishopPassiveParams _params;

    public EarthBishopPassive() { _params = new EarthBishopPassiveParams(); }
    public EarthBishopPassive(EarthBishopPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }
    public void OnTurnStart(int currentTurnColor) { }

    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs)
    {
        if (capturer.elementalPiece == null)
        {
            var ep = capturer.gameObject.AddComponent<ElementalPiece>();
            ep.Init(ChessConstants.ELEMENT_NONE, null, null, 0);
        }
        capturer.elementalPiece.AddStatusEffect(StatusEffectType.Stunned, _params.stunDuration);
        Debug.Log(capturer.printPieceName() + " is stunned by Earthen Shield!");
    }
}
