using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager instance;

    [Header("Lobby UI Elements")]
    [SerializeField] private GameObject FindLobbiesPanel;
    [SerializeField] private Text LobbyNameText;
    [SerializeField] private GameObject ContentPanel;
    [SerializeField] private GameObject PlayerListItemPrefab;
    [SerializeField] private Button ReadyUpButton;
    [SerializeField] private Button StartGameButton;

    [Header("Character Select UI Elements")]
    [SerializeField] private GameObject CommanderSelectionPanel;
    [SerializeField] private GameObject changeCommanderButton;

    public bool havePlayerListItemsBeenCreated = false;
    private List<PlayerListItem> playerListItems = new List<PlayerListItem>();
    public GameObject localLobbyPlayerObject;
    public LobbyPlayer localLobbyPlayerScript;


    public ulong currentLobbyId;
    // Start is called before the first frame update
    private NetworkManagerCC game;
    private NetworkManagerCC Game
    {
        get
        {
            if (game != null)
            {
                return game;
            }
            return game = NetworkManagerCC.singleton as NetworkManagerCC;
        }
    }
    void Awake()
    {
        MakeInstance();
        ReadyUpButton.gameObject.SetActive(true);
        ReadyUpButton.GetComponentInChildren<Text>().text = "Ready Up";
        StartGameButton.gameObject.SetActive(false);
        CommanderSelectionPanel.SetActive(false);
        FindLobbiesPanel.SetActive(true);
        changeCommanderButton.SetActive(false);
        ReadyUpButton.gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {

    }
    void MakeInstance()
    {
        if (instance == null)
            instance = this;
    }
    public void FindLocalLobbyPlayer()
    {
        localLobbyPlayerObject = GameObject.Find("LocalLobbyPlayer");
        localLobbyPlayerScript = localLobbyPlayerObject.GetComponent<LobbyPlayer>();
    }
    public void UpdateLobbyName()
    {
        Debug.Log("UpdateLobbyName");
        currentLobbyId = Game.GetComponent<SteamLobby>().current_lobbyID;
        string lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)currentLobbyId, "name");
        Debug.Log("UpdateLobbyName: new lobby name will be: " + lobbyName);
        LobbyNameText.text = lobbyName;
    }
    public void UpdateUI()
    {
        Debug.Log("Executing UpdateUI");
        if (!havePlayerListItemsBeenCreated)
            CreatePlayerListItems();
        if (playerListItems.Count < Game.LobbyPlayers.Count)
            CreateNewPlayerListItems();
        if (playerListItems.Count > Game.LobbyPlayers.Count)
            RemovePlayerListItems();
        if (playerListItems.Count == Game.LobbyPlayers.Count)
            UpdatePlayerListItems();
    }
    private void CreatePlayerListItems()
    {
        Debug.Log("Executing CreatePlayerListItems. This many players to create: " + Game.LobbyPlayers.Count.ToString());
        foreach (LobbyPlayer player in Game.LobbyPlayers)
        {
            Debug.Log("CreatePlayerListItems: Creating playerlistitem for player: " + player.PlayerName);
            GameObject newPlayerListItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem newPlayerListItemScript = newPlayerListItem.GetComponent<PlayerListItem>();

            newPlayerListItemScript.PlayerName = player.PlayerName;
            newPlayerListItemScript.ConnectionId = player.ConnectionId;
            newPlayerListItemScript.isPlayerReady = player.isPlayerReady;
            newPlayerListItemScript.SetPlayerListItemValues();


            newPlayerListItem.transform.SetParent(ContentPanel.transform);
            newPlayerListItem.transform.localScale = Vector3.one;

            playerListItems.Add(newPlayerListItemScript);
        }
        havePlayerListItemsBeenCreated = true;
    }
    private void CreateNewPlayerListItems()
    {
        Debug.Log("Executing CreateNewPlayerListItems");
        foreach (LobbyPlayer player in Game.LobbyPlayers)
        {
            if (!playerListItems.Any(b => b.ConnectionId == player.ConnectionId))
            {
                Debug.Log("CreateNewPlayerListItems: Player not found in playerListItems: " + player.PlayerName);
                GameObject newPlayerListItem = Instantiate(PlayerListItemPrefab) as GameObject;
                PlayerListItem newPlayerListItemScript = newPlayerListItem.GetComponent<PlayerListItem>();

                newPlayerListItemScript.PlayerName = player.PlayerName;
                newPlayerListItemScript.ConnectionId = player.ConnectionId;
                newPlayerListItemScript.isPlayerReady = player.isPlayerReady;
                newPlayerListItemScript.SetPlayerListItemValues();


                newPlayerListItem.transform.SetParent(ContentPanel.transform);
                newPlayerListItem.transform.localScale = Vector3.one;

                playerListItems.Add(newPlayerListItemScript);
            }
        }

    }
    private void RemovePlayerListItems()
    {
        List<PlayerListItem> playerListItemsToRemove = new List<PlayerListItem>();
        foreach (PlayerListItem playerListItem in playerListItems)
        {
            if (!Game.LobbyPlayers.Any(b => b.ConnectionId == playerListItem.ConnectionId))
            {
                Debug.Log("RemovePlayerListItems: player list item fro connection id: " + playerListItem.ConnectionId.ToString() + " does not exist in the game players list");
                playerListItemsToRemove.Add(playerListItem);
            }
        }
        if (playerListItemsToRemove.Count > 0)
        {
            foreach (PlayerListItem playerListItemToRemove in playerListItemsToRemove)
            {
                GameObject playerListItemToRemoveObject = playerListItemToRemove.gameObject;
                playerListItems.Remove(playerListItemToRemove);
                Destroy(playerListItemToRemoveObject);
                playerListItemToRemoveObject = null;
            }
        }
    }
    private void UpdatePlayerListItems()
    {
        Debug.Log("Executing UpdatePlayerListItems");
        foreach (LobbyPlayer player in Game.LobbyPlayers)
        {
            foreach (PlayerListItem playerListItemScript in playerListItems)
            {
                if (playerListItemScript.ConnectionId == player.ConnectionId)
                {
                    playerListItemScript.PlayerName = player.PlayerName;
                    playerListItemScript.isPlayerReady = player.isPlayerReady;
                    playerListItemScript.SetPlayerListItemValues();
                    if (player.isCommanderSelected)
                    {
                        playerListItemScript.commanderName = player.nameOfCommanderSelected;
                        playerListItemScript.SetCommanderNameText(true);
                    }
                    else
                    {
                        playerListItemScript.commanderName = "";
                        playerListItemScript.SetCommanderNameText(false);
                    }
                    
                    if (player == localLobbyPlayerScript)
                        ChangeReadyUpButtonText();
                }
            }
        }
        CheckIfAllPlayersAreReady();
    }
    public void PlayerReadyUp()
    {
        Debug.Log("Executing PlayerReadyUp");
        localLobbyPlayerScript.ChangeReadyStatus();
    }
    void ChangeReadyUpButtonText()
    {
        if (localLobbyPlayerScript.isPlayerReady)
            ReadyUpButton.GetComponentInChildren<Text>().text = "Unready";
        else
            ReadyUpButton.GetComponentInChildren<Text>().text = "Ready Up";
    }
    void CheckIfAllPlayersAreReady()
    {
        Debug.Log("Executing CheckIfAllPlayersAreReady");
        bool areAllPlayersReady = false;
        foreach (LobbyPlayer player in Game.LobbyPlayers)
        {
            if (player.isPlayerReady)
            {
                areAllPlayersReady = true;
            }
            else
            {
                Debug.Log("CheckIfAllPlayersAreReady: Not all players are ready. Waiting for: " + player.PlayerName);
                areAllPlayersReady = false;
                break;
            }
        }
        if (areAllPlayersReady)
        {
            Debug.Log("CheckIfAllPlayersAreReady: All players are ready!");
            if (localLobbyPlayerScript.IsGameLeader)
            {
                Debug.Log("CheckIfAllPlayersAreReady: Local player is the game leader. They can start the game now.");
                StartGameButton.gameObject.SetActive(true);
            }
        }
        else
        {
            if (StartGameButton.gameObject.activeInHierarchy)
                StartGameButton.gameObject.SetActive(false);
        }
    }
    public void DestroyPlayerListItems()
    {
        foreach (PlayerListItem playerListItem in playerListItems)
        {
            GameObject playerListItemObject = playerListItem.gameObject;
            Destroy(playerListItemObject);
            playerListItemObject = null;
        }
        playerListItems.Clear();
    }
    public void StartGame()
    {
        localLobbyPlayerScript.CanLobbyStartGame();
    }
    public void PlayerQuitLobby()
    {
        localLobbyPlayerScript.QuitLobby();
    }
    public void ChangeCommanderButtonPressed()
    {
        Debug.Log("Executing ChangeCommanderButtonPressed");
        if (localLobbyPlayerScript.isPlayerReady)
        {
            localLobbyPlayerScript.ChangeReadyStatus();
        }
        localLobbyPlayerScript.SelectCommander("", false);
        if (changeCommanderButton.activeInHierarchy)
            changeCommanderButton.SetActive(false);
    }
    public void ActivateChangeCommanderButton()
    {
        Debug.Log("Executing ActivateChangeCommanderButton");
        if (localLobbyPlayerScript.isCommanderSelected)
        {
            Debug.Log("ActivateChangeCommanderButton: Activating buttons");
            changeCommanderButton.SetActive(true);
            ReadyUpButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("ActivateChangeCommanderButton: DE-Activating buttons");
            changeCommanderButton.SetActive(false);
            ReadyUpButton.gameObject.SetActive(false);
        }
            
    }

}
