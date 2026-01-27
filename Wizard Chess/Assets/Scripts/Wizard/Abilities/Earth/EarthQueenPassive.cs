using System.Collections.Generic;

/// <summary>
/// Tectonic Presence: All friendly Stone Walls have bonus HP while this Queen is alive.
/// Bonus HP value is read from AbilityBalanceConfig at setup time.
/// </summary>
public class EarthQueenPassive : IPassiveAbility
{
    private readonly EarthQueenPassiveParams _params;

    public EarthQueenPassive() { _params = new EarthQueenPassiveParams(); }
    public EarthQueenPassive(EarthQueenPassiveParams p) { _params = p; }

    public List<Square> ModifyMoveGeneration(List<Square> moves, PieceMove piece, BoardState bs) => moves;
    public bool OnBeforeCapture(PieceMove attacker, PieceMove defender, BoardState bs) => true;
    public void OnAfterCapture(PieceMove attacker, PieceMove defender, BoardState bs) { }
    public void OnAfterMove(PieceMove piece, int fromX, int fromY, int toX, int toY, BoardState bs) { }

    public void OnTurnStart(int currentTurnColor)
    {
        // The bonus HP is applied at wall creation time via SquareEffectManager.stoneWallBonusHP
    }

    public void OnPieceCaptured(PieceMove capturedPiece, PieceMove capturer, BoardState bs)
    {
        var sem = capturedPiece.gm.squareEffectManager;
        if (sem != null)
        {
            sem.stoneWallBonusHP = 0;
        }
    }
}
