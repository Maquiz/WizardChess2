using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonNetwork;

public class Launcher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Connecting to Photon Network");

        roomJoinUI.setActive(false);
        buttonLoadArena.setActive(false);

        ConnectToPhoton();
    }

    void Awake() {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    //Helper Methods
    public void SetPlayerName(string name) {
        playerName = name; 
    }

    public void SetRoomName(string name) {
        roomName = name; 
    }

    void ConnectToPhoton() {
        connectionStatus.text = "Connecting...";
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void JoinRoom() {
        if (PhotonNetwork.IsConnected){
            PhotonNetwork.LocalPlayer.NickName = playerName;
            Debug.Log("PhotonNetwork.IsConnected! | Trying to Create/Join Room " + roomNameField.text);

            RoomOptions roomOptions = new RoomOptions();
            TypedLobby = typedLobby = new TypedLobby(SetRoomName, LobbyType.Default);
            PhotonNetwork.JoinOrCreateRoom(SetRoomName, roomOptions, typedLobby);
        }
    }

    public void LoadArena() {
        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            PhotonNetwork.LoadlLevel("MainArena");
        }
        else 
        {
            playerStatus.text = "Minimum 2 Players required to Load Arena!";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
