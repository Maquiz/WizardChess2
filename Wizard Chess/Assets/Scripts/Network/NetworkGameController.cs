using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// In-game move synchronization via Photon RPCs.
/// Handles color assignment, deck reconstruction, move/ability sync, and disconnect handling.
/// Attached to GameMaster at runtime when isOnlineMatch is true.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class NetworkGameController : MonoBehaviourPunCallbacks
{
    // Fixed scene ViewID — both clients must use the same ID for RPCs to route correctly.
    // AllocateSceneViewID only works on MasterClient, so we use a deterministic constant instead.
    private const int NETWORK_VIEW_ID = 901;

    private GameMaster gm;
    private PhotonView pv;
    private int localColor;

    public int LocalColor => localColor;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
        }

        // Both clients set the same fixed ViewID so RPCs route correctly
        if (pv.ViewID == 0)
        {
            pv.ViewID = NETWORK_VIEW_ID;
            Debug.Log("[Network] PhotonView assigned ViewID: " + NETWORK_VIEW_ID);
        }
    }

    /// <summary>
    /// Initialize the network controller. Must be called before deck setup.
    /// Assigns colors, sets camera, and rebuilds draft data from Photon properties.
    /// </summary>
    public void Init(GameMaster gameMaster)
    {
        gm = gameMaster;

        // Color assignment: master = White, joiner = Black
        if (PhotonNetwork.IsMasterClient)
        {
            localColor = ChessConstants.WHITE;
        }
        else
        {
            localColor = ChessConstants.BLACK;
        }
        MatchConfig.localPlayerColor = localColor;

        // Set camera to local player's perspective
        SetCameraForColor(localColor);

        // Build draft data from both players' custom properties
        BuildDraftDataFromProperties();

        Debug.Log("[Network] Initialized. Local color: " + (localColor == ChessConstants.WHITE ? "White" : "Black"));
    }

    /// <summary>
    /// Returns true when it's the remote player's turn (blocks local input).
    /// </summary>
    public bool IsRemotePlayerTurn()
    {
        if (gm == null) return false;
        return gm.currentMove != localColor;
    }

    /// <summary>
    /// Send a move to the remote player via RPC.
    /// </summary>
    public void SendMove(int fromX, int fromY, int toX, int toY)
    {
        if (pv == null || pv.ViewID == 0)
        {
            Debug.LogError("[Network] Cannot send move — PhotonView is invalid (ViewID=" + (pv != null ? pv.ViewID : 0) + ")");
            return;
        }
        pv.RPC("RPC_ReceiveMove", RpcTarget.Others, fromX, fromY, toX, toY);
        Debug.Log("[Network] Sent move: (" + fromX + "," + fromY + ") -> (" + toX + "," + toY + ") via ViewID " + pv.ViewID);
    }

    /// <summary>
    /// Send an ability use to the remote player via RPC.
    /// </summary>
    public void SendAbility(int pieceX, int pieceY, int targetX, int targetY)
    {
        if (pv == null || pv.ViewID == 0)
        {
            Debug.LogError("[Network] Cannot send ability — PhotonView is invalid (ViewID=" + (pv != null ? pv.ViewID : 0) + ")");
            return;
        }
        pv.RPC("RPC_ReceiveAbility", RpcTarget.Others, pieceX, pieceY, targetX, targetY);
        Debug.Log("[Network] Sent ability: piece(" + pieceX + "," + pieceY + ") -> target(" + targetX + "," + targetY + ") via ViewID " + pv.ViewID);
    }

    // ========== Resign / Draw ==========

    public void SendResign()
    {
        if (pv == null || pv.ViewID == 0) return;
        pv.RPC("RPC_ReceiveResign", RpcTarget.Others);
    }

    public void SendDrawOffer()
    {
        if (pv == null || pv.ViewID == 0) return;
        pv.RPC("RPC_ReceiveDrawOffer", RpcTarget.Others);
    }

    public void SendDrawResponse(bool accepted)
    {
        if (pv == null || pv.ViewID == 0) return;
        pv.RPC("RPC_ReceiveDrawResponse", RpcTarget.Others, accepted);
    }

    // ========== RPCs ==========

    [PunRPC]
    private void RPC_ReceiveResign()
    {
        if (gm.inGameMenuUI != null) gm.inGameMenuUI.OnOpponentResigned();
    }

    [PunRPC]
    private void RPC_ReceiveDrawOffer()
    {
        if (gm.inGameMenuUI != null) gm.inGameMenuUI.ShowDrawOffer();
    }

    [PunRPC]
    private void RPC_ReceiveDrawResponse(bool accepted)
    {
        if (gm.inGameMenuUI != null) gm.inGameMenuUI.OnDrawResponseReceived(accepted);
    }

    [PunRPC]
    private void RPC_ReceiveMove(int fromX, int fromY, int toX, int toY)
    {
        Debug.Log("[Network] Received move: (" + fromX + "," + fromY + ") -> (" + toX + "," + toY + ")");
        StartCoroutine(ExecuteRemoteMove(fromX, fromY, toX, toY));
    }

    [PunRPC]
    private void RPC_ReceiveAbility(int pieceX, int pieceY, int targetX, int targetY)
    {
        Debug.Log("[Network] Received ability: piece(" + pieceX + "," + pieceY + ") -> target(" + targetX + "," + targetY + ")");
        StartCoroutine(ExecuteRemoteAbility(pieceX, pieceY, targetX, targetY));
    }

    private IEnumerator ExecuteRemoteMove(int fromX, int fromY, int toX, int toY)
    {
        // Wait until element setup is complete (prevents race condition
        // where RPC arrives before DeckBasedSetup applies elements)
        yield return new WaitUntil(() => gm != null && gm.isSetupComplete);

        PieceMove piece = gm.boardState.GetPieceAt(fromX, fromY);
        if (piece == null)
        {
            Debug.LogError("[Network] No piece found at (" + fromX + "," + fromY + ")");
            yield break;
        }

        // Regenerate moves for state consistency
        piece.createPieceMoves(piece.piece);

        // Check for capture at destination
        PieceMove victim = gm.boardState.GetPieceAt(toX, toY);

        // Also check if there's a piece on the square (via Square.piece) for en passant etc.
        Square targetSquare = piece.getSquare(toX, toY);

        if (victim != null)
        {
            // Capture via piece at target position
            if (gm.TryCapture(piece, victim))
            {
                GameLogUI.LogCapture(gm.turnNumber, gm.currentMove, piece, victim, toX, toY);
                piece.movePiece(toX, toY, victim.curSquare);
            }
        }
        else if (targetSquare != null && targetSquare.taken && targetSquare.piece != null && targetSquare.piece.color != piece.color)
        {
            // Capture via square occupant (e.g., en passant target)
            PieceMove squareVictim = targetSquare.piece;
            if (gm.TryCapture(piece, squareVictim))
            {
                GameLogUI.LogCapture(gm.turnNumber, gm.currentMove, piece, squareVictim, toX, toY);
                piece.movePiece(toX, toY, targetSquare);
            }
        }
        else
        {
            // Normal move
            GameLogUI.LogPieceMove(gm.turnNumber, gm.currentMove, piece, toX, toY);
            if (targetSquare != null)
            {
                piece.movePiece(toX, toY, targetSquare);
            }
        }

        gm.deSelectPiece();
        gm.EndTurn();
    }

    private IEnumerator ExecuteRemoteAbility(int pieceX, int pieceY, int targetX, int targetY)
    {
        // Wait until element setup is complete
        yield return new WaitUntil(() => gm != null && gm.isSetupComplete);

        PieceMove piece = gm.boardState.GetPieceAt(pieceX, pieceY);
        if (piece == null)
        {
            Debug.LogError("[Network] No piece found at (" + pieceX + "," + pieceY + ") for ability");
            yield break;
        }

        if (piece.elementalPiece == null || piece.elementalPiece.active == null)
        {
            Debug.LogError("[Network] Piece at (" + pieceX + "," + pieceY + ") has no active ability");
            yield break;
        }

        Square target = piece.getSquare(targetX, targetY);
        if (target == null)
        {
            Debug.LogError("[Network] Invalid target square (" + targetX + "," + targetY + ")");
            yield break;
        }

        // Execute directly — skip validation (already validated on sender's side)
        bool success = piece.elementalPiece.active.Execute(piece, target, gm.boardState, gm.squareEffectManager);
        if (success)
        {
            piece.elementalPiece.cooldown.StartCooldown();
            GameLogUI.LogAbility(gm.turnNumber, gm.currentMove, piece, targetX, targetY);
            Debug.Log("[Network] Remote ability executed: piece(" + pieceX + "," + pieceY + ") -> target(" + targetX + "," + targetY + ")");
        }
        else
        {
            Debug.LogWarning("[Network] Remote ability execution failed at (" + targetX + "," + targetY + ")");
        }

        gm.deSelectPiece();
        gm.EndTurn();
    }

    // ========== Camera Setup ==========

    private void SetCameraForColor(int color)
    {
        CameraMove cam = FindFirstObjectByType<CameraMove>();
        Transform cameraTransform = null;

        if (cam != null && cam.C != null)
        {
            cameraTransform = cam.C;
        }
        else
        {
            // Fallback: find main camera directly
            Camera mainCam = Camera.main;
            if (mainCam != null) cameraTransform = mainCam.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("[Network] Could not find camera for color setup");
            return;
        }

        cameraTransform.position = new Vector3(3, 12, 3.5f);
        cameraTransform.localScale = Vector3.one;

        if (color == ChessConstants.WHITE)
        {
            cameraTransform.eulerAngles = new Vector3(90, 180, 0);
        }
        else
        {
            cameraTransform.eulerAngles = new Vector3(90, 0, 0);
        }

        Debug.Log("[Network] Camera set for " + (color == ChessConstants.WHITE ? "White" : "Black") +
                  " at " + cameraTransform.position);
    }

    // ========== Deck Reconstruction ==========

    private void BuildDraftDataFromProperties()
    {
        DraftData draft = new DraftData();

        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player player in players)
        {
            Hashtable props = player.CustomProperties;
            if (props == null || !props.ContainsKey("deck")) continue;

            string deckStr = (string)props["deck"];
            int[] elements = ParseDeckString(deckStr);

            // Master = White, non-master = Black
            int playerColor = player.IsMasterClient ? ChessConstants.WHITE : ChessConstants.BLACK;
            for (int i = 0; i < 16 && i < elements.Length; i++)
            {
                draft.SetElement(playerColor, i, elements[i]);
            }
        }

        MatchConfig.draftData = draft;
        MatchConfig.useDeckSystem = true;
        Debug.Log("[Network] Draft data rebuilt from Photon player properties.");
    }

    private int[] ParseDeckString(string deckStr)
    {
        int[] result = new int[16];
        if (string.IsNullOrEmpty(deckStr)) return result;

        string[] parts = deckStr.Split(',');
        for (int i = 0; i < 16 && i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out int val))
            {
                result[i] = val;
            }
            else
            {
                result[i] = ChessConstants.ELEMENT_FIRE;
            }
        }
        return result;
    }

    // ========== Disconnect Handling ==========

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("[Network] Opponent disconnected: " + otherPlayer.NickName);
        GameLogUI.LogEvent("<color=#FF4444>Opponent disconnected. You win!</color>");

        // Set game state to local player's win
        if (localColor == ChessConstants.WHITE)
        {
            gm.currentGameState = GameState.WhiteWins;
        }
        else
        {
            gm.currentGameState = GameState.BlackWins;
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[Network] Connection lost: " + cause.ToString());
        GameLogUI.LogEvent("<color=#FFAA44>Connection lost.</color>");
    }
}
