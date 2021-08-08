using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class PlayerListItem : MonoBehaviour
{
    public string PlayerName;
    public int ConnectionId;
    public bool isPlayerReady;
    public string commanderName;

    [SerializeField] private Text PlayerNameText;
    [SerializeField] private Text PlayerReadyStatus;
    [SerializeField] private GameObject playerSelectCommanderButton;
    [SerializeField] private GameObject playerCommanderText;

    public GameObject localLobbyPlayerObject;
    public LobbyPlayer localLobbyPlayerScript;

    private bool isLocalPlayerFoundYet = false;

    // Start is called before the first frame update
    void Start()
    {
        FindLocalLobbyPlayer();
        IsThisForLocalLobbyPlayer();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetPlayerListItemValues()
    {
        PlayerNameText.text = PlayerName;
        UpdatePlayerItemReadyStatus();
    }
    public void UpdatePlayerItemReadyStatus()
    {
        if (isPlayerReady)
        {
            PlayerReadyStatus.text = "Ready";
            PlayerReadyStatus.color = Color.green;
        }
        else
        {
            PlayerReadyStatus.text = "Not Ready";
            PlayerReadyStatus.color = Color.red;
        }
    }
    public void FindLocalLobbyPlayer()
    {
        localLobbyPlayerObject = GameObject.Find("LocalLobbyPlayer");
        localLobbyPlayerScript = localLobbyPlayerObject.GetComponent<LobbyPlayer>();
        isLocalPlayerFoundYet = true;
    }
    public void IsThisForLocalLobbyPlayer()
    {
        if (this.PlayerName == localLobbyPlayerScript.PlayerName && this.ConnectionId == localLobbyPlayerScript.ConnectionId)
        {
            if(localLobbyPlayerScript.myPlayerListItem == null)
                localLobbyPlayerScript.myPlayerListItem = this;
            ActivateSelectCommanderButton();
        }
        else
        {
            playerSelectCommanderButton.SetActive(false);
            playerCommanderText.SetActive(false);
        }
    }
    public void ActivateSelectCommanderButton()
    {
        if (localLobbyPlayerScript.isCommanderSelected)
        {
            playerSelectCommanderButton.SetActive(false);
            playerCommanderText.SetActive(true);
        }
        else
        {
            playerSelectCommanderButton.SetActive(true);
            playerCommanderText.SetActive(false);
        }
    }
    public void SelectCommanderButtonPressed()
    {
        localLobbyPlayerScript.SelectCommanderButtonPressed();
    }
    /*public void SetCommanderNameText(string CommanderName)
    {
        commanderName = CommanderName;
        playerCommanderText.GetComponent<Text>().text = commanderName;
    }*/
    public void SetCommanderNameText(bool DoesPlayerHaveACommanderChosen)
    {
        Debug.Log("Executing SetCommanderNameText with value: " + DoesPlayerHaveACommanderChosen.ToString());
        if (DoesPlayerHaveACommanderChosen)
        {
            playerSelectCommanderButton.SetActive(false);
            playerCommanderText.SetActive(true);            
            playerCommanderText.GetComponent<Text>().text = commanderName;
        }
        else
        {
            playerCommanderText.GetComponent<Text>().text = commanderName;
            playerCommanderText.SetActive(false);
            if(isLocalPlayerFoundYet)
                IsThisForLocalLobbyPlayer();
        }
    }
}
