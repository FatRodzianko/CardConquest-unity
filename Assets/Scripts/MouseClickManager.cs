﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Threading;

public class MouseClickManager : MonoBehaviour
{
    public static MouseClickManager instance;

    [Header("Clickable Layers")]
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private LayerMask landLayer;
    [SerializeField] private LayerMask playerCardLayer;

    [Header("Player Units")]
    public List<GameObject> unitsSelected;
    public GameObject cardSelected;

    [Header("GamePlayers")]
    [SerializeField] private GameObject LocalGamePlayer;
    [SerializeField] private GamePlayer LocalGamePlayerScript;

    public bool canSelectUnitsInThisPhase = false;
    public bool canSelectPlayerCardsInThisPhase = false;

    private void Awake()
    {
        Debug.Log("Awake MouseClickManager.cs");
        MakeInstance();
    }
    void Start()
    {
        Debug.Log("Start MouseClickManager.cs");
        
        unitsSelected = new List<GameObject>();
        GetLocalGamePlayer();
    }
    void MakeInstance()
    {
        Debug.Log("MakeInstance MouseClickManager");
        if (instance == null)
            instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && EscMenuManager.instance.IsMainMenuOpen == false)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePosition2d = new Vector2(mousePosition.x, mousePosition.y);
            RaycastHit2D rayHitUnit = Physics2D.Raycast(mousePosition2d, Vector2.zero, Mathf.Infinity, unitLayer);
            RaycastHit2D rayHitLand = Physics2D.Raycast(mousePosition2d, Vector2.zero, Mathf.Infinity, landLayer);
            
