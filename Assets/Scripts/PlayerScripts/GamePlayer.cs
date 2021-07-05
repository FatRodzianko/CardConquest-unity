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
    [SyncVar] public Vector3 myPlayerBasePosition;
    [SyncVar] public uint myPlayerBaseNetId;
    public GameObject myUnitHolder;
    public GameObject myPlayerCardHand;
    public SyncList<uint> playerUnitNetIds = new SyncList<uint>();
    public SyncList<uint> playerCardHandNetIds = new SyncList<uint>();

    [Header("Player Statuses")]
    [SyncVar] public bool HaveSpawnedUnits = false;
    [SyncVar] public bool GotPlayerBase = false;
    [SyncVar] public bool HaveSpawnedCards = false;
    [SyncVar(hook = nameof(HandlePlayerReadyStatusUpdate))] public bool ReadyForNextPhase = false;

    [Header("Reinforcements")]
    [SyncVar(hook = nameof(HandleCanPlayerReinforce))] public bool canPlayerReinforce = false;
    [SyncVar] public bool didPlayerReinforce = false;
    public SyncList<uint> unitsAvailableForReinforcement = new SyncList<uint>();
    public SyncList<uint> playerReinforcementNetIds = new SyncList<uint>();
    [SyncVar] public int playerReinforcementsNumberOfInf;
    [SyncVar] public int playerReinforcementsNumberOfTanks;

    [Header("Player Battle Info")]
    //public SyncList<uint> battleSiteNetIds = new SyncList<uint>();
    //[SyncVar(hook = nameof(HandleBattleSitesSet))] bool BattleSitesHaveBeenSet = false;
    [SyncVar(hook = nameof(HandleUpdatedUnitPositionsForBattleSites))] public bool updatedUnitPositionsForBattleSites = false;
    public SyncList<uint> playerArmyNetIds = new SyncList<uint>();
    [SyncVar] public int playerArmyNumberOfInf;
    [SyncVar] public int playerArmyNumberOfTanks;
    [SyncVar] public int playerArmyReinforcementPower;
    [SyncVar(hook = nameof(HandlePlayerBattleScoreValueSet))] public int playerBattleScore;
    [SyncVar(hook = nameof(HandleBattleScoreSet))] public bool isPlayerBattleScoreSet = false;
    [SyncVar(hook = nameof(HandlePlayerCardAbilityActivated))] public bool didPlayerCardAbilityActivate = false;
    [SyncVar] public int playerCardAbilityNumber = 0;

    [Header("Battle Card Info")]
    [SyncVar(hook = nameof(HandleUpdatedPlayerBattleCard))] public uint playerBattleCardNetId;
    public GameObject selectedCard;

    [Header("Retreat Units Section")]
    [SyncVar] public bool doesPlayerNeedToRetreat = false;
    [SyncVar] public bool canPlayerRetreat = false;

    [Header("End of Game Statuses")]
    [SyncVar(hook = nameof(HandlePlayerWin))] bool didPlayerWin = false;
    [SyncVar(hook = nameof(HandlePlayerEliminatedFromUnitsLost))] bool wasPlayerEliminatedFromUnitsLost = false;
    [SyncVar(hook = nameof(HandlePlayerEliminatedFromBaseCaptured))] bool wasPlayerEliminatedFromBaseCaptured = false;
    

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
                        cardScript.myPlayerHandObject = playerCardHand;
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
                                cardScript.myPlayerHandObject = playerCardHand;
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
                requestingPlayer.myPlayerBasePosition = playerBase.transform.position;
                requestingPlayer.myPlayerBaseNetId = playerBase.GetComponent<NetworkIdentity>().netId;
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

                

                //Check for possible reinforcements before starting the battle
                bool canPlayersReinforce = CheckForPossibleReinforcements();
                if (canPlayersReinforce)
                {
                    Debug.Log("CheckForPossibleReinforcements returned true");
                    Game.CurrentGamePhase = "Reinforcements:\nBattle #" + GameplayManager.instance.battleNumber.ToString();
                    Debug.Log("Game phase changed to Reinforcements");

                    foreach (KeyValuePair<int, uint> battles in GameplayManager.instance.battleSiteNetIds)
                    {
                        if (battles.Key == GameplayManager.instance.battleNumber)
                        {
                            //GameplayManager.instance.currentBattleSite = battles.Value;
                            GameplayManager.instance.HandleReinforcementsBattleSiteUpdate(GameplayManager.instance.reinforcementsBattleSite, battles.Value);
                            break;
                        }
                    }

                    RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                    return;
                }

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

                foreach (GamePlayer player in Game.GamePlayers)
                {
                    RpcUpdateIsPlayerBattleScoreSet();
                }

                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            if (Game.CurrentGamePhase == "Battle Results")
            {
                AfterBattleCleanUp();
                
                MovePlayedCardToDiscard();

                // Check if the previous battle was a base defense. If so, check if the defending player lost the base defense
                if (GameplayManager.instance.isPlayerBaseDefense)
                {
                    Debug.Log("Server: Previous battle was a player base defense. Checking if the defending player lost");
                    CheckForWinnerFromPlayerBaseBattle();
                }

                DestroyUnitsLostInBattle();

                //Check if a player won the game after the battle. end the game if so
                foreach (GamePlayer player in Game.GamePlayers)
                {
                    if (player.didPlayerWin)
                    {
                        Game.CurrentGamePhase = "Game Over";
                        Debug.Log("Changing phase to Game Over");
                        RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                        return;
                    }
                }
                bool unitsLeftToRetreat = CheckIfUnitsAreLeftToRetreat();
                if (unitsLeftToRetreat)
                {
                    Debug.Log("Units left to retreat");
                    Game.CurrentGamePhase = "Retreat Units";
                    Debug.Log("Game phase changed to Retreat Units");
                }
                else 
                {
                    Debug.Log("Server: There were no units left to retreat. Checking for another battle.");
                    CleanupPreviousBattleInfo();
                    bool moreBattles = CheckForMoreBattles();
                    if (moreBattles)
                    {
                        Debug.Log("At least one more battle to fight. Moving to the next battle.");
                        ChangeToNextBattle();
                    }
                    else
                    {
                        Game.CurrentGamePhase = "Unit Movement";
                        Debug.Log("Changing phase to Unit Movement");
                        RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                        return;
                    }
                }         
                
                //DestroyUnitsLostInBattle();
                //MovePlayedCardToDiscard();
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            if (Game.CurrentGamePhase == "Retreat Units")
            {
                bool didAllUnitsRetreat = VerifyAllUnitsRetreated();
                if (didAllUnitsRetreat)
                {
                    Debug.Log("Server: All players retreated.");
                    if (GameplayManager.instance.reasonForWinning.StartsWith("Draw"))
                    {
                        Debug.Log("The last battle was a draw. Check if players retreated to same tile and started a new battle.");
                        bool newBattlesAfterRetreat = CheckForNewBattlesAfterDrawRetreat();
                        if (newBattlesAfterRetreat)
                        {
                            Debug.Log("New battle detected after draw retreat.");
                            CleanupPreviousBattleInfo();
                            Game.CurrentGamePhase = "New Battle Detected";
                            Debug.Log("Changing phase to New Battle Detected");
                            RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                            return;
                        }
                        else
                        {
                            Debug.Log("NO new battle detected after draw retreat.");
                        }
                    }
                    CleanupPreviousBattleInfo();
                    bool moreBattles = CheckForMoreBattles();
                    if (moreBattles)
                    {
                        Debug.Log("At least one more battle to fight. Moving to the next battle.");
                        ChangeToNextBattle();
                    }
                    else
                    {
                        Game.CurrentGamePhase = "Unit Movement";
                        Debug.Log("Changing phase to Unit Movement");
                        RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                        return;
                    }
                }
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            if (Game.CurrentGamePhase == "New Battle Detected")
            {
                bool moreBattles = CheckForMoreBattles();
                if (moreBattles)
                {
                    Debug.Log("At least one more battle to fight. Moving to the next battle.");
                    ChangeToNextBattle();
                }
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
            }
            if (Game.CurrentGamePhase.StartsWith("Reinforcements"))
            {
                Debug.Log("Previous game phase was Reinforcements");
                SetIsUnitReinforcingBattleOnUnits();
                // Place holder to force to the first battle. Change later!!!
                foreach (KeyValuePair<int, uint> battles in GameplayManager.instance.battleSiteNetIds)
                {
                    if (battles.Key == GameplayManager.instance.battleNumber)
                    {
                        //GameplayManager.instance.currentBattleSite = battles.Value;
                        GameplayManager.instance.HandleCurrentBattleSiteUpdate(GameplayManager.instance.currentBattleSite, battles.Value);
                        break;
                    }
                }

                Game.CurrentGamePhase = "Choose Cards:\nBattle #" + GameplayManager.instance.battleNumber;
                Debug.Log("Game phase changed to Choose Cards");
                RpcAdvanceToNextPhase(allPlayersReady, Game.CurrentGamePhase);
                return;
                // Place holder to force to the first battle. Change later!!!
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
        if (Game.CurrentGamePhase == "Battle(s) Detected" || Game.CurrentGamePhase == "New Battle Detected")
            requestingPlayer.GetComponent<GamePlayer>().updatedUnitPositionsForBattleSites = true;
    }
    [Server]
    public bool CheckForPossibleBattles()
    {
        Debug.Log("Ran CheckForPossibleBattles");
        GameObject landTileHolder = GameObject.FindGameObjectWithTag("LandHolder");
        bool wasBattleDetected = false;
        GameplayManager.instance.battleSiteNetIds.Clear();

        //Get a list of the Vector3 coordinates for all player bases
        Dictionary<Vector3, int> playerBaseAndPlayerNumber = new Dictionary<Vector3, int>();
        foreach (GamePlayer player in Game.GamePlayers)
        {
            playerBaseAndPlayerNumber.Add(player.myPlayerBasePosition, player.playerNumber);
        }

        foreach (Transform landObject in landTileHolder.transform)
        {
            LandScript landScript = landObject.gameObject.GetComponent<LandScript>();
            if (playerBaseAndPlayerNumber.ContainsKey(landObject.transform.position))
            {
                Debug.Log("CheckForPossibleBattles: Checking for units on a player base");
                int basePlayerNumber = playerBaseAndPlayerNumber[landObject.transform.position];
                if (landScript.UnitNetIdsAndPlayerNumber.Count > 0)
                {
                    Debug.Log("CheckForPossibleBattles: At least one unit on player base: " + landObject.transform.position.ToString());
                    foreach (KeyValuePair<uint, int> units in landScript.UnitNetIdsAndPlayerNumber)
                    {
                        if (basePlayerNumber != units.Value)
                        {
                            Debug.Log("CheckForPossibleBattles opposing player discovered on player base at: " + landObject.transform.position.ToString()); ;
                            wasBattleDetected = true;
                            int battleNumber = GameplayManager.instance.battleSiteNetIds.Count + 1;
                            GameplayManager.instance.battleSiteNetIds.Add(battleNumber, landObject.GetComponent<NetworkIdentity>().netId);
                            break;
                        }
                    }
                }
                else
                    Debug.Log("CheckForPossibleBattles: NO UNITS on player base: " + landObject.transform.position.ToString());
            }
            else if (landScript.UnitNetIdsAndPlayerNumber.Count > 1)
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
        requestingPlayer.playerArmyReinforcementPower = 0;
        requestingPlayer.playerBattleScore = 0;

        GameObject battleSite = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;
        LandScript battleSiteScript = battleSite.GetComponent<LandScript>();

        foreach (KeyValuePair<uint, int> battleUnits in battleSiteScript.UnitNetIdsAndPlayerNumber)
        {
            if (battleUnits.Value == requestingPlayer.playerNumber)
            {
                requestingPlayer.playerArmyNetIds.Add(battleUnits.Key);
                // Set isUnitReinforcingBattle to true for each unit in the player's army so that they can't reinforce any future battles
                NetworkIdentity.spawned[battleUnits.Key].gameObject.GetComponent<UnitScript>().isUnitReinforcingBattle = true;
                if (NetworkIdentity.spawned[battleUnits.Key].gameObject.tag == "infantry")
                    requestingPlayer.playerArmyNumberOfInf++;
                else if (NetworkIdentity.spawned[battleUnits.Key].gameObject.tag == "tank")
                    requestingPlayer.playerArmyNumberOfTanks++;
            }
        }
        requestingPlayer.playerBattleScore = requestingPlayer.playerArmyNumberOfInf;
        requestingPlayer.playerBattleScore += (requestingPlayer.playerArmyNumberOfTanks * 2);

        // Check to see if player is defending their own base. If they are, add 2 to their battle score
        if (battleSite.transform.position == requestingPlayer.myPlayerBasePosition)
        {
            Debug.Log(requestingPlayer.PlayerName + " is defending their base. +2 to battle score");
            requestingPlayer.playerBattleScore += 2;
            GameplayManager.instance.HandleIsPlayerBaseDefense(GameplayManager.instance.isPlayerBaseDefense, true);
        }
        if (requestingPlayer.didPlayerReinforce)
        {
            Debug.Log("CmdSetGamePlayerArmy: The requesting player: " + requestingPlayer.PlayerName + " is reinforcing their army!");
            if (requestingPlayer.playerReinforcementNetIds.Count > 0)
            {
                foreach (uint reinforcement in requestingPlayer.playerReinforcementNetIds)
                {
                    UnitScript reinforcementScript = NetworkIdentity.spawned[reinforcement].gameObject.GetComponent<UnitScript>();
                    if (reinforcementScript.canUnitReinforce && reinforcementScript.isUnitReinforcingBattle)
                    {
                        if (reinforcementScript.gameObject.tag == "infantry")
                            requestingPlayer.playerArmyReinforcementPower++;
                        else if (reinforcementScript.gameObject.tag == "tank")
                            requestingPlayer.playerArmyReinforcementPower += 2;
                    }
                }
            }
            requestingPlayer.playerBattleScore += requestingPlayer.playerArmyReinforcementPower;
        }

        HandleBattleScoreSet(requestingPlayer.isPlayerBattleScoreSet, true);
        //RpcUpdatePlayerBattlePanels();
    }
    void HandleBattleScoreSet(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.isPlayerBattleScoreSet = newValue;
        }
        if (isClient && newValue)
        {
            Debug.Log("!!!! Running HandleBattleScoreSet as a client. New value set to true.");
            GameplayManager.instance.CheckIfAllPlayerBattleScoresSet();
        }
        else if (!newValue)
        {
            Debug.Log("!!!! Running HandleBattleScoreSet as a client. isPlayerBattleScoreSet set to false");
            Debug.Log("!!!! Value of isPlayerBattleScoreSet for player " + this.PlayerName + " is: " + this.isPlayerBattleScoreSet.ToString() + " value of new value is: " + newValue.ToString());
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
            requestingPlayer.HandleUpdatedPlayerBattleCard(requestingPlayer.playerBattleCardNetId, playerCardNetworkId);
        }
        else 
        {
            Debug.Log("Player card: " + playerCardNetworkId + " IS NOT in " + requestingPlayer.PlayerName + "'s hand.");
        }
    }
    public void HandleUpdatedPlayerBattleCard(uint oldValue, uint newValue)
    {
        if (isServer)
        {
            playerBattleCardNetId = newValue;
        }
        if (isClient && newValue != 0)
        {
            Debug.Log("Running HandleUpdatedPlayerBattleCard as a client");
            Debug.Log("HandleUpdatedPlayerBattleCard: new value is: " + newValue.ToString());
            if (hasAuthority)
            {
                Debug.Log("HandleUpdatedPlayerBattleCard: Client has authority to update battle card to this value: " + newValue.ToString());
                GameplayManager.instance.HidePlayerHandPressed();
                RemoveSelectedCardFromHandAndReposition(newValue);
            }
        }
        if (isClient && newValue == 0)
        {
            Debug.Log("HandleUpdatedPlayerBattleCard: new value is 0: " + newValue.ToString());
            selectedCard = null;
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
        GameplayManager.instance.loserOfBattleName = "";
        GameplayManager.instance.loserOfBattlePlayerNumber = -1;
        GameplayManager.instance.loserOfBattlePlayerConnId = -1;
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
        Debug.Log("DetermineWhoWonBattle: Will get player 1 card: " + player1.playerBattleCardNetId.ToString());
        Card player1Card = NetworkIdentity.spawned[player1.playerBattleCardNetId].gameObject.GetComponent<Card>();
        Debug.Log("DetermineWhoWonBattle: Will get player 2 card: " + player2.playerBattleCardNetId.ToString());
        Card player2Card = NetworkIdentity.spawned[player2.playerBattleCardNetId].gameObject.GetComponent<Card>();

        int player1BattleScore = 0;
        int player2BattleScore = 0;
        //Calculate player 1 score
        player1BattleScore = player1.playerBattleScore;
        player1BattleScore += player1Card.Power;
        //Calculate player2 score
        player2BattleScore = player2.playerBattleScore;
        player2BattleScore += player2Card.Power;
        if (CheckIfCardPowerActivatesForBattle())
        {
            Debug.Log("DetermineWhoWonBattle: CheckIfCardPowerActivatesForBattle returned true. Checking which player had their card ability activate.");
            if (player1.didPlayerCardAbilityActivate)
            {
                Debug.Log("DetermineWhoWonBattle: Card ability activated for player: " + player1.PlayerName);
                Card playerBattleCard = NetworkIdentity.spawned[player1.playerBattleCardNetId].gameObject.GetComponent<Card>();
                if (playerBattleCard.SpecialAbilityNumber == 1 && player1.playerCardAbilityNumber == 1)
                {
                    Debug.Log("DetermineWhoWonBattle: Card Ability 1 for \"If your army has inf but no tanks, +2 battle score\" activated for player: " + player1.PlayerName);
                    //player1.playerBattleScore += 2;
                    player1.HandlePlayerBattleScoreValueSet(player1.playerBattleScore, player1.playerBattleScore += 2);
                    player1BattleScore +=2;
                }
            }
            if (player2.didPlayerCardAbilityActivate)
            {
                Debug.Log("DetermineWhoWonBattle: Card ability activated for player: " + player2.PlayerName);
                Card playerBattleCard = NetworkIdentity.spawned[player2.playerBattleCardNetId].gameObject.GetComponent<Card>();
                if (playerBattleCard.SpecialAbilityNumber == 1 && player2.playerCardAbilityNumber == 1)
                {
                    Debug.Log("DetermineWhoWonBattle: Card Ability 1 for \"If your army has inf but no tanks, +2 battle score\" activated for player: " + player2.PlayerName);
                    //player2.playerBattleScore += 2;
                    player2.HandlePlayerBattleScoreValueSet(player2.playerBattleScore, player2.playerBattleScore += 2);
                    player2BattleScore += 2;
                }
            }
        }
        else
        {
            Debug.Log("DetermineWhoWonBattle: CheckIfCardPowerActivatesForBattle returned FALSE.");
        }

        if (player1BattleScore > player2BattleScore)
        {
            Debug.Log("Player 1 wins battle. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
            GameplayManager.instance.winnerOfBattleName = player1.PlayerName;
            GameplayManager.instance.winnerOfBattlePlayerNumber = player1.playerNumber;
            GameplayManager.instance.winnerOfBattlePlayerConnId = player1.ConnectionId;
            GameplayManager.instance.loserOfBattleName = player2.PlayerName;
            GameplayManager.instance.loserOfBattlePlayerNumber = player2.playerNumber;
            GameplayManager.instance.loserOfBattlePlayerConnId = player2.ConnectionId;
            GameplayManager.instance.reasonForWinning = "Battle Score";

            UnitsLostFromBattle(player1, player2);
        }
        else if (player1BattleScore < player2BattleScore)
        {
            Debug.Log("Player 2 wins battle. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
            GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
            GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
            GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
            GameplayManager.instance.loserOfBattleName = player1.PlayerName;
            GameplayManager.instance.loserOfBattlePlayerNumber = player1.playerNumber;
            GameplayManager.instance.loserOfBattlePlayerConnId = player1.ConnectionId;
            GameplayManager.instance.reasonForWinning = "Battle Score";

            UnitsLostFromBattle(player2, player1);
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
                GameplayManager.instance.loserOfBattleName = player2.PlayerName;
                GameplayManager.instance.loserOfBattlePlayerNumber = player2.playerNumber;
                GameplayManager.instance.loserOfBattlePlayerConnId = player2.ConnectionId;
                GameplayManager.instance.reasonForWinning = "Tie Breaker 1: Highest Card Power";

                UnitsLostFromBattle(player1, player2);
            }
            else if (player1Card.Power < player2Card.Power)
            {
                Debug.Log("Player 2 wins first tie breaker: Higher card power");
                GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
                GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
                GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
                GameplayManager.instance.loserOfBattleName = player1.PlayerName;
                GameplayManager.instance.loserOfBattlePlayerNumber = player1.playerNumber;
                GameplayManager.instance.loserOfBattlePlayerConnId = player1.ConnectionId;
                GameplayManager.instance.reasonForWinning = "Tie Breaker 1: Highest Card Power";

                UnitsLostFromBattle(player2, player1);
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
                    GameplayManager.instance.loserOfBattleName = player2.PlayerName;
                    GameplayManager.instance.loserOfBattlePlayerNumber = player2.playerNumber;
                    GameplayManager.instance.loserOfBattlePlayerConnId = player2.ConnectionId;
                    GameplayManager.instance.reasonForWinning = "Tie Breaker 2: Most Infantry";

                    UnitsLostFromBattle(player1, player2);
                }
                else if (player1.playerArmyNumberOfInf < player2.playerArmyNumberOfInf)
                {
                    Debug.Log("Player 2 wins second tie breaker: More infantry than other player");
                    GameplayManager.instance.winnerOfBattleName = player2.PlayerName;
                    GameplayManager.instance.winnerOfBattlePlayerNumber = player2.playerNumber;
                    GameplayManager.instance.winnerOfBattlePlayerConnId = player2.ConnectionId;
                    GameplayManager.instance.loserOfBattleName = player1.PlayerName;
                    GameplayManager.instance.loserOfBattlePlayerNumber = player1.playerNumber;
                    GameplayManager.instance.loserOfBattlePlayerConnId = player1.ConnectionId;
                    GameplayManager.instance.reasonForWinning = "Tie Breaker 2: Most Infantry";

                    UnitsLostFromBattle(player2, player1);
                }
                else if (player1.playerArmyNumberOfInf == player2.playerArmyNumberOfInf)
                {
                    Debug.Log("The battle was a tie! Same card power and same number of infantry. Player1 score: " + player1BattleScore.ToString() + " Player2 score: " + player2BattleScore.ToString());
                    GameplayManager.instance.winnerOfBattleName = "tie";
                    GameplayManager.instance.winnerOfBattlePlayerNumber = -1;
                    GameplayManager.instance.winnerOfBattlePlayerConnId = -1;
                    GameplayManager.instance.loserOfBattleName = "tie";
                    GameplayManager.instance.loserOfBattlePlayerNumber = -1;
                    GameplayManager.instance.loserOfBattlePlayerConnId = -1;
                    GameplayManager.instance.reasonForWinning = "Draw: No Winner";
                    UnitsLostFromBattle(null, null);
                }
            }                     
        }
        GameplayManager.instance.HandleAreBattleResultsSet(GameplayManager.instance.areBattleResultsSet, true);
    }
    [Server]
    void UnitsLostFromBattle(GamePlayer winningPlayer, GamePlayer losingPlayer)
    {
        Debug.Log("Executing UnitsLostFromBattle");
        //Clear out old data
        GameplayManager.instance.numberOfTanksLost = 0;
        GameplayManager.instance.numberOfInfLost = 0;
        GameplayManager.instance.unitNetIdsLost.Clear();
        if (winningPlayer && losingPlayer)
        {
            Card winningCard = NetworkIdentity.spawned[winningPlayer.playerBattleCardNetId].gameObject.GetComponent<Card>();
            Card losingCard = NetworkIdentity.spawned[losingPlayer.playerBattleCardNetId].gameObject.GetComponent<Card>();

            int winningPlayerAttackValue = winningCard.AttackValue;

            if (winningCard.AttackValue > losingCard.DefenseValue)
            {
                Debug.Log("Attack value greater than defense. Units will be lost. Attack value: " + winningCard.AttackValue.ToString() + " Defense Value: " + losingCard.DefenseValue.ToString());

                int unitsToLose = winningCard.AttackValue - losingCard.DefenseValue;
                if (unitsToLose > losingPlayer.playerArmyNetIds.Count)
                {
                    if (losingPlayer.didPlayerReinforce && losingPlayer.playerReinforcementNetIds.Count > 0)
                    {
                        int reinforcingUnitsToLose = unitsToLose - losingPlayer.playerArmyNetIds.Count;
                        Debug.Log("UnitsLostFromBattle: Losing player: " + losingPlayer.PlayerName + " reinforced and they should lose more units than what is in their main army. Will need to kill this many reinforcing units: " + reinforcingUnitsToLose.ToString());
                        KillUnitsFromBattle(losingPlayer, reinforcingUnitsToLose, true);
                        GameplayManager.instance.HandleReinforcementsLost(GameplayManager.instance.reinforcementsLost, true);
                    }
                    unitsToLose = losingPlayer.playerArmyNetIds.Count;
                }
                KillUnitsFromBattle(losingPlayer, unitsToLose, false);
            }
            else
            {
                Debug.Log("No units lost. Attack value: " + winningCard.AttackValue.ToString() + " Defense Value: " + losingCard.DefenseValue.ToString());
            }
            //GameplayManager.instance.HandleAreUnitsLostCalculated(GameplayManager.instance.unitsLostCalculated, true);
        }
        CheckWhichPlayersNeedToRetreat();
        GameplayManager.instance.HandleAreUnitsLostCalculated(GameplayManager.instance.unitsLostCalculated, true);
    }
    [Server]
    void CheckWhichPlayersNeedToRetreat()
    {
        List<GamePlayer> playersToRetreat = new List<GamePlayer>();
        //If the battle is a tie, make sure that all players involved are set to retreat
        if (GameplayManager.instance.reasonForWinning == "Draw: No Winner")
        {
            //Get all the GamePlayers involved in the battle
            LandScript battleSiteLandScript = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject.GetComponent<LandScript>();
            List<int> battlePlayerNumbers = new List<int>();
            foreach (KeyValuePair<uint, int> battleUnitPlayer in battleSiteLandScript.UnitNetIdsAndPlayerNumber)
            {
                bool isPlayerNumberInList = battlePlayerNumbers.Contains(battleUnitPlayer.Value);
                if (!isPlayerNumberInList)
                    battlePlayerNumbers.Add(battleUnitPlayer.Value);
            }

            if (battlePlayerNumbers.Count > 0)
            {
                //Check to see if the battle was a player base defense. you do not need to retreat if you are defending your base
                foreach (GamePlayer gamePlayer in Game.GamePlayers)
                {
                    if (battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                    {
                        if (gamePlayer.myPlayerBasePosition == battleSiteLandScript.gameObject.transform.position)
                        {
                            Debug.Log("CheckWhichPlayersNeedToRetreat: Player: " + gamePlayer.PlayerName + " was defending their base. They do not need to retreat on a draw.");
                            gamePlayer.doesPlayerNeedToRetreat = false;
                        }
                        else
                        {
                            gamePlayer.doesPlayerNeedToRetreat = true;
                            playersToRetreat.Add(gamePlayer);
                        }                        
                    }
                    else
                        gamePlayer.doesPlayerNeedToRetreat = false;
                }
            }
        }
        else
        {
            foreach (GamePlayer gamePlayer in Game.GamePlayers)
            {
                if (gamePlayer.playerNumber == GameplayManager.instance.loserOfBattlePlayerNumber && gamePlayer.ConnectionId == GameplayManager.instance.loserOfBattlePlayerConnId)
                {
                    gamePlayer.doesPlayerNeedToRetreat = true;
                    playersToRetreat.Add(gamePlayer);
                }
                else
                    gamePlayer.doesPlayerNeedToRetreat = false;
            }
        }
        CanPlayerRetreatFromBattle(playersToRetreat, GameplayManager.instance.currentBattleSite);
    }
    [Server]
    void CanPlayerRetreatFromBattle(List<GamePlayer> playersToRetreat, uint battleSiteId)
    {
        //Get list of land tiles 1 distance away from battle site
        List<LandScript> landsToRetreatTo = new List<LandScript>();
        GameObject battleSiteLand = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;
        GameObject allLand = GameObject.FindGameObjectWithTag("LandHolder");
        foreach (Transform landObject in allLand.transform)
        {
            if (landObject.transform.position != battleSiteLand.transform.position)
            {
                float distanceFromBattle = Vector3.Distance(battleSiteLand.transform.position, landObject.gameObject.transform.position);
                if (distanceFromBattle < 3.01f)
                {
                    Debug.Log("Can retreat to " + landObject.gameObject + ". Distance from battlesite: " + distanceFromBattle.ToString("0.00") + ". " + battleSiteLand.transform.position + " " + landObject.gameObject.transform.position);
                    LandScript landScript = landObject.gameObject.GetComponent<LandScript>();
                    landsToRetreatTo.Add(landScript);
                }
            }            
        }

        //Check if the available lands have units from another player on it. Player's can only retreat to lands with no units on them or only have their own units
        foreach (GamePlayer retreatingPlayer in playersToRetreat)
        {
            //Get number of units player has to retreat
            int numberOfUnitsToRetreat = 0;
            foreach (KeyValuePair<uint, int> unit in battleSiteLand.GetComponent<LandScript>().UnitNetIdsAndPlayerNumber)
            {
                if (unit.Value == retreatingPlayer.playerNumber)
                    numberOfUnitsToRetreat++;
            }
            //remove killed units from card power from the number to retreat
            int numberOfUnitsAlreadyKilled = 0;
            foreach (uint unit in GameplayManager.instance.unitNetIdsLost)
            {
                if (retreatingPlayer.playerUnitNetIds.Contains(unit))
                {
                    numberOfUnitsAlreadyKilled++;
                }
            }
            //numberOfUnitsToRetreat -= GameplayManager.instance.unitNetIdsLost.Count;
            numberOfUnitsToRetreat -= numberOfUnitsAlreadyKilled;
            Debug.Log(retreatingPlayer.PlayerName + ":" + retreatingPlayer.playerNumber + " must retreat " + numberOfUnitsToRetreat.ToString() + " number of units.");
            
            // If the retreating player has units remaining to retreat, check each land if there are enemy units on, and keep track of the available space on land. If there isn't enough "room" on a land tile to retreat all units, more units will need to be destroyed.
            int numberAvailableToRetreat = 0;
            if (numberOfUnitsToRetreat > 0)
            {
                List<LandScript> landWithNoEnemies = new List<LandScript>();
                foreach (LandScript landToRetreatTo in landsToRetreatTo)
                {
                    bool enemyUnitsOnLand = false;
                    foreach (KeyValuePair<uint, int> unit in landToRetreatTo.UnitNetIdsAndPlayerNumber)
                    {
                        if (unit.Value != retreatingPlayer.playerNumber)
                        {
                            enemyUnitsOnLand = true;
                            break;
                        }
                    }
                    if (!enemyUnitsOnLand)
                    {
                        Debug.Log("No enemy units on " + landToRetreatTo.gameObject);
                        if (landToRetreatTo.UnitNetIdsAndPlayerNumber.Count < 5)
                        {
                            // Check where the units are in relation to the requesting player's base. Player's will only be able to retreat toward their own base, or to a tile with the same x value as the battle site
                            // If the base's x position is greater than the unit's x position, the player is player 2 and their base is to their right
                            // If it is less than the unit's position, the player is player 1 and the base is to the left
                            bool isLandMovingAwayFromPlayerBase = false;
                            if (retreatingPlayer.myPlayerBasePosition.x > battleSiteLand.transform.position.x)
                            {
                                Debug.Log(retreatingPlayer.PlayerName + " must retreat to the RIGHT");
                                if (landToRetreatTo.gameObject.transform.position.x < battleSiteLand.transform.position.x)
                                {
                                    Debug.Log(landToRetreatTo.gameObject + " is to the LEFT of battle site " + battleSiteLand + ". " + retreatingPlayer.PlayerName + " CANNOT RETREAT HERE.");
                                    isLandMovingAwayFromPlayerBase = true;
                                }
                            }
                            else if (retreatingPlayer.myPlayerBasePosition.x < battleSiteLand.transform.position.x)
                            {
                                Debug.Log(retreatingPlayer.PlayerName + " must retreat to the LEFT");
                                if (landToRetreatTo.gameObject.transform.position.x > battleSiteLand.transform.position.x)
                                {
                                    Debug.Log(landToRetreatTo.gameObject + " is to the RIGHT of battle site " + battleSiteLand + ". " + retreatingPlayer.PlayerName + " CANNOT RETREAT HERE.");
                                    isLandMovingAwayFromPlayerBase = true;
                                }
                            }
                            else if (retreatingPlayer.myPlayerBasePosition.x == battleSiteLand.transform.position.x)
                            {
                                Debug.Log(retreatingPlayer.PlayerName + " is on their own base. Cannot retreat from own base.");
                                isLandMovingAwayFromPlayerBase = true;
                            }
                            if (!isLandMovingAwayFromPlayerBase)
                            {
                                Debug.Log("No enemy units AND available space to retreat to AND toward player base " + landToRetreatTo.gameObject);
                                landWithNoEnemies.Add(landToRetreatTo);
                                numberAvailableToRetreat += 5 - landToRetreatTo.UnitNetIdsAndPlayerNumber.Count;
                            }                            
                        }
                    }
                }
            }

            //Check if all units are able to retreat
            if (numberOfUnitsToRetreat <= numberAvailableToRetreat)
            {
                Debug.Log("Enough space for all of " + retreatingPlayer.PlayerName + ":" + retreatingPlayer.playerNumber + " to retreat all units.");
            }
            else if (numberAvailableToRetreat == 0)
            {
                Debug.Log("NO space for ANY of " + retreatingPlayer.PlayerName + ":" + retreatingPlayer.playerNumber + " to retreat units. ALL UNITS WILL BE DESTROYED!");
                KillUnitsFromBattle(retreatingPlayer, numberOfUnitsToRetreat, false);
                GameplayManager.instance.HandleUnitsLostFromRetreat(GameplayManager.instance.unitsLostFromRetreat, true);
            }
            else
            {
                int numberOfUnitsToDestroy = numberOfUnitsToRetreat - numberAvailableToRetreat;
                Debug.Log("Not enough space for " + retreatingPlayer.PlayerName + ":" + retreatingPlayer.playerNumber + " to retreat all units. " + numberOfUnitsToDestroy.ToString() + " will need to be destroyed.");
                KillUnitsFromBattle(retreatingPlayer, numberOfUnitsToDestroy, false);
                GameplayManager.instance.HandleUnitsLostFromRetreat(GameplayManager.instance.unitsLostFromRetreat, true);
            }
            
        }
    }
    [Server]
    void KillUnitsFromBattle(GamePlayer retreatingPlayer, int numberOfUnitsToKill, bool killReinforcingUnits)
    {
        Debug.Log("Executing KillUnitsThatCantRetreat. Will destroy " + numberOfUnitsToKill.ToString() + " for player: " + retreatingPlayer.PlayerName + ":" + retreatingPlayer.playerNumber.ToString());
        Debug.Log("KillUnitsFromBattle: Killing reinforcements? " + killReinforcingUnits.ToString());
        // Get all the player's units from the battle site
        GameObject battleSiteLand = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;
        List<uint> retreatingPlayerUnits = new List<uint>();
        if (killReinforcingUnits)
        {
            foreach (uint reinforcementNetId in retreatingPlayer.playerReinforcementNetIds)
            {
                if (!retreatingPlayerUnits.Contains(reinforcementNetId))
                    retreatingPlayerUnits.Add(reinforcementNetId);
            }
        }
        else
        {
            foreach (KeyValuePair<uint, int> unit in battleSiteLand.GetComponent<LandScript>().UnitNetIdsAndPlayerNumber)
            {
                if (unit.Value == retreatingPlayer.playerNumber)
                    retreatingPlayerUnits.Add(unit.Key);
            }
        }        

        //Remove uint IDs that have already been "killed" in UnitsLostFromBattle
        foreach (uint unitLost in GameplayManager.instance.unitNetIdsLost)
        {
            if (retreatingPlayerUnits.Contains(unitLost))
                retreatingPlayerUnits.Remove(unitLost);
        }

        if (retreatingPlayerUnits.Count > 0)
        {
            if (numberOfUnitsToKill > retreatingPlayerUnits.Count)
                numberOfUnitsToKill = retreatingPlayerUnits.Count;
            //Determine what remaining units are tanks versus what units are infantry. Kill tanks before infantry
            List<uint> playerTanks = new List<uint>();
            List<uint> playerInf = new List<uint>();
            foreach (uint unitNetId in retreatingPlayerUnits)
            {
                if (NetworkIdentity.spawned[unitNetId].gameObject.tag == "tank")
                {
                    playerTanks.Add(unitNetId);
                }
                else if (NetworkIdentity.spawned[unitNetId].gameObject.tag == "infantry")
                {
                    playerInf.Add(unitNetId);
                }
            }
            //Go through and kill tanks
            if (playerTanks.Count > 0)
            {
                foreach (uint unitNetId in playerTanks)
                {
                    GameplayManager.instance.unitNetIdsLost.Add(unitNetId);
                    GameplayManager.instance.numberOfTanksLost++;
                    numberOfUnitsToKill--;
                    if (numberOfUnitsToKill <= 0)
                        break;
                }
            }
            //Go through and kill infantry
            if (numberOfUnitsToKill > 0 && playerInf.Count > 0)
            {
                foreach (uint unitNetId in playerInf)
                {
                    GameplayManager.instance.unitNetIdsLost.Add(unitNetId);
                    GameplayManager.instance.numberOfInfLost++;
                    numberOfUnitsToKill--;
                    if (numberOfUnitsToKill <= 0)
                        break;
                }
            }
        }
    }
    [Server]
    void MovePlayedCardToDiscard()
    {
        Debug.Log("Executing MovePlayedCardToDiscard on the server");
        //Get game players involved in the battle
        List<GamePlayer> battlePlayers = new List<GamePlayer>();
        LandScript battleSiteLandScript = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject.GetComponent<LandScript>();
        List<int> battlePlayerNumbers = new List<int>();
        foreach (KeyValuePair<uint, int> battleUnitPlayer in battleSiteLandScript.UnitNetIdsAndPlayerNumber)
        {
            bool isPlayerNumberInList = battlePlayerNumbers.Contains(battleUnitPlayer.Value);
            if (!isPlayerNumberInList)
                battlePlayerNumbers.Add(battleUnitPlayer.Value);
        }

        if (battlePlayerNumbers.Count > 0)
        {
            foreach (GamePlayer gamePlayer in Game.GamePlayers)
            {
                if (battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                    battlePlayers.Add(gamePlayer);
                else if (GameplayManager.instance.isPlayerBaseDefense)
                {
                    if (gamePlayer.myPlayerBasePosition == battleSiteLandScript.gameObject.transform.position)
                    {
                        Debug.Log("MovePlayedCardToDiscard: Battle was a base defense and the defending player was found");
                        if (!battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                            battlePlayers.Add(gamePlayer);

                    }
                }
            }
        }

        // Move the player's card to the discard
        foreach (GamePlayer battlePlayer in battlePlayers)
        {
            if(battlePlayer.playerCardHandNetIds.Contains(battlePlayer.playerBattleCardNetId))
                battlePlayer.myPlayerCardHand.GetComponent<PlayerHand>().MoveCardToDiscard(battlePlayer.playerBattleCardNetId);
        }
        RpcClearSelectedCard();
    }
    [Server]
    void DestroyUnitsLostInBattle()
    {
        Debug.Log("Executing DestroyUnitsLostInBattle on the server");
        if (GameplayManager.instance.unitNetIdsLost.Count > 0)
        {
            Debug.Log("unitNetIdsLost is greater than 0, must destroy this many units: " + GameplayManager.instance.unitNetIdsLost.Count.ToString());
            bool didPlayerLoseAllUnits = false;
            foreach (uint unitNetId in GameplayManager.instance.unitNetIdsLost)
            {
                UnitScript unitNetIdScript = NetworkIdentity.spawned[unitNetId].gameObject.GetComponent<UnitScript>();

                foreach (GamePlayer gamePlayer in Game.GamePlayers)
                {
                    if (gamePlayer.ConnectionId == unitNetIdScript.ownerConnectionId && gamePlayer.playerNumber == unitNetIdScript.ownerPlayerNumber)
                    {
                        // Remove the unit net ids from the player's list of units and their army list
                        if (gamePlayer.playerUnitNetIds.Contains(unitNetId))
                            gamePlayer.playerUnitNetIds.Remove(unitNetId);
                        if (gamePlayer.playerArmyNetIds.Contains(unitNetId))
                            gamePlayer.playerArmyNetIds.Remove(unitNetId);

                        //Check the battle site's lists for the units that were destroyed and remove them from that list
                        LandScript battleSiteScript = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject.GetComponent<LandScript>();
                        if (battleSiteScript.UnitNetIdsOnLand.Contains(unitNetId))
                            battleSiteScript.UnitNetIdsOnLand.Remove(unitNetId);
                        if (battleSiteScript.UnitNetIdsAndPlayerNumber.ContainsKey(unitNetId))
                            battleSiteScript.UnitNetIdsAndPlayerNumber.Remove(unitNetId);

                        if (unitNetIdScript.currentLandOccupied)
                        {
                            if (unitNetIdScript.currentLandOccupied.transform.position != battleSiteScript.gameObject.transform.position)
                            {
                                Debug.Log("DestroyUnitsLostInBattle: Removing unit from their occupied land lists");
                                LandScript currentOccupiedLandScript = unitNetIdScript.currentLandOccupied.GetComponent<LandScript>();
                                if (currentOccupiedLandScript.UnitNetIdsOnLand.Contains(unitNetId))
                                    currentOccupiedLandScript.UnitNetIdsOnLand.Remove(unitNetId);
                                if (currentOccupiedLandScript.UnitNetIdsAndPlayerNumber.ContainsKey(unitNetId))
                                    currentOccupiedLandScript.UnitNetIdsAndPlayerNumber.Remove(unitNetId);
                            }
                        }

                        if (gamePlayer.playerUnitNetIds.Count == 0)
                        {
                            Debug.Log("No more units remaining for: " + gamePlayer.PlayerName);
                            didPlayerLoseAllUnits = true;
                        }
                        //RpcRemoveUnitFromLandLists(GameplayManager.instance.currentBattleSite, unitNetIdScript.gameObject);
                        break;
                    } 
                }
                GameObject unitToDestroy = NetworkIdentity.spawned[unitNetId].gameObject;
                if (unitToDestroy)
                    NetworkServer.Destroy(unitToDestroy);
                Debug.Log("Server destroyed unit with network id: " + unitNetId.ToString());
                unitToDestroy = null;
            }
            if (didPlayerLoseAllUnits)
            {
                Debug.Log("At least one player has no more remaining units after the battle.");
                CheckForWinnerFromLostUnits();
            }
        }
    }
    [Server]
    bool CheckIfUnitsAreLeftToRetreat()
    {
        Debug.Log("Executing CheckIfUnitsAreLeftToRetreat on the server.");
        bool areAnyUnitsLeftToRetreat = false;
        foreach (GamePlayer player in Game.GamePlayers)
        {
            if (player.doesPlayerNeedToRetreat)
            {
                foreach (uint unit in player.playerArmyNetIds)
                {
                    if (!GameplayManager.instance.unitNetIdsLost.Contains(unit))
                    {
                        Debug.Log("At least one unit left to retreat");
                        areAnyUnitsLeftToRetreat = true;
                        break;
                    }
                }
            }
            if (areAnyUnitsLeftToRetreat)
                break;
        }
        Debug.Log("CheckIfUnitsAreLeftToRetreat: will return: " + areAnyUnitsLeftToRetreat.ToString());
        return areAnyUnitsLeftToRetreat;
    }
    [Server]
    void AfterBattleCleanUp()
    {
        Debug.Log("Executing: AfterBattleCleanUp on the server");
        //Reset a bunch of syncvars and stuff back to false that were set during previous battle scenario
        GameplayManager.instance.HandleAreBattleResultsSet(GameplayManager.instance.areBattleResultsSet, false);
        GameplayManager.instance.HandleAreUnitsLostCalculated(GameplayManager.instance.unitsLostCalculated,false);
        GameplayManager.instance.HandleUnitsLostFromRetreat(GameplayManager.instance.unitsLostFromRetreat, false);
        GameplayManager.instance.HandleReinforcementsLost(GameplayManager.instance.reinforcementsLost, false);
        List<GamePlayer> battlePlayers = new List<GamePlayer>();
        foreach (GamePlayer player in Game.GamePlayers)
        {
            //RpcUpdateIsPlayerBattleScoreSet();
        }
        
    }
    [Server]
    bool VerifyAllUnitsRetreated()
    {
        Debug.Log("Executing VerifyAllUnitsRetreated on the server.");
        bool allUnitsRetreated = false;

        GameObject battleSite = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;

        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (gamePlayer.doesPlayerNeedToRetreat)
            {
                Debug.Log("VerifyAllUnitsRetreated: Retreating player found: " + gamePlayer.PlayerName);
                foreach (uint unitToRetreatNetId in gamePlayer.playerArmyNetIds)
                {
                    GameObject unitToRetreat = NetworkIdentity.spawned[unitToRetreatNetId].gameObject;
                    if (unitToRetreat.GetComponent<UnitScript>().newPosition == battleSite.transform.position)
                    {
                        Debug.Log("VerifyAllUnitsRetreated: At least one unit HAS NOT retreated off the battle site");
                        allUnitsRetreated = false;
                        break;
                    }
                    else 
                    {
                        Debug.Log("VerifyAllUnitsRetreated: Unit has retreated from the battle site");
                        allUnitsRetreated = true;
                    }
                }
                if (!allUnitsRetreated)
                {
                    Debug.Log("VerifyAllUnitsRetreated: At least one unit did not retreat. Returning false");
                    break;
                }
            }   
        }
        return allUnitsRetreated;
    }
    [Server]
    void CleanupPreviousBattleInfo()
    {
        //Remove the previous battle site from the list of battle sites based on its battle number
        if(GameplayManager.instance.battleNumber != 0)
            GameplayManager.instance.battleSiteNetIds.Remove(GameplayManager.instance.battleNumber);
        GameplayManager.instance.BattleSitesHaveBeenSet = false;
        
        RpcUpdatedUnitPositionsForBattleSites();
        //RpcRemoveBattleHighlightAndBattleTextFromPreviousBattle(GameplayManager.instance.currentBattleSite);
        GameplayManager.instance.RpcRemoveBattleHighlightAndBattleTextFromPreviousBattle(GameplayManager.instance.currentBattleSite);
        GameplayManager.instance.HandleCurrentBattleSiteUpdate(GameplayManager.instance.currentBattleSite, 0);
        GameplayManager.instance.HandleIsPlayerBaseDefense(GameplayManager.instance.isPlayerBaseDefense, false);

        //Cleanup stuff for reinforcements
        CleanupPlayerReinforcementInfo();
        CleanupGameplayManagerReinforcementInfo();

        //Cleanup stuff for card abilities
        RpcClearCardAbilityInfo();
    }
    /*
    [ClientRpc]
    public void RpcRemoveBattleHighlightAndBattleTextFromPreviousBattle(uint battleSiteNetId)
    {
        Debug.Log("RpcRemoveBattleHighlightAndBattleTextFromPreviousBattle: Instructing player to remove battle highlight and text on land with net id: battleSiteNetId");
        LandScript battleSiteScript = NetworkIdentity.spawned[battleSiteNetId].gameObject.GetComponent<LandScript>();
        battleSiteScript.RemoveBattleSiteHighlightAndText();
        battleSiteScript.ResetUnitPositionAndUnitTextAfterBattle();
    }
    */
    [Server]
    bool CheckForMoreBattles()
    {
        Debug.Log("Executing CheckForMoreBattles");
        bool moreBattles = false;
        if (GameplayManager.instance.battleSiteNetIds.Count > 0)
        {
            Debug.Log("CheckForMoreBattles: More battle remain to fight.");
            moreBattles = true;
        }
        if (!moreBattles)
        {
            Debug.Log("CheckForMoreBattles: no battles remaining. Resetting the reinforcement values for all units.");
            ResetUnitReinforcementValues();
        }
        return moreBattles;
    }
    [Server]
    void ChangeToNextBattle()
    {
        Debug.Log("Executing ChangeToNextBattle on the server.");
        GameplayManager.instance.battleNumber++;
        if (GameplayManager.instance.battleSiteNetIds.ContainsKey(GameplayManager.instance.battleNumber))
        {
            bool areThereReinforcements = CheckForPossibleReinforcements();
            if (areThereReinforcements)
            {
                Debug.Log("ChangeToNextBattle: Reinforcements available for at least 1 player. changing to reinforcements phase.");
                GameplayManager.instance.HandleReinforcementsBattleSiteUpdate(GameplayManager.instance.reinforcementsBattleSite, GameplayManager.instance.battleSiteNetIds[GameplayManager.instance.battleNumber]);
                Game.CurrentGamePhase = "Reinforcements:\nBattle #" + GameplayManager.instance.battleNumber.ToString();
                Debug.Log("ChangeToNextBattle: Game phase changed to " + Game.CurrentGamePhase);
            }
            else
            {
                Debug.Log("ChangeToNextBattle: No reinforcements available. Changing to next battle/Choose Cards");
                GameplayManager.instance.HandleCurrentBattleSiteUpdate(GameplayManager.instance.currentBattleSite, GameplayManager.instance.battleSiteNetIds[GameplayManager.instance.battleNumber]);
                Game.CurrentGamePhase = "Choose Cards:\nBattle #" + GameplayManager.instance.battleNumber.ToString();
                Debug.Log("ChangeToNextBattle: Game phase changed to " + Game.CurrentGamePhase);
            }
        }
    }
    [ClientRpc]
    void RpcUpdateIsPlayerBattleScoreSet()
    {
        Debug.Log("Executing RpcUpdateIsPlayerBattleScoreSet for player: " + this.PlayerName);
        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();
        LocalGamePlayerScript.UpdateIsPlayerBattleScoreSet();
    }
    public void UpdateIsPlayerBattleScoreSet()
    {
        if (hasAuthority)
        {
            Debug.Log("UpdateIsPlayerBattleScoreSet call from local player object. isPlayerBattleScoreSet is set to: " + isPlayerBattleScoreSet.ToString() + " for player: " + this.PlayerName);
            if (isPlayerBattleScoreSet)
            {
                Debug.Log("Executing CmdUpdateIsPlayerBattleScoreSet to set isPlayerBattleScoreSet to false on the server.");
                CmdUpdateIsPlayerBattleScoreSet();
            }
                
        }
    }
    [Command]
    void CmdUpdateIsPlayerBattleScoreSet()
    {        
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdUpdateIsPlayerBattleScoreSet for " + requestingPlayer.PlayerName);
        requestingPlayer.HandleBattleScoreSet(requestingPlayer.isPlayerBattleScoreSet, false);
    }
    [ClientRpc]
    void RpcClearSelectedCard()
    {
        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();
        LocalGamePlayerScript.ClearSelectedCard();
    }
    public void ClearSelectedCard()
    {
        if (hasAuthority && selectedCard)
            CmdClearSelectedCard();
    }
    [Command]
    void CmdClearSelectedCard()
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdClearSelectedCard for " + requestingPlayer.PlayerName);
        requestingPlayer.HandleUpdatedPlayerBattleCard(playerBattleCardNetId, 0);
    }
    [ClientRpc]
    void RpcUpdatedUnitPositionsForBattleSites()
    {
        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();
        LocalGamePlayerScript.ResetUpdatedUnitPositionsForBattleSites();
    }
    void ResetUpdatedUnitPositionsForBattleSites()
    {
        if (hasAuthority)
            CmdResetUpdatedUnitPositionsForBattleSites();
    }
    [Command]
    void CmdResetUpdatedUnitPositionsForBattleSites()
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdResetUpdatedUnitPositionsForBattleSites for " + requestingPlayer.PlayerName);
        requestingPlayer.updatedUnitPositionsForBattleSites = false;
    }
    [Server]
    bool CheckForNewBattlesAfterDrawRetreat()
    {
        Debug.Log("Executing CheckForNewBattlesAfterDrawRetreat on the server.");
        bool areThereNewBattlesAfterRetreat = false;

        GameObject landTileHolder = GameObject.FindGameObjectWithTag("LandHolder");
        foreach (Transform landObject in landTileHolder.transform)
        {
            //check land objects not already in the battle site list
            if (!GameplayManager.instance.battleSiteNetIds.Any(b => b.Value == landObject.GetComponent<NetworkIdentity>().netId))
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
                            areThereNewBattlesAfterRetreat = true;
                            //int battleNumber = GameplayManager.instance.battleSiteNetIds.Count + 1;
                            int battleNumber = 0;
                            //Get the largest "battle number" in the dictionary. Dictionary values would have been removed at this point so can't just use battleSiteNetIds.Count+1
                            foreach (KeyValuePair<int, uint> battleSite in GameplayManager.instance.battleSiteNetIds)
                            {
                                if (battleNumber < battleSite.Key)
                                    battleNumber = battleSite.Key;
                            }
                            battleNumber++;
                            GameplayManager.instance.battleSiteNetIds.Add(battleNumber, landObject.GetComponent<NetworkIdentity>().netId);
                            break;
                        }
                        playerNumber = units.Value;
                    }
                }
            }
        }
        return areThereNewBattlesAfterRetreat;
    }
    [Server]
    void CheckForWinnerFromLostUnits()
    {
        Debug.Log("Executing CheckForWinnerFromLostUnits on the server.");
        List<GamePlayer> playersWithNoUnits = new List<GamePlayer>();
        List<GamePlayer> playersWithUnits = new List<GamePlayer>();

        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (gamePlayer.playerUnitNetIds.Count == 0)
                playersWithNoUnits.Add(gamePlayer);
            else
                playersWithUnits.Add(gamePlayer);
        }

        if (playersWithUnits.Count == 1)
        {
            Debug.Log("Only one player still has units. That player wins the game. Player is: " + playersWithUnits[0].PlayerName);
            playersWithUnits[0].HandlePlayerWin(playersWithUnits[0].didPlayerWin, true);
        }
        else
        {
            Debug.Log("More than 1 player with units remain. Game still continues...");
        }
        foreach (GamePlayer loser in playersWithNoUnits)
        {
            loser.HandlePlayerEliminatedFromUnitsLost(loser.wasPlayerEliminatedFromUnitsLost, true);
        }
    }
    [Server]
    void CheckForWinnerFromPlayerBaseBattle()
    {
        Debug.Log("Executing CheckForWinnerFromPlayerBaseBattle on the server");
        bool didPlayerLoseBaseDefense = false;

        GameObject battleSite = NetworkIdentity.spawned[GameplayManager.instance.currentBattleSite].gameObject;
        GamePlayer defendingPlayer = null;
        foreach (GamePlayer player in Game.GamePlayers)
        {
            if (player.myPlayerBasePosition == battleSite.transform.position)
            {
                Debug.Log("CheckForWinnerFromPlayerBaseBattle: found defending player: " + player.PlayerName);
                defendingPlayer = player;
                break;
            }
        }
        //Check if the defending player lost the base defense. If they did, set wasPlayerEliminatedFromBaseCaptured to true;
        if (defendingPlayer)
        {
            if (defendingPlayer.PlayerName == GameplayManager.instance.loserOfBattleName && defendingPlayer.ConnectionId == GameplayManager.instance.loserOfBattlePlayerConnId && defendingPlayer.playerNumber == GameplayManager.instance.loserOfBattlePlayerNumber)
            {
                Debug.Log("Player lost their base defense: " + defendingPlayer.PlayerName);
                defendingPlayer.HandlePlayerEliminatedFromBaseCaptured(defendingPlayer.wasPlayerEliminatedFromBaseCaptured, true);
                didPlayerLoseBaseDefense = true;
            }
            Debug.Log("Defending player: " + defendingPlayer.PlayerName + " successfully defending their base.");
        }
        // Check to see if more than 1 player remaining in game. If only 1 remains, declare that player the winner of the game
        if (didPlayerLoseBaseDefense)
        {
            List<GamePlayer> remainingPlayers = new List<GamePlayer>();
            foreach (GamePlayer player in Game.GamePlayers)
            {
                if (!player.wasPlayerEliminatedFromBaseCaptured && !player.wasPlayerEliminatedFromUnitsLost)
                {
                    Debug.Log("CheckForWinnerFromPlayerBaseBattle: Player who was no eliminated by player base capture or lost units: " + player.PlayerName);
                    remainingPlayers.Add(player);
                }
            }
            if (remainingPlayers.Count == 1)
            {
                Debug.Log("Only 1 player remaining. Naming the following player the winner of the game: " + remainingPlayers[0].PlayerName);
                remainingPlayers[0].HandlePlayerWin(remainingPlayers[0].didPlayerWin, true);
            }
        }
    }
    void HandlePlayerWin(bool oldValue, bool newValue)
    {
        if (isServer)
            didPlayerWin = newValue;
        if (newValue && hasAuthority)
        {
            GameplayManager.instance.LocalPlayerVictory();
        }
    }
    void HandlePlayerEliminatedFromUnitsLost(bool oldValue, bool newValue)
    {
        if (isServer)
            wasPlayerEliminatedFromUnitsLost = newValue;
        if (newValue && hasAuthority)
        {
            GameplayManager.instance.LocalPlayerEliminatedByUnitsLost();
        }
    }
    void HandlePlayerEliminatedFromBaseCaptured(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            wasPlayerEliminatedFromBaseCaptured = newValue;
        }
        if (newValue && hasAuthority)
        {
            GameplayManager.instance.LocalPlayerEliminatedByBaseCaptured();
        }
    }
    public void HostLostGame()
    { 
        if(Game.GamePlayers.Contains(this))
            Game.GamePlayers.Remove(this);
    }
    public void HostShutDownServer()
    {
        Game.HostShutDownServer();
    }
    public void QuitGame()
    {
        if (hasAuthority)
        {
            if (isServer)
            {
                Game.StopHost();
                Game.HostShutDownServer();
            }
            else
            {
                Game.StopClient();
                Game.HostShutDownServer();
            }
        }
    }
    [Server]
    bool CheckForPossibleReinforcements()
    {
        Debug.Log("Executing CheckForPossibleReinforcements on the server");
        bool canPlayersReinforce = false;

        uint battleSiteNetId = GameplayManager.instance.battleSiteNetIds[GameplayManager.instance.battleNumber];
        GameObject battleSite = NetworkIdentity.spawned[battleSiteNetId].gameObject;
        LandScript battleSiteLandScript = battleSite.GetComponent<LandScript>();         

        //Get a list of the lands within 1 tile of the battle site
        List<LandScript> landsToReinforceFrom = new List<LandScript>();
        Debug.Log("CheckForPossibleReinforcements: Checking for possible reinforcements for battle site with network id: " + battleSiteNetId.ToString());
        GameObject allLand = GameObject.FindGameObjectWithTag("LandHolder");
        foreach (Transform landObject in allLand.transform)
        {
            float disFromBattle = Vector3.Distance(landObject.transform.position, battleSite.transform.position);
            if (disFromBattle < 3.01f)
            {
                //check if the land object is already a battle site. If so, can't reinforce from a battle site
                bool isLandABattleSite = false;
                foreach (KeyValuePair<int, uint> battleSites in GameplayManager.instance.battleSiteNetIds)
                {
                    if (battleSites.Value == landObject.gameObject.GetComponent<NetworkIdentity>().netId)
                    {
                        Debug.Log("CheckForPossibleReinforcements: " + landObject.gameObject + " is a battle site. Can't reinforce from here.");
                        isLandABattleSite = true;
                    }
                }
                if (landObject.transform.position == battleSite.transform.position)
                    isLandABattleSite = true;
                if (!isLandABattleSite)
                {
                    LandScript landScript = landObject.gameObject.GetComponent<LandScript>();
                    if (landScript.UnitNetIdsAndPlayerNumber.Count > 0)
                    {
                        Debug.Log("CheckForPossibleReinforcements: " + landObject.gameObject + " has at least 1 unit and isn't a battle site. Adding to landsToReinforceFrom list");
                        landsToReinforceFrom.Add(landScript);
                    }
                }
            }
        }

        //If there are any lands to reinforce from, check which players can reinforce
        if (landsToReinforceFrom.Count > 0)
        {
            //Get a list of all players involved in the battle
            List<GamePlayer> battlePlayers = new List<GamePlayer>();
            List<int> battlePlayerNumbers = new List<int>();
            foreach (KeyValuePair<uint, int> battleUnitPlayer in battleSiteLandScript.UnitNetIdsAndPlayerNumber)
            {
                bool isPlayerNumberInList = battlePlayerNumbers.Contains(battleUnitPlayer.Value);
                if (!isPlayerNumberInList)
                    battlePlayerNumbers.Add(battleUnitPlayer.Value);
            }

            if (battlePlayerNumbers.Count > 0)
            {
                foreach (GamePlayer gamePlayer in Game.GamePlayers)
                {
                    if (battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                        battlePlayers.Add(gamePlayer);
                    else if (battleSite.transform.position == gamePlayer.myPlayerBasePosition)
                    {
                        if (!battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                            battlePlayers.Add(gamePlayer);
                    }
                }
            }

            // clear out previous reinforcement data for battle players
            foreach (GamePlayer gamePlayer in battlePlayers)
            {
                gamePlayer.unitsAvailableForReinforcement.Clear();
                gamePlayer.playerReinforcementNetIds.Clear();
            }

            // Go through each land in landsToReinforceFrom, see which players have units that can reinforce on them
            // If a player can reinforce, set their canPlayerReinforce value to true
            // Check each unit to see if they can reinforce the battle. If they can reinforce, set their canUnitReinforce to true.
            
            foreach (LandScript landScript in landsToReinforceFrom)
            {
                foreach (KeyValuePair<uint, int> unit in landScript.UnitNetIdsAndPlayerNumber)
                {
                    if (battlePlayerNumbers.Contains(unit.Value))
                    {
                        Debug.Log("CheckForPossibleReinforcements: Found unit on land for player number: " + unit.Value.ToString() + " on land object: " + landScript.gameObject);
                        foreach (GamePlayer gamePlayer in battlePlayers)
                        {
                            if (gamePlayer.playerNumber == unit.Value)
                            {
                                UnitScript unitScript = NetworkIdentity.spawned[unit.Key].gameObject.GetComponent<UnitScript>();
                                if (!unitScript.canUnitReinforce && !unitScript.isUnitReinforcingBattle)
                                {
                                    Debug.Log("CheckForPossibleReinforcements: Unit has player owner number of: " + unitScript.ownerPlayerNumber.ToString() + " and the battle player number is: " + gamePlayer.playerNumber + ". Unit has not reinforced for a previous battle. Setting unit as a reinforceable unit.");
                                    unitScript.canUnitReinforce = true;

                                    if (!gamePlayer.canPlayerReinforce)
                                        gamePlayer.HandleCanPlayerReinforce(gamePlayer.canPlayerReinforce, true);
                                        
                                    if (!canPlayersReinforce)
                                        canPlayersReinforce = true;

                                    if (!gamePlayer.unitsAvailableForReinforcement.Contains(unitScript.gameObject.GetComponent<NetworkIdentity>().netId))
                                        gamePlayer.unitsAvailableForReinforcement.Add(unitScript.gameObject.GetComponent<NetworkIdentity>().netId);
                                }
                                else if (unitScript.isUnitReinforcingBattle)
                                {
                                    Debug.Log("CheckForPossibleReinforcements: Unit reinforced on a previous battle. Setting canUnitReinforce to false");
                                    unitScript.canUnitReinforce = false;
                                }
                                else
                                {
                                    unitScript.canUnitReinforce = false;
                                }
                                break;
                            }
                            
                        }
                    }
                }
            }
        }
        return canPlayersReinforce;
    }
    void HandleCanPlayerReinforce(bool oldValue, bool newValue)
    {
        if (isServer)
            canPlayerReinforce = newValue;
        if (isClient && hasAuthority)
            GameplayManager.instance.CanLocalPlayerReinforce(newValue);
    }
    public void SelectUnitsForReinfocements(List<GameObject> unitsToReinforce)
    {
        if (hasAuthority)
        {
            Debug.Log("Executing SelectUnitsForReinfocements on gameplayer you have authority over");
            List<uint> UnitsToReinforceNetIds = new List<uint>();
            if (unitsToReinforce.Count > 0)
            {
                foreach (GameObject unit in unitsToReinforce)
                {
                    if(!UnitsToReinforceNetIds.Contains(unit.GetComponent<NetworkIdentity>().netId))
                        UnitsToReinforceNetIds.Add(unit.GetComponent<NetworkIdentity>().netId);
                }
            }
            Debug.Log("SelectUnitsForReinfocements: Will submit this many units to add as reinforcements: " + UnitsToReinforceNetIds.Count.ToString());
            CmdPlayerSelectUnitsForReinforcements(UnitsToReinforceNetIds);
        }
    }
    [Command]
    void CmdPlayerSelectUnitsForReinforcements(List<uint> UnitsToReinforceNetIds)
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        
        //Clear out previous data
        requestingPlayer.playerReinforcementNetIds.Clear();
        requestingPlayer.playerReinforcementsNumberOfInf = 0;
        requestingPlayer.playerReinforcementsNumberOfTanks = 0;

        Debug.Log("Executing CmdPlayerSelectUnitsForReinforcements for: " + requestingPlayer.PlayerName + ":" + requestingPlayer.ConnectionId);
        if (UnitsToReinforceNetIds.Count > 0)
        {
            Debug.Log("CmdPlayerSelectUnitsForReinforcements: Player is selecting " + UnitsToReinforceNetIds.Count.ToString() + " reinforcements");
            foreach (uint unitNetId in UnitsToReinforceNetIds)
            { 
                UnitScript unitNetIdScript = NetworkIdentity.spawned[unitNetId].gameObject.GetComponent<UnitScript>();
                if (unitNetIdScript.canUnitReinforce && !requestingPlayer.playerReinforcementNetIds.Contains(unitNetId) && !unitNetIdScript.isUnitReinforcingBattle)
                {
                    Debug.Log("CmdPlayerSelectUnitsForReinforcements: Add the following unit with net id: " + unitNetId.ToString() + " to reinforcement list of player: " + requestingPlayer.PlayerName);
                    requestingPlayer.playerReinforcementNetIds.Add(unitNetId);
                    if (unitNetIdScript.gameObject.tag == "tank")
                        requestingPlayer.playerReinforcementsNumberOfTanks++;
                    else if(unitNetIdScript.gameObject.tag == "infantry")
                        requestingPlayer.playerReinforcementsNumberOfInf++;
                }
                    
            }
        }
        else
        {
            Debug.Log("CmdPlayerSelectUnitsForReinforcements: Player is selecting 0 reinforcements");
            requestingPlayer.playerReinforcementNetIds.Clear();
            requestingPlayer.playerReinforcementsNumberOfInf = 0;
            requestingPlayer.playerReinforcementsNumberOfTanks = 0;
        }
        TargetUpdateReinforcementText(connectionToClient, requestingPlayer.playerReinforcementsNumberOfInf, requestingPlayer.playerReinforcementsNumberOfTanks);
    }
    [TargetRpc]
    void TargetUpdateReinforcementText(NetworkConnection target, int numberOfInf, int numberOfTanks)
    {
        Debug.Log("Executing TargetUpdateReinforcementText");
        if (hasAuthority)
        {
            GameplayManager.instance.UpdateReinforcementText(numberOfInf, numberOfTanks);
        }
    }
    [Server]
    void SetIsUnitReinforcingBattleOnUnits()
    {
        Debug.Log("Executing SetIsUnitReinforcingBattleOnUnits on the server");
        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (gamePlayer.playerReinforcementNetIds.Count > 0)
            {
                gamePlayer.didPlayerReinforce = true;
                GameplayManager.instance.HandleDidAPlayerReinforce(GameplayManager.instance.didAPlayerReinforce, true);
                foreach (uint unitNetId in gamePlayer.playerReinforcementNetIds)
                { 
                    UnitScript unitNetIdScript = NetworkIdentity.spawned[unitNetId].gameObject.GetComponent<UnitScript>();
                    if (unitNetIdScript.canUnitReinforce && !unitNetIdScript.isUnitReinforcingBattle)
                    {
                        Debug.Log("SetIsUnitReinforcingBattleOnUnits: set isUnitReinforcingBattle for unit: " + unitNetIdScript.gameObject + " that has a network id of: " + unitNetId.ToString());
                        unitNetIdScript.isUnitReinforcingBattle = true;
                    }
                }
            }
        }
    }
    [Server]
    void CleanupPlayerReinforcementInfo()
    {
        Debug.Log("Executing CleanupPlayerReinforcementInfo on the server.");
        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            if (gamePlayer.didPlayerReinforce)
            {
                gamePlayer.HandleCanPlayerReinforce(gamePlayer.canPlayerReinforce, false);

                gamePlayer.playerReinforcementsNumberOfInf = 0;
                gamePlayer.playerReinforcementsNumberOfTanks = 0;
                gamePlayer.playerArmyReinforcementPower = 0;

                /*if (gamePlayer.playerReinforcementNetIds.Count > 0)
                {
                    Debug.Log("CleanupPlayerReinforcementInfo: setting reinforcing units canUnitReinforce value to false");
                    foreach (uint reinforcementNetId in gamePlayer.playerReinforcementNetIds)
                    { 
                        UnitScript reinforcementNetIdScript = NetworkIdentity.spawned[reinforcementNetId].gameObject.GetComponent<UnitScript>();
                        if (reinforcementNetIdScript.canUnitReinforce && reinforcementNetIdScript.isUnitReinforcingBattle)
                        {
                            reinforcementNetIdScript.HandleCanUnitReinforce(reinforcementNetIdScript.canUnitReinforce, false);
                        }
                    }
                }*/

                gamePlayer.playerReinforcementNetIds.Clear();
                gamePlayer.didPlayerReinforce = false;   
            }
            //Set all units with canReinforce as true back to false
            GameObject unitHolder = gamePlayer.myUnitHolder;
            PlayerUnitHolder unitHolderScript = unitHolder.GetComponent<PlayerUnitHolder>();
            if (unitHolderScript.ownerConnectionId == gamePlayer.ConnectionId)
            {
                Debug.Log("CleanupPlayerReinforcementInfo: setting all units canUnitReinforce value to false for player: " + gamePlayer.PlayerName);
                foreach (Transform unitChild in unitHolder.transform)
                {
                    UnitScript unitChildScript = unitChild.GetComponent<UnitScript>();
                    if (unitChildScript.canUnitReinforce)
                    {
                        unitChildScript.HandleCanUnitReinforce(unitChildScript.canUnitReinforce, false);
                    }
                }
            }
        }
    }
    [Server]
    void CleanupGameplayManagerReinforcementInfo()
    {
        Debug.Log("Executing: CleanupGameplayManagerReinforcementInfo");
        /*if (GameplayManager.instance.didAPlayerReinforce)
        {
            GameplayManager.instance.HandleReinforcementsBattleSiteUpdate(GameplayManager.instance.reinforcementsBattleSite, 0);
            GameplayManager.instance.HandleDidAPlayerReinforce(GameplayManager.instance.didAPlayerReinforce, false);
        }*/
        GameplayManager.instance.HandleReinforcementsBattleSiteUpdate(GameplayManager.instance.reinforcementsBattleSite, 0);
        GameplayManager.instance.HandleDidAPlayerReinforce(GameplayManager.instance.didAPlayerReinforce, false);
    }
    [Server]
    void ResetUnitReinforcementValues()
    {
        Debug.Log("Executing ResetUnitReinforcementValues on the server");
        foreach (GamePlayer gamePlayer in Game.GamePlayers)
        {
            GameObject unitHolder = gamePlayer.myUnitHolder;
            PlayerUnitHolder unitHolderScript = unitHolder.GetComponent<PlayerUnitHolder>();
            if (unitHolderScript.ownerConnectionId == gamePlayer.ConnectionId)
            {
                Debug.Log("CleanupPlayerReinforcementInfo: setting all units reinforcement values to false for player: " + gamePlayer.PlayerName);
                foreach (Transform unitChild in unitHolder.transform)
                {
                    UnitScript unitChildScript = unitChild.GetComponent<UnitScript>();
                    if (unitChildScript.canUnitReinforce)
                    {
                        unitChildScript.HandleCanUnitReinforce(unitChildScript.canUnitReinforce, false);
                    }
                    if (unitChildScript.isUnitReinforcingBattle)
                    {
                        unitChildScript.isUnitReinforcingBattle = false;
                    }
                }
            }
        }

    }
    [Server]
    bool CheckIfCardPowerActivatesForBattle()
    {
        Debug.Log("Executing CheckIfCardPowerActivatesForBattle on the server");

        //Dictionary<int, int> playerNumberAndCardAbility = new Dictionary<int, int>();
        //cardAbilityNumberAndPlayerNumber.Add(0, 0);
        bool didPalyerCardAbilityActivate = false;

        List<GamePlayer> BattlePlayers = GetBattlePlayers(GameplayManager.instance.currentBattleSite);
        if (BattlePlayers.Count > 0)
        {
            List<Card> battlePlayerCards = new List<Card>();
            foreach (GamePlayer battlePlayer in BattlePlayers)
            {
                battlePlayerCards.Add(NetworkIdentity.spawned[battlePlayer.playerBattleCardNetId].gameObject.GetComponent<Card>());
            }
            if (battlePlayerCards.Any(x => x.SpecialAbilityNumber != 0))
            {
                Debug.Log("CheckIfCardPowerActivatesForBattle: at least 1 player battle card has a special ability to check.");
                /*foreach (GamePlayer battlePlayer in BattlePlayers)
                {
                    Card playerBattleCard = NetworkIdentity.spawned[battlePlayer.playerBattleCardNetId].gameObject.GetComponent<Card>();
                }*/
                foreach (Card playerBattleCard in battlePlayerCards)
                {
                    Debug.Log("CheckIfCardPowerActivatesForBattle: Card to check: " + playerBattleCard.CardName + " with special ability number: " + playerBattleCard.SpecialAbilityNumber.ToString() + " for player: " + playerBattleCard.ownerPlayerName);
                    if (playerBattleCard.SpecialAbilityNumber == 0)
                    {
                        Debug.Log("CheckIfCardPowerActivatesForBattle: found player battle card that has special ability 0 for player: " + playerBattleCard.ownerPlayerName + ". Moving to next card.");
                        continue;
                    }
                    else if (playerBattleCard.SpecialAbilityNumber == 1)
                    {
                        Debug.Log("CheckIfCardPowerActivatesForBattle: found player battle card that has special ability 1 for player: " + playerBattleCard.ownerPlayerName);
                        foreach (GamePlayer battlePlayer in BattlePlayers)
                        {
                            if (battlePlayer.playerNumber == playerBattleCard.ownerPlayerNumber && battlePlayer.ConnectionId == playerBattleCard.ownerConnectionId)
                            {
                                if (DidCardAbility1Activate(battlePlayer))
                                {
                                    Debug.Log("CheckIfCardPowerActivatesForBattle: DidCardAbility1Activate returned true for player: " + battlePlayer.PlayerName);
                                    //playerNumberAndCardAbility.Add(battlePlayer.playerNumber, 1);
                                    battlePlayer.HandlePlayerCardAbilityActivated(battlePlayer.didPlayerCardAbilityActivate, true);
                                    battlePlayer.playerCardAbilityNumber = 1;
                                    didPalyerCardAbilityActivate = true;
                                }
                                else
                                {
                                    Debug.Log("CheckIfCardPowerActivatesForBattle: DidCardAbility1Activate returned FALSE for player: " + battlePlayer.PlayerName);
                                }
                            }
                        }
                    }
                    else if (playerBattleCard.SpecialAbilityNumber == 2)
                    {
                        Debug.Log("CheckIfCardPowerActivatesForBattle: found player battle card that has special ability 2 for player: " + playerBattleCard.ownerPlayerName);
                        foreach (GamePlayer battlePlayer in BattlePlayers)
                        {
                            if (battlePlayer.playerNumber == playerBattleCard.ownerPlayerNumber && battlePlayer.ConnectionId == playerBattleCard.ownerConnectionId)
                            {
                                if (DidCardAbility2Activate(battlePlayer, BattlePlayers))
                                {
                                    Debug.Log("CheckIfCardPowerActivatesForBattle: DidCardAbility2Activate returned true for player: " + battlePlayer.PlayerName);
                                    //playerNumberAndCardAbility.Add(battlePlayer.playerNumber, 1);
                                    battlePlayer.HandlePlayerCardAbilityActivated(battlePlayer.didPlayerCardAbilityActivate, true);
                                    battlePlayer.playerCardAbilityNumber = 2;
                                    playerBattleCard.HandleAbilityActivated(playerBattleCard.didAbilityActivate, true);
                                    didPalyerCardAbilityActivate = true;
                                }
                                else
                                {
                                    Debug.Log("CheckIfCardPowerActivatesForBattle: DidCardAbility2Activate returned FALSE for player: " + battlePlayer.PlayerName);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Log("CheckIfCardPowerActivatesForBattle: No player battle cards had any special abilities. Returning 0");
                //playerNumberAndCardAbility.Clear();
                //Add card ability number of 0 and player number of 0 aka no special ability
                //cardAbilityNumberAndPlayerNumber.Add(0, 0);
                //return playerNumberAndCardAbility;
                didPalyerCardAbilityActivate = false;
                return didPalyerCardAbilityActivate;

            }
        }
        //Debug.Log("CheckIfCardPowerActivatesForBattle: returning the following value for what card ability was activated: " + playerNumberAndCardAbility.ToString());
        //return playerNumberAndCardAbility;
        Debug.Log("CheckIfCardPowerActivatesForBattle: returning the following value: " + didPalyerCardAbilityActivate.ToString());
        return didPalyerCardAbilityActivate;
    }
    [Server]
    List<GamePlayer> GetBattlePlayers(uint battleSiteNetId)
    {
        Debug.Log("Executing GetBattlePlayers on the server.");
        List<GamePlayer> BattlePlayers = new List<GamePlayer>();

        LandScript battleSiteLandScript = NetworkIdentity.spawned[battleSiteNetId].gameObject.GetComponent<LandScript>();
        List<int> battlePlayerNumbers = new List<int>();
        foreach (KeyValuePair<uint, int> battleUnitPlayer in battleSiteLandScript.UnitNetIdsAndPlayerNumber)
        {
            bool isPlayerNumberInList = battlePlayerNumbers.Contains(battleUnitPlayer.Value);
            if (!isPlayerNumberInList)
                battlePlayerNumbers.Add(battleUnitPlayer.Value);
        }

        if (battlePlayerNumbers.Count > 0)
        {
            foreach (GamePlayer gamePlayer in Game.GamePlayers)
            {
                if (battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                    BattlePlayers.Add(gamePlayer);
                else if (GameplayManager.instance.isPlayerBaseDefense)
                {
                    if (gamePlayer.myPlayerBasePosition == battleSiteLandScript.gameObject.transform.position)
                    {
                        Debug.Log("GetBattlePlayers: Battle was a base defense and the defending player was found");
                        if (!battlePlayerNumbers.Contains(gamePlayer.playerNumber))
                            BattlePlayers.Add(gamePlayer);

                    }
                }
            }
        }

        Debug.Log("GetBattlePlayers: Will return the following number of battle players: " + BattlePlayers.Count.ToString());
        return BattlePlayers;
    }
    [Server]
    bool DidCardAbility1Activate(GamePlayer playerWith1Card)
    {
        Debug.Log("Executing DidCardAbility1Activate for player: " + playerWith1Card.PlayerName + " That ability is: \"If your army has inf but no tanks, +2 battle score\"");
        bool didAbilityActivate = false;

        if (playerWith1Card.playerArmyNetIds.Count > 0)
        {
            foreach (uint unitNetId in playerWith1Card.playerArmyNetIds)
            {
                GameObject unit = NetworkIdentity.spawned[unitNetId].gameObject;
                if (unit.tag == "infantry")
                    didAbilityActivate = true;
                else
                {
                    Debug.Log("DidCardAbility1Activate: Found unit that is not tagged as an infantry. Returning false.");
                    didAbilityActivate = false;
                    return didAbilityActivate;
                }
            }
        }
        if (playerWith1Card.didPlayerReinforce && playerWith1Card.playerReinforcementNetIds.Count > 0)
        {
            foreach (uint unitNetId in playerWith1Card.playerReinforcementNetIds)
            {
                GameObject unit = NetworkIdentity.spawned[unitNetId].gameObject;
                if (unit.tag == "infantry")
                    didAbilityActivate = true;
                else
                {
                    Debug.Log("DidCardAbility1Activate: Found REINFORCING unit that is not tagged as an infantry. Returning false.");
                    didAbilityActivate = false;
                    return didAbilityActivate;
                }
            }
        }
        Debug.Log("DidCardAbility1Activate: Returning: " + didAbilityActivate.ToString() + " for player: " + playerWith1Card.PlayerName);
        return didAbilityActivate;
    }
    [Server]
    bool DidCardAbility2Activate(GamePlayer playerWith2Card, List<GamePlayer> BattlePlayers)
    {
        Debug.Log("Executing DidCardAbility2Activate for player: " + playerWith2Card.PlayerName + " That ability is: \"If opp. army has no tanks, +1 attack value\"");
        bool didAbilityActivate = false;
        // Get the opponent player object
        GamePlayer opponentPlayer = null;
        foreach (GamePlayer battlePlayer in BattlePlayers)
        {
            if (battlePlayer.PlayerName == playerWith2Card.PlayerName && battlePlayer.ConnectionId == playerWith2Card.ConnectionId && battlePlayer.playerNumber == playerWith2Card.playerNumber)
            {
                continue;
            }
            else
            {
                Debug.Log("DidCardAbility2Activate: found opponent player. Player with card is: " + playerWith2Card.PlayerName + " opponent is: " + battlePlayer.PlayerName);
                opponentPlayer = battlePlayer;
                break;
            }
        }
        if (opponentPlayer != null)
        {
            if (opponentPlayer.playerArmyNetIds.Count > 0)
            {
                foreach (uint unitNetId in opponentPlayer.playerArmyNetIds)
                {
                    GameObject unit = NetworkIdentity.spawned[unitNetId].gameObject;
                    if (unit.tag == "infantry")
                        didAbilityActivate = true;
                    else
                    {
                        Debug.Log("DidCardAbility2Activate: Found unit that is not tagged as an infantry. Returning false.");
                        didAbilityActivate = false;
                        return didAbilityActivate;
                    }
                }
            }
        }
        Debug.Log("DidCardAbility1Activate: Returning: " + didAbilityActivate.ToString() + " for player: " + playerWith2Card.PlayerName);
        return didAbilityActivate;
    }
    [ClientRpc]
    void RpcClearCardAbilityInfo()
    {
        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();
        LocalGamePlayerScript.ClearCardAbilityInfo();
    }
    public void ClearCardAbilityInfo()
    {
        if (hasAuthority)
            CmdClearCardAbilityInfo();
    }
    [Command]
    void CmdClearCardAbilityInfo()
    {
        NetworkIdentity networkIdentity = connectionToClient.identity;
        GamePlayer requestingPlayer = networkIdentity.GetComponent<GamePlayer>();
        Debug.Log("Executing CmdClearCardAbilityInfo for " + requestingPlayer.PlayerName);
        //requestingPlayer.didPlayerCardAbilityActivate = false;
        requestingPlayer.HandlePlayerCardAbilityActivated(requestingPlayer.didPlayerCardAbilityActivate, false);
        requestingPlayer.playerCardAbilityNumber = 0;
    }
    void HandlePlayerBattleScoreValueSet(int oldValue, int newValue)
    {
        if (isServer)
        {
            this.playerBattleScore = newValue;
        }
        if (isClient && newValue != 0)
        {
            Debug.Log("Running HandlePlayerBattleScoreValueSet as a client. New value set to " + newValue.ToString());
            GameplayManager.instance.UpdatePlayerBattleScoreInPanel(this);
        }
    }
    void HandlePlayerCardAbilityActivated(bool oldValue, bool newValue)
    {
        if (isServer)
        {
            this.didPlayerCardAbilityActivate = newValue;
        }
        if (isClient && newValue)
        {
            Debug.Log("Running HandlePlayerCardAbilityActivated as a client. New value set to " + newValue.ToString());
            GameplayManager.instance.UpdatePlayerCardAbilityText(this);
        }
    }

}

