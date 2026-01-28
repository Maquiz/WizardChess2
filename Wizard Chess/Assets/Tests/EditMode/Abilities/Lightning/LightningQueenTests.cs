using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class LightningQueenTests
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

    private void PlaceKings()
    {
        builder.PlacePiece(ChessConstants.KING, ChessConstants.WHITE, 4, 7);
        builder.PlacePiece(ChessConstants.KING, ChessConstants.BLACK, 4, 0);
    }

    // ========== LightningQueenPassive (Swiftness) ==========

    [Test]
    public void Passive_AddsKnightMoves()
    {
        // Swiftness: queen can also move like a knight (L-shape)
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(queen);

        // Knight L-shape moves from (3,4): (4,6),(5,5),(5,3),(4,2),(2,2),(1,3),(1,5),(2,6)
        // These should be added if empty (default: allowKnightCapture=false, so only empty squares)
        TestExtensions.AssertContainsMove(moves, 4, 6, "Knight move (4,6) should be added");
        TestExtensions.AssertContainsMove(moves, 5, 5, "Knight move (5,5) should be added");
        TestExtensions.AssertContainsMove(moves, 5, 3, "Knight move (5,3) should be added");
        TestExtensions.AssertContainsMove(moves, 4, 2, "Knight move (4,2) should be added");
        TestExtensions.AssertContainsMove(moves, 2, 2, "Knight move (2,2) should be added");
        TestExtensions.AssertContainsMove(moves, 1, 3, "Knight move (1,3) should be added");
        TestExtensions.AssertContainsMove(moves, 1, 5, "Knight move (1,5) should be added");
        TestExtensions.AssertContainsMove(moves, 2, 6, "Knight move (2,6) should be added");
    }

    [Test]
    public void Passive_KnightMoves_NoCapture_ByDefault()
    {
        // Default: allowKnightCapture=false, so knight-move squares with enemies are NOT added
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place enemy on a knight-move square
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 5);

        var moves = builder.GenerateMoves(queen);

        // (5,5) is a knight-move from (3,4), but allowKnightCapture=false
        // and the square is not empty, so it should NOT be added by the passive
        // HOWEVER, the queen's normal diagonal move can also reach (5,5) if path is clear
        // The queen at (3,4) diagonal (1,1): (4,5),(5,6),(6,7) - does not reach (5,5)
        // Diagonal (1,-1): (4,3),(5,2),(6,1),(7,0) - no
        // So (5,5) is not a normal queen move, only a knight-move. Should not be in moves.
        // Actually let's check: queen diagonal from (3,4) in direction (1,1): (4,5), (5,6), ...
        // In direction (1,-1): (4,3), (5,2), ...
        // So (5,5) is only reachable via knight move. With allowKnightCapture=false, it's skipped.
        TestExtensions.AssertDoesNotContainMove(moves, 5, 5,
            "Knight-move square occupied by enemy should not be added when allowKnightCapture=false");
    }

    [Test]
    public void Passive_NoDuplicateMoves()
    {
        // If a queen's normal move already covers a knight-move square, it should not be duplicated
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(queen);

        // Check for duplicates
        HashSet<(int, int)> seen = new HashSet<(int, int)>();
        foreach (var m in moves)
        {
            bool added = seen.Add((m.x, m.y));
            Assert.IsTrue(added, $"Duplicate move found at ({m.x},{m.y})");
        }
    }

    [Test]
    public void Passive_KnightMoves_SkipFriendlyOccupied()
    {
        // Knight-move squares occupied by friendly pieces should not be added
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 5, 5);

        var moves = builder.GenerateMoves(queen);

        // (5,5) is a knight-move occupied by friendly piece - should not be in moves
        // (also not a normal queen move because diagonal from (3,4) goes (4,5),(5,6),...)
        TestExtensions.AssertDoesNotContainMove(moves, 5, 5,
            "Knight-move square occupied by friendly should not be added");
    }

    [Test]
    public void Passive_OnlyAppliesToQueens()
    {
        // ModifyMoveGeneration returns unmodified list for non-queens
        PlaceKings();
        var rook = builder.PlacePiece(ChessConstants.ROOK, ChessConstants.WHITE, 3, 4);

        var passive = new LightningQueenPassive();
        var baseMoves = new List<Square>();
        var result = passive.ModifyMoveGeneration(baseMoves, rook, builder.BoardState);
        Assert.AreEqual(0, result.Count, "Should not add knight moves for non-queen pieces");
    }

    [Test]
    public void Passive_KnightMovesOutOfBoundsIgnored()
    {
        // Knight-move squares out of bounds should be ignored
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 0, 0,
            ChessConstants.ELEMENT_LIGHTNING);

        var moves = builder.GenerateMoves(queen);

        // From (0,0), knight moves are: (1,2),(2,1) - only these are in bounds
        // (-1,2),(-2,1),(1,-2),(2,-1),(-1,-2),(-2,-1) are all out of bounds
        foreach (var m in moves)
        {
            Assert.IsTrue(m.x >= 0 && m.x < 8 && m.y >= 0 && m.y < 8,
                $"Move ({m.x},{m.y}) is out of bounds");
        }
    }

    // ========== LightningQueenActive (Tempest) ==========

    [Test]
    public void Active_CanActivate_Always()
    {
        // Tempest always returns true for CanActivate
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        Assert.IsTrue(queen.elementalPiece.active.CanActivate(queen, builder.BoardState, builder.SEM),
            "Tempest should always be activatable");
    }

    [Test]
    public void Active_GetTargetSquares_ReturnsSelfSquare()
    {
        // Tempest targets the queen's own square
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var targets = queen.elementalPiece.active.GetTargetSquares(queen, builder.BoardState);

        Assert.AreEqual(1, targets.Count, "Should have exactly one target");
        Assert.AreEqual(3, targets[0].x);
        Assert.AreEqual(4, targets[0].y, "Target should be queen's own square");
    }

    [Test]
    public void Active_Execute_PushesNearbyEnemyAway()
    {
        // Tempest pushes enemies within detectionRange=3 (Chebyshev) away by pushDistance=2
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        // Place enemy at (4,4) - 1 square to the right. Push direction is (+1,0), push distance=2
        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 4);

        var target = builder.GetSquare(3, 4);
        queen.elementalPiece.active.Execute(queen, target, builder.BoardState, builder.SEM);

        // Enemy should be pushed from (4,4) to (6,4) (2 squares in direction +x)
        Assert.AreEqual(6, enemy.curx, "Enemy should be pushed 2 squares right");
        Assert.AreEqual(4, enemy.cury, "Enemy y should not change");
    }

    [Test]
    public void Active_Execute_PushStopsAtOccupiedSquare()
    {
        // Push stops if another piece blocks the path
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var enemy = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 4);
        // Blocker 1 square in push direction
        builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 5, 4);

        var target = builder.GetSquare(3, 4);
        queen.elementalPiece.active.Execute(queen, target, builder.BoardState, builder.SEM);

        // Enemy at (4,4) push direction is (+1,0). First step to (5,4) is occupied, so no push.
        Assert.AreEqual(4, enemy.curx, "Enemy should not move when path is blocked");
        Assert.AreEqual(4, enemy.cury);
    }

    [Test]
    public void Active_Execute_DoesNotPushFriendlyPieces()
    {
        // Tempest only pushes enemies
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var friendly = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.WHITE, 2, 4);

        var target = builder.GetSquare(3, 4);
        queen.elementalPiece.active.Execute(queen, target, builder.BoardState, builder.SEM);

        Assert.AreEqual(2, friendly.curx, "Friendly piece should not be pushed");
        Assert.AreEqual(4, friendly.cury);
    }

    [Test]
    public void Active_Execute_PushesMultipleEnemies()
    {
        // Multiple enemies within range should all be pushed
        PlaceKings();
        var queen = builder.PlaceElementalPiece(ChessConstants.QUEEN, ChessConstants.WHITE, 3, 4,
            ChessConstants.ELEMENT_LIGHTNING);

        var enemy1 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 4, 4);
        var enemy2 = builder.PlacePiece(ChessConstants.PAWN, ChessConstants.BLACK, 2, 4);

        var target = builder.GetSquare(3, 4);
        queen.elementalPiece.active.Execute(queen, target, builder.BoardState, builder.SEM);

        // enemy1 at (4,4): push direction (+1,0), pushed to (6,4)
        Assert.AreEqual(6, enemy1.curx, "Enemy1 should be pushed right");
        // enemy2 at (2,4): push direction (-1,0), pushed to (0,4)
        Assert.AreEqual(0, enemy2.curx, "Enemy2 should be pushed left");
    }
}
