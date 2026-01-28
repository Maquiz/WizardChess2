using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Persistent singleton managing Photon connect/disconnect lifecycle.
/// Survives scene loads via DontDestroyOnLoad.
/// </summary>
public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    public static PhotonConnectionManager Instance { get; private set; }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        ConnectedToMaster,
        JoiningRoom,
        InRoom,
        Error
    }

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public string LastError { get; private set; }

    // Events
    public event Action OnConnectedToMasterEvent;
    public event Action<string> OnJoinedRoomEvent;
    public event Action OnOpponentJoinedEvent;
    public event Action<string> OnConnectionErrorEvent;
    public event Action OnOpponentLeftEvent;

    private bool intentionalDisconnect = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            State = ConnectionState.ConnectedToMaster;
            OnConnectedToMasterEvent?.Invoke();
            return;
        }

        State = ConnectionState.Connecting;
        intentionalDisconnect = false;
        PhotonNetwork.NickName = "Player_" + UnityEngine.Random.Range(1000, 9999);
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("[Photon] Connecting to Photon...");
    }

    public void CreateRoom(string code)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Photon] Not connected. Cannot create room.");
            return;
        }

        State = ConnectionState.JoiningRoom;
        RoomOptions opts = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = false,
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(code, opts);
        Debug.Log("[Photon] Creating private room: " + code);
    }

    public void JoinRoom(string code)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Photon] Not connected. Cannot join room.");
            return;
        }

        State = ConnectionState.JoiningRoom;
        PhotonNetwork.JoinRoom(code);
        Debug.Log("[Photon] Joining room: " + code);
    }

    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Photon] Not connected. Cannot join random room.");
            return;
        }

        State = ConnectionState.JoiningRoom;
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("[Photon] Joining random room...");
    }

    public void Disconnect()
    {
        intentionalDisconnect = true;
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        State = ConnectionState.Disconnected;
        Debug.Log("[Photon] Disconnected.");
    }

    // ========== PUN Callbacks ==========

    public override void OnConnectedToMaster()
    {
        State = ConnectionState.ConnectedToMaster;
        Debug.Log("[Photon] Connected to master server.");
        OnConnectedToMasterEvent?.Invoke();
    }

    public override void OnJoinedRoom()
    {
        State = ConnectionState.InRoom;
        string roomName = PhotonNetwork.CurrentRoom.Name;
        Debug.Log("[Photon] Joined room: " + roomName + " (" + PhotonNetwork.CurrentRoom.PlayerCount + " players)");
        OnJoinedRoomEvent?.Invoke(roomName);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[Photon] No random room found. Creating a new one...");
        RoomOptions opts = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(null, opts);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        State = ConnectionState.Error;
        LastError = "Failed to create room: " + message;
        Debug.LogWarning("[Photon] " + LastError);
        OnConnectionErrorEvent?.Invoke(LastError);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        State = ConnectionState.Error;
        LastError = "Failed to join room: " + message;
        Debug.LogWarning("[Photon] " + LastError);
        OnConnectionErrorEvent?.Invoke(LastError);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("[Photon] Opponent joined: " + newPlayer.NickName);
        OnOpponentJoinedEvent?.Invoke();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("[Photon] Opponent left: " + otherPlayer.NickName);
        OnOpponentLeftEvent?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        State = ConnectionState.Disconnected;
        if (!intentionalDisconnect)
        {
            LastError = "Disconnected: " + cause.ToString();
            Debug.LogWarning("[Photon] " + LastError);
            OnConnectionErrorEvent?.Invoke(LastError);
        }
        intentionalDisconnect = false;
    }
}
