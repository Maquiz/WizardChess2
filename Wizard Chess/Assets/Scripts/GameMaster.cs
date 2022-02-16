using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GameMaster : MonoBehaviour
{

    //Physical Board Components
    //Player Controlled - Can B more than 2 colors
    public GameObject[] BPieces;
    //Black Piece Array
    public GameObject[] WPieces;
    //white Piece Array
    [SerializeField] public GameObject[,] boardPos;
    public GameObject[] boardRows;
    public GameObject Board;
    private Vector3 hiddenIsland = new Vector3(-1000f, -1000f, -1000f);
    public int boardSize;
    public int currentMove = 2;
    //Assuming the board is 8x8
    public string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
    enum PieceType
    {
        PAWN, ROOK, KNIGHT, BISHOP, QUEEN, KING
    }
    enum PieceColor
    {
        BLACK, WHITE
    }

    //UI
    public Canvas boardUI;
    enum MouseUI
    {
        CANMOVE, CANTMOVE, TAKEPIECE, START
    }

    public PieceUI selectedUI, canMoveUI, cantMoveUI, takeMoveUI;
    public LineRenderer lr;
    public Material lineMaterial;

    //Logic Vars
    public bool isPieceSelected;
    public PieceMove selectedPiece;
    public List<ChessMove> moveHistory;

    //Instantiated objects
    public GameObject blackSquare;
    public GameObject whiteSquare;
    public GameObject[] pieces;
    public GameObject[] pieceUI;
    private Transform lastHittedObject = null;
    public bool showMoves;

    void Start()
    {
        boardPos = new GameObject[boardSize, boardSize];
        boardSize = 8;
        createBoard(boardSize);
        moveHistory = new List<ChessMove>();
        lr = this.gameObject.AddComponent<LineRenderer>();
        swapUIIcon(MouseUI.START);
    }


    //Game LOOP
    void Update()
    {
        //All one player how do we turn this into multiplayer?

        //Maybe these controls should be in a Player object instead of game master
        //Checking the mouses click down position should only be accessable on a players turn
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (isPieceSelected)
        {
            if (Input.GetMouseButtonDown(1))
            {
                selectedPiece.hideMovesHelper();
                deSelectPiece();
                return;
            }
            //Line renderer
            setUpLine();

            lr.SetPosition(1, Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
            lr.alignment = LineAlignment.View;
            //PieceMove checkMove(int x,int y, Square square) checkSquare with raycast
            //Physics.Raycast(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)), new Vector3(0,-1, 0), out hit)
            //if(Physics.Raycast(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)), new Vector3(0,-1, 0),out hit)){
            Transform lastHittedObject = null;
            if (Physics.Raycast(ray, out hit))
            {
                
               
               // Debug.Log("Last Hitted " + lastHittedObject +"hit collider" + hit.collider.transform);
                

                if (lastHittedObject != hit.collider.transform)
                {
                   
                    //Selecting a piece
                    //Debug.Log("update square");
                    if (hit.collider.gameObject.tag == "Piece")
                    {

                        PieceMove p = hit.collider.gameObject.GetComponent<PieceMove>();
                        if ((p.color != selectedPiece.color) && selectedPiece.checkMoves(p.curx, p.cury))
                        {
                            swapUIIcon(MouseUI.TAKEPIECE);
                            if (Input.GetMouseButtonDown(0))
                            {
                                takePiece(p);
                                selectedPiece.movePiece(p.curx, p.cury, p.curSquare);
                                currentMove = currentMove == 1 ? 2 : 1;
                                deSelectPiece();
                            }
                        }
                    }

                    //Move to location & sent piece to grave yard ?  graveyard a physical location where all pieces line up in order they die or in order they are placed on board

                    //if it is 
                    else if (hit.collider.gameObject.tag == "Board")
                    {
                        //Debug.Log("Check Board");
                        Square s = hit.collider.gameObject.GetComponent<Square>();
                        if (selectedPiece != null)
                        {
                            if (s.taken && (s.piece.color != selectedPiece.color) && selectedPiece.checkMoves(s.x, s.y))
                            {
                                swapUIIcon(MouseUI.TAKEPIECE);
                                if (Input.GetMouseButtonDown(0))
                                {
                                    takePiece(s.piece);
                                    selectedPiece.movePiece(s.piece.curx, s.piece.cury, s.piece.curSquare);
                                    currentMove = currentMove == 1 ? 2 : 1;
                                    deSelectPiece();
                                }
                            }

                            else if (selectedPiece.checkMoves(s.x, s.y))
                            {
                                swapUIIcon(MouseUI.CANMOVE);
                                //Move Piece to non occupied spot
                                if (Input.GetMouseButtonDown(0))
                                {
                                    selectedPiece.movePiece(selectedPiece.lastx, selectedPiece.lasty, s);
                                    currentMove = currentMove == 1 ? 2 : 1;
                                    deSelectPiece();
                                }
                            }
                            else
                            {
                                swapUIIcon(MouseUI.CANTMOVE);
                            }
                        }
                    }
                    else
                    {
                        swapUIIcon(MouseUI.CANTMOVE);
                    }
                    lastHittedObject = hit.collider.transform;
                }
                

                else if (lastHittedObject) {
                    lastHittedObject = null;
                }

            }
        }
    }

    //Game Piece Control
    void deSelectPiece()
    {
        //only works for 2 colors
        selectedPiece = null;
        swapUIIcon(MouseUI.START);
        lr.SetPosition(1, hiddenIsland);
        lr.SetPosition(0, hiddenIsland);
        isPieceSelected = false;
        lr.enabled = false;
    }

    public void selectPiece(Transform t, PieceMove piece)
    {
        lr.enabled = true;
        isPieceSelected = true;
        selectedUI.setTransformPosition(t.position);
        selectedPiece = piece;
        selectedPiece.createPieceMoves(selectedPiece.piece);
        selectedPiece.printMovesList();
    }

    public void takePiece(PieceMove p)
    {
        //Currently just moves the piece doesnt score or move it into a position where it can come back
        p.pieceTaken();
    }

    //Game Creation
    private void createPiece(int piece, int color)
    { //1 pawn, 2 rook, 3 knight, 4 bishop, 5 queen, 6 king, 
        /*											//Color: 1 Black, 2 White, 3 Green, 4 Blue, 5 Red, 6 Yellow
		if (piece == 1) {
			if (color == 1) {
				//Instantiate piece

				pieceMeshCollider.sharedMesh = 

				}
			else if (color == 2) {
				createdPC = PieceColor.WHITE;
			}

		}
		else if (piece == 2) {
			createdPT = PieceType.ROOK;
		}
		else if (piece == 3) {
			createdPT = PieceType.KNIGHT;
		}
		else if (piece == 4) {
			createdPT = PieceType.BISHOP;
		}
		else if (piece == 5) {
			createdPT = PieceType.QUEEN;
		}
		else if (piece == 6) {
			createdPT = PieceType.KING;
		}
		*/
        //Instantiate (pieces[piece-1], , Quaternion.identity);


        //create new piece
        // attatch ui 
    }


    private void createBoard(int size)
    {
        Vector3 boardSquarePos = new Vector3(0.0f, 0.0f, 0.0f);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i % 2 == 0)
                {
                    if (j % 2 == 0)
                    {
                        boardPos[i, j] = Instantiate(blackSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();

                    }
                    else
                    {
                        boardPos[i, j] = (GameObject)Instantiate(whiteSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                }
                else
                {
                    if (j % 2 == 0)
                    {
                        boardPos[i, j] = (GameObject)Instantiate(whiteSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                    else
                    {
                        boardPos[i, j] = (GameObject)Instantiate(blackSquare, boardSquarePos, Quaternion.identity);
                        boardPos[i, j].GetComponent<Square>().x = j;
                        boardPos[i, j].GetComponent<Square>().y = i;
                        boardPos[i, j].name = letters[i] + (j + 1).ToString();
                    }
                }
                boardSquarePos.x += 1;

                boardPos[i, j].transform.parent = boardRows[i].transform;
            }
            boardSquarePos.z += 1;
            boardSquarePos.x = 0;
        }
    }

    //UI
    void setUpLine()
    {
        lr.alignment = LineAlignment.View;
        lr.endWidth = 0.50f;
        lr.startWidth = 0.5f;
        lr.positionCount = 2;
        lr.SetPosition(0, Camera.main.ScreenToWorldPoint((selectedUI.gameObject.transform.position)));
        lr.material = lineMaterial;
    }

    void swapUIIcon(MouseUI m)
    {
        if (m == MouseUI.CANMOVE)
        {
            takeMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.CANTMOVE)
        {
            takeMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.TAKEPIECE)
        {
            cantMoveUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            takeMoveUI.followTarget(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 8.5f)));
        }

        if (m == MouseUI.START)
        {
            selectedUI.setTransformPosition(hiddenIsland);
            canMoveUI.setTransformPosition(hiddenIsland);
            cantMoveUI.setTransformPosition(hiddenIsland);
            takeMoveUI.setTransformPosition(hiddenIsland);
        }
    }
}