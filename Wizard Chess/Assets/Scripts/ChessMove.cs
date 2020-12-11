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
	//Readd saved data like locations to have a record readd getters and setters

	//Constructor for ChessMove
	public ChessMove (PieceMove pm) {
		piece = pm;
		isTaken = false;
	}

	public ChessMove (PieceMove pm, PieceMove tp) {
		piece = pm;
		takenPiece = tp;
		isTaken = true;
	}

	public void printMove() {
		string output = "";

		//regular Move
		output += piece.printPieceName() + " Moves to " + piece.printCurSquare();

		//Taken Move
		if (isTaken) {
			output += " has Taken " + takenPiece.printPieceName();
		}
		//isQueened Move
		//isCastled Move
		//Debug.Log (output);
	}
}