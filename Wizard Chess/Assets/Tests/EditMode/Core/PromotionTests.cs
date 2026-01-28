using NUnit.Framework;

[TestFixture]
public class PromotionTests
{
    private ChessBoardBuilder builder;

    [SetUp]
    public void SetUp()
    {
        builder = new ChessBoardBuilder();
        builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        builder.Cleanup();
    }

    [Test]
    public void WhitePawn_CanReachRank0()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 7);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 1);
        pawn.firstMove = false;
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 4, 0, "White pawn should be able to reach rank 0");
    }

    [Test]
    public void BlackPawn_CanReachRank7()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 0);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 6);
        pawn.firstMove = false;
        var moves = builder.GenerateMoves(pawn);
        TestExtensions.AssertContainsMove(moves, 4, 7, "Black pawn should be able to reach rank 7");
    }

    [Test]
    public void PromoteTo_ChangesPieceType()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 1);
        pawn.firstMove = false;

        Assert.AreEqual(ChessConstants.PAWN, pawn.piece);
        pawn.PromoteTo(ChessConstants.QUEEN);
        Assert.AreEqual(ChessConstants.QUEEN, pawn.piece);
    }

    [Test]
    public void Promotion_ChangesMoveset()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        pawn.firstMove = false;

        // As pawn: only forward moves
        var pawnMoves = builder.GenerateMoves(pawn);
        int pawnMoveCount = pawnMoves.Count;

        // Promote to queen
        pawn.PromoteTo(ChessConstants.QUEEN);
        var queenMoves = builder.GenerateMoves(pawn);
        Assert.IsTrue(queenMoves.Count > pawnMoveCount,
            "Queen should have more moves than pawn");
    }

    [Test]
    public void PromoteTo_Rook_GeneratesRookMoves()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 0, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 7, 0);
        var pawn = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 4, 4);
        pawn.firstMove = false;

        pawn.PromoteTo(ChessConstants.ROOK);
        var moves = builder.GenerateMoves(pawn);
        // Should move like a rook (cardinal directions)
        TestExtensions.AssertContainsMove(moves, 4, 0);
        TestExtensions.AssertContainsMove(moves, 0, 4);
    }
}
