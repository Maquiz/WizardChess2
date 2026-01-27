using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The Data structure to replay and record a game of chess
//Records the movements of every piece and its state
public class ChessMove {

	private PieceMove piece;
	public PieceMove Piece {
		get { return piece; }
		set { piece = value; }
	}

	private PieceMove takenPiece;
	private PieceMove TakenPiece {
		get { return takenPiece; }
		set { takenPiece = value; }
	}

	private bool isTaken;
	private bool IsTaken {
		get { return isTaken; }
		set { isTaken = value; }
	}

	//How Do we deal with changing pieces?
	private bool isQueened;
	private bool IsQueened {
		get { return isQueened; }
		set { isQueened = value; }
	}

	//How do we castle?
	private bool isCastled;
	private bool IsCastled {
		get { return isCastled; }
		set { isCastled = value; }
	}

	// Ability use tracking
	private bool isAbilityUse;
	public bool IsAbilityUse {
		get { return isAbilityUse; }
	}
	private string usedAbilityName;
	private int targetX, targetY;

	//Constructor for ChessMove (regular move)
	public ChessMove (PieceMove pm) {
		piece = pm;
		isTaken = false;
		isAbilityUse = false;
	}

	//Constructor for ChessMove (capture move)
	public ChessMove (PieceMove pm, PieceMove tp) {
		piece = pm;
		takenPiece = tp;
		isTaken = true;
		isAbilityUse = false;
	}

	//Constructor for ChessMove (ability use)
	public ChessMove (PieceMove pm, string abilityName, int tX, int tY) {
		piece = pm;
		usedAbilityName = abilityName;
		targetX = tX;
		targetY = tY;
		isAbilityUse = true;
		isTaken = false;
	}

	public void printMove() {
		string output = "";

		//regular Move
		output += piece.printPieceName() + " Moves to " + piece.printSquare(piece.curx, piece.cury);

		//Taken Move
		if (isTaken) {
			output += " has Taken " + takenPiece.printPieceName();
		}
		//isQueened Move
		//isCastled Move
		Debug.Log (output);
	}
}