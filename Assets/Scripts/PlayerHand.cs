using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class PlayerHand : NetworkBehaviour
{
    [SyncVar] public string ownerPlayerName;
    [SyncVar] public int ownerConnectionId;
    [SyncVar] public int ownerPlayerNumber;

    [SyncVar] public bool serverHandInitialized = false;
    public bool localHandInitialized = false;

    public List<GameObject> Hand = new List<GameObject>();
    public List<GameObject> DiscardPile = new List<GameObject>();
    public SyncList<uint> HandNetId = new SyncList<uint>();
    public SyncList<uint> DiscardPileNetId = new SyncList<uint>();

    public bool isPlayerViewingTheirHand = false;    

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitializePlayerHand()
    {
        if (!localHandInitialized)
        {
            GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
            foreach (GameObject card in allCards)
            {
                Card cardScript = card.GetComponent<Card>();
                if (cardScript.ownerConnectionId == this.ownerConnectionId)
                {
                    this.Hand.Add(card);                    
                }
            }
            Hand = Hand.OrderByDescending(o => o.GetComponent<Card>().Power).ToList();
            localHandInitialized = true;
            if(hasAuthority)
                CmdInitializePlayerHand();
            Debug.Log("Hand initialized for: " + ownerPlayerName);
        }        
    }
    [Command]
    void CmdInitializePlayerHand()
    {
        if (!this.serverHandInitialized)
        {
            GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
            foreach (GameObject card in allCards)
            {
                Card cardScript = card.GetComponent<Card>();
                if (cardScript.ownerConnectionId == this.ownerConnectionId)
                {
                    this.HandNetId.Add(card.GetComponent<NetworkIdentity>().netId);
                }
            }
            this.serverHandInitialized = true;
            Debug.Log("Hand initialized for: " + ownerPlayerName);
        }
    }
    public void ShowPlayerHandOnScreen()
    {
        isPlayerViewingTheirHand = true;
        if (GameplayManager.instance.currentGamePhase.StartsWith("Choose Card"))
        {
            Vector3 cardLocation = Camera.main.transform.position;
            cardLocation.x -= 7f;
            cardLocation.z = 0f;
            Vector3 cardScale = new Vector3(1.5f, 1.5f, 0f);
            foreach (GameObject playerCard in Hand)
            {
                if (!playerCard.activeInHierarchy)
                {
                    playerCard.SetActive(true);
                }
                playerCard.transform.position = cardLocation;
                playerCard.transform.localScale = cardScale;
                cardLocation.x += 3.5f;
            }
            if (GameplayManager.instance.localPlayerBattlePanel && GameplayManager.instance.opponentPlayerBattlePanel)
            {
                GameplayManager.instance.localPlayerBattlePanel.SetActive(false);
                GameplayManager.instance.opponentPlayerBattlePanel.SetActive(false);
            }
        }
        else
        {
            Vector3 cardLocation = new Vector3(-10f, 1.5f, 0f);
            Vector3 cardScale = new Vector3(1.75f, 1.75f, 0f);
            foreach (GameObject playerCard in Hand)
            {
                if (!playerCard.activeInHierarchy)
                {
                    playerCard.SetActive(true);
                }
                playerCard.transform.position = cardLocation;
                playerCard.transform.localScale = cardScale;
                cardLocation.x += 4.5f;
            }
        }
        // Hide land text since it displays over cards
        GameObject landHolder = GameObject.FindGameObjectWithTag("LandHolder");
        foreach (Transform landChild in landHolder.transform)
        {
            LandScript landScript = landChild.GetComponent<LandScript>();
            landScript.HideUnitText();
        }
    }
    public void HidePlayerHandOnScreen()
    {
        isPlayerViewingTheirHand = false;
        foreach (GameObject playerCard in Hand)
        {
            if (playerCard.activeInHierarchy)
            {
                playerCard.SetActive(false);
            }
        }
        if (GameplayManager.instance.currentGamePhase.StartsWith("Choose Card"))
        {
            GameObject landHolder = GameObject.FindGameObjectWithTag("LandHolder");
            foreach (Transform landChild in landHolder.transform)
            {
                if (landChild.gameObject.GetComponent<NetworkIdentity>().netId == GameplayManager.instance.currentBattleSite)
                {
                    LandScript landScript = landChild.GetComponent<LandScript>();
                    landScript.UnHideUnitText();
                }                
            }
            if (GameplayManager.instance.localPlayerBattlePanel && GameplayManager.instance.opponentPlayerBattlePanel)
            {
                GameplayManager.instance.localPlayerBattlePanel.SetActive(true);
                GameplayManager.instance.opponentPlayerBattlePanel.SetActive(true);
            }
            if (GameplayManager.instance.showingNearbyUnits)
                GameplayManager.instance.ShowUnitsOnMap();
        }
        else
        {
            GameObject landHolder = GameObject.FindGameObjectWithTag("LandHolder");
            foreach (Transform landChild in landHolder.transform)
            {
                LandScript landScript = landChild.GetComponent<LandScript>();
                landScript.UnHideUnitText();
            }
        }
    }
    public void AddCardBackToHand(GameObject cardToAdd)
    {
        Debug.Log("Executing AddCardBackToHand for card: " + cardToAdd);
        if (Hand.Contains(cardToAdd))
            return;
        Hand.Add(cardToAdd);
        cardToAdd.transform.SetParent(this.gameObject.transform);
        cardToAdd.transform.localScale = new Vector3(1.5f, 1.5f, 0f);
        cardToAdd.SetActive(false);
        Hand = Hand.OrderByDescending(o => o.GetComponent<Card>().Power).ToList();
    }
    [Server]
    public void MoveCardToDiscard(uint cardtoDiscardNetId)
    {
        Debug.Log("Executing MoveCardToDiscard to discard card with network id: " + cardtoDiscardNetId.ToString());
        if (HandNetId.Contains(cardtoDiscardNetId))
            HandNetId.Remove(cardtoDiscardNetId);
        if (!DiscardPileNetId.Contains(cardtoDiscardNetId))
            DiscardPileNetId.Add(cardtoDiscardNetId);

        // If cards in the hand still remain, have player remove cards locally and stuff
        if (HandNetId.Count > 0)
        {
            RpcMoveCardToDiscard(cardtoDiscardNetId);
        }
    }
    [ClientRpc]
    void RpcMoveCardToDiscard(uint cardtoDiscardNetId)
    {
        Debug.Log("Executing RpcMoveCardToDiscard to discard card with net id: " + cardtoDiscardNetId.ToString() + " for player: " + ownerPlayerName);
        GameObject cardToDiscard = NetworkIdentity.spawned[cardtoDiscardNetId].gameObject;
        if (cardToDiscard)
        {
            Debug.Log("RpcMoveCardToDiscard: Moving this card to discard: " + cardToDiscard);
            if (Hand.Contains(cardToDiscard))
                Hand.Remove(cardToDiscard);
            if (!DiscardPile.Contains(cardToDiscard))
                DiscardPile.Add(cardToDiscard);

            // If the card is not a child of the PlayerCardHand object, set it as a child of the PlayerCardHand object
            if (!cardToDiscard.transform.IsChildOf(this.transform))
                cardToDiscard.transform.SetParent(this.transform);
            if (cardToDiscard.activeInHierarchy)
                cardToDiscard.SetActive(false);
        }
    }
}
