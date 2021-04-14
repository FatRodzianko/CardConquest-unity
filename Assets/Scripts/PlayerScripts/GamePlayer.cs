using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class GamePlayer : NetworkBehaviour
{
    [Header("Player Info")]
    [SyncVar] public string PlayerName;
    [SyncVar] public int ConnectionId;
    [SyncVar] public int playerNumber;

    [Header("Player Unit Prefabs")]
    [SerializeField] GameObject Player1UnitHolder;
    [SerializeField] GameObject Player2UnitHolder;
    [SerializeField] GameObject Player1Inf;
    [SerializeField] GameObject Player1Tank;
    [SerializeField] GameObject Player2Inf;
    [SerializeField] GameObject Player2Tank;

    [Header("Player Card Prefabs")]
    [SerializeField] GameObject PlayerCardHand;
    [SerializeField] GameObject[] Cards;

    [Header("Player Base/Units")]
    [SyncVar] public GameObject myPlayerBase;
    public GameObject myUnitHolder;
    public GameObject myPlayerCardHand;
    public SyncList<uint> playerUnitNetIds = new SyncList<uint>();
    public SyncList<uint> playerCardHandNetIds = new SyncList<uint>();

    [Header("Player Statuses")]
    [SyncVar] public bool HaveSpawnedUnits = false;
    [SyncVar] public bool GotPlayerBase = false;
    [SyncVar] public bool HaveSpawnedCards = false;
    [SyncVar(hook = nameof(HandlePlayerReadyStatusUpdate))] public bool ReadyForNextPhase = false;

    [Header("Player Battle Info")]
    //public SyncList<uint> battleSiteNetIds = new SyncList<uint>();
    //[SyncVar(hook = nameof(HandleBattleSitesSet))] bool BattleSitesHaveBeenSet = false;
    [SyncVar(hook = nameof(HandleUpdatedUnitPositionsForBattleSites))] public bool updatedUnitPositionsForBattleSites = false;
    public SyncList<uint> playerArmyNetIds = new SyncList<uint>();
    [SyncVar] public int playerArmyNumberOfInf;
    [SyncVar] public int playerArmyNumberOfTanks;
    [SyncVar] public int playerBattleScore;
    [SyncVar(hook = nameof(HandleBattleScoreSet))] public bool isPlayerBattleScoreSet = false;

    [Header("Battle Card Info")]
    [SyncVar(hook = nameof(HandleUpdatedPlayerBattleCard))] public uint playerBattleCardNetId;
    public GameObject selectedCard;

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
        gameObject.name = "LocalGamePlayer";
        gameObject.tag = "LocalGamePlayer";
        Debug.Log("Labeling the local player: " + this.PlayerName);
    }
    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        Game.GamePlayers.Add(this);
        Debug.Log("Added to GamePlayer list: " + this.PlayerName);
        //SpawnPlayerUnits();

    }
    public override void OnStopClient()
    {
        Debug.Log(PlayerName + " is quiting the game.");
        Game.GamePlayers.Remove(this);
        Debug.Log("Removed player from the GamePlayer list: " + this.PlayerName);
    }
    [Server]
    public void SetPlayerName(string playerName)
    {
        this.PlayerName = playerName;
    }
    [Server]
    public void SetConnectionId(int connId)
    {
        this.ConnectionId = connId;
    }
    [Server]
    public void SetPlayerNumber(int playerNum)
    {
        this.playerNumber = playerNum;
    }

    void Start()
    {
        //NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        Debug.Log("Start for: " + this.PlayerName);
        //SpawnPlayerUnits();       
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetCurrentGamePhase()
    {
        if (hasAuthority)
            CmdGetCurrentGamePhaseFromServer();
    }
    [Command]
    void CmdGetCurrentGamePhaseFromServer()
    {
        TargetSetCurrentGamePhase(connectionToClient, Game.CurrentGamePhase);
    }
    [TargetRpc]
    void TargetSetCurrentGamePhase(NetworkConnection target, string serverGamePhase)
    {
        Debug.Log("Current game phase fromthe server is: " + serverGamePhase);
        GameplayManager.instance.currentGamePhase = serverGamePhase;
        GameplayManager.instance.SetGamePhaseText();
    }
    
    public void SpawnPlayerUnits()
    {
        if (!this.HaveSpawnedUnits)
        {
            Debug.Log("SpawnPlayerUnits() for: " + this.PlayerName + " with player number: " + this.playerNumber);
            CmdSpawnPlayerUnits();
        }        
    }
    [Command]
    public void CmdSpawnPlayerUnits()
    {
        Debug.Log("Running CmdSpawnPlayerUnits on the server.");
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        if (requestingPlayer.playerNumber == 1 && !requestingPlayer.HaveSpawnedUnits)
        {
            //Instantiate the unit holder
            GameObject playerUnitHolder = Instantiate(Player1UnitHolder, transform.position, Quaternion.identity);
            //Get the unit holder's script to set the owner variables
            PlayerUnitHolder script = playerUnitHolder.GetComponent<PlayerUnitHolder>();
            script.ownerPlayerName = requestingPlayer.PlayerName;
            script.ownerConnectionId = requestingPlayer.ConnectionId;
            script.ownerPlayerNumber = requestingPlayer.playerNumber;
            //Spawn the unity holder on the network and assign owner/authority to the requesting client
            NetworkServer.Spawn(playerUnitHolder, connectionToClient);
            //Spawn the player1 infantry units
            for (int i = 0; i < 6; i++)
            {
                GameObject playerInfantry = Instantiate(Player1Inf, transform.position, Quaternion.identity);
                UnitScript unitScript = playerInfantry.GetComponent<UnitScript>();
                unitScript.ownerPlayerName = requestingPlayer.PlayerName;
                unitScript.ownerConnectionId = requestingPlayer.ConnectionId;
                unitScript.ownerPlayerNumber = requestingPlayer.playerNumber;
                NetworkServer.Spawn(playerInfantry, connectionToClient);
                requestingPlayer.playerUnitNetIds.Add(playerInfantry.GetComponent<NetworkIdentity>().netId);
            }
            //Spawn player1 tanks
            for (int i = 0; i < 4; i++)
            {
                GameObject playerTank = Instantiate(Player1Tank, transform.position, Quaternion.identity);
                UnitScript unitScript = playerTank.GetComponent<UnitScript>();
                unitScript.ownerPlayerName = requestingPlayer.PlayerName;
                unitScript.ownerConnectionId = requestingPlayer.ConnectionId;
                unitScript.ownerPlayerNumber = requestingPlayer.playerNumber;
                NetworkServer.Spawn(playerTank, connectionToClient);
                requestingPlayer.playerUnitNetIds.Add(playerTank.GetComponent<NetworkIdentity>().netId);
            }
            requestingPlayer.HaveSpawnedUnits = true;
            //Tell all clients to "show" the PlayerUnitHolder - set the correct parent to all the unity holders and run GameplayManager's PutUnitsInUnitBox
            RpcShowSpawnedPlayerUnits(playerUnitHolder);
            Debug.Log("Spawned Player1UnitHolder.");
        }
        else if (requestingPlayer.playerNumber == 2 && !requestingPlayer.HaveSpawnedUnits)
        {
            GameObject playerUnitHolder = Instantiate(Player2UnitHolder, transform.position, Quaternion.identity);
            PlayerUnitHolder script = playerUnitHolder.GetComponent<PlayerUnitHolder>();
            script.ownerPlayerName = requestingPlayer.PlayerName;
            script.ownerConnectionId = requestingPlayer.ConnectionId;
            script.ownerPlayerNumber = requestingPlayer.playerNumber;
            NetworkServer.Spawn(playerUnitHolder, connectionToClient);
            for (int i = 0; i < 6; i++)
            {
                GameObject playerInfantry = Instantiate(Player2Inf, transform.position, Quaternion.identity);
                UnitScript unitScript = playerInfantry.GetComponent<UnitScript>();
                unitScript.ownerPlayerName = requestingPlayer.PlayerName;
                unitScript.ownerConnectionId = requestingPlayer.ConnectionId;
                unitScript.ownerPlayerNumber = requestingPlayer.playerNumber;
                NetworkServer.Spawn(playerInfantry, connectionToClient);
                requestingPlayer.playerUnitNetIds.Add(playerInfantry.GetComponent<NetworkIdentity>().netId);
            }
            //Spawn player1 tanks
            for (int i = 0; i < 4; i++)
            {
                GameObject playerTank = Instantiate(Player2Tank, transform.position, Quaternion.identity);
                UnitScript unitScript = playerTank.GetComponent<UnitScript>();
                unitScript.ownerPlayerName = requestingPlayer.PlayerName;
                unitScript.ownerConnectionId = requestingPlayer.ConnectionId;
                unitScript.ownerPlayerNumber = requestingPlayer.playerNumber;
                NetworkServer.Spawn(playerTank, connectionToClient);
                requestingPlayer.playerUnitNetIds.Add(playerTank.GetComponent<NetworkIdentity>().netId);
            }
            requestingPlayer.HaveSpawnedUnits = true;
            RpcShowSpawnedPlayerUnits(playerUnitHolder);
            Debug.Log("Spawned Player2UnitHolder.");
        }
        else
        {
            Debug.Log("NO PlayerUnitHolder spawned.");
        }
    }
    [ClientRpc]
    void RpcShowSpawnedPlayerUnits(GameObject playerUnitHolder)
    {
        Debug.Log("You: " + this.PlayerName + " are running RpcShowSpawnedPlayerUnits()");
        GameObject[] infantryUnits = GameObject.FindGameObjectsWithTag("infantry");
        GameObject[] tankUnits = GameObject.FindGameObjectsWithTag("tank");

        if (playerUnitHolder.GetComponent<NetworkIdentity>().hasAuthority && hasAuthority)
        {
            Debug.Log("You: " + this.PlayerName + " have authority over: " + playerUnitHolder);
            playerUnitHolder.SetActive(true);
            playerUnitHolder.transform.SetParent(gameObject.transform);
            PlayerUnitHolder unitHolderScript = playerUnitHolder.GetComponent<PlayerUnitHolder>();
            myUnitHolder = playerUnitHolder;

            foreach (GameObject inf in infantryUnits)
            {
                UnitScript infScript = inf.GetComponent<UnitScript>();
                if (unitHolderScript.ownerConnectionId == infScript.ownerConnectionId)
                {
                    inf.transform.SetParent(playerUnitHolder.transform);
                }
            }
            
            foreach (GameObject tank in tankUnits)
            {
                UnitScript tankScript = tank.GetComponent<UnitScript>();
                if (unitHolderScript.ownerConnectionId == tankScript.ownerConnectionId)
                {
                    tank.transform.SetParent(playerUnitHolder.transform);
                }
            }
            //HaveSpawnedUnits = true;
            GameplayManager.instance.PutUnitsInUnitBox();
        }
        else 
        {
            Debug.Log("You: " + this.PlayerName + " DO NOT have authority over: " + playerUnitHolder);
        }

        GameObject[] PlayerUnitHolders = GameObject.FindGameObjectsWithTag("PlayerUnitHolder");
        foreach (GameObject unitHolder in PlayerUnitHolders)
        {
            PlayerUnitHolder unitHolderScript = unitHolder.GetComponent<PlayerUnitHolder>();
            GameObject[] gamePlayers = GameObject.FindGameObjectsWithTag("GamePlayer");
            foreach (GameObject gamePlayer in gamePlayers)
            {
                GamePlayer gamePlayerScript = gamePlayer.GetComponent<GamePlayer>();
                if (gamePlayerScript.ConnectionId == unitHolderScript.ownerConnectionId)
                {
                    foreach (GameObject inf in infantryUnits)
                    {
                        UnitScript infScript = inf.GetComponent<UnitScript>();
                        if (gamePlayerScript.ConnectionId == unitHolderScript.ownerConnectionId && unitHolderScript.ownerConnectionId == infScript.ownerConnectionId)
                        {
                            inf.transform.SetParent(unitHolder.transform);
                            inf.transform.position = new Vector3(-1000, -1000, 0);
                        }
                    }

                    foreach (GameObject tank in tankUnits)
                    {
                        UnitScript tankScript = tank.GetComponent<UnitScript>();
                        if (gamePlayerScript.ConnectionId == unitHolderScript.ownerConnectionId && unitHolderScript.ownerConnectionId == tankScript.ownerConnectionId)
                        {
                            tank.transform.SetParent(unitHolder.transform);
                            tank.transform.position = new Vector3(-1000, -1000, 0);
                        }
                    }
                    unitHolder.transform.SetParent(gamePlayer.transform);
                    gamePlayerScript.myUnitHolder = unitHolder;
                    //unitHolder.SetActive(false);
                }
            }
        }
    }
    public void SpawnPlayerCards()
    {
        if (!this.HaveSpawnedCards)
        {
            Debug.Log("SpawnPlayerUnits() for: " + this.PlayerName + " with player number: " + this.playerNumber);
            CmdSpawnPlayerCards();
        }
    }
    [Command]
    void CmdSpawnPlayerCards()
    {
        Debug.Log("Running CmdSpawnPlayerCards on the server.");
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        if (!requestingPlayer.HaveSpawnedCards)
        {
            //Instantiate the unit holder
            GameObject playerHand = Instantiate(PlayerCardHand, transform.position, Quaternion.identity);
            //Get the unit holder's script to set the owner variables
            PlayerHand playerHandScript = playerHand.GetComponent<PlayerHand>();
            playerHandScript.ownerPlayerName = requestingPlayer.PlayerName;
            playerHandScript.ownerConnectionId = requestingPlayer.ConnectionId;
            playerHandScript.ownerPlayerNumber = requestingPlayer.playerNumber;
            //Spawn the unity holder on the network and assign owner/authority to the requesting client
            NetworkServer.Spawn(playerHand, connectionToClient);
            foreach (GameObject card in Cards)
            {
                GameObject playerCard = Instantiate(card, transform.position, Quaternion.identity);
                Card playerCardScript = playerCard.GetComponent<Card>();
                playerCardScript.ownerPlayerName = requestingPlayer.PlayerName;
                playerCardScript.ownerConnectionId = requestingPlayer.ConnectionId;
                playerCardScript.ownerPlayerNumber = requestingPlayer.playerNumber;
                playerCard.transform.position = new Vector3(-1000, -1000, 0);
                NetworkServer.Spawn(playerCard, connectionToClient);
                requestingPlayer.playerCardHandNetIds.Add(playerCard.GetComponent<NetworkIdentity>().netId);
            }
            requestingPlayer.HaveSpawnedCards = true;
            //Tell all clients to "show" the PlayerUnitHolder - set the correct parent to all the unity holders and run GameplayManager's PutUnitsInUnitBox
            RpcSpawnPlayerCards();
            Debug.Log("Spawned PlayerHand and Player Cards.");
        }
        else
        {
            Debug.Log("NO PlayerHand or Player cards spawned.");
        }
    }
    [ClientRpc]
    void RpcSpawnPlayerCards()
    {
        GameObject[] allPlayerCardHands = GameObject.FindGameObjectsWithTag("PlayerHand");
        GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");

        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();

        foreach (GameObject playerCardHand in allPlayerCardHands)
        {
            PlayerHand playerCardHandScript = playerCardHand.GetComponent<PlayerHand>();
            if (playerCardHandScript.ownerConnectionId == LocalGamePlayerScript.ConnectionId)
            {
                playerCardHand.transform.SetParent(LocalGamePlayer.transform);
                foreach (GameObject card in allCards)
                {
                    Card cardScript = card.GetComponent<Card>();
                    if (cardScript.ownerConnectionId == LocalGamePlayerScript.ConnectionId)
                    {
                        card.transform.SetParent(playerCardHand.transform);
                        //card.transform.position = new Vector3(-1000, -1000, 0);
                    }
                }
                myPlayerCardHand = playerCardHand;
            }
            else 
            {
                GameObject[] allGamePlayers = GameObject.FindGameObjectsWithTag("GamePlayer");
                foreach (GameObject gamePlayer in allGamePlayers)
                {
                    GamePlayer gamePlayerScript = gamePlayer.GetComponent<GamePlayer>();
                    if (playerCardHandScript.ownerConnectionId == gamePlayerScript.ConnectionId)
                    {
                        playerCardHand.transform.SetParent(gamePlayerScript.transform);
                        foreach (GameObject card in allCards)
                        {
                            Card cardScript = card.GetComponent<Card>();
                            if (cardScript.ownerConnectionId == gamePlayerScript.ConnectionId)
                            {
                                card.transform.SetParent(playerCardHand.transform);
                                //card.transform.position = new Vector3(-1000, -1000, 0);
                            }
                        }
                        gamePlayerScript.myPlayerCardHand = playerCardHand;
                    }
                }
            }
        }
    }
    public void GetPlayerBase()
    {
        if (!GotPlayerBase)
        {
            Debug.Log("Calling CmdSetPlayerBase from: " + this.PlayerName);
            CmdSetPlayerBase();
        }
    }
    [Command]
    void CmdSetPlayerBase()
    {
       
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();

        Debug.Log("Running CmdSetPlayerBase on the server for: " +requestingPlayer.PlayerName);

        GameObject[] playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
        foreach (GameObject playerBase in playerBases)
        {
            PlayerBaseScript playerBaseScript = playerBase.GetComponent<PlayerBaseScript>();
            if (requestingPlayer.playerNumber == playerBaseScript.ownerPlayerNumber)
            {
                playerBaseScript.ownerPlayerName = requestingPlayer.PlayerName;
                playerBaseScript.ownerConnectionId = requestingPlayer.ConnectionId;
                requestingPlayer.GotPlayerBase = true;
                CanPlayerPlaceOnLand(requestingPlayer, playerBase);
                RpcGetPlayerBase();
                break;
            }
        }
        

    }
    [Server]
    void CanPlayerPlaceOnLand(GamePlayer gamePlayer, GameObject playerBase)
    {
        Debug.Log("Server: Running CanPlayerPlaceOnLand for: " + gamePlayer.PlayerName + " using this base: " + playerBase);
        Vector3 playerBaseLocation = playerBase.transform.position;
        GameObject LandTileHolder = GameObject.FindGameObjectWithTag("LandHolder");

        foreach (Transform landObject in LandTileHolder.transform)
        {
            LandScript landScript = landObject.gameObject.GetComponent<LandScript>();
            float disFromBase = Vector3.Distance(landObject.transform.position, playerBaseLocation);
            Debug.Log(landScript.gameObject.name + "'s distance from player base: " + disFromBase.ToString());
            if (disFromBase <= 6.0f)
            {
                landScript.PlayerCanPlaceHere = gamePlayer.playerNumber;
            }
        }
    }
    [ClientRpc]
    void RpcGetPlayerBase()
    {
        if (!this.GotPlayerBase)
        {
            Debug.Log("Looking for playerbase for: " + this.PlayerName);
            GameObject[] playerBases = GameObject.FindGameObjectsWithTag("PlayerBase");
            foreach (GameObject playerBase in playerBases)
            {
                PlayerBaseScript playerBaseScript = playerBase.GetComponent<PlayerBaseScript>();
                Debug.Log("Playerbase: " + playerBase + " with ownerPlayerNumber: " + playerBaseScript.ownerPlayerNumber + " from player number: " + this.playerNumber);
                if (playerBaseScript.ownerPlayerNumber == this.playerNumber)
                {
                    this.myPlayerBase = playerBase;
                    this.GotPlayerBase = true;
                    Debug.Log("Found playerbase for: " + this.PlayerName);
                }
            }
        }            
    }
    public void ChangeReadyForNextPhaseStatus()
    {
        if (hasAuthority)
        {
            CmdChangeReadyForNextPhaseStatus();
        }
    }
    [Command]
    void CmdChangeReadyForNextPhaseStatus()
    {
        Debug.Log("Running CmdChangeReadyForNextPhaseStatus on the server.");
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        requestingPlayer.ReadyForNextPhase = !requestingPlayer.ReadyForNextPhase;
        CheckIfAllPlayersAreReadyForNextPhase();
    }
    [Server]
    void CheckIfAllPlayersAreReadyForNextPhase()
    {
        bool allPlayersReady = false;
        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (!gamePlayer.ReadyForNextPhase)
            {
                allPlayersReady = false;
                break;
            }
            else
            {
                allPlayersReady = true;
            }
        }
        if (allPlayersReady)
        {
            foreach (GamePlayer gamePlayer in Game.GamePlayers)
            {
                gamePlayer.ReadyForNextPhase = false;
                
            }
            if (Game.CurrentGamePhase == "Unit Placement")
            {   
                Game.CurrentGamePhase = "Unit Movement";
                Debug.Log("Changing phase to Unit Movement");
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }                
            if (Game.CurrentGamePhase == "Unit Movement")
            {
                // Placeholder code for real code which will do things like check for battles
                bool areThereAnyBattles = CheckForPossibleBattles();                
                Debug.Log("Current phase is Unit Movement");
                if (areThereAnyBattles)
                {
                    Debug.Log("Battle detected. Changing Game phase to 'Battle(s) Detected'");
                    Game.CurrentGamePhase = "Battle(s) Detected";
                    RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                    return;
                }
                else 
                {
                    Debug.Log("No battles detected");
                    Debug.Log("Game phase remains on Unit Movement");
                    RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                    return;
                }
            }
            if (Game.CurrentGamePhase == "Battle(s) Detected")
            {
                GameplayManager.instance.battleNumber = 1;
                foreach (KeyValuePair<int, uint> battles in GameplayManager.instance.battleSiteNetIds)
                {
                    if (battles.Key == GameplayManager.instance.battleNumber)
                    {
                        //GameplayManager.instance.currentBattleSite = battles.Value;
                        GameplayManager.instance.HandleCurrentBattleSiteUpdate(GameplayManager.instance.currentBattleSite, battles.Value);
                        break;
                    }
                }
                Game.CurrentGamePhase = "Choose Cards:\nBattle #1";
                Debug.Log("Game phase changed to Choose Cards");
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            if (Game.CurrentGamePhase.StartsWith("Choose Card"))
            {
                DetermineWhoWonBattle();
                Game.CurrentGamePhase = "Battle Results";
                Debug.Log("Game phase changed to Battle Results");
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
        }
        else
        {
            RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
        }
    }
    [ClientRpc]
    void RpcAdvanceToNextPhase(bool allPlayersReady, string newGamePhase)
    {
        Debug.Log("Are all players ready for next phase?: " + allPlayersReady);
        if (allPlayersReady)
        {
            Debug.Log("Advancing phase from player: " + this.PlayerName);
            GameplayManager.instance.ChangeGamePhase(newGamePhase);
        }
    }
    public void HandlePlayerReadyStatusUpdate(bool oldValue, bool newValue)
    {
        Debug.Log("Player ready status has been has been updated for " + this.PlayerName + ": " + oldValue + " to new value: " + newValue);
        if (hasAuthority)
        {
            GameplayManager.instance.UpdateReadyButton();
        }
        GameplayManager.instance.UpdatePlayerReadyText(this.PlayerName, this.ReadyForNextPhase);
    }
    public void UpdateUnitPositions()
    {
        if (hasAuthority)
            CmdUpdateUnitPositions();
    }
    [Command]
    void CmdUpdateUnitPositions()
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        GameObject[] PlayerUnitHolders = GameObject.FindGameObjectsWithTag("PlayerUnitHolder");
        foreach (GameObject unitHolder in PlayerUnitHolders)
        {
            PlayerUnitHolder unitHolderScript = unitHolder.GetComponent<PlayerUnitHolder>();
            if (unitHolderScript.ownerConnectionId == requestingPlayer.ConnectionId)
            {
                foreach (Transform unitChild in unitHolder.transform)
                {
                    UnitScript unitScript = unitChild.transform.gameObject.GetComponent<UnitScript>();
                    if (unitScript.ownerConnectionId == requestingPlayer.ConnectionId && unitScript.startingPosition != unitScript.newPosition)
                    {
                        unitScript.startingPosition = unitScript.newPosition;
                    }
                }
                break;
            }
        }
        if (Game.CurrentGamePhase == "Battle(s) Detected")
            requestingPlayer.GetComponent<GamePlayer>().updatedUnitPositionsForBattleSites = true;

    }
    [Server]
    public bool CheckForPossibleBattles()
    {
        Debug.Log("Ran CheckForPossibleBattles");
        GameObject landTileHolder = GameObject.FindGameObjectWithTag("LandHolder");
        bool wasBattleDetected = false;
        GameplayManager.instance.battleSiteNetIds.Clear();
        foreach (Transform landObject in landTileHolder.transform)
        {
            LandScript landScript = landObject.gameObject.GetComponent<LandScript>();
            if (landScript.UnitNetIdsAndPlayerNumber.Count > 1)
            {
                int playerNumber = -1;
                foreach (KeyValuePair<uint, int> units in landScript.UnitNetIdsAndPlayerNumber)
                {
                    if (playerNumber != units.Value && playerNumber != -1)
                    {
                        Debug.Log("Two different player values discovered. Value 1: " + playerNumber + " Value 2: " + units.Value);
                        wasBattleDetected = true;
                        int battleNumber = GameplayManager.instance.battleSiteNetIds.Count + 1;
                        GameplayManager.instance.battleSiteNetIds.Add(battleNumber,landObject.GetComponent<NetworkIdentity>().netId);
                        break;
                    }
                    playerNumber = units.Value;
                }
            }
        }
        return wasBattleDetected;
    }
    [Command]
    public void CmdCheckIfBattleSitesHaveBeenSet()
    {
        if (GameplayManager.instance.battleSiteNetIds.Count > 0)
            GameplayManager.instance.BattleSitesHaveBeenSet = true;
    }
    void HandleUpdatedUnitPositionsForBattleSites(bool oldValue, bool newValue)
    {
        if (newValue)
            GameplayManager.instance.CheckIfAllUpdatedUnitPositionsForBattleSites();
    }
    public void SetGamePlayerArmy()
    {
        if (hasAuthority)
            CmdSetGamePlayerArmy();
    }
    [Command]
    void CmdSetGamePlayerArmy()
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdSetGamePlayerArmy for: " + requestingPlayer.PlayerName + ":" + requestingPlayer.ConnectionId);
        //Clear out any previous army data
        requestingPlayer.playerArmyNetIds.Clear();
        requestingPlayer.playerArmyNumberOfInf = 0;
        requestingPlayer.playerArmyNumberOfTanks = 0;
        requestingPlayer.playerBattleScore = 0;

        GameObject battleSite = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;
        LandScript battleSiteScript = battleSite.GetComponent<LandScript>();

        foreach (KeyValuePair<uint, int> battleUnits in battleSiteScript.UnitNetIdsAndPlayerNumber)
        {
            if (battleUnits.Value == requestingPlayer.playerNumber)
            {
                requestingPlayer.playerArmyNetIds.Add(battleUnits.Key);
                if (NetworkIdentity.spawned[battleUnits.Key].gameObject.tag == "infantry")
                    requestingPlayer.playerArmyNumberOfInf++;
                else if (NetworkIdentity.spawned[battleUnits.Key].gameObject.tag == "tank")
                    requestingPlayer.playerArmyNumberOfTanks++;
            }
        }
        requestingPlayer.playerBattleScore = requestingPlayer.playerArmyNumberOfInf;
        requestingPlayer.playerBattleScore += (requestingPlayer.playerArmyNumberOfTanks * 2);
        HandleBattleScoreSet(requestingPlayer.isPlayerBattleScoreSet, true);
    }
    void HandleBattleScoreSet(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.isPlayerBattleScoreSet = newValue;
        }
        if (isClient && newValue)
        {
            Debug.Log("Running HandleBattleScoreSet as a client.");
            GameplayManager.instance.CheckIfAllPlayerBattleScoresSet();
        }    
        
    }
    public void SelectThisCard(GameObject playerCard)
    {
        if (hasAuthority)
        {
            CmdPlayerSelectCardForBattle(playerCard.GetComponent<NetworkIdentity>().netId);
        }

    }
    [Command]
    void CmdPlayerSelectCardForBattle(uint playerCardNetworkId)
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdPlayerSelectCardForBattle for: " + requestingPlayer.PlayerName + ":" + requestingPlayer.ConnectionId);
        if (requestingPlayer.playerCardHandNetIds.Contains(playerCardNetworkId))
        {
            Debug.Log("Player card: " + playerCardNetworkId + " is in " + requestingPlayer.PlayerName + "'s hand.");
            //requestingPlayer.playerBattleCardNetId = playerCardNetworkId;
            HandleUpdatedPlayerBattleCard(requestingPlayer.playerBattleCardNetId, playerCardNetworkId);
        }
        else 
        {
            Debug.Log("Player card: " + playerCardNetworkId + " IS NOT in " + requestingPlayer.PlayerName + "'s hand.");
        }
    }
    void HandleUpdatedPlayerBattleCard(uint oldValue, uint newValue)
    {
        if (isServer)
        {
            playerBattleCardNetId = newValue;
        }
        if (isClient)
        {
            Debug.Log("Running HandleUpdatedPlayerBattleCard as a client");
            if (hasAuthority)
            {
                GameplayManager.instance.HidePlayerHandPressed();
                RemoveSelectedCardFromHandAndReposition(newValue);
            }                
        }
    }
    void RemoveSelectedCardFromHandAndReposition(uint SelectedCardNetId)
    {
        Debug.Log("Executing RemoveSelectedCardFromHandAndReposition for card with network id:" + SelectedCardNetId.ToString());
        // if a card is already selected by the player, remove it as their selected card and add it back to their Hand
        if (selectedCard)
        {
            myPlayerCardHand.GetComponent<PlayerHand>().AddCardBackToHand(selectedCard);
            selectedCard.GetComponent<Card>().isClickable = true;
            selectedCard = null;
        }        
        if (!selectedCard)
        {
            // set selectedCard and remove from the PlayerHand's Hand list
            selectedCard = NetworkIdentity.spawned[SelectedCardNetId].gameObject;
            myPlayerCardHand.GetComponent<PlayerHand>().Hand.Remove(selectedCard);
            selectedCard.GetComponent<Card>().isClickable = false;

            // move the card to be in the local player battle panel
            //selectedCard.transform.position = new Vector3(-5.25f, -1.5f, 0);
            //selectedCard.transform.localScale = new Vector3(1f, 1f, 1);
            selectedCard.SetActive(true);
            selectedCard.transform.SetParent(GameplayManager.instance.localPlayerBattlePanel.transform);
            selectedCard.transform.localPosition = new Vector3(-27f, -110f, 1f);
            selectedCard.transform.localScale = new Vector3(70f, 70f, 1f);
            GameplayManager.instance.ShowPlayerCardScore();
        }        
    }
    [Server]
    void DetermineWhoWonBattle()
    {
        Debug.Log("Executing DetermineWhoWonBattle on server");
        GameplayManager.instance.winnerOfBattleName = "";
        GameplayManager.instance.winnerOfBattlePlayerNumber = -1;
        GameplayManager.instance.winnerOfBattlePlayerConnId = -1;
        GameplayManager.instance.reasonForWinning = "";
        GamePlayer player1 = null;
        GamePlayer player2 = null;

        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (gamePlayer.playerNumber == 1)
                player1 = gamePlayer;
            else
                player2 = gamePlayer;
        }
        Card player1Card = NetworkIdentity.spawned[player1.playerBattleCardNetId].gameObject.GetComponent<Card>();
        Card player2Card = NetworkIdentity.spawned[player2.playerBattleCardNetId].gameObject.GetComponent<Card>();

        int player1BattleScore = 0;
        int player2BattleScore = 0;
        //Calculate player 1 score
        player1BattleScore = player1.playerBattleScore;
        player1BattleScore += player1Card.Power;
        //Calculate player2 score
        player2BattleScore = player2.playerBattleScore;
        player2BattleScore += player2Card.Power;

        if (player1BattleScore > player2BattleScore)
        {
            Debug.Log("Player 1 wins battle. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
            GameplayManager.instance.winnerOfBattleName = player1.PlayerName;
            GameplayManager.instance.winnerOfBattlePlayerNumber = player1.playerNumber;
            GameplayManager.instance.winnerOfBattlePlayerConnId = player1.ConnectionId;
            GameplayManager.instance.reasonForWinning = "Battle Score";
        }
        else if (player1BattleScore < player2BattleScore)
        {
            Debug.Log("Player 2 wins battle. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
            GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
            GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
            GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
            GameplayManager.instance.reasonForWinning = "Battle Score";
        }
        else if (player1BattleScore == player2BattleScore)
        {
            //First tie breaker: Player with highest card value wins
            if (player1Card.Power > player2Card.Power)
            {
                Debug.Log("Player 1 wins first tie breaker: Higher card power");
                GameplayManager.instance.winnerOfBattleName = player1.PlayerName;
                GameplayManager.instance.winnerOfBattlePlayerNumber = player1.playerNumber;
                GameplayManager.instance.winnerOfBattlePlayerConnId = player1.ConnectionId;
                GameplayManager.instance.reasonForWinning = "Tie Breaker 1: Highest Card Power";
            }
            else if (player1Card.Power < player2Card.Power)
            {
                Debug.Log("Player 2 wins first tie breaker: Higher card power");
                GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
                GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
                GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
                GameplayManager.instance.reasonForWinning = "Tie Breaker 1: Highest Card Power";
            }
            else if (player1Card.Power == player2Card.Power)
            {
                //Checking for second tie breaker: Player with most infantry wins
                if (player1.playerArmyNumberOfInf > player2.playerArmyNumberOfInf)
                {
                    Debug.Log("Player 1 wins second tie breaker: More infantry than other player");
                    GameplayManager.instance.winnerOfBattleName = player1.PlayerName;
                    GameplayManager.instance.winnerOfBattlePlayerNumber = player1.playerNumber;
                    GameplayManager.instance.winnerOfBattlePlayerConnId = player1.ConnectionId;
                    GameplayManager.instance.reasonForWinning = "Tie Breaker 2: Most Infantry";
                }
                else if (player1.playerArmyNumberOfInf < player2.playerArmyNumberOfInf)
                {
                    Debug.Log("Player 2 wins second tie breaker: More infantry than other player");
                    GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
                    GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
                    GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
                    GameplayManager.instance.reasonForWinning = "Tie Breaker 2: Most Infantry";
                }
                else if (player1.playerArmyNumberOfInf == player2.playerArmyNumberOfInf)
                {
                    Debug.Log("The battle was a tie! Same card power and same number of infantry. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
                    GameplayManager.instance.winnerOfBattleName = "tie";
                    GameplayManager.instance.winnerOfBattlePlayerNumber = -1;
                    GameplayManager.instance.winnerOfBattlePlayerConnId = -1;
                    GameplayManager.instance.reasonForWinning = "Draw: No Winner";
                }
            }                     
        }
        GameplayManager.instance.HandleAreBattleResultsSet(GameplayManager.instance.areBattleResultsSet, true);
    }
}
