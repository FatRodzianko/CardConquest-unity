using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;

public class LandScript : NetworkBehaviour
{
    public List<GameObject> infantryOnLand;
    public List<GameObject> tanksOnLand;
    public SyncList<uint> UnitNetIdsOnLand = new SyncList<uint>();
    public SyncDictionary<uint, int> UnitNetIdsAndPlayerNumber = new SyncDictionary<uint, int>();

    public GameObject infTextHolder;
    public GameObject tankTextHolder;

    private GameObject infText;
    private GameObject tankText;

    public GameObject landOutline;
    private GameObject landOutlineObject;
    private GameObject battleOutlineObject;

    public GameObject cannotPlaceHereOutline;
    private GameObject cannotPlaceHereOutlineObject;
    public bool cannotPlaceHere = false;

    public bool isLandSelected = false;
    public bool multipleUnitsOnLand = false;

    [SyncVar(hook = nameof(HandlePlayerCanPlaceHereUpdate))] public int PlayerCanPlaceHere;

    [Header("Battle Unit Lists")]
    public List<GameObject> Player1Inf = new List<GameObject>();
    public List<GameObject> Player1Tank = new List<GameObject>();
    public List<GameObject> Player2Inf = new List<GameObject>();
    public List<GameObject> Player2Tank = new List<GameObject>();
    public Dictionary<GameObject,int> BattleUnitTexts = new Dictionary<GameObject, int>();

    [Header("Text Objects")]
    [SerializeField] private GameObject battleNumberTextPrefab;
    public GameObject battleNumberTextObject;


