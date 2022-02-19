﻿using UnityEngine;
using System.Collections;
using DG.Tweening;
using System.Globalization;
using System.Collections.Generic;

//Control of moving from one square to anotehr
public class PieceMove : MonoBehaviour
{

    private int pieceId
    {
        get { return pieceId; }
        set { pieceId = value; }
    }
    public int color, piece;
    private MeshFilter pieceMeshFilter;
    private MeshCollider pieceMeshCollider;
    private MeshRenderer pieceMeshRenderer;
    public int lastx, lasty;
    public int curx, cury;
    public GameObject last;
    public GameObject checker;

    public List<Square> moves = new List<Square>();

    public bool canMove;
    public bool showMoves;
    public bool firstMove;

    private bool isSet;
    public Square curSquare;
    public GameObject Board;
    public GameMaster gm;

    private Vector3 hiddenIsland = new Vector3(-1000f, -1000f, -1000f);

    public void createPiece(int _piece, int _color, MeshCollider _mc, MeshFilter _mf, MeshRenderer _mr)
    {
        //InitializeValues
        piece = _piece;
        color = _color;
        pieceMeshCollider = _mc;
        pieceMeshFilter = _mf;
        pieceMeshRenderer = _mr;
    }

    public void Start()
    {
        canMove = true;
        last = new GameObject();
        curx = cury = lastx = lasty = 0;
        isSet = false;
        curSquare = new Square();
        Board = GameObject.FindGameObjectWithTag("Board");
        gm = GameObject.FindGameObjectWithTag("GM").GetComponent<GameMaster>();
        pieceMeshFilter = this.gameObject.GetComponent<MeshFilter>();
        pieceMeshCollider = this.gameObject.GetComponent<MeshCollider>();
        pieceMeshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        showMoves = false;
        firstMove = true;
    }

    void OnMouseDown()
    {
        //Check if you are taking or piece if the player color = piece color
        if (!gm.isPieceSelected && gm.currentMove == color)
        {            
            gm.selectPiece(this.gameObject.transform, this);

        }
    }

    public void setIntitialPiece(int x, int y, GameObject sq)
    {
        setPieceLocation(x, y);
        setLastPieceLocation(x, y);
        curSquare = sq.GetComponent<Square>();
        last = sq;
        //createRookMoves();
        //printMovesList();
        ChessMove cm = new ChessMove(this);
        gm.moveHistory.Add(cm);
        gm.moveHistory[gm.moveHistory.Count - 1].printMove();
        createPieceMoves(piece);

        Debug.Log("Initiate Piece");
    }

    public void movePiece(int x, int y, Square square)
    {
    
            //Physical Movement 
            removePieceFromSquare();
            hideMovesHelper();
            //  moves.Clear();
            Debug.Log("MOVE PIECE");
            Transform t = this.gameObject.transform;
            t.DOPause();
            t.DOMove(new Vector3(this.transform.position.x, 1, this.transform.position.z), .5f);
            t.DOComplete();
            t.DOMove(new Vector3(square.gameObject.transform.position.x, this.transform.position.y, square.gameObject.transform.position.z), .5f);
            t.DOComplete();

        if (isSet) { firstMove = false; } else { isSet = true; }
            
            setLastPieceLocation(curx, cury);
            setPieceLocation(x, y);
            //Movement
            square.piece = this;
            curSquare = square;
            //Debug.Log(p.color.ToString() + p.piece.ToString() + " to " + x.ToString() + y);
            last = square.gameObject;

            //if not taking another piece
            ChessMove cm = new ChessMove(this);
            //Adding  possible moves list

            // createRookMoves();
            //printMovesList();
            gm.moveHistory.Add(cm);
            gm.moveHistory[gm.moveHistory.Count - 1].printMove();
            //moves.Clear();
            createPieceMoves(piece);
    }


    public bool checkMoves(int x, int y)
    {
        if (!showMoves)
        {
            showMoves = true;
            showMovesHelper();
        }
        for (int i = 0; i < moves.Count; i++)
        {
            if (x == moves[i].x && y == moves[i].y)
            {
                return true;
            }
        }
        return false;
    }

