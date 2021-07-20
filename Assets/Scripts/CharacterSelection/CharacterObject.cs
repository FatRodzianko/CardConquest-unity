using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CharacterObject : MonoBehaviour
{
    [Header("Character Properties")]
    [SerializeField] public string characterName;
    [SerializeField] public int numberOfInfantry;
    [SerializeField] public int numberOfTanks;
    [SerializeField] public GameObject[] characterCards;
    public List<GameObject> createdCards = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ShowCards()
    {
        Debug.Log("Executing ShowCards from CharacterObject");
        CreateCards();
        Vector3 cardLocation = Camera.main.transform.position;
        cardLocation.x -= 7.5f;
        cardLocation.z = 0f;
        Vector3 cardScale = new Vector3(1.15f, 1.15f, 0f);
        foreach (GameObject playerCard in createdCards)
        {
            if (!playerCard.activeInHierarchy)
            {
                playerCard.SetActive(true);
            }
            playerCard.transform.position = cardLocation;
            playerCard.transform.localScale = cardScale;
            cardLocation.x += 2.5f;
        }
    }
    public void HideCards()
    {
        Debug.Log("Executing HideCards from CharacterObject");
        foreach (GameObject playerCard in characterCards)
        {
            if (playerCard.activeInHierarchy)
            {
                playerCard.SetActive(false);
            }
        }
        DestroyCards();
    }
    void CreateCards()
    {
        Debug.Log("Executing CreateCards from CharacterObject");
        if (createdCards.Count > 0)
            DestroyCards();
        foreach (GameObject playerCard in characterCards)
        {
            GameObject playerCardCreated = Instantiate(playerCard, CharacterSelectionManager.instance.gameObject.transform);
            createdCards.Add(playerCardCreated);
        }
        createdCards = createdCards.OrderByDescending(o => o.GetComponent<Card>().Power).ToList();
    }
    void DestroyCards()
    {
        Debug.Log("Executing DestroyCards from CharacterObject");
        if (createdCards.Count > 0)
        {
            foreach (GameObject playerCard in createdCards)
            {
                GameObject cardToDestroy = playerCard;
                Destroy(playerCard);
            }
        }
        createdCards.Clear();
    }
    private void OnDestroy()
    {
        DestroyCards();
    }
}
