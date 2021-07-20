using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class LobbyPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandlePlayerNameUpdate))] public string PlayerName;
    [SyncVar] public int ConnectionId;
    [SyncVar] public int playerNumber;

    [Header("Game Info")]
    public bool IsGameLeader = false;
    [SyncVar(hook = nameof(HandlePlayerReadyStatusUpdate))] public bool IsReady = false;
    [SyncVar(hook = nameof(HandleIsCommanderSelected))] public bool isCommanderSelected = false;
    [SyncVar(hook = nameof(HandleNameOfCommanderSelected))] public string nameOfCommanderSelected;

    [Header("UI")]
    [SerializeField] private GameObject PlayerLobyUI;
    [SerializeField] private GameObject Player1ReadyPanel;
    [SerializeField] private GameObject Player2ReadyPanel;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject changeCommanderButton;
    [SerializeField] private GameObject player1SelectCommanderButton;
    [SerializeField] private GameObject player2SelectCommanderButton;
    [SerializeField] private GameObject player1CommanderText;
    [SerializeField] private GameObject player2CommanderText;
    [SerializeField] private GameObject CommanderSelectionPanel;

    [Header("LobbyPlayers")]
    [SerializeField] private GameObject LocalLobbyPlayer;
    [SerializeField] private LobbyPlayer LocalLobbyPlayerScript;
    public bool isLocalPlayerFound = false;

    private const string PlayerPrefsNameKey = "PlayerName";

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

    // Start is called before the first frame update
    void Start()
    {
        GetLocalLobbyPlayer();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public override void OnStartAuthority()
    {
        CmdSetPlayerName(PlayerPrefs.GetString(PlayerPrefsNameKey));
        if (!PlayerLobyUI.activeInHierarchy)
            PlayerLobyUI.SetActive(true);
        Debug.Log("LobbyUI activated for: " + this.PlayerName);
        gameObject.name = "LocalLobbyPlayer";
        Debug.Log("Labeling the local player: " + this.PlayerName);
        CharacterSelectionManager.instance.FindLocalLobbyPlayer();        
    }
    [Command]
    private void CmdSetPlayerName(string playerName)
    {
        PlayerName = playerName;
        Debug.Log("Player display name set to: " + playerName);
    }
    public override void OnStartClient()
    {
        Game.LobbyPlayers.Add(this);
        Debug.Log("Added to GamePlayer list: " + this.PlayerName);
        UpdateLobbyUI();
    }
    public override void OnStopClient()
    {
        Debug.Log(PlayerName + " is quiting the game.");
        Game.LobbyPlayers.Remove(this);
        Debug.Log("Removed player from the GamePlayer list: " + this.PlayerName);
        UpdateLobbyUI();
    }
    public void UpdateLobbyUI()
    {
        Debug.Log("Updating UI for: " + this.PlayerName);
        GameObject localPlayer = GameObject.Find("LocalLobbyPlayer");
        if (localPlayer != null)
        {
            localPlayer.GetComponent<LobbyPlayer>().ActivateLobbyUI();
            localPlayer.GetComponent<LobbyPlayer>().CheckIfAllPlayersAreReady();
        }
    }
    public void ActivateLobbyUI()
    {
        Debug.Log("Activating lobby UI");
        if (!PlayerLobyUI.activeInHierarchy && !CharacterSelectionManager.instance.isPlayerViewCharacterSelection)
            PlayerLobyUI.SetActive(true);
        if (Game.LobbyPlayers.Count() > 0)
        {
            Player1ReadyPanel.SetActive(true);
            Debug.Log("Player1 Ready Panel activated");
            Player2ReadyPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Player1 Ready Panel not activated. Player count: " + Game.LobbyPlayers.Count().ToString());
        }
        if (Game.LobbyPlayers.Count() > 1)
        {
            Player2ReadyPanel.SetActive(true);
            Debug.Log("Player2 Ready Panel activated");
        }
        else
        {
            Debug.Log("Player2 Ready Panel not activated. Player count: " + Game.LobbyPlayers.Count().ToString());
        }
        UpdatePlayerReadyText();
    }
    public void UpdatePlayerReadyText()
    {
        Debug.Log("UpdatePlayerReadyText");
        if (Player1ReadyPanel.activeInHierarchy && Game.LobbyPlayers.Count() > 0)
        {
            foreach (Transform childText in Player1ReadyPanel.transform)
            {
                if (childText.name == "Player1Name")
                    childText.GetComponent<Text>().text = Game.LobbyPlayers[0].PlayerName;
                if (childText.name == "Player1ReadyText")
                {
                    bool isPlayerReady = Game.LobbyPlayers[0].IsReady;
                    if (isPlayerReady)
                    {
                        childText.GetComponent<Text>().text = "Ready";
                        childText.GetComponent<Text>().color = Color.green;
                    }
                    else
                    {
                        childText.GetComponent<Text>().text = "Not Ready";
                        childText.GetComponent<Text>().color = Color.red;
                    }
                }
                if (isLocalPlayerFound)
                {
                    if (childText.name == "SelectArmyCommander")
                    {
                        if (LocalLobbyPlayerScript.playerNumber == 1)
                        {
                            if (!LocalLobbyPlayerScript.isCommanderSelected)
                            {
                                childText.gameObject.SetActive(true);
                                readyButton.gameObject.SetActive(false);
                                player1CommanderText.SetActive(false);
                            }
                            else
                            {
                                childText.gameObject.SetActive(false);
                                readyButton.gameObject.SetActive(true);
                                player1CommanderText.SetActive(true);
                                if (!string.IsNullOrEmpty(LocalLobbyPlayerScript.nameOfCommanderSelected))
                                {
                                    player1CommanderText.gameObject.GetComponent<Text>().text = LocalLobbyPlayerScript.nameOfCommanderSelected;
                                }
                            }
                        }
                        else
                        {
                            if (Game.LobbyPlayers[0].isCommanderSelected)
                            {
                                player1CommanderText.SetActive(true);
                                if (!string.IsNullOrEmpty(Game.LobbyPlayers[0].nameOfCommanderSelected))
                                {
                                    player1CommanderText.gameObject.GetComponent<Text>().text = Game.LobbyPlayers[0].nameOfCommanderSelected;
                                }
                            }
                            else
                            {
                                player1CommanderText.SetActive(false);
                            }
                        }
                    }
                }
                
            }
        }
        if (Player2ReadyPanel.activeInHierarchy && Game.LobbyPlayers.Count() > 1)
        {
            foreach (Transform childText in Player2ReadyPanel.transform)
            {
                if (childText.name == "Player2Name")
                    childText.GetComponent<Text>().text = Game.LobbyPlayers[1].PlayerName;
                if (childText.name == "Player2ReadyText")
                {
                    bool isPlayerReady = Game.LobbyPlayers[1].IsReady;
                    if (isPlayerReady)
                    {
                        childText.GetComponent<Text>().text = "Ready";
                        childText.GetComponent<Text>().color = Color.green;
                    }
                    else
                    {
                        childText.GetComponent<Text>().text = "Not Ready";
                        childText.GetComponent<Text>().color = Color.red;
                    }
                }
                if (isLocalPlayerFound)
                {
                    if (childText.name == "SelectArmyCommander")
                    {
                        if (LocalLobbyPlayerScript.playerNumber == 2)
                        {
                            if (!LocalLobbyPlayerScript.isCommanderSelected)
                            {
                                childText.gameObject.SetActive(true);
                                readyButton.gameObject.SetActive(false);
                                player2CommanderText.SetActive(false);
                            }
                            else
                            {
                                childText.gameObject.SetActive(false);
                                readyButton.gameObject.SetActive(true);
                                player2CommanderText.SetActive(true);
                                if (!string.IsNullOrEmpty(LocalLobbyPlayerScript.nameOfCommanderSelected))
                                {
                                    player2CommanderText.gameObject.GetComponent<Text>().text = LocalLobbyPlayerScript.nameOfCommanderSelected;
                                }
                            }
                                
                        }
                        else
                        {
                            if (Game.LobbyPlayers[1].isCommanderSelected)
                            {
                                player2CommanderText.SetActive(true);
                                if (!string.IsNullOrEmpty(Game.LobbyPlayers[1].nameOfCommanderSelected))
                                {
                                    player2CommanderText.gameObject.GetComponent<Text>().text = Game.LobbyPlayers[1].nameOfCommanderSelected;
                                }
                            }
                            else
                            {
                                player2CommanderText.SetActive(false);
                            }
                        }
                    }
                }
                Debug.Log("Updated Player2 Ready panel with player name: " + Game.LobbyPlayers[1].PlayerName + " and ready status: " + Game.LobbyPlayers[1].IsReady);
            }
        }
        if (IsReady)
        {
            readyButton.GetComponentInChildren<Text>().text = "Unready";
        }
        else
        {
            readyButton.GetComponentInChildren<Text>().text = "Ready Up";
        }
    }
    public void CheckIfAllPlayersAreReady()
    {
        Debug.Log("Checking if all players are ready.");
        bool arePlayersReady = false;
        foreach (LobbyPlayer player in Game.LobbyPlayers)
        {
            if (!player.IsReady)
            {
                Debug.Log(player.PlayerName + "is not ready.");
                arePlayersReady = false;
                startGameButton.SetActive(false);
                break;
            }
            else
            {
                arePlayersReady = true;
            }

        }
        if (arePlayersReady)
            Debug.Log("All players are ready");

        if (arePlayersReady && IsGameLeader && Game.LobbyPlayers.Count() >= Game.minPlayers)
        {
            Debug.Log("All players are ready and minimum number of players in game. Activating the StartGame button on Game leader's UI.");
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }

    }
    public void HandlePlayerNameUpdate(string oldValue, string newValue)
    {
        Debug.Log("Player name has been updated for: " + oldValue + " to new value: " + newValue);
        UpdateLobbyUI();
    }
    public void HandlePlayerReadyStatusUpdate(bool oldValue, bool newValue)
    {
        Debug.Log("Player ready status has been has been updated for " + this.PlayerName + ": " + oldValue + " to new value: " + newValue);
        UpdateLobbyUI();
    }
    [Command]
    public void CmdReadyUp()
    {
        IsReady = !IsReady;
        Debug.Log("Ready status changed for: " + PlayerName);
    }
    [Command]
    public void CmdStartGame()
    {
        Game.StartGame();
    }
    public void QuitLobby()
    {
        if (hasAuthority)
        {
            if (IsGameLeader)
            {
                Game.StopHost();
            }
            else
            {
                Game.StopClient();
            }
        }
    }
    private void OnDestroy()
    {
        if (hasAuthority)
            TitleScreenManager.instance.ReturnToMainMenu();
        Debug.Log("LobbyPlayer destroyed. Returning to main menu.");
    }
    void GetLocalLobbyPlayer()
    {
        LocalLobbyPlayer = GameObject.Find("LocalLobbyPlayer");
        LocalLobbyPlayerScript = LocalLobbyPlayer.GetComponent<LobbyPlayer>();
        isLocalPlayerFound = true;
        UpdateLobbyUI();
    }
    public void SelectCommanderButtonPressed()
    {
        /*Debug.Log("SelectCommander button clicked");
        if (hasAuthority)
            CmdSelectCommander();
        */
        PlayerLobyUI.SetActive(false);
        CharacterSelectionManager.instance.ActivateCharacterSelectionUI();
        //CommanderSelectionPanel.SetActive(true);
        //UpdateLobbyUI();
    }
    [Command]
    void CmdSelectCommander(string CommanderToSelect, bool isPlayerSelectingCommander)
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        LobbyPlayer requestingPlayer = networkIdentity.GetComponent<LobbyPlayer>();
        Debug.Log("Executing CmdSelectCommander for " + requestingPlayer.PlayerName);
        Debug.Log("CmdSelectCommander: Commander name is: " + CommanderToSelect + " and player is selecting a commander: " + isPlayerSelectingCommander.ToString());
        if (isPlayerSelectingCommander)
        {
            Debug.Log("CmdSelectCommander: requesting player " + requestingPlayer.PlayerName + " is selecting a commander.");
            if (!requestingPlayer.isCommanderSelected)
            {
                requestingPlayer.HandleIsCommanderSelected(requestingPlayer.isCommanderSelected, true);
            }
            requestingPlayer.HandleNameOfCommanderSelected(requestingPlayer.nameOfCommanderSelected, CommanderToSelect);
        }
        else
        {
            Debug.Log("CmdSelectCommander: requesting player " + requestingPlayer.PlayerName + " is UNselecting a commander.");
            if (requestingPlayer.isCommanderSelected)
            {
                requestingPlayer.HandleIsCommanderSelected(requestingPlayer.isCommanderSelected, false);
            }
            requestingPlayer.HandleNameOfCommanderSelected(requestingPlayer.nameOfCommanderSelected, CommanderToSelect);
        }
                    

    }
    public void HandleIsCommanderSelected(bool oldValue, bool newValue)
    {
        Debug.Log("HandleIsCommanderSelected: " + newValue.ToString());
        if (isServer)
        {
            this.isCommanderSelected = newValue;
        }
        if (isClient)
        {
            UpdateLobbyUI();
            if (hasAuthority && !changeCommanderButton.activeInHierarchy && newValue)
            {
                UpdateLobbyUI();
                changeCommanderButton.SetActive(true);
            }                
        }
    }
    public void BackToLobby()
    {
        //CommanderSelectionPanel.SetActive(false);
        PlayerLobyUI.SetActive(true);
        UpdateLobbyUI();
    }
    public void SelectCommander(string CommanderToSelect, bool isPlayerSelectingCommander)
    {
        if (hasAuthority)
            CmdSelectCommander(CommanderToSelect, isPlayerSelectingCommander);
    }
    public void HandleNameOfCommanderSelected(string oldValue, string newValue)
    {
        Debug.Log("HandleNameOfCommanderSelected: " + newValue);
        if (isServer)
        {
            this.nameOfCommanderSelected = newValue;
        }
        if (isClient)
        {
            UpdateLobbyUI();
        }
    }
    public void ChangeCommanderButton()
    {
        if (LocalLobbyPlayerScript.IsReady)
            CmdReadyUp();
        SelectCommander("", false);
        if (changeCommanderButton.activeInHierarchy)
            changeCommanderButton.SetActive(false);
    }

}
