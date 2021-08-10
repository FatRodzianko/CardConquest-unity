using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using Steamworks;

public class LobbyPlayer : NetworkBehaviour
{
    [Header("GamePlayer Info")]
    [SyncVar(hook = nameof(HandlePlayerNameUpdate))] public string PlayerName;
    [SyncVar] public int ConnectionId;
    [SyncVar] public int playerNumber;
    [SyncVar] public ulong playerSteamId;
    [Header("Game Info")]
    [SyncVar] public bool IsGameLeader = false;
    [SyncVar(hook = nameof(HandlePlayerReadyStatusChange))] public bool isPlayerReady;
    [SyncVar(hook = nameof(HandleIsCommanderSelected))] public bool isCommanderSelected = false;
    [SyncVar(hook = nameof(HandleNameOfCommanderSelected))] public string nameOfCommanderSelected;
    public PlayerListItem myPlayerListItem;

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
    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalLobbyPlayer";
        LobbyManager.instance.FindLocalLobbyPlayer();
        LobbyManager.instance.UpdateLobbyName();
        CharacterSelectionManager.instance.FindLocalLobbyPlayer();
    }
    [Command]
    private void CmdSetPlayerName(string PlayerNameSubmitted)
    {
        Debug.Log("CmdSetPlayerName: Setting player name to: " + PlayerNameSubmitted);
        string playerNameToSet = "";
        if (PlayerNameSubmitted.Length > 12)
            playerNameToSet = PlayerNameSubmitted.Substring(0, 12);
        else
            playerNameToSet = PlayerNameSubmitted;
        this.HandlePlayerNameUpdate(this.PlayerName, playerNameToSet);
    }
    public override void OnStartClient()
    {
        Game.LobbyPlayers.Add(this);
        LobbyManager.instance.UpdateLobbyName();
        LobbyManager.instance.UpdateUI();
    }
    // Start is called before the first frame update
    void Start()
    {
        //DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void HandlePlayerNameUpdate(string oldValue, string newValue)
    {
        Debug.Log("Player name has been updated for: " + oldValue + " to new value: " + newValue);
        if (isServer)
            this.PlayerName = newValue;
        if (isClient)
        {
            LobbyManager.instance.UpdateUI();
        }

    }
    public void ChangeReadyStatus()
    {
        Debug.Log("Executing ChangeReadyStatus for player: " + this.PlayerName);
        if (hasAuthority)
            CmdChangePlayerReadyStatus();
    }
    [Command]
    void CmdChangePlayerReadyStatus()
    {
        Debug.Log("Executing CmdChangePlayerReadyStatus on the server for player: " + this.PlayerName);
        this.HandlePlayerReadyStatusChange(this.isPlayerReady, !this.isPlayerReady);
    }
    void HandlePlayerReadyStatusChange(bool oldValue, bool newValue)
    {
        if (isServer)
            this.isPlayerReady = newValue;
        if (isClient)
            LobbyManager.instance.UpdateUI();
    }
    public void CanLobbyStartGame()
    {
        if (hasAuthority)
            CmdCanLobbyStartGame();
    }
    [Command]
    void CmdCanLobbyStartGame()
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
        {
            LobbyManager.instance.DestroyPlayerListItems();
            SteamMatchmaking.LeaveLobby((CSteamID)LobbyManager.instance.currentLobbyId);
        }
        Debug.Log("LobbyPlayer destroyed. Returning to main menu.");
    }
    public override void OnStopClient()
    {
        Debug.Log(PlayerName + " is quiting the game.");
        Game.LobbyPlayers.Remove(this);
        Debug.Log("Removed player from the GamePlayer list: " + this.PlayerName);
        LobbyManager.instance.UpdateUI();
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
            if (hasAuthority)
            {
                //myPlayerListItem.ActivateSelectCommanderButton();
                LobbyManager.instance.ActivateChangeCommanderButton();
            }                
        }
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
            LobbyManager.instance.UpdateUI();
            //UpdateLobbyUI();
            //if (hasAuthority)
                //myPlayerListItem.SetCommanderNameText(newValue);
        }
    }
    public void SelectCommanderButtonPressed()
    {
        Debug.Log("SelectCommander button clicked");
      
        //PlayerLobyUI.SetActive(false);
        CharacterSelectionManager.instance.ActivateCharacterSelectionUI();
        //CommanderSelectionPanel.SetActive(true);

    }
    public void SelectCommander(string CommanderToSelect, bool isPlayerSelectingCommander)
    {
        if (hasAuthority)
            CmdSelectCommander(CommanderToSelect, isPlayerSelectingCommander);
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

}