     public void createPieceMoves(int piece)
    {
        //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king, 
        //Color: 1 Black, 2 White, 3 Green, 4 Blue, 5 Red, 6 Yellow

        moves.Clear();
        if (piece == 6)
        { 
            createKingMoves();
        }
        else if (piece == 5)
        { //Queen
            createQueenMoves();
        }
        else if (piece == 4)
        { 
            createBishopMoves();
        }
        else if (piece == 3)
        { 
            createKnightMoves();
        }
        else if (piece == 2)
        { 
            createRookMoves();
        }
        else {
            createPawnMoves();
        }
    }

    public void createKingMoves()
    {
        //No Check checking
        if (isCoordsInBounds(cury - 1))
        {
            Debug.Log("King Move");
            Square curSquare = getSquare(curx, (cury - 1));
            if (curSquare != null && curSquare.taken)
            {
                Debug.Log("King Not Null");
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                }
            }
            else {
                moves.Add(curSquare);
            }
            

            if (isCoordsInBounds(curx - 1)) {
                curSquare = getSquare(curx - 1, cury - 1);
                if (curSquare != null && curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }

            if (isCoordsInBounds(curx + 1))
            {
                curSquare = getSquare(curx + 1, cury - 1);
                if (curSquare != null && curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }

        }

        if (isCoordsInBounds(cury + 1))
        {
            Square curSquare = getSquare(curx, cury + 1);
            if (curSquare != null && curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                }
            }
            else
            {
                moves.Add(curSquare);
            }

            if (isCoordsInBounds(curx - 1))
            {
                curSquare = getSquare(curx - 1, cury + 1);
                if (curSquare != null &&  curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }

            if (isCoordsInBounds(curx + 1))
            {
                curSquare = getSquare(curx + 1, cury + 1);
                if (curSquare!=null && curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }
        }

        if (isCoordsInBounds(curx))
        {
            Square curSquare = getSquare(curx, cury);
            if (isCoordsInBounds(curx - 1))
            {
                curSquare = getSquare(curx - 1, cury);
                if (curSquare != null &&  curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }

            if (isCoordsInBounds(curx + 1))
            {
                curSquare = getSquare(curx + 1, cury);
                if (curSquare != null &&  curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                    }
                }
                else
                {
                    moves.Add(curSquare);
                }
            }
        }
    }

    public void createQueenMoves() {
        createRookMoves();
        createBishopMoves();
    }

    public void createBishopMoves() {
        int i = curx;
        int j = cury;

        Debug.Log("bishop check1: " + i + " " + j);
        while (isCoordsInBounds(i + 1) && isCoordsInBounds(j+1))
        {

            Square curSquare = getSquare(i+1, j + 1);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i++;
            j++;

        }

        i = curx;
        j = cury;
        Debug.Log("bishop check2: " + i + " " + j);
        while (isCoordsInBounds(i - 1) && isCoordsInBounds(j-1))
        {
            Square curSquare = getSquare(i-1, j - 1);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i--;
            j--;
        }

        i = curx;
        j = cury;
        Debug.Log("bishop check3: " + i + " " + j);
        while (isCoordsInBounds(i - 1) && isCoordsInBounds(j+1))
        {
            Square curSquare = getSquare(i - 1, j+1);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i--;
            j++;
        }

        i = curx;
        j = cury;
        Debug.Log("bishop check4: " + i + " " + j);
        while (isCoordsInBounds(i + 1) && isCoordsInBounds(j-1))
        {
            Square curSquare = getSquare(i + 1, j-1);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i++;
            j--;
        }
    }

    public void createKnightMoves() { 
    }

    public void createRookMoves()
    {
        int i = cury;
  
            while (isCoordsInBounds(i + 1))
            {
                
                Square curSquare = getSquare(curx, i +1);
                if (curSquare.taken)
                {
                    if (color != curSquare.piece.color)
                    {
                        moves.Add(curSquare);
                        break;
                    }
                    break;
                }
                moves.Add(curSquare);
                i++;

            }

        i = cury;
        while (isCoordsInBounds(i -1)) 
        {
            Square curSquare = getSquare(curx, i -1);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i--;
        }

        i = curx;
        while (isCoordsInBounds(i -1 ))
        {
            Square curSquare = getSquare(i -1 , cury);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i--;
        }

        i = curx;
        while (isCoordsInBounds(i + 1))//!getSquare(i, cury).taken || 
        {
            Square curSquare = getSquare(i +1, cury);
            if (curSquare.taken)
            {
                if (color != curSquare.piece.color)
                {
                    moves.Add(curSquare);
                    break;
                }
                break;
            }
            moves.Add(curSquare);
            i++;
        }
    }

    public void createPawnMoves() {
        //if you havent moved can move twice, else can move once forward
        //check forward direction
        //can take at a diagonal forward
        //if firstmove = true
        //black color = 1 moves -y

        //white color = 2 moves +y

        int direction = color == 2 ? -1 : 1; 
  
            if (isCoordsInBounds(cury + direction))
            {
                Square curSquare = getSquare(curx, (cury + direction));
                if (curSquare != null && !curSquare.taken)
                {
                    moves.Add(curSquare);
                }
            }
            if (firstMove) {
                Square curSquare = getSquare(curx, (cury + (2 * direction)));
                if (curSquare != null && !curSquare.taken)
                {
                    moves.Add(curSquare);
                }
            }
            if (isCoordsInBounds(cury + direction)) {
                if (isCoordsInBounds(curx + direction)) {
                    Square curSquare = getSquare((curx +1) , (cury + direction));
                    if (curSquare != null && curSquare.taken)
                    {
                        if (color != curSquare.piece.color)
                        {
                            moves.Add(curSquare);
                        }
                    }
                }
                if (isCoordsInBounds(curx - direction))
                {
                    Square curSquare = getSquare((curx - 1), (cury + direction));
                    if (curSquare != null && curSquare.taken)
                    {
                        if (color != curSquare.piece.color)
                        {
                            moves.Add(curSquare);
                        }
                    }
                }
            }
        
    }
    public void returnpiece()
    {
        Transform t = this.transform;
        t.DOMove(new Vector3(last.transform.position.x, last.transform.position.y + 6f, last.transform.position.z), .5f);
        //  t.DOMove(new Vector3(last.transform.position.x, last.transform.position.y, last.transform.position.z), .3f);
    }

    public void pieceTaken()
    {
        Transform t = this.gameObject.transform;
        t.DOMove(hiddenIsland, .1f);
        t.DOComplete();
        curSquare.taken = false;
    }

    public void setLastPieceLocation(int x, int y)
    {
        lastx = x;
        lasty = y;
    }

    public void setPieceLocation(int x, int y)
    {
        curx = x;
        cury = y;
    }

    public bool getIsSet()
    {
        return isSet;
    }

    public void removePieceFromSquare()
    {
        curSquare.taken = false;
        curSquare.piece = null;
    }

    public Square getSquare(int x, int y)
    {
        if (isCoordsInBounds(x) && isCoordsInBounds(y))
        {
            return gm.boardRows[y].gameObject.transform.GetChild(x).gameObject.GetComponent<Square>();
        }
        else return null;
    }

    public void showMovesHelper()
    {
        foreach (Square move in moves)
        {
            getSquare(move.x, move.y).showMoveSquare.SetActive(true);
        }
    }

    public void hideMovesHelper()
    {
        foreach (Square move in moves)
        {
            getSquare(move.x, move.y).showMoveSquare.SetActive(false);
        }
        showMoves = false;
    }

    public void printMovesList()
    {
        Debug.Log("***********************MOVESLIST*START**************");
        foreach (Square move in moves)
        {
            Debug.Log(move.x + ", " + move.y + " ");
        }
        Debug.Log("***********************END**************************");
    }
    public bool isCoordsInBounds(int x)
    {
        if (x < gm.boardSize && x >= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Piece and Square Names
    public string printPieceName()
    {
        string outString = "";
        if (color == 1)
        {
            outString += "Black ";
        }
        else if (color == 2)
        {
            outString += "White ";
        }

        if (piece == 1)
        {
            outString += "Pawn";
        }
        else if (piece == 2)
        {
            outString += "Rook";
        }
        else if (piece == 3)
        {
            outString += "Knight";
        }
        else if (piece == 4)
        {
            outString += "Bishop";
        }
        else if (piece == 5)
        {
            outString += "Queen";
        }
        else if (piece == 6)
        {
            outString += "King";
        }
        return outString;
    }

    public string printCurSquare()
    {
        string outString = "";

        if (curx == 0)
        {
            outString += "A";
        }
        else if (curx == 1)
        {
            outString += "B";
        }
        else if (curx == 2)
        {
            outString += "C";
        }
        else if (curx == 3)
        {
            outString += "D";
        }
        else if (curx == 4)
        {
            outString += "E";
        }
        else if (curx == 5)
        {
            outString += "F";
        }
        else if (curx == 6)
        {
            outString += "G";
        }
        else if (curx == 7)
        {
            outString += "H";
        }
        else if (curx == 8)
        {
            outString += "I";
        }

        outString += cury + 1;
        return outString;
    }
}

/*	//This method is deals with the selection and movement of a piece
void OnMouseDrag() {
    //if (canMove) {

    curSquare.taken = false;
    curSquare.piece = null;
    Vector3 mousePosition = new Vector3 (Input.mousePosition.x, Input.mousePosition.y, pieceHeight);
    Vector3 objPosition = Camera.main.ScreenToWorldPoint (mousePosition);
    transform.position = objPosition;

    //}
}*/
/*
 * 
 * public bool checkRookPath(int x, int y){
		if (x == curx)
		{
			if (y < cury)
			{
				print("checking lesser y");
				for (int i = cury; i > y; i--)
				{
					//print(x.ToString() + i.ToString());
					// Checking the path of squares
					//Not getting the correct square
					//Coming back as null
					//Debug.Log(curx);//(gm.boardRows[curx].transform.GetChild(i).name);
					if (gm.boardRows[curx].gameObject.transform.GetChild(i).gameObject.GetComponent<Square>().taken)
						return false;
				
				}
				return true;
			}
			if (y > cury)
			{
				print("checking greater y");
				for (int i = cury; i < y; i++)
				{
					//print (x.ToString () + i.ToString ());
					// Checking the path of squares 		
					//if (gm.boardRows[curx].transform.GetChild(i).gameObject.GetComponent<Square>().taken) {
					//	return false;
					//}
					//print(gm.boardRows[curx].transform.GetChild(i).name);
					//print(x.ToString() + i.ToString());
					if (gm.boardRows[curx].gameObject.transform.GetChild(i).gameObject.GetComponent<Square>().taken)
						return false;
				}
				return true;
			}
			return true;
		}
		return true;
	}

	public void checkMove (int x, int y, Square square) {
		//1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king, 
		//Color: 1 Black, 2 White, 3 Green, 4 Blue, 5 Red, 6 Yellow

		//Need to check if spot is taken
		//Needs to check if the move causes a check
		if (piece == 6) { //King
			if (x == lastx && y == lasty) {
				movePiece (x, y, square); //Returning to previous square
			} else if (((x == lastx + 1) || (x == lastx) || (x == lastx - 1)) && ((y == lasty + 1) || (y == lasty) || (y == lasty - 1))) {
				movePiece (x, y, square);
				canMove = false;
			} else {
				returnpiece ();
			}
		} else if (piece == 2) { //Rook
			if (x == lastx && y == lasty) {
				movePiece (x, y, square);
			} else if (((x == lastx) && ((y > lasty) || (y < lasty))) || ((y == lasty) && ((x > lastx) || (x < lastx)))) {
				if (checkPath (x, y)) {
					movePiece (x, y, square);
					canMove = false;
				}

			} else {
				returnpiece ();
			}
		}
	}
		public bool checkPath (int x, int y) {
		//Testing to see what pieces are in the way
		//if move legal return true
		//for straight check each square if it is occupied, else check angle
		//else return false
		if (x == curx && y == cury) {
			return false;
		}else if (piece == 6) {//King
			if (((x == curx + 1) || (x == curx) || (x == curx - 1)) && ((y == cury + 1) || (y == cury) || (y == cury - 1))) {
	
				return true;
			} else {
				return false;
			}
		} else if (piece == 2){ //Rook
		if (((x == curx) && ((y > cury) || (y < cury))) || ((y == cury) && ((x > curx) || (x < curx)))) { //Availiable positions
				return checkRookPath(x, y); //Check any spaces inbetween //this should create a range be inclusive if it is an opposing piece
			} else {
				return false;
			}
		}
		return false;
	}
*/

/*
		(piece == 'R') {
		if (x == lastx) {
			if (y < lasty) {
				for (int i = lasty; i > y; i--) {
					print (x.ToString () + i.ToString ());
					// Checking the path of squares 		
					if (gm.boardPos [0, i - 1].gameObject.GetComponent<Square> ().taken) {
						return false;
					}
				}
				return true;
			}
		}
	}
}

//	return false;
//	}

//	}*/