            bool playerViewingHand = false;
            try
            {
                playerViewingHand = LocalGamePlayerScript.myPlayerCardHand.GetComponent<PlayerHand>().isPlayerViewingTheirHand;
            }
            catch
            {
                Debug.Log("Can't access PlayerHand");
            }
            bool playerViewingOpponentHand = false;
            try
            {
                playerViewingOpponentHand = GameplayManager.instance.isPlayerViewingOpponentHand;
            }
            catch
            {
                Debug.Log("Can't access GameplayManager");
            }
            bool playerReadyForNextPhase = false;
            try
            {
                playerReadyForNextPhase = LocalGamePlayerScript.ReadyForNextPhase;
            }
            catch
            {
                Debug.Log("Can't access LocalGamePlayer");
            }
            if (rayHitUnit.collider != null)
            {

                if (rayHitUnit.collider.gameObject.GetComponent<NetworkIdentity>().hasAuthority && !playerViewingHand && !playerViewingOpponentHand && !playerReadyForNextPhase && canSelectUnitsInThisPhase && GameplayManager.instance.currentGamePhase != "Retreat Units" && !GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
                {
                    Debug.Log("Player has authority over unit");
                    UnitScript unitScript = rayHitUnit.collider.GetComponent<UnitScript>();
                    if (!unitScript.currentlySelected)
                    {
                        Debug.Log("Selecting a new unit.");
                        unitsSelected.Add(rayHitUnit.collider.gameObject);
                        unitScript.currentlySelected = !unitScript.currentlySelected;
                        unitScript.ClickedOn();

                        if (unitScript.currentLandOccupied != null)
                        {
                            LandScript landScript = unitScript.currentLandOccupied.GetComponent<LandScript>();
                            landScript.HighlightLandArea();
                        }
                    }
                    else
                    {
                        unitsSelected.Remove(rayHitUnit.collider.gameObject);
                        Debug.Log("Deselecting the unit unit.");
                        unitScript.currentlySelected = !unitScript.currentlySelected;
                        unitScript.ClickedOn();
                        unitScript.CheckLandForRemainingSelectedUnits();
                        if (unitScript.currentLandOccupied != null)
                        {
                            LandScript landScript = unitScript.currentLandOccupied.GetComponent<LandScript>();
                            if (landScript.multipleUnitsOnLand)
                            {
                                Debug.Log("UN-Selected unit on land with multiple units.");
                            }
                        }
                    }
                }
                else if (rayHitUnit.collider.gameObject.GetComponent<NetworkIdentity>().hasAuthority && !playerViewingHand && !playerViewingOpponentHand && !playerReadyForNextPhase && canSelectUnitsInThisPhase && GameplayManager.instance.currentGamePhase == "Retreat Units")
                {
                    //Unit movement specifically for retreating units
                    //only allow player to select unit that has to retreat
                    if (LocalGamePlayerScript.playerArmyNetIds.Contains(rayHitUnit.collider.gameObject.GetComponent<NetworkIdentity>().netId))
                    {
                        UnitScript unitScript = rayHitUnit.collider.GetComponent<UnitScript>();
                        if (!unitScript.currentlySelected)
                        {
                            Debug.Log("Selecting a new unit.");
                            unitsSelected.Add(rayHitUnit.collider.gameObject);
                            unitScript.currentlySelected = !unitScript.currentlySelected;
                            unitScript.ClickedOn();
                            if (unitScript.currentLandOccupied != null && unitScript.currentLandOccupied.GetComponent<NetworkIdentity>().netId != GameplayManager.instance.currentBattleSite)
                            {
                                LandScript landScript = unitScript.currentLandOccupied.GetComponent<LandScript>();
                                landScript.HighlightLandArea();
                            }
                        }
                        else
                        {
                            unitsSelected.Remove(rayHitUnit.collider.gameObject);
                            Debug.Log("Deselecting the unit unit.");
                            unitScript.currentlySelected = !unitScript.currentlySelected;
                            unitScript.ClickedOn();
                            unitScript.CheckLandForRemainingSelectedUnits();
                        }
                    }                    
                    else
                    {
                        Debug.Log("Player clicked on unit that doesn't have to retreat.");
                    }                    
                }
                else if (rayHitUnit.collider.gameObject.GetComponent<NetworkIdentity>().hasAuthority && !playerViewingHand && !playerViewingOpponentHand && !playerReadyForNextPhase && canSelectUnitsInThisPhase && GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
                {
                    // unit selection for Reinforcements
                    Debug.Log("Clicked on unit during reinforcements phase");
                    UnitScript unitScript = rayHitUnit.collider.GetComponent<UnitScript>();
                    if (!unitScript.currentlySelected && unitScript.canUnitReinforce)
                    {
                        Debug.Log("Selecting a new unit.");
                        unitsSelected.Add(rayHitUnit.collider.gameObject);
                        unitScript.currentlySelected = !unitScript.currentlySelected;
                        unitScript.ClickedOn();
                        GameplayManager.instance.ToggleSelectReinforcementsButton();

                    }
                    else if (unitScript.currentlySelected && unitScript.canUnitReinforce)
                    {
                        unitsSelected.Remove(rayHitUnit.collider.gameObject);
                        Debug.Log("Deselecting the unit unit.");
                        unitScript.currentlySelected = !unitScript.currentlySelected;
                        unitScript.ClickedOn();
                        GameplayManager.instance.ToggleSelectReinforcementsButton();
                    }
                }
                else
                {
                    Debug.Log("Player DOES NOT have authority over unit");
                }
            }
            else if (rayHitLand.collider != null && unitsSelected.Count > 0 && rayHitUnit.collider == null && !GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements")) // if the player has selected units previously and clicks on a land, check if the units can be moved)
            {
                // Begin asking server if the desired move is allowed
                if (!playerViewingHand)
                {
                    unitsSelected[0].GetComponent<UnitScript>().AskServerCanUnitsMove(rayHitLand.collider.gameObject, rayHitLand.collider.gameObject.transform.position, unitsSelected);
                }
                
                //Debug.Log("canMove from the server is: " + unitsSelected[0].GetComponent<UnitScript>().AskServerCanUnitsMove(rayHitLand.collider.gameObject).ToString());
                /*
                if (unitsSelected[0].GetComponent<UnitScript>().CanAllSelectedUnitsMove(rayHitLand.collider.gameObject))
                {
                    MoveAllUnits(rayHitLand.collider.gameObject);
                    if (GameplayManager.instance.currentGamePhase == "Unit Movement")
                        GameplayManager.instance.UnitsHaveMoved();
                }
                */
                //ClearUnitSelection();
            }
            if (canSelectPlayerCardsInThisPhase)
            {
                RaycastHit2D rayHitCard = Physics2D.Raycast(mousePosition2d, Vector2.zero, Mathf.Infinity, playerCardLayer);
                if (rayHitCard.collider != null)
                {
                    SelectCardClicked(rayHitCard.collider.gameObject);
                }
            }
        }
        if (Input.GetMouseButtonDown(1) && EscMenuManager.instance.IsMainMenuOpen == false)
        {
            bool playerViewingHand = false;
            try
            {
                playerViewingHand = LocalGamePlayerScript.myPlayerCardHand.GetComponent<PlayerHand>().isPlayerViewingTheirHand;
            }
            catch
            {
                Debug.Log("Can't access PlayerHand");
            }
            bool playerReadyForNextPhase = false;
            try
            {
                playerReadyForNextPhase = LocalGamePlayerScript.ReadyForNextPhase;
            }
            catch
            {
                Debug.Log("Can't access LocalGamePlayer");
            }
            Debug.Log("Right clicked.");
            if (!playerViewingHand && !playerReadyForNextPhase)
                ClearUnitSelection();
        }
    }
    public void ClearUnitSelection()
    {
        Debug.Log("Executing: ClearUnitSelection");
        if (unitsSelected.Count > 0)
        {
            Debug.Log("ClearUnitSelection: Begin unselecting units");
            foreach (GameObject unit in unitsSelected)
            {
                Debug.Log("ClearUnitSelection: Will unselect: " + unit);
                UnitScript unitScript = unit.GetComponent<UnitScript>();
                unitScript.currentlySelected = !unitScript.currentlySelected;
                unitScript.ClickedOn();
                unitScript.CheckLandForRemainingSelectedUnits();
            }
            Debug.Log("ClearUnitSelection: Done unselecting units");
            unitsSelected.Clear();
            if (GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
                GameplayManager.instance.ToggleSelectReinforcementsButton();
        }
        else
        {
            Debug.Log("ClearUnitSelection: No units to clear");
        }
    }
    public void MoveAllUnits(GameObject landClicked)
    {
        if (unitsSelected.Count > 0)
        {
            Debug.Log("Moving selected units.");
            foreach (GameObject unit in unitsSelected)
            {
                UnitScript unitScript = unit.GetComponent<UnitScript>();
                unitScript.MoveUnit(landClicked);
            }
            if(GameplayManager.instance.currentGamePhase == "Unit Placement")
                GameplayManager.instance.CheckIfAllUnitsHaveBeenPlaced();
            if (GameplayManager.instance.currentGamePhase == "Unit Movement")
                GameplayManager.instance.UnitsHaveMoved();
            if (GameplayManager.instance.currentGamePhase == "Retreat Units")
                GameplayManager.instance.CheckIfUnitsHaveRetreated();
        }
    }
    [Client]
    void GetLocalGamePlayer()
    {
        Debug.Log("GetLocalGamePlayer: MouseClickManager");
        LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();
    }
    public void SelectCardClicked(GameObject cardClicked)
    {
        if (cardClicked)
        {
            if (cardClicked.GetComponent<NetworkIdentity>().hasAuthority)
            {
                Card cardScript = cardClicked.GetComponent<Card>();
                if (cardScript.isClickable)
                {
                    cardScript.CardClickedOn();
                    if (cardSelected != cardClicked && cardSelected)
                    {
                        Card cardSelectedScript = cardSelected.GetComponent<Card>();
                        cardSelectedScript.CardClickedOn();
                    }
                    if (cardSelected == cardClicked)
                    {
                        cardSelected = null;
                    }
                    else
                    {
                        cardSelected = cardClicked;
                    }
                    GameplayManager.instance.ToggleSelectThisCardButton();
                }                
            }
        }               
    }
}
