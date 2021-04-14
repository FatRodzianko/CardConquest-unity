using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerUnitHolder : NetworkBehaviour
{
    [SyncVar] public string ownerPlayerName;
    [SyncVar] public int ownerConnectionId;
    [SyncVar] public int ownerPlayerNumber;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void RunCmdTest()
    {
        Debug.Log("Running CmdTest");
        CmdTest();
    }
    [Command]
    public void CmdTest()
    {
        Debug.Log("Test successful");
    }
}
