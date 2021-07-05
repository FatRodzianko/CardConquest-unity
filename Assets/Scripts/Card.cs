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
        {1,"If your army + reinforce. has inf but no tanks, +2 battle score"},
        {2,"If opp. main army has no tanks, +1 attack value"},
        {3,"If opp. reinforced but you did not, +3 battle score" }
    };

    [Header("Card Components")]
    [SerializeField] private GameObject AttackIcon;
    [SerializeField] private GameObject DefenseIcon;
    [SerializeField] private TextMeshPro cardPowerText;
    [SerializeField] private TextMeshPro cardSpecialAbilityText;
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
    public int DefenseValue;
    public int SpecialAbilityNumber;

    public bool currentlySelected = false;
    [SerializeField] GameObject cardOutlinePrefab;
    public GameObject cardOutlineObject;
    public bool isClickable = true;
    // Start is called before the first frame update
    void Start()
    {
        if (this.gameObject.name.StartsWith("new"))
        {
            UpdateCardTextObjects();
            SpawnAttackDefenseIcons();
        }
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
        if(SpecialAbilityNumber == 2)
            ActivateCard2Ability(wasAbilityActivated);
    }
    void ActivateCard2Ability(bool wasAbilityActivated)
    {
        Debug.Log("Executing ActivateCard2Ability with wasAbilityActivated as: " + wasAbilityActivated.ToString());
        if (wasAbilityActivated)
        {
            AttackValue++;
            SpawnAttackDefenseIcons();
        }
        else
        {
            AttackValue--;
            SpawnAttackDefenseIcons();
        }
    }
}
