using UnityEngine;
using System.Collections;

/// <summary>
/// Most people probably have no clue what this 'En Passant' move is - if you don't then google it, its a legal pawn move in chess, in which a pawn captures a pawn next to it that has just performed its double move, while move diagonally forward.
/// </summary>
public class cgEnPassantMove :cgSimpleMove  {
    public byte attackingSquare;
    public cgEnPassantMove(byte fromp, byte tomp, sbyte posval, byte attackSquare)
        :base(fromp,tomp,posval)
    {
        attackingSquare = attackSquare;
    }
	
}