    // Start is called before the first frame update
    void Start()
    {
        infantryOnLand = new List<GameObject>();
        tanksOnLand = new List<GameObject>();
        if (PlayerCanPlaceHere == 0)
        {
            cannotPlaceHere = true;
            CreateCannotPlaceHereOutline();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //CheckIfUnitTextNeedsToBeRemoved();
    }
    public void MultipleUnitsUIText(string unitType)
    {
        Debug.Log("Executing MultipleUnitsUIText for unit type: " + unitType + " for land object: " + this.gameObject);
        if (unitType == "infantry")
        {
            if (infText == null)
            {
                Debug.Log("Creating text box for multiple infantry " + " on: " + this.gameObject.transform.position);
                infText = Instantiate(infTextHolder, gameObject.transform);
                infText.transform.position = transform.position;
                infText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + infantryOnLand.Count.ToString());
            }
            else
            {
                Debug.Log("Landscript: Updating infText to " + infantryOnLand.Count.ToString() + " on: " + this.gameObject.transform.position);
                infText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + infantryOnLand.Count.ToString());
            }
        }
        else if (unitType == "tank")
        {
            if (tankText == null)
            {
                Debug.Log("Creating text box for multiple tanks " + " on: " + this.gameObject.transform.position);
                tankText = Instantiate(tankTextHolder, gameObject.transform);
                tankText.transform.position = transform.position;
                tankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + tanksOnLand.Count.ToString());
            }
            else
            {
                Debug.Log("Landscript: Updating tankText to " + tanksOnLand.Count.ToString() + " on: " + this.gameObject.transform.position);
                tankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + tanksOnLand.Count.ToString());
            }
        }
        if (GameplayManager.instance.currentGamePhase.StartsWith("Choose Card") || GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
        {
            if (infText != null)
            {
                infText.SetActive(false);
            }
            if (tankText != null)
            {
                tankText.SetActive(false);
            }
        }
        multipleUnitsOnLand = true;    

    }
    void CheckIfUnitTextNeedsToBeRemoved()
    {
        if (infText != null && infantryOnLand.Count == 0)
        {
            Destroy(infText);
        }
        if (tankText != null && tanksOnLand.Count == 0)
        {
            Destroy(tankText);
        }
    }
    public void UpdateUnitText()
    {
        Debug.Log("Executing UpdateUnitText for land object: " + this.gameObject);
        if (infText != null)
        {
            Debug.Log("Updating inf text. Current number of infantry " + infantryOnLand.Count.ToString() + " on: " + this.gameObject.transform.position);
            if (infantryOnLand.Count > 1)
                infText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + infantryOnLand.Count.ToString());
            else
            {
                Debug.Log("1 or less infantry. Deactivating infText object from: " + this.gameObject.transform.position);
                Destroy(infText);
                infText = null;
                if (infantryOnLand.Any(x => x.GetComponent<UnitScript>().canUnitReinforce == true))
                {
                    Debug.Log("CollapseUnits: At least 1 unit has canUnitReinforce set to true. Expanding for reinforcements for land object: " + this.gameObject);
                    ExpandForReinforcements(true);
                    HideUnitText();
                }
                else
                    CollapseUnits();
            }

        }
        if (tankText != null)
        {
            Debug.Log("Updating tank text. Current number of tanks: " + tanksOnLand.Count.ToString() + " on: " + this.gameObject.transform.position + " for land object: " + this.gameObject);
            if (tanksOnLand.Count > 1)
                tankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + tanksOnLand.Count.ToString());
            else
            {
                Debug.Log("1 or less tank. Deactivating tankText object from: " + this.gameObject.transform.position);
                Destroy(tankText);
                tankText = null;
                if (tanksOnLand.Any(x => x.GetComponent<UnitScript>().canUnitReinforce == true))
                {
                    Debug.Log("CollapseUnits: At least 1 unit has canUnitReinforce set to true. Expanding for reinforcements for land object: " + this.gameObject);
                    ExpandForReinforcements(true);
                    HideUnitText();
                }
                else
                    CollapseUnits();
            }                
        }
        if (GameplayManager.instance.currentGamePhase.StartsWith("Choose Card") || GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
        {
            if (infText != null)
            {
                infText.SetActive(false);
            }
            if (tankText != null)
            {
                tankText.SetActive(false);
            }
        }
        CheckIfMultipleUnitsOnLand();
    }
    public void HighlightLandArea()
    {
        if (landOutlineObject == null)
        {
            Debug.Log("Creating land outline");
            landOutlineObject = Instantiate(landOutline, transform.position,Quaternion.identity);
            //landOutlineObject = Instantiate(landOutline, gameObject.transform, Quaternion.identity);
            landOutlineObject.transform.SetParent(gameObject.transform);
            //Vector3 temp;
            //if (infantryOnLand.Count > 1)
            //{
            //    for (int i = 1; i < infantryOnLand.Count; i++)
            //    {
            //        if (i == 1)
            //        {
            //            temp = infantryOnLand[i].transform.position;
            //            temp.x += 0.65f;
            //            infantryOnLand[i].transform.position = temp;
            //        }
            //        else if (i == 2)
            //        {
            //            temp = infantryOnLand[i].transform.position;
            //            temp.x -= 0.6f;
            //            infantryOnLand[i].transform.position = temp;
            //        }
            //        else if (i == 3)
            //        {
            //            temp = infantryOnLand[i].transform.position;
            //            temp.y -= 0.8f;
            //            infantryOnLand[i].transform.position = temp;
            //        }
            //    }
            //}
            //if (tanksOnLand.Count > 1)
            //{
            //    for (int i = 1; i < tanksOnLand.Count; i++)
            //    {
            //        if (i == 1)
            //        {
            //            temp = tanksOnLand[i].transform.position;
            //            temp.x += 0.95f;
            //            tanksOnLand[i].transform.position = temp;
            //        }
            //        else if (i == 2)
            //        {
            //            temp = tanksOnLand[i].transform.position;
            //            temp.x -= 0.95f;
            //            tanksOnLand[i].transform.position = temp;
            //        }
            //        else if (i == 3)
            //        {
            //            temp = tanksOnLand[i].transform.position;
            //            temp.y += 0.6f;
            //            tanksOnLand[i].transform.position = temp;
            //        }
            //    }
            //}
            //HideUnitText();
            if(infantryOnLand.Count > 1 || tanksOnLand.Count > 1)
                ExpandUnits();
        }
    }
    void ExpandUnits()
    {
        Vector3 temp;
        /*if (infantryOnLand.Count > 1)
        {
            for (int i = 1; i < infantryOnLand.Count; i++)
            {
                if (i == 1)
                {
                    temp = infantryOnLand[i].transform.position;
                    temp.x += 0.65f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = infantryOnLand[i].transform.position;
                    temp.x -= 0.6f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = infantryOnLand[i].transform.position;
                    temp.y -= 0.8f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = infantryOnLand[i].transform.position;
                    temp.y += 0.8f;
                    infantryOnLand[i].transform.position = temp;
                }
            }
        }
        if (tanksOnLand.Count > 1)
        {
            for (int i = 1; i < tanksOnLand.Count; i++)
            {
                if (i == 1)
                {
                    temp = tanksOnLand[i].transform.position;
                    temp.x += 0.95f;
                    tanksOnLand[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = tanksOnLand[i].transform.position;
                    temp.x -= 0.95f;
                    tanksOnLand[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = tanksOnLand[i].transform.position;
                    temp.y += 0.6f;
                    tanksOnLand[i].transform.position = temp;
                }
            }
        }*/
        if (infantryOnLand.Count > 1)
        {
            for (int i = 1; i < infantryOnLand.Count; i++)
            {
                if (i == 1)
                {
                    temp = transform.position;
                    temp.x += 0.65f;
                    temp.y -= 0.5f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = transform.position;
                    temp.x -= 0.6f;
                    temp.y -= 0.5f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = transform.position;
                    temp.y -= 0.5f;
                    temp.y -= 0.8f;
                    infantryOnLand[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = transform.position;
                    temp.y -= 0.5f;
                    temp.y += 0.8f;
                    infantryOnLand[i].transform.position = temp;
                }
            }
        }
        if (tanksOnLand.Count > 1)
        {
            for (int i = 1; i < tanksOnLand.Count; i++)
            {
                if (i == 1)
                {
                    temp = transform.position;
                    temp.x += 0.95f;
                    temp.y += 0.5f;
                    tanksOnLand[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = transform.position;
                    temp.x -= 0.95f;
                    temp.y += 0.5f;
                    tanksOnLand[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = transform.position;
                    temp.y += 0.5f;
                    temp.y += 0.6f;
                    tanksOnLand[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = transform.position;
                    temp.y += 0.5f;
                    temp.y -= 0.6f;
                    tanksOnLand[i].transform.position = temp;
                }
            }
        }
        HideUnitText();
    }
    public void RemoveHighlightLandArea()
    {
        if (landOutlineObject != null)
        {
            Destroy(landOutlineObject);
            //Vector3 temp;
            //// move units back?
            //if (infantryOnLand.Count > 0)
            //{

            //    foreach (GameObject inf in infantryOnLand)
            //    {
            //        temp = transform.position;
            //        temp.y -= 0.5f;
            //        inf.transform.position = temp;
            //    }
            //}
            //if (tanksOnLand.Count > 0)
            //{
            //    foreach (GameObject tank in tanksOnLand)
            //    {
            //        temp = transform.position;
            //        temp.y += 0.5f;
            //        tank.transform.position = temp;
            //    }
            //}
            //UnHideUnitText();
            if (infantryOnLand.Count > 1 || tanksOnLand.Count > 1)
                CollapseUnits();
        }
        //CheckIfMultipleUnitsOnLand();
    }
    public void CollapseUnits()
    {
        Debug.Log("Executing CollapseUnits for land object: " + this.gameObject);
        /*if (GameplayManager.instance.currentGamePhase.StartsWith("Reinforcements"))
        {
            Debug.Log("CollapseUnits: For reinforcements phase for land object: " + this.gameObject);
            if (infantryOnLand.Any(x => x.GetComponent<UnitScript>().canUnitReinforce == true) || tanksOnLand.Any(x => x.GetComponent<UnitScript>().canUnitReinforce == true))
            {
                Debug.Log("CollapseUnits: At least 1 unit has canUnitReinforce set to true. Expanding for reinforcements for land object: " + this.gameObject);
                ExpandForReinforcements(true);
                HideUnitText();
            }
            
        }
        else
        {
            Debug.Log("CollapseUnits: NOT FOR reinforcements phase for land object: " + this.gameObject);
            
        }*/
        Vector3 temp;
        // move units back?
        if (infantryOnLand.Count > 0)
        {

            foreach (GameObject inf in infantryOnLand)
            {
                temp = transform.position;
                temp.y -= 0.5f;
                inf.transform.position = temp;
            }
        }
        if (tanksOnLand.Count > 0)
        {
            foreach (GameObject tank in tanksOnLand)
            {
                temp = transform.position;
                temp.y += 0.5f;
                tank.transform.position = temp;
            }
        }
        UnHideUnitText();
    }
    void CheckIfMultipleUnitsOnLand()
    {
        if (infantryOnLand.Count < 2 && tanksOnLand.Count < 2)
        {
            multipleUnitsOnLand = false;
        }
    }
    public void HideUnitText()
    {
        if (infText != null)
        {
            infText.SetActive(false);
        }
        if (tankText != null)
        {
            tankText.SetActive(false);
        }
        if (BattleUnitTexts.Count > 0)
        {

            foreach (KeyValuePair<GameObject, int> battleText in BattleUnitTexts)
            {
                if (battleText.Key)
                    battleText.Key.SetActive(false);
            }
            /*foreach (GameObject battleText in BattleUnitTexts)
            {
                if(battleText)
                    battleText.SetActive(false);
            }*/
        }
        if (battleNumberTextObject)
            battleNumberTextObject.SetActive(false);
    }
    public void UnHideUnitText()
    {
        if (infText != null)
        {
            infText.SetActive(true);
        }
        if (tankText != null)
        {
            tankText.SetActive(true);
        }
        if (BattleUnitTexts.Count > 0)
        {
            foreach (KeyValuePair<GameObject, int> battleText in BattleUnitTexts)
            {
                if (battleText.Key)
                    battleText.Key.SetActive(true);
            }
            /*foreach (GameObject battleText in BattleUnitTexts)
            {
                if (battleText)
                    battleText.SetActive(true);
            }*/
        }
        if (battleNumberTextObject)
            battleNumberTextObject.SetActive(true);
    }
    public void CheckForSelectedUnits()
    {
        bool anySelected = false;
        if (tanksOnLand.Count > 0)
        {
            foreach (GameObject unit in tanksOnLand)
            {
                UnitScript unitScript = unit.GetComponent<UnitScript>();
                if (unitScript.currentlySelected)
                {
                    anySelected = true;
                    break;
                }
            }
        }
        if (infantryOnLand.Count > 0)
        {
            foreach (GameObject unit in infantryOnLand)
            {
                UnitScript unitScript = unit.GetComponent<UnitScript>();
                if (unitScript.currentlySelected)
                {
                    anySelected = true;
                    break;
                }
            }
        }
        if (!anySelected)
        {
            RemoveHighlightLandArea();
        }

    }
    public void CreateCannotPlaceHereOutline()
    {
        if (cannotPlaceHereOutlineObject == null && cannotPlaceHere)
        {
            cannotPlaceHereOutlineObject = Instantiate(cannotPlaceHereOutline, transform.position, Quaternion.identity);
            cannotPlaceHereOutlineObject.transform.parent = this.gameObject.transform;
        }
    }
    public void RemoveCannotPlaceHereOutline()
    {
        if (cannotPlaceHereOutlineObject != null)
        {
            Destroy(cannotPlaceHereOutlineObject);
        }
    }
    public void HandlePlayerCanPlaceHereUpdate(int oldValue, int newValue)
    {
        Debug.Log("PlayerCanPlaceHere updated to: " + PlayerCanPlaceHere);
        GameObject LocalGamePlayer = GameObject.Find("LocalGamePlayer");
        GamePlayer LocalGamePlayerScript = LocalGamePlayer.GetComponent<GamePlayer>();

        if (PlayerCanPlaceHere == LocalGamePlayerScript.playerNumber)
        {
            cannotPlaceHere = false;
            RemoveCannotPlaceHereOutline();
        }
        else 
        {
            cannotPlaceHere = true;
            CreateCannotPlaceHereOutline();
        }
    }
    public void HighlightBattleSite()
    {
        if (!battleOutlineObject)
        {
            Debug.Log("Creating the highlight for the battle site");
            battleOutlineObject = Instantiate(landOutline, transform.position, Quaternion.identity);
            //landOutlineObject = Instantiate(landOutline, gameObject.transform, Quaternion.identity);
            battleOutlineObject.transform.SetParent(gameObject.transform);
        }
    }
    public void MoveUnitsForBattleSite()
    {
        Debug.Log("Executing MoveUnitsForBattleSite");
        //Clear out any old data
        Player1Inf.Clear();
        Player1Tank.Clear();
        Player2Inf.Clear();
        Player2Tank.Clear();
        //For now this will be "hard coded" for a 2 player game where the playernumbers are either 1 or 2
        foreach (KeyValuePair<uint, int> units in UnitNetIdsAndPlayerNumber)
        {            
            GameObject unitObject = NetworkIdentity.spawned[units.Key].gameObject;
            //Vector3 newPosition = unitObject.transform.position;
            Vector3 newPosition = this.transform.position;
            // Adjust units for player 1
            if (units.Value == 1)
            {                
                if (unitObject.tag == "infantry")
                {
                    newPosition.x -= 0.5f;
                    newPosition.y -= 0.5f;
                    unitObject.transform.position = newPosition;
                    Player1Inf.Add(unitObject);
                }
                else if (unitObject.tag == "tank")
                {
                    newPosition.x -= 0.7f;
                    newPosition.y += 0.5f;
                    unitObject.transform.position = newPosition;
                    Player1Tank.Add(unitObject);
                }
            }
            //Adjust units for Player 2
            else if (units.Value == 2)
            {
                if (unitObject.tag == "infantry")
                {
                    newPosition.x += 0.5f;
                    newPosition.y -= 0.5f;
                    unitObject.transform.position = newPosition;
                    Player2Inf.Add(unitObject);
                }
                else if (unitObject.tag == "tank")
                {
                    newPosition.x += 0.7f;
                    newPosition.y += 0.5f;
                    unitObject.transform.position = newPosition;
                    Player2Tank.Add(unitObject);
                }
            }
        }
        UnitTextForBattles();
    }
    public void UnitTextForBattles()
    {
        if (infText)
        {
            Destroy(infText);
            infText = null;
        }
        if (tankText)
        {
            Destroy(tankText);
            tankText = null;
        }
        if (BattleUnitTexts.Count > 0)
        {
            foreach (KeyValuePair<GameObject, int> text in BattleUnitTexts)
            {
                GameObject textToDestroy = text.Key;
                Destroy(textToDestroy);
                textToDestroy = null;
            }
            BattleUnitTexts.Clear();
        }

        //Spawn unit text for player 1
        if (Player1Inf.Count > 1)
        {
            Debug.Log("Creating text box for multiple infantry for player 1"); 
            GameObject player1InfText = Instantiate(infTextHolder, gameObject.transform);

            player1InfText.transform.position = transform.position;
            Vector3 player1InfTextPosition = new Vector3(-1.75f, -0.75f, 0.0f);
            player1InfText.transform.localPosition = player1InfTextPosition;

            player1InfText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player1Inf.Count.ToString());
            BattleUnitTexts.Add(player1InfText,1);
        }
        if (Player1Tank.Count > 1)
        {
            Debug.Log("Creating text box for multiple tanks for player 1");
            GameObject player1TankText = Instantiate(tankTextHolder, gameObject.transform);

            player1TankText.transform.position = transform.position;
            Vector3 player1TankTextPosition = new Vector3(-3.0f, -0.75f, 0.0f);
            player1TankText.transform.localPosition = player1TankTextPosition;

            player1TankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player1Tank.Count.ToString());
            BattleUnitTexts.Add(player1TankText,1);
        }

        //Spawn unit text for player2
        if (Player2Inf.Count > 1)
        {
            Debug.Log("Creating text box for multiple infantry for player 2");
            GameObject player2InfText = Instantiate(infTextHolder, gameObject.transform);

            player2InfText.transform.position = transform.position;
            Vector3 player2InfTextPosition = new Vector3(0.3f, -0.75f, 0.0f);
            player2InfText.transform.localPosition = player2InfTextPosition;

            player2InfText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player2Inf.Count.ToString());
            BattleUnitTexts.Add(player2InfText,2);
        }
        if (Player2Tank.Count > 1)
        {
            Debug.Log("Creating text box for multiple tanks for player 2");
            GameObject player2TankText = Instantiate(tankTextHolder, gameObject.transform);

            player2TankText.transform.position = transform.position;
            Vector3 player2TankTextPosition = new Vector3(0.0f, -0.75f, 0.0f);
            player2TankText.transform.localPosition = player2TankTextPosition;

            player2TankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player2Tank.Count.ToString());
            BattleUnitTexts.Add(player2TankText,2);
        }
    }
    public void SpawnBattleNumberText(int battleSiteNumber)
    {
        if (!battleNumberTextObject)
        {
            battleNumberTextObject = Instantiate(battleNumberTextPrefab, this.transform);
            battleNumberTextObject.transform.position = transform.position;
            battleNumberTextObject.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("#" + battleSiteNumber);
        }
    }
    public void HideBattleHighlight()
    {
        if (battleOutlineObject)
        {
            battleOutlineObject.SetActive(false);
        }
    }
    public void UnHideBattleHighlight()
    {
        if (battleOutlineObject)
        {
            battleOutlineObject.SetActive(true);
        }
    }
    public void ExpandLosingUnits(int losingPlayerNumber)
    {
        if (losingPlayerNumber != -1)
        {
            Debug.Log("Expanding the units of player number: " + losingPlayerNumber.ToString());
            List<GameObject> losingPlayerTanks = new List<GameObject>();
            List<GameObject> losingPlayerInf = new List<GameObject>();
            Vector3 winngingPlayerPosition = new Vector3(0, 0, 0);

            foreach (KeyValuePair<uint, int> units in UnitNetIdsAndPlayerNumber)
            {
                if (units.Value == losingPlayerNumber)
                {
                    GameObject unit = NetworkIdentity.spawned[units.Key].gameObject;
                    if (unit.gameObject.tag == "tank")
                        losingPlayerTanks.Add(unit);
                    else if (unit.gameObject.tag == "infantry")
                        losingPlayerInf.Add(unit);
                }
                else
                    winngingPlayerPosition = NetworkIdentity.spawned[units.Key].gameObject.transform.position;
            }
            Vector3 temp;
            int playerXMultiplier = 0;
            if (losingPlayerInf.Count > 1)
            {
                if (losingPlayerInf[0].transform.position.x > winngingPlayerPosition.x)
                    playerXMultiplier = 1;
                else
                    playerXMultiplier = -1;
                /*for (int i = 1; i < losingPlayerInf.Count; i++)
                {
                    if (i == 1)
                    {
                        temp = losingPlayerInf[i].transform.position;
                        temp.x += (0.65f * playerXMultiplier);
                        losingPlayerInf[i].transform.position = temp;
                    }
                    else if (i == 2)
                    {
                        temp = losingPlayerInf[i].transform.position;
                        temp.y -= 0.8f;
                        losingPlayerInf[i].transform.position = temp;
                    }
                    else if (i == 3)
                    {
                        temp = losingPlayerInf[i].transform.position;
                        temp.y -= 0.8f;
                        temp.x += (0.65f * playerXMultiplier);
                        losingPlayerInf[i].transform.position = temp;
                    }
                    else if (i == 4)
                    {
                        temp = losingPlayerInf[i].transform.position;
                        temp.y += 0.8f;
                        losingPlayerInf[i].transform.position = temp;
                    }
                }*/
            }
            else if(losingPlayerTanks.Count > 1)
            {
                if (losingPlayerTanks[0].transform.position.x > winngingPlayerPosition.x)
                    playerXMultiplier = 1;
                else
                    playerXMultiplier = -1;
                /*for (int i = 1; i < losingPlayerTanks.Count; i++)
                {
                    if (i == 1)
                    {
                        temp = losingPlayerTanks[i].transform.position;
                        temp.x += (0.95f * playerXMultiplier);
                        losingPlayerTanks[i].transform.position = temp;
                    }
                    else if (i == 2)
                    {
                        temp = losingPlayerTanks[i].transform.position;
                        temp.y += 0.6f;
                        losingPlayerTanks[i].transform.position = temp;
                    }
                    else if (i == 3)
                    {
                        temp = losingPlayerTanks[i].transform.position;
                        temp.y += 0.6f;
                        temp.x += (0.95f * playerXMultiplier);
                        losingPlayerTanks[i].transform.position = temp;
                    }
                }*/
            }
            if(playerXMultiplier != 0)
                ExpandUnitsForBattleResults(losingPlayerTanks, losingPlayerInf, playerXMultiplier);
            //remove battle texts for losing player
            List<GameObject> losingPlayerBattleTextToDestroy = new List<GameObject>();
            foreach (KeyValuePair<GameObject, int> battleText in BattleUnitTexts)
            {
                if (battleText.Value == losingPlayerNumber)
                {
                    losingPlayerBattleTextToDestroy.Add(battleText.Key);
                }                    
            }
            foreach (GameObject textToDestroy in losingPlayerBattleTextToDestroy)
            {
                BattleUnitTexts.Remove(textToDestroy);
                GameObject textObject = textToDestroy;
                Destroy(textObject);
                textObject = null;
            }
            losingPlayerBattleTextToDestroy.Clear();
        }
    }
    public void ExpandForTie()
    {
        int player1Multiplier;
        int player2Multiplier;

        Vector3 player1UnitStartPosition = new Vector3(0, 0, 0);
        Vector3 player2UnitStartPosition = new Vector3(0, 0, 0);

        if (Player1Inf.Count > 0)
            player1UnitStartPosition = Player1Inf[0].gameObject.transform.position;
        else
            player1UnitStartPosition = Player1Tank[0].gameObject.transform.position;

        if (Player2Inf.Count > 0)
            player2UnitStartPosition = Player2Inf[0].gameObject.transform.position;
        else
            player2UnitStartPosition = Player2Tank[0].gameObject.transform.position;

        if (player1UnitStartPosition.x > player2UnitStartPosition.x)
        {
            player1Multiplier = 1;
            player2Multiplier = -1;
        }
        else
        {
            player1Multiplier = -1;
            player2Multiplier = 1;
        }
        ExpandUnitsForBattleResults(Player1Tank, Player1Inf, player1Multiplier);
        ExpandUnitsForBattleResults(Player2Tank, Player2Inf, player2Multiplier);
        foreach (KeyValuePair<GameObject, int> battleText in BattleUnitTexts)
        {
            GameObject battleTextToDestroy = battleText.Key;
            Destroy(battleTextToDestroy);
            battleTextToDestroy = null;
        }
        BattleUnitTexts.Clear();
    }
    void ExpandUnitsForBattleResults(List<GameObject> tanks, List<GameObject> inf, int playerXMultiplier)
    {
        Vector3 temp;
        if (inf.Count > 1)
        {
            for (int i = 1; i < inf.Count; i++)
            {
                if (i == 1)
                {
                    temp = inf[i].transform.position;
                    temp.x += (0.65f * playerXMultiplier);
                    inf[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = inf[i].transform.position;
                    temp.y -= 0.8f;
                    inf[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = inf[i].transform.position;
                    temp.y -= 0.8f;
                    temp.x += (0.65f * playerXMultiplier);
                    inf[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = inf[i].transform.position;
                    temp.y += 0.8f;
                    inf[i].transform.position = temp;
                }
            }
        }
        if (tanks.Count > 1)
        {
            for (int i = 1; i < tanks.Count; i++)
            {
                if (i == 1)
                {
                    temp = tanks[i].transform.position;
                    temp.x += (0.95f * playerXMultiplier);
                    tanks[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = tanks[i].transform.position;
                    temp.y += 0.6f;
                    tanks[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = tanks[i].transform.position;
                    temp.y += 0.6f;
                    temp.x += (0.95f * playerXMultiplier);
                    tanks[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = tanks[i].transform.position;
                    temp.y -= 0.6f;
                    tanks[i].transform.position = temp;
                }
            }
        }
    }
    public void RearrangeUnitsAfterTheyAreKilledFromBattle(int playerNumber)
    {
        Debug.Log("Executing RearrangeUnitsAfterTheyAreKilledFromBattle for land object: " + this.gameObject);
        if (playerNumber == 1)
        {
            if (Player1Inf.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y -= 0.5f;
                temp.x -= 0.5f;
                foreach (GameObject inf in Player1Inf)
                    inf.transform.position = temp;
            }
            if (Player1Tank.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y += 0.5f;
                temp.x -= 0.7f;
                foreach (GameObject tank in Player1Tank)
                    tank.transform.position = temp;
            }
            ExpandUnitsForBattleResults(Player1Tank, Player1Inf, -1);
        }
        else if (playerNumber == 2)
        {
            if (Player2Inf.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y -= 0.5f;
                temp.x += 0.5f;
                foreach (GameObject inf in Player2Inf)
                    inf.transform.position = temp;
            }
            if (Player2Tank.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y += 0.5f;
                temp.x += 0.7f;
                foreach (GameObject tank in Player2Tank)
                    tank.transform.position = temp;
            }
            ExpandUnitsForBattleResults(Player2Tank, Player2Inf, 1);
        }
        else if (playerNumber == -1)
        {
            if (Player1Inf.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y -= 0.5f;
                temp.x -= 0.5f;
                foreach (GameObject inf in Player1Inf)
                    inf.transform.position = temp;
            }
            if (Player1Tank.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y += 0.5f;
                temp.x -= 0.7f;
                foreach (GameObject tank in Player1Tank)
                    tank.transform.position = temp;
            }
            if (Player2Inf.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y -= 0.5f;
                temp.x += 0.5f;
                foreach (GameObject inf in Player2Inf)
                    inf.transform.position = temp;
            }
            if (Player2Tank.Count > 0)
            {
                Vector3 temp = this.transform.position;
                temp.y += 0.5f;
                temp.x += 0.7f;
                foreach (GameObject tank in Player2Tank)
                    tank.transform.position = temp;
            }
            ExpandUnitsForBattleResults(Player1Tank, Player1Inf, -1);
            ExpandUnitsForBattleResults(Player2Tank, Player2Inf, 1);
        }
        UpdateUnitText();
    }
    public void RemoveBattleSiteHighlightAndText()
    {
        if (battleOutlineObject)
        {
            Destroy(battleOutlineObject);
            battleOutlineObject = null;
        }
        if (battleNumberTextObject)
        {
            Destroy(battleNumberTextObject);
            battleNumberTextObject = null;
        }
    }
    public void ResetUnitPositionAndUnitTextAfterBattle()
    {
        Debug.Log("Executing ResetUnitPositionAndUnitTextAfterBattle on land object: " + this.gameObject.name);
        // Clear out old army info since the battle is over
        Player1Inf.Clear();
        Player1Tank.Clear();
        Player2Inf.Clear();
        Player2Tank.Clear();
        
        //Destroy and clear out the BattleUnitTexts
        foreach (KeyValuePair<GameObject, int> battleText in BattleUnitTexts)
        {
            Destroy(battleText.Key);
        }
        BattleUnitTexts.Clear();

        //Reposition the units still on the land
        if (tanksOnLand.Count > 0)
        {
            foreach (GameObject tank in tanksOnLand)
            {
                Vector3 temp = this.transform.position;
                temp.y += 0.5f;
                tank.transform.position = temp;
                
            }
        }
        if (infantryOnLand.Count > 0)
        {
            foreach (GameObject inf in infantryOnLand)
            {
                Vector3 temp = this.transform.position;
                temp.y -= 0.5f;
                inf.transform.position = temp;

            }
        }
        // Create unit texts if multiple units are on the land
        if (infantryOnLand.Count > 1)
        {
            MultipleUnitsUIText("infantry");
            if (GameplayManager.instance.battleSiteNetIds.Count > 0)
            {
                Debug.Log("ResetUnitPositionAndUnitTextAfterBattle: Hiding inf text because of other battles");
                if (infText.activeInHierarchy)
                    infText.SetActive(false);
            }
            
        }
        if (tanksOnLand.Count > 1)
        {
            MultipleUnitsUIText("tank");
            if (GameplayManager.instance.battleSiteNetIds.Count > 0)
            {
                Debug.Log("ResetUnitPositionAndUnitTextAfterBattle: Hiding tank text because of other battles");
                if (tankText.activeInHierarchy)
                    tankText.SetActive(false);
            }
        }      
    }
    public void ExpandForReinforcements(bool isThisForReinforcements)
    {
        Debug.Log("Executing ExpandForReinforcements on land object: " + this.gameObject + " with isThisForReinforcements set to " + isThisForReinforcements.ToString());
        List<GameObject> infCanRetreat = new List<GameObject>();
        List<GameObject> tankCanRetreat = new List<GameObject>();

        if (infantryOnLand.Count > 0)
        {
            foreach (GameObject infantry in infantryOnLand)
            {
                if (isThisForReinforcements)
                {
                    if (infantry.GetComponent<UnitScript>().canUnitReinforce)
                        infCanRetreat.Add(infantry);
                }
                else
                {
                    if (infantry.GetComponent<UnitScript>().canUnitReinforce && infantry.GetComponent<UnitScript>().isUnitReinforcingBattle)
                        infCanRetreat.Add(infantry);
                }

            }
        }
        if (tanksOnLand.Count > 0)
        {
            foreach (GameObject tank in tanksOnLand)
            {
                if (isThisForReinforcements)
                {
                    if (tank.GetComponent<UnitScript>().canUnitReinforce)
                        tankCanRetreat.Add(tank);
                }
                else
                {
                    if (tank.GetComponent<UnitScript>().canUnitReinforce && tank.GetComponent<UnitScript>().isUnitReinforcingBattle)
                        tankCanRetreat.Add(tank);
                }
                
            }
        }
        Vector3 temp;
        if (infCanRetreat.Count > 1)
        {
            for (int i = 0; i < infCanRetreat.Count; i++)
            {
                if (i == 0)
                {
                    temp = transform.position;
                    temp.y -= 0.5f;
                    infCanRetreat[i].transform.position = temp;
                }
                if (i == 1)
                {
                    temp = transform.position;
                    temp.x += 0.65f;
                    temp.y -= 0.5f;
                    infCanRetreat[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = transform.position;
                    temp.x -= 0.6f;
                    temp.y -= 0.5f;
                    infCanRetreat[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = transform.position;
                    temp.y -= 0.5f;
                    temp.y -= 0.8f;
                    infCanRetreat[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = transform.position;
                    temp.y -= 0.5f;
                    temp.y += 0.8f;
                    infCanRetreat[i].transform.position = temp;
                }
            }
        }
        if (tankCanRetreat.Count > 1)
        {
            for (int i = 0; i < tankCanRetreat.Count; i++)
            {
                if (i == 0)
                {
                    temp = transform.position;
                    temp.y += 0.5f;
                    tankCanRetreat[i].transform.position = temp;
                }
                if (i == 1)
                {
                    temp = transform.position;
                    temp.x += 0.95f;
                    temp.y += 0.5f;
                    tankCanRetreat[i].transform.position = temp;
                }
                else if (i == 2)
                {
                    temp = transform.position;
                    temp.x -= 0.95f;
                    temp.y += 0.5f;
                    tankCanRetreat[i].transform.position = temp;
                }
                else if (i == 3)
                {
                    temp = transform.position;
                    temp.y += 0.5f;
                    temp.y += 0.6f;
                    tankCanRetreat[i].transform.position = temp;
                }
                else if (i == 4)
                {
                    temp = transform.position;
                    temp.y += 0.5f;
                    temp.y -= 0.6f;
                    tankCanRetreat[i].transform.position = temp;
                }
            }
        }
        HideUnitText();
    }
}
