using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager instance;
    public List<GameObject> unitsSelected;
    private void Awake()
    {
        MakeInstance();
    }
    // Start is called before the first frame update
    void Start()
    {        
        unitsSelected = new List<GameObject>();
        
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
}
