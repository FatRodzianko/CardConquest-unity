using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

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
    public List<GameObject> BattleUnitTexts = new List<GameObject>();

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
                CollapseUnits();
            }

        }
        if (tankText != null)
        {
            Debug.Log("Updating tank text. Current number of tanks: " + tanksOnLand.Count.ToString() + " on: " + this.gameObject.transform.position);
            if (tanksOnLand.Count > 1)
                tankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + tanksOnLand.Count.ToString());
            else
            {
                Debug.Log("1 or less tank. Deactivating tankText object from: " + this.gameObject.transform.position);
                Destroy(tankText);
                tankText = null;
                CollapseUnits();
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
        if (infantryOnLand.Count > 1)
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
    void CollapseUnits()
    {
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
            foreach (GameObject battleText in BattleUnitTexts)
            {
                if(battleText)
                    battleText.SetActive(false);
            }
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
            foreach (GameObject battleText in BattleUnitTexts)
            {
                if (battleText)
                    battleText.SetActive(true);
            }
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
        //For now this will be "hard coded" for a 2 player game where the playernumbers are either 1 or 2
        foreach (KeyValuePair<uint, int> units in UnitNetIdsAndPlayerNumber)
        {            
            GameObject unitObject = NetworkIdentity.spawned[units.Key].gameObject;
            Vector3 newPosition = unitObject.transform.position;
            // Adjust units for player 1
            if (units.Value == 1)
            {
                
                if (unitObject.tag == "infantry")
                {
                    newPosition.x -= 0.5f;
                    unitObject.transform.position = newPosition;
                    Player1Inf.Add(unitObject);
                }
                else if (unitObject.tag == "tank")
                {
                    newPosition.x -= 0.7f;
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
                    unitObject.transform.position = newPosition;
                    Player2Inf.Add(unitObject);
                }
                else if (unitObject.tag == "tank")
                {
                    newPosition.x += 0.7f;
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

        //Spawn unit text for player 1
        if (Player1Inf.Count > 1)
        {
            Debug.Log("Creating text box for multiple infantry for player 1"); 
            GameObject player1InfText = Instantiate(infTextHolder, gameObject.transform);

            player1InfText.transform.position = transform.position;
            Vector3 player1InfTextPosition = new Vector3(-1.75f, -0.75f, 0.0f);
            player1InfText.transform.localPosition = player1InfTextPosition;

            player1InfText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player1Inf.Count.ToString());
            BattleUnitTexts.Add(player1InfText);
        }
        if (Player1Tank.Count > 1)
        {
            Debug.Log("Creating text box for multiple tanks for player 1");
            GameObject player1TankText = Instantiate(tankTextHolder, gameObject.transform);

            player1TankText.transform.position = transform.position;
            Vector3 player1TankTextPosition = new Vector3(-3.0f, -0.75f, 0.0f);
            player1TankText.transform.localPosition = player1TankTextPosition;

            player1TankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player1Tank.Count.ToString());
            BattleUnitTexts.Add(player1TankText);
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
            BattleUnitTexts.Add(player2InfText);
        }
        if (Player2Tank.Count > 1)
        {
            Debug.Log("Creating text box for multiple tanks for player 2");
            GameObject player2TankText = Instantiate(tankTextHolder, gameObject.transform);

            player2TankText.transform.position = transform.position;
            Vector3 player2TankTextPosition = new Vector3(0.0f, -0.75f, 0.0f);
            player2TankText.transform.localPosition = player2TankTextPosition;

            player2TankText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText("x" + Player2Tank.Count.ToString());
            BattleUnitTexts.Add(player2TankText);
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
}
