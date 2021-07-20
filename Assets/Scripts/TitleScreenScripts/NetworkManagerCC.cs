﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class NetworkManagerCC : NetworkManager
{
    [SerializeField] public int minPlayers = 2;
    [SerializeField] private LobbyPlayer lobbyPlayerPrefab;
    [SerializeField] private GamePlayer gamePlayerPrefab;
    public List<LobbyPlayer> LobbyPlayers { get; } = new List<LobbyPlayer>();
    public List<GamePlayer> GamePlayers { get; } = new List<GamePlayer>();
    public string CurrentGamePhase;
    public override void OnStartServer()
    {
        spawnPrefabs = Resources.LoadAll<GameObject>("Prefabs").ToList();
    }
    public override void OnStartClient()
    {
        Debug.Log("Starting client...");
        List<GameObject> spawnablePrefabs = Resources.LoadAll<GameObject>("Prefabs").ToList();
        Debug.Log("Spawnable Prefab count: " + spawnablePrefabs.Count());

        foreach (GameObject prefab in spawnablePrefabs)
        {
            ClientScene.RegisterPrefab(prefab);
            Debug.Log("Registering prefab: " + prefab);
        }
    }
    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("Client connected.");
        base.OnClientConnect(conn);
    }
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.Log("Client disconnected.");
        base.OnClientDisconnect(conn);
    }
    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("Connecting to server...");
        if (numPlayers >= maxConnections) // prevents players joining if the game is full
        {
            Debug.Log("Too many players. Disconnecting user.");
            conn.Disconnect();
            return;
        }
        if (SceneManager.GetActiveScene().name != "TitleScreen") // prevents players from joining a game that has already started. When the game starts, the scene will no longer be the "TitleScreen"
        {
            Debug.Log("Player did not load from correct scene. Disconnecting user. Player loaded from scene: " + SceneManager.GetActiveScene().name);
            conn.Disconnect();
            return;
        }
        Debug.Log("Server Connected");
    }
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("Checking if player is in correct scene. Player's scene name is: " + SceneManager.GetActiveScene().name.ToString() + ". Correct scene name is: TitleScreen");
        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            bool isGameLeader = LobbyPlayers.Count == 0; // isLeader is true if the player count is 0, aka when you are the first player to be added to a server/room

            LobbyPlayer lobbyPlayerInstance = Instantiate(lobbyPlayerPrefab);

            lobbyPlayerInstance.IsGameLeader = isGameLeader;
            lobbyPlayerInstance.ConnectionId = conn.connectionId;
            lobbyPlayerInstance.playerNumber = LobbyPlayers.Count + 1;

            NetworkServer.AddPlayerForConnection(conn, lobbyPlayerInstance.gameObject);
            Debug.Log("Player added. Player name: " + lobbyPlayerInstance.PlayerName + ". Player connection id: " + lobbyPlayerInstance.ConnectionId.ToString());
        }
    }
    public void StartGame()
    {
        if (CanStartGame() && SceneManager.GetActiveScene().name == "TitleScreen")
        {
            ServerChangeScene("Gameplay");
        }
    }
    private bool CanStartGame()
    {
        if (numPlayers < minPlayers)
            return false;
        foreach (LobbyPlayer player in LobbyPlayers)
        {
            if (!player.IsReady)
                return false;
        }
        return true;
    }
    public override void ServerChangeScene(string newSceneName)
    {
        //Changing from the menu to the scene
        if (SceneManager.GetActiveScene().name == "TitleScreen" && newSceneName == "Gameplay")
        {
            Debug.Log("Changing scene to: " + newSceneName);
            for (int i = LobbyPlayers.Count - 1; i >= 0; i--)
            {
                var conn = LobbyPlayers[i].connectionToClient;
                var gamePlayerInstance = Instantiate(gamePlayerPrefab);

                gamePlayerInstance.SetPlayerName(LobbyPlayers[i].PlayerName);
                gamePlayerInstance.SetConnectionId(LobbyPlayers[i].ConnectionId);
                gamePlayerInstance.SetPlayerNumber(LobbyPlayers[i].playerNumber);
                gamePlayerInstance.SetPlayerNameOfCommander(LobbyPlayers[i].nameOfCommanderSelected);

                NetworkServer.Destroy(conn.identity.gameObject);
                NetworkServer.ReplacePlayerForConnection(conn, gamePlayerInstance.gameObject, true);
                Debug.Log("Spawned new GamePlayer: " + gamePlayerInstance.PlayerName);
            }
            CurrentGamePhase = "Unit Placement";
        }
        base.ServerChangeScene(newSceneName);
    }
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.identity != null)
        {
            LobbyPlayer player = conn.identity.GetComponent<LobbyPlayer>();
            LobbyPlayers.Remove(player);
        }
        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        LobbyPlayers.Clear();
        GamePlayers.Clear();
    }
    public void HostShutDownServer()
    {
        GameObject NetworkManagerObject = GameObject.Find("NetworkManager");
        Destroy(NetworkManagerObject);
        Shutdown();
        //SceneManager.LoadScene("empty");
        
        Start();

    }
}
