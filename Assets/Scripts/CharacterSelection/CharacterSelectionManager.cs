using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class CharacterSelectionManager : NetworkBehaviour
{
    public static CharacterSelectionManager instance;

    public int currentCommanderIndex = 0;
    public bool isPlayerViewCharacterSelection = false;
    public bool isPlayerViewCards = false;

    [Header("Commanders")]
    [SerializeField] public CharacterObject[] commanders;    

    [Header("LobbyPlayers")]
    [SerializeField] private GameObject LocalLobbyPlayer;
    [SerializeField] private LobbyPlayer LocalLobbyPlayerScript;

    [Header("UI Objects")]
    [SerializeField] GameObject CommanderSelectionPanel;
    [SerializeField] GameObject CommanderTextObjects;
    [SerializeField] Text CommanderNameText;
    [SerializeField] Text CommanderInfantryText;
    [SerializeField] Text CommanderTankText;
    [SerializeField] GameObject ButtonHolderObject;
    [SerializeField] GameObject FindLobbiesPanel;

    // Start is called before the first frame update

    private void Awake()
    {
        MakeInstance();

        /*GameObject[] objs = GameObject.FindGameObjectsWithTag("CharacterSelectionManager");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);*/
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void MakeInstance()
    {
        if (instance == null)
            instance = this;
    }
    
    public void FindLocalLobbyPlayer()
    {
        GetLocalLobbyPlayer();
    }
    void GetLocalLobbyPlayer()
    {
        LocalLobbyPlayer = GameObject.Find("LocalLobbyPlayer");
        LocalLobbyPlayerScript = LocalLobbyPlayer.GetComponent<LobbyPlayer>();
    }
    public void ActivateCharacterSelectionUI()
    {
        isPlayerViewCharacterSelection = true;
        CommanderSelectionPanel.SetActive(true);
        FindLobbiesPanel.SetActive(false);
        DisplayCommander();
    }
    public void BackToLobby()
    {
        isPlayerViewCharacterSelection = false;
        CommanderSelectionPanel.SetActive(false);
        FindLobbiesPanel.SetActive(true);
        //LocalLobbyPlayerScript.BackToLobby();
    }
    public void NextCommander()
    {
        currentCommanderIndex++;
        if (currentCommanderIndex >= commanders.Length)
            currentCommanderIndex = 0;
        DisplayCommander();
    }
    public void PreviousCommander()
    {
        currentCommanderIndex--;
        if (currentCommanderIndex < 0)
            currentCommanderIndex = (commanders.Length - 1);
        DisplayCommander();
    }
    public void SelectCommander()
    {
        LocalLobbyPlayerScript.SelectCommander(commanders[currentCommanderIndex].characterName, true);
        BackToLobby();
    }
    void DisplayCommander()
    {
        CharacterObject currentCommander = commanders[currentCommanderIndex];
        CommanderNameText.text = currentCommander.characterName;
        CommanderInfantryText.text = "x" + currentCommander.numberOfInfantry.ToString();
        CommanderTankText.text = "x" + currentCommander.numberOfTanks.ToString();
    }
    public void ViewOrHideCommanderCards()
    {
        Debug.Log("executing ViewOrHideCommanderCards");
        isPlayerViewCards = !isPlayerViewCards;
        if (isPlayerViewCards)
        {
            Debug.Log("ViewOrHideCommanderCards Player is viewing their cards.");
            commanders[currentCommanderIndex].ShowCards();
            CommanderTextObjects.SetActive(false);
            ButtonHolderObject.SetActive(false);
        }
        else
        {
            Debug.Log("ViewOrHideCommanderCards Player is HIDING their cards.");
            commanders[currentCommanderIndex].HideCards();
            CommanderTextObjects.SetActive(true);
            ButtonHolderObject.SetActive(true);
        }            
    }
}
