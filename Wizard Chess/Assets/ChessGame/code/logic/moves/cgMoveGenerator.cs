using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// We generate all possible moves(regardless of blocking) for all pieces once here.
/// So this class is used once at the very beginning of the very first game.
/// This technique saves a huge amount of computation for the Engine, as it only has to look up these moves and test their legality on a given board - instead of actually generating them.
/// </summary>
public class cgMoveGenerator
{
    /// <summary>
    /// Emulates all possible bishop movements from target index position.
    /// </summary>
    /// <param name="pos">The 0-64 index position</param>
    /// <returns>All possible moves.</returns>
    public static List<int> EmulateBishopAt(int pos)
    {
        List<int> directions = new List<int>() { -9, -7, 7, 9 };
        return FindMoves(directions, pos);
    }

    /// <summary>
    /// Emulates all possible king movement from target index position
    /// </summary>
    /// <param name="indexPosition">The 0-64 index position</param>
    /// <returns></returns>
    public static List<int> EmulateKingAt(int indexPosition)
    {
        List<int> moves = new List<int>();
        List<int> directions = new List<int>() { -9, -8, -7, -1, 1, 7, 8, 9 };
        foreach (int dir in directions)
        {
            int prevIndex = indexPosition;
            int nextIndex = indexPosition + dir;
            // If its on right border and going right, it cannot go further hence continue. Same for leftbreach
            bool leftBreach = cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(dir);
            bool rightBreach = cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(dir);
            if (nextIndex >= 0 && nextIndex < 64 && !leftBreach && !rightBreach) moves.Add(nextIndex);
        }
        return moves;
    }

    /// <summary>
    /// Emulates all possible rook movements from target index position.
    /// </summary>
    /// <param name="pos">The 0-64 index position</param>
    /// <returns>All possible moves.</returns>
    public static List<int> EmulateRookAt(int pos)
    {
        List<int> directions = new List<int>() { -8, -1, 1, 8 };
        return FindMoves(directions, pos);
    }

    /// <summary>
    /// Emulates all possible queen movements from target index position.
    /// </summary>
    /// <param name="pos">The 0-64 index position</param>
    /// <returns>All possible moves.</returns>
    public static List<int> EmulateQueenAt(int pos)
    {
        List<int> directions = new List<int>() { -9, -8, -7, -1, 1, 7, 8, 9 };
        return FindMoves(directions, pos);
    }

    /// <summary>
    /// Emulates all possible knight movements from target index position. And the knight is a tricky motherF!?cker to generate moves for.
    /// </summary>
    /// <param name="pos">The 0-64 index position</param>
    /// <returns>All possible moves.</returns>
    public static List<int> EmulateKnightAt(int indexPosition)
    {
        List<int> eligibleIndexPositions = new List<int>();

        List<int> pattern1 = new List<int>() { -8, -8, -1 };
        List<int> pattern2 = new List<int>() { -8, -8, 1 };
        List<int> pattern3 = new List<int>() { -1, -1, -8 };
        List<int> pattern4 = new List<int>() { -1, -1, 8 };
        List<int> pattern5 = new List<int>() { 8, 8, 1 };
        List<int> pattern6 = new List<int>() { 8, 8, -1 };
        List<int> pattern7 = new List<int>() { 1, 1, -8 };
        List<int> pattern8 = new List<int>() { 1, 1, 8 };

        //Each of these 8 patterns represent one possible move for the knight, each number in the pattern signifies a direction.
        List<List<int>> patterns = new List<List<int>>() { pattern1, pattern2, pattern3, pattern4, pattern5, pattern6, pattern7, pattern8 };
        //Going  into each of the 8 patterns
        foreach (List<int> pattern in patterns)
        {
            int prevIndex = indexPosition;
            int nextIndex = indexPosition;
            int count = 0;
            // If its on right border and going right, it cannot go further.
            bool leftBreach = cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(pattern[count]);
            bool rightBreach = cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(pattern[count]);
            while (nextIndex >= 0 && nextIndex < 64 && !leftBreach && !rightBreach && count < pattern.Count + 1)
            {

                if (count == (pattern.Count))
                {
                    //reached the end of the pattern, since the while loop hasn't broken we can safely assume the square is inside the board, and thus add it.
                    eligibleIndexPositions.Add(nextIndex);
                    break;
                }
                prevIndex = nextIndex;
                nextIndex = nextIndex + pattern[count];
                leftBreach = (cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(pattern[count]));
                rightBreach = (cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(pattern[count]));
                count++;
            }
            // each -1 in the array signifies the beginning of a new 'ray'
            //Since knights can't be blocked theres no need for the ray.

        }
        return eligibleIndexPositions;
    }

