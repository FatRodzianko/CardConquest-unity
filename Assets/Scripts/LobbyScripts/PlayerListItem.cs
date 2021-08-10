using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Mirror;



public class PlayerListItem : MonoBehaviour
{
    public string PlayerName;
    public int ConnectionId;
    public bool isPlayerReady;
    public string commanderName;
    public ulong playerSteamId;
    private bool avatarRetrieved;

    [SerializeField] private Text PlayerNameText;
    [SerializeField] private Text PlayerReadyStatus;
    [SerializeField] private GameObject playerSelectCommanderButton;
    [SerializeField] private GameObject playerCommanderText;
    [SerializeField] private RawImage playerAvatar;

    public GameObject localLobbyPlayerObject;
    public LobbyPlayer localLobbyPlayerScript;

    private bool isLocalPlayerFoundYet = false;

    protected Callback<AvatarImageLoaded_t> avatarImageLoaded;

    // Start is called before the first frame update
    void Start()
    {
        FindLocalLobbyPlayer();
        IsThisForLocalLobbyPlayer();
        avatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SetPlayerListItemValues()
    {
        PlayerNameText.text = PlayerName;
        UpdatePlayerItemReadyStatus();
        if (!avatarRetrieved)
            GetPlayerAvatar();
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
    void GetPlayerAvatar()
    {
        int imageId = SteamFriends.GetLargeFriendAvatar((CSteamID)playerSteamId);

        if (imageId == -1)
        {
            Debug.Log("GetPlayerAvatar: Avatar not in cache. Will need to download from steam.");
            return;
        }

        playerAvatar.texture = GetSteamImageAsTexture(imageId);
    }
    private Texture2D GetSteamImageAsTexture(int iImage)
    {
        Debug.Log("Executing GetSteamImageAsTexture for player: " + this.PlayerName);
        Texture2D texture = null;

        bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
        if (isValid)
        {
            Debug.Log("GetSteamImageAsTexture: Image size is valid?");
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));

            if (isValid)
            {
                Debug.Log("GetSteamImageAsTexture: Image size is valid for GetImageRBGA?");
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }
        avatarRetrieved = true;
        return texture;
    }
    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID.m_SteamID == playerSteamId)
        {
            Debug.Log("OnAvatarImageLoaded: Avatar downloaded from steam.");
            playerAvatar.texture = GetSteamImageAsTexture(callback.m_iImage);
        }
        else
        {
            return;
        }
    }
}
