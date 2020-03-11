using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class holds some simple general purpose properties and methods.
/// </summary>

public class cgGlobal  {

    /// <summary>
    /// Cell names according to index position.
    /// </summary>
    public static string[] SquareNames = {
        "a8","b8","c8","d8","e8","f8","g8","h8",
        "a7","b7","c7","d7","e7","f7","g7","h7",
        "a6","b6","c6","d6","e6","f6","g6","h6",
        "a5","b5","c5","d5","e5","f5","g5","h5",
        "a4","b4","c4","d4","e4","f4","g4","h4",
        "a3","b3","c3","d3","e3","f3","g3","h3",
        "a2","b2","c2","d2","e2","f2","g2","h2",
        "a1","b1","c1","d1","e1","f1","g1","h1",
                                       "Out ouf bounds"};
    /**
     * 0  1  2  3  4  5  6  7
    *  8  9  10 11 12 13 14 15
    *  16 17 18 19 20 21 22 23
    *  24 25 26 27 28 29 30 31
    *  32 33 34 35 36 37 38 39 
    *  40 41 42 43 44 45 46 47
    *  48 49 50 51 52 53 54 55
    *  56 57 58 59 60 61 62 63
    * */

   
    public static string MoveToString(cgSimpleMove move)
    {
        if (move == null) return "";
        return SquareNames[move.from] + "->" + SquareNames[move.to];
    }
    /// <summary>
    /// Translates an index position to a coordinate name(i.e. e4, d1 etc.)
    /// </summary>
    /// <param name="indexPos">the index between 0-64 on the board to translate</param>
    /// <returns>the cell name at the given index position</returns>
    public static string PosToString(int indexPos)
    {
        return (indexPos>0 && indexPos<SquareNames.Length)?SquareNames[indexPos]:"no";
    }
    /// <summary>
    /// Translates a coordinate string(e3, d1 etc.) to an index(0-64).
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public static byte IndexFromCellName(string p)
    {
        for (byte i = 0; i < SquareNames.Length; i++) if (SquareNames[i] == p) return i;
        return 0;
    }

    /// <summary>
    /// Translates a list of ints to a easily readable string(used for debugging purposes).
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ListToString(List<int> list)
    {
        string str = "";
        for (int i = 0; i < list.Count; i++) str += "[" + list[i] + "] ";
        return str;
    }
    /// <summary>
    /// Translates a list of Simple Moves to human readable string(used for debugging purposes).
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ListToString(List<cgSimpleMove> list)
    {
        string str = "";
        for (int i = 0; i < list.Count; i++) str += "[" + MoveToString(list[i]) + " (" + list[i].val + ")" + "(" + list[i].positionalVal + ")] ";
        return str;
    }

    /// <summary>
    /// Translates a list of sbytes to a string, used for Board.squares to read the abstract board representation.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ListToString(List<sbyte> list)
    {
        string str = "";
        for (int i = 0; i < list.Count; i++)
        {
            if ((i + 1) % 8 == 0) str += "\n";
            str += "["+list[i]+"]";
        }
        return str;
    }

    //All the following variabels are used to generate moves in the MoveGenerator.
    public static List<int> LeftDir = new List<int>() { -9, -1, 7 };
    public static List<int> RightDir = new List<int>() { -7, 1, 9 };
    public static List<int> TopDir = new List<int>() { -9, -8, -7 };
    public static List<int> BotDir = new List<int>() { 7, 8, 9 };

    public static List<int> RightBorder = new List<int>() { 7, 15, 23, 31, 39, 47, 55, 63 };
    public static List<int> LeftBorder = new List<int>() { 0, 8, 16, 24, 32, 40, 48, 56 };
    public static List<int> TopBorder = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };
    public static List<int> BotBorder = new List<int>() { 56, 57, 58, 59, 60, 61, 62, 63 };

    public static List<int> WhitePawnRow = new List<int>() { 48, 49, 50, 51, 52, 53, 54, 55 };
    public static List<int> BlackPawnRow = new List<int>() { 8, 9, 10, 11, 12, 13, 14, 15 };
  
}

/*! \mainpage Main Page
 *
 * A fully functional chess game for Unity - with a modifiable AI opponent.

 * -A well code commented and modifiable AI opponent.
 * -A rule adherent chess board(promotions, castling etc.)
 * -Nifty functions: Revert last move, flip board and reset board.
 * -Game modes: AI vs Player, Player vs Player
 * -Copy and pasting game notations(ctrl+c and ctrl+v) into or out of the game. 
 * 
 * */