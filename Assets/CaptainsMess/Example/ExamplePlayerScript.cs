using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;


public class ExamplePlayerScript : CaptainsMessPlayer
{
    public Image image;
    public Text nameField;
    public Text readyField;
    public Text rollResultField;
    public Text totalPointsField;

    [SyncVar]
    public Color myColour;

    // Simple game states for a dice-rolling game

    [SyncVar]
    public int rollResult;

    [SyncVar]
    public int totalPoints;

    [SyncVar]
    public bool locationSynced;

    public GameObject spherePrefab;

    private byte[] savedBytes;

    private bool locationSent;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();


        // Send custom player info
        // This is an example of sending additional information to the server that might be needed in the lobby (eg. colour, player image, personal settings, etc.)

        myColour = UnityEngine.Random.ColorHSV(0, 1, 1, 1, 1, 1);
        CmdSetCustomPlayerInfo(myColour);
    }



    [Command]
    public void CmdSetCustomPlayerInfo(Color aColour)
    {
        myColour = aColour;
    }

    [Command]
    public void CmdRollDie()
    {
        rollResult = UnityEngine.Random.Range(1, 7);
    }

    //command is executed on the server so the current bezier of client is never updated



    [Command]
    public void CmdPlayAgain()
    {
        ExampleGameSession.instance.PlayAgain();
    }

    public override void OnClientEnterLobby()
    {
        base.OnClientEnterLobby();

        // Brief delay to let SyncVars propagate
        Invoke("ShowPlayer", 0.5f);
    }

    public override void OnClientReady(bool readyState)
    {
        if (readyState)
        {
            readyField.text = "READY!";
            readyField.color = Color.green;
        }
        else
        {
            readyField.text = "not ready";
            readyField.color = Color.red;
        }
    }

    void ShowPlayer()
    {
        transform.SetParent(GameObject.Find("Canvas/PlayerContainer").transform, false);

        image.color = myColour;
        nameField.text = deviceName;
        readyField.gameObject.SetActive(true);

        rollResultField.gameObject.SetActive(false);
        totalPointsField.gameObject.SetActive(false);

        OnClientReady(IsReady());
    }

    int countTouch = 0;
    Vector3 prevPosition = Vector3.zero;
    Vector3 touchPoint = Vector3.zero;
    float coneDistance = 2;

    //keep a list of curves that the player drew
    //all the players have a different list because lists aren't networked yet
    public List<GameObject> curvesDrew;

    GameObject currentBezier = null;

    //detect touch of player here
    public void Update()
    {
        string synced = locationSynced ? "SYNC" : "NO";
        totalPointsField.text = "Points: " + totalPoints.ToString() + synced;
        if (rollResult > 0)
        {
            rollResultField.text = "Roll: " + rollResult.ToString();
        }
        else
        {
            rollResultField.text = "";
        }


        //the instantiation current curve logic
        ExampleGameSession gameSession = ExampleGameSession.instance;
        if (isLocalPlayer && gameSession && gameSession.gameState == GameState.WaitingForRolls)
        {

            if (canStartDraw && Input.GetMouseButtonDown(0))
            {

                Debug.Log("ENTERED instantiation");
                Ray raycast = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 point = raycast.GetPoint(2);
                canStartDraw = false;
                //instantiate bezier across all clients
                CmdMakeSphere(point);

            }
            else if (Input.GetMouseButtonDown(1))
            {
                //if the user right click, then finished this bezier
                Debug.Log("stop draw");
                currentBezier.GetComponent<BezierMaster.BezierMaster>().stop = true;
                currentBezier = null;
                canStartDraw = true;
            }
        }
    }

    [Command]
    public void CmdMakeSphere(Vector3 position)
    {
        GameObject obj = (GameObject)Instantiate(spherePrefab, position, Quaternion.identity);
        NetworkServer.SpawnWithClientAuthority(obj, connectionToClient);
        NetworkInstanceId id = obj.GetComponent<NetworkIdentity>().netId;
        Debug.Log("network id: " + id.ToString());
        Debug.Log("INSTANTIATED!!!!!!!!!!!!");
        //dont set colour for now
        //RpcSetSphereColor (sphere, myColour.r, myColour.g, myColour.b);
        RpcgetReferenceToObject(id);
    }

    [ClientRpc]
    void RpcgetReferenceToObject(NetworkInstanceId id)
    {
        //only update currentBezier if it's local player
        if (isLocalPlayer)
        {
            currentBezier = ClientScene.FindLocalObject(id);
            Debug.Log("assigned");
        }
    }

    [ClientRpc]
    public void RpcOnStartedGame()
    {
        readyField.gameObject.SetActive(false);

        rollResultField.gameObject.SetActive(true);
        totalPointsField.gameObject.SetActive(true);
    }

    //this local bool is for drawing one bezier curve for now
    bool canStartDraw = true;
    //the gui that is displayed on the user's screen
    void OnGUI()
    {
        if (isLocalPlayer)
        {
            GUILayout.BeginArea(new Rect(0, Screen.height * 0.8f, Screen.width, 100));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            ExampleGameSession gameSession = ExampleGameSession.instance;
            if (gameSession)
            {
                if (gameSession.gameState == GameState.Lobby ||
                    gameSession.gameState == GameState.Countdown)
                {
                    if (GUILayout.Button(IsReady() ? "Not ready" : "Ready", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100)))
                    {
                        if (IsReady())
                        {
                            SendNotReadyToBeginMessage();
                        }
                        else
                        {
                            SendReadyToBeginMessage();
                        }
                    }
                }
                else if (gameSession.gameState == GameState.WaitingForRolls)
                {


                }
                else if (gameSession.gameState == GameState.GameOver)
                {
                    if (isServer)
                    {
                        if (GUILayout.Button("Play Again", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100)))
                        {
                            CmdPlayAgain();
                        }
                    }
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}