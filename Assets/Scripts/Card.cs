using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class Card : NetworkBehaviour
{
    public static readonly Dictionary<int, string> SpecialAbilities = new Dictionary<int, string>
    {
        {1,"If your army + reinforce. has inf but no tanks, +2 battle score"}, //inf commander - power 7 - complete
        {2,"If opp. main army has no tanks, +1 attack value"}, //tank commander - power 4 - complete
        {3,"If opp. reinforced but you did not, +3 battle score" }, //tank commander  - power 5 - complete
        {4,"If opp. main army has <2 inf., +1 for each inf. in your army+reinforce" }, //inf commander - power 1 - complete
        {5,"If opp. main army has <2 tanks, +1 for each tank in your main army" }, //tank commander - power 2 - complete
        {6,"If opp. played 0 power card, set att. value equal to def. value" }, //tank commander - power 3 - complete
        {7,"If opp. card has >=3 att. value, +1 def. value for this card" }, //inf commander - power 3 - complete
        {8,"If your card power > opp. card power & you have no reinf., opp. reinf. = 0" }, //inf commander - power 5 - complete
        {9, "If you are defending your base, +2 battle score" }, //inf commander - power 4 - completed
        {10,"If your army + reinforce. has tanks but no inf, +1 attack value"} // tank commander - power 7 - complete
    };

    [Header("Card Components")]
    [SerializeField] private GameObject AttackIcon;
    [SerializeField] private GameObject DefenseIcon;
    [SerializeField] private TextMeshPro cardPowerText;
    [SerializeField] private TextMeshPro cardSpecialAbilityText;
    [SerializeField] private GameObject cardAttTextHolder;
    [SerializeField] private TextMeshPro cardAttText;
    [SerializeField] private GameObject cardDefTextHolder;
    [SerializeField] private TextMeshPro cardDefText;
    public List<GameObject> AttackIcons = new List<GameObject>();
    public List<GameObject> DefenseIcons = new List<GameObject>();
    [SyncVar(hook = nameof(HandleAbilityActivated))] public bool didAbilityActivate = false;
    public bool localDidAbilityActivate = false;


    [Header("Player Owned Information")]
    [SyncVar] public string ownerPlayerName;
    [SyncVar] public int ownerConnectionId;
    [SyncVar] public int ownerPlayerNumber;
    public GameObject myPlayerHandObject;

    public string CardName;
    public int Power;
    public int AttackValue;
    public int AttackValueStatic;
    public int DefenseValue;
    public int DefenseValueStatic;
    public int SpecialAbilityNumber;

    public bool currentlySelected = false;
    [SerializeField] GameObject cardOutlinePrefab;
    public GameObject cardOutlineObject;
    public bool isClickable = true;
    // Start is called before the first frame update
    void Start()
    {
        SetAttackAndDefenseStaticValues();
        UpdateCardTextObjects();
        SpawnAttackDefenseIcons();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CardClickedOn()
    {
        currentlySelected = !currentlySelected;
        if (currentlySelected)
        {
            if (!cardOutlineObject)
            {
                cardOutlineObject = Instantiate(cardOutlinePrefab, transform.position, Quaternion.identity);
                cardOutlineObject.transform.SetParent(this.transform);
                Vector3 cardScale = new Vector3(1f, 1f, 0f);
                cardOutlineObject.transform.localScale = cardScale;
            }
        }
        else if (!currentlySelected)
        {
            if (cardOutlineObject)
            {
                Destroy(cardOutlineObject);
                cardOutlineObject = null;
            }
        }
    }
    void UpdateCardTextObjects()
    {
        Debug.Log("Executing UpdateCardTextObjects");
        foreach (Transform child in this.transform)
        {
            if (child.name == "cardPowerTextHolder")
            {
                Debug.Log("UpdateCardTextObjects : found cardPowerTextHolder object");
                GameObject cardPowerTextObject = child.Find("cardPowerText").gameObject;
                if (cardPowerTextObject)
                {
                    cardPowerText = cardPowerTextObject.GetComponent<TextMeshPro>();
                    cardPowerText.SetText("Power " + Power.ToString());
                }

            }
            else if (child.name == "cardSpecialAbilityTextHolder")
            {
                Debug.Log("UpdateCardTextObjects : found cardSpecialAbilityTextHolder object");
                GameObject cardSpecialAbilityTextObject = child.Find("cardSpecialAbilityText").gameObject;
                if (cardSpecialAbilityTextObject)
                {
                    cardSpecialAbilityText = cardSpecialAbilityTextObject.GetComponent<TextMeshPro>();
                    GetCardSpecialAbilityText();
                }

            }
            else if (child.name == "cardAttTextHolder")
            {
                Debug.Log("UpdateCardTextObjects : found cardAttTextHolder object");
                cardAttTextHolder = child.transform.gameObject;
                GameObject cardAttTextObject = child.Find("cardAttText").gameObject;
                if (cardAttTextObject)
                {
                    cardAttText = cardAttTextObject.GetComponent<TextMeshPro>();
                }

            }
            else if (child.name == "cardDefTextHolder")
            {
                Debug.Log("UpdateCardTextObjects : found cardDefTextHolder object");
                cardDefTextHolder = child.transform.gameObject;
                GameObject cardDefTextObject = child.Find("cardDefText").gameObject;
                if (cardDefTextObject)
                {
                    cardDefText = cardDefTextObject.GetComponent<TextMeshPro>();
                }

            }
        }
    }
    void GetCardSpecialAbilityText()
    {
        Debug.Log("Executing GetCardSpecialAbilityText");
        if (SpecialAbilityNumber != 0)
        {
            Debug.Log("GetCardSpecialAbilityText: getting text for special ability: " + SpecialAbilityNumber.ToString());
            cardSpecialAbilityText.SetText(SpecialAbilities[SpecialAbilityNumber]);
        }
        else
        {
            Debug.Log("GetCardSpecialAbilityText: No special ability text.");
            cardSpecialAbilityText.SetText("No special ability.");
        }
    }
    
    void SpawnAttackDefenseIcons()
    {
        Debug.Log("Executing SpawnAttackDefenseIcons");
        if (AttackIcons.Count > 0)
        {
            foreach (GameObject attackIcon in AttackIcons)
            {
                GameObject iconToDestroy = attackIcon;
                Destroy(iconToDestroy);
                iconToDestroy = null;
            }
            AttackIcons.Clear();
        }
        if (DefenseIcons.Count > 0)
        {
            foreach (GameObject defenseIcon in DefenseIcons)
            {
                GameObject iconToDestroy = defenseIcon;
                Destroy(iconToDestroy);
                iconToDestroy = null;
            }
            DefenseIcons.Clear();
        }
        if (AttackValue > 0)
        {
            Debug.Log("SpawnAttackDefenseIcons: Attack value greater than 0. Spawning this many attack icons: " + AttackValue.ToString());
            if (AttackValue > 5)
            {
                Debug.Log("SpawnAttackDefenseIcons: Attack value greater than 5. Creating the attack text thing.");
                GameObject newAttackDefenseIcon = Instantiate(AttackIcon, this.transform);
                Vector3 newPosition = new Vector3(-0.7f, 0.7f, 0f);
                newAttackDefenseIcon.transform.localPosition = newPosition;
                AttackIcons.Add(newAttackDefenseIcon);
                if (!cardAttTextHolder.activeInHierarchy)
                    cardAttTextHolder.SetActive(true);
                cardAttText.SetText("x" + AttackValue.ToString());
            }
            else
            {
                if (cardAttTextHolder.activeInHierarchy)
                    cardAttTextHolder.SetActive(false);
                for (int i = 0; i < AttackValue; i++)
                {
                    Debug.Log("SpawnAttackDefenseIcons: Spawning attack icon number: " + i.ToString());
                    GameObject newAttackDefenseIcon = Instantiate(AttackIcon, this.transform);
                    Vector3 newPosition = new Vector3(-0.7f, 0.7f, 0f);
                    newAttackDefenseIcon.transform.localPosition = newPosition;
                    if (i > 0)
                    {
                        float xPositionValue = 0.35f * i;
                        newPosition.x += xPositionValue;
                        newAttackDefenseIcon.transform.localPosition = newPosition;
                    }
                    AttackIcons.Add(newAttackDefenseIcon);
                }
            }
            
        }
        if (DefenseValue > 0)
        {
            Debug.Log("SpawnAttackDefenseIcons: Defense value greater than 0. Spawning this many defense icons: " + DefenseValue.ToString());
            for (int i = 0; i < DefenseValue; i++)
            {
                Debug.Log("SpawnAttackDefenseIcons: Spawning Defense icon number: " + i.ToString());
                GameObject newAttackDefenseIcon = Instantiate(DefenseIcon, this.transform, false);
                Vector3 newPosition = new Vector3(-0.7f, 0.3f, 0f);
                newAttackDefenseIcon.transform.localPosition = newPosition;
                if (i > 0)
                {
                    float xPositionValue = 0.35f * i;
                    newPosition.x += xPositionValue;
                    newAttackDefenseIcon.transform.localPosition = newPosition;
                }
                DefenseIcons.Add(newAttackDefenseIcon);
            }
        }
    }
    public void HandleAbilityActivated(bool oldValue, bool newValue)
    {
        Debug.Log("Executing HandleAbilityActivated");
        if (isServer)
        {
            didAbilityActivate = newValue;
        }
        if (isClient)
        {
            if (newValue && !localDidAbilityActivate)
            {
                localDidAbilityActivate = true;
                ActivateCardAbility(localDidAbilityActivate);
            }
            else if (!newValue && localDidAbilityActivate)
            {
                localDidAbilityActivate = false;
                ActivateCardAbility(localDidAbilityActivate);
            }
        }
    }
    void ActivateCardAbility(bool wasAbilityActivated)
    {
        Debug.Log("Executing ActivateCardAbility with wasAbilityActivated as: " + wasAbilityActivated.ToString());
        if (SpecialAbilityNumber == 2 || SpecialAbilityNumber == 10)
            ActivateCard2OrCard10Ability(wasAbilityActivated);
        else if (SpecialAbilityNumber == 6)
            ActivateCard6Ability(wasAbilityActivated);
        else if(SpecialAbilityNumber == 7)
            ActivateCard7Ability(wasAbilityActivated);
    }
    void ActivateCard2OrCard10Ability(bool wasAbilityActivated)
    {
        Debug.Log("Executing ActivateCard2OrCard10Ability with wasAbilityActivated as: " + wasAbilityActivated.ToString());
        if (wasAbilityActivated)
        {
            AttackValue = AttackValueStatic + 1;
            SpawnAttackDefenseIcons();
        }
        else
        {
            AttackValue = AttackValueStatic;
            SpawnAttackDefenseIcons();
        }
    }
    void ActivateCard6Ability(bool wasAbilityActivated)
    {
        Debug.Log("Executing ActivateCard6Ability with wasAbilityActivated as: " + wasAbilityActivated.ToString());
        Debug.Log("ActivateCard6Ability: \"If opp. played 0 power card, set att. value equal to def. value\"");
        if (wasAbilityActivated)
        {
            AttackValue = DefenseValueStatic;
            SpawnAttackDefenseIcons();
        }
        else
        {
            AttackValue = AttackValueStatic;
            SpawnAttackDefenseIcons();
        }
    }
    void ActivateCard7Ability(bool wasAbilityActivated)
    {
        Debug.Log("Executing ActivateCard7Ability with wasAbilityActivated as: " + wasAbilityActivated.ToString());
        Debug.Log("ActivateCard7Ability: \"If opp. card has >=3 att. value, +1 def. value for this card\"");
        if (wasAbilityActivated)
        {
            DefenseValue = DefenseValueStatic + 1;
            SpawnAttackDefenseIcons();
        }
        else
        {
            DefenseValue = DefenseValueStatic;
            SpawnAttackDefenseIcons();
        }
    }
    void SetAttackAndDefenseStaticValues()
    {
        AttackValueStatic = AttackValue;
        DefenseValueStatic = DefenseValue;
    }
}