    /// <summary>
    /// Emulates all possible pawn movements from target index position.
    /// </summary>
    /// <param name="pos">The 0-64 index position</param>
    /// <returns>All possible moves.</returns>
    public static List<int> EmulatePawnAt(int indexPosition, bool white)
    {
        List<int> moves = new List<int>();
        List<int> directions = new List<int>();
        if (white) directions.Add(-8);
        else if (!white) directions.Add(8);

        //add basic forward move.
        foreach (int dir in directions)
        {
            int prevIndex = indexPosition;
            int nextIndex = indexPosition + dir;
            // If its on right border and going right, it cannot go further hence continue.
            bool leftBreach = cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(dir);
            bool rightBreach = cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(dir);
            if (nextIndex >= 0 && nextIndex < 64 && !leftBreach && !rightBreach) moves.Add(nextIndex);
        }



        //add double move if still at start row.
        if (white && cgGlobal.WhitePawnRow.Contains(indexPosition))
        {
            moves.Add(indexPosition - 16);
            moves.Add(-1);
        }
        else if (!white && cgGlobal.BlackPawnRow.Contains(indexPosition))
        {
            moves.Add(indexPosition + 16);
            moves.Add(-1);
        }

        //add attack moves
        directions = new List<int>();
        if (white)
        {
            directions.Add(-7);
            directions.Add(-9);
        }
        else if (!white)
        {
            directions.Add(7);
            directions.Add(9);
        }
        moves.Add(-2);
        foreach (int dir in directions)
        {

            int prevIndex = indexPosition;
            int nextIndex = indexPosition + dir;
            // If its on right border and going right, it cannot go further hence continue.
            bool leftBreach = cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(dir);
            bool rightBreach = cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(dir);
            if (nextIndex >= 0 && nextIndex < 64 && !leftBreach && !rightBreach)
            {
                moves.Add(nextIndex);
            }
        }
        //UnityEngine.Debug.Log("all pawn moves on:" + indexPosition + " as White:" + white + " " + pceGlobal.ListToString(moves));
        return moves;
    }

    /// <summary>
    /// Moves as far in each direction as possible  without going outside the 1x64 chess board.
    /// </summary>
    /// <param name="directions">Directions, as specified by how much it should add to the current position, i.e -9 = top left, -8 = top, -7 = top right, -1=left, 1 = right etc.</param>
    /// <param name="indexPosition">Starting position</param>
    /// <returns>A List of all possible moves inside the 1x64 chess board</returns>
    public static List<int> FindMoves(List<int> directions, int indexPosition)
    {
        List<int> eligibleIndexPositions = new List<int>();
        //Going  into each of the 8 directions.
        foreach (int dir in directions)
        {
            int prevIndex = indexPosition;
            int nextIndex = indexPosition + dir;
            // If its on right border and going right, it cannot go further hence continue.
            bool leftBreach = cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(dir);
            bool rightBreach = cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(dir);
            while (nextIndex >= 0 && nextIndex < 64 && !leftBreach && !rightBreach)
            {

                eligibleIndexPositions.Add(nextIndex);
                prevIndex = nextIndex;
                nextIndex = nextIndex + dir;
                leftBreach = (cgGlobal.LeftBorder.Contains(prevIndex) && cgGlobal.LeftDir.Contains(dir));
                rightBreach = (cgGlobal.RightBorder.Contains(prevIndex) && cgGlobal.RightDir.Contains(dir));
            }
            // each -1 in the array signifies the beginning of a new 'ray'
            eligibleIndexPositions.Add(-1);

        }
        return eligibleIndexPositions;
    }

}

