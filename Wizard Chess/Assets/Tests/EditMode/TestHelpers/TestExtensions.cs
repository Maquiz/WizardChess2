using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Assert helpers for chess move validation in tests.
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Assert that a move list contains a move to the given coordinates.
    /// </summary>
    public static void AssertContainsMove(List<Square> moves, int x, int y, string context = "")
    {
        foreach (var m in moves)
        {
            if (m.x == x && m.y == y) return;
        }
        Assert.Fail($"Move to ({x},{y}) not found in move list.{(context != "" ? " " + context : "")} " +
                     $"Moves: {FormatMoves(moves)}");
    }

    /// <summary>
    /// Assert that a move list does NOT contain a move to the given coordinates.
    /// </summary>
    public static void AssertDoesNotContainMove(List<Square> moves, int x, int y, string context = "")
    {
        foreach (var m in moves)
        {
            if (m.x == x && m.y == y)
            {
                Assert.Fail($"Move to ({x},{y}) should NOT be in move list.{(context != "" ? " " + context : "")}");
            }
        }
    }

    /// <summary>
    /// Assert the move count equals expected.
    /// </summary>
    public static void AssertMoveCount(List<Square> moves, int expected, string context = "")
    {
        Assert.AreEqual(expected, moves.Count,
            $"Expected {expected} moves but got {moves.Count}.{(context != "" ? " " + context : "")} " +
            $"Moves: {FormatMoves(moves)}");
    }

    private static string FormatMoves(List<Square> moves)
    {
        var sb = new System.Text.StringBuilder("[");
        bool first = true;
        foreach (var m in moves)
        {
            if (!first) sb.Append(", ");
            sb.Append($"({m.x},{m.y})");
            first = false;
        }
        sb.Append("]");
        return sb.ToString();
    }
}
