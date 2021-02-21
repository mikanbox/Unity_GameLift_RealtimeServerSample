using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameMgr : MonoBehaviour {

    public static Color[] playercolorlist = new Color[10];

    public static GameMgr _GameMgr;
    public MoveCharacter chalacter;
    public RoomMgr roomMgr;

    public List<int> otherplayerlist = new List<int> ();
    public Dictionary<int, GameObject> otherplayerobjectlist = new Dictionary<int, GameObject> ();
    public Dictionary<int, string> otherplayerNameList = new Dictionary<int, string> ();

    private float timeElapsed = 0;

    public UnityEngine.UI.Text DebugUI;

    public string userName;
    public InputField userNameInput;

    public GameObject userNameListChild;

    public GameObject popupMessageObj;

    void Awake () {
        playercolorlist[0] = Color.white;
        playercolorlist[1] = Color.red;
        playercolorlist[2] = Color.yellow;
        playercolorlist[3] = Color.cyan;
        playercolorlist[4] = Color.green;
        playercolorlist[5] = Color.gray;
        playercolorlist[6] = Color.magenta;
        _GameMgr = this;
    }
    void Start () {
        roomMgr = new RoomMgr ();
        roomMgr.DebugUI = DebugUI;
        DebugUI.text = "initialize";
    }
    void Update () {
        UpdateMyCharacterPosition ();
        userName = userNameInput.text;
    }

    public void UpdateMyCharacterPosition () {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= 0.05f) {
            if (roomMgr.realTimeClient != null) {
                if (roomMgr.realTimeClient.IsConnected ()) {
                    roomMgr.SendMessageToAll (chalacter.transform.position.ToString (), (int) MessageCode.PLAYER_POSITION_UPDATE);
                }
            }
            timeElapsed = 0.0f;
        }
    }

    // ------------------------------------------------------------#
    // Room Creation and Join
    // ------------------------------------------------------------#
    public void CreateRoom () {
        roomMgr.CreateRoom ();
        roomMgr.JoinRoom ();
    }

    public void SearchRoom () {
        roomMgr.SearchRooms ();
        roomMgr.JoinRoom ();
    }

    // ------------------------------------------------------------#
    // Overall Game State Update
    // ------------------------------------------------------------#
    public void StartGame(){
        roomMgr.SendMessageToAll (chalacter.transform.position.ToString (), (int) MessageCode.START_GAME);
    }
    public void EndGame(){
        roomMgr.SendMessageToAll (chalacter.transform.position.ToString (), (int) MessageCode.END_GAME);
    }
    public void GetStartGameResponse(){
        popUpMessage("Start Game");
    }
    public void GetEndGameResponse(){
        popUpMessage("End Game");
    }

    public void popUpMessage(string message){
        popupMessageObj.transform.GetChild (0).gameObject.GetComponent<Text> ().text = message;
        popupMessageObj.SetActive(true);

        StartCoroutine(DelayMethod(3.5f, () =>
        {
            popupMessageObj.SetActive(false);
        }));
    }


    private IEnumerator DelayMethod(float waitTime, Action action)
    {
        yield return new WaitForSeconds(waitTime);
        action();
    }

    // ------------------------------------------------------------#
    // User Position Update
    // ------------------------------------------------------------#
    public void UpdateOtherUserPosition (int playerpeerid, string playerpos) {
        int id = GameMgr._GameMgr.otherplayerlist.IndexOf (playerpeerid);
        if (id != -1) {
            Vector3 pos = StringToVector3 (playerpos);
            otherplayerobjectlist[playerpeerid].transform.position = pos;
        }
    }

    public void AddOtherUser (int playerpeerid) {
        otherplayerlist.Add (playerpeerid);
        GameObject obj = Instantiate (Resources.Load ("Prefabs/otherPlayer") as GameObject);
        obj.transform.position = Vector3.zero;

        changeUserColor (false, obj, playerpeerid);

        otherplayerobjectlist.Add (
            playerpeerid, obj
        );
    }

    public void changePlayerColor (int playerpeerid) {
        changeUserColor (true, new GameObject (), playerpeerid);
    }

    public void changeUserColor (bool isPlayer, GameObject obj, int id) {
        GameObject tmp = obj;
        if (isPlayer) {
            tmp = chalacter.gameObject;
        }
        tmp.transform.GetChild (0).GetComponent<UnityEngine.SpriteRenderer> ().color = playercolorlist[id];
    }

    public void AddOtherPlayerName (bool isPlayer, string name, int playerpeerid) {
            var clone = GameObject.Instantiate (userNameListChild) as GameObject;
            clone.transform.parent = userNameListChild.transform.parent;

        if (isPlayer) {
            clone.GetComponent<Text> ().text = userName;
        } else {
            clone.GetComponent<Text> ().text = name;
            otherplayerNameList[playerpeerid] = name;
        }
    }

    public void AddUserNameToObject (bool isPlayer, int playerpeerid, string name) {
        GameObject obj;
        if (isPlayer) {
            obj = chalacter.gameObject;
            obj.transform.GetChild (1).GetChild (0).gameObject.GetComponent<Text> ().text = userName;
        } else {
            obj = otherplayerobjectlist[playerpeerid];
            obj.transform.GetChild (1).GetChild (0).gameObject.GetComponent<Text> ().text = name;
        }
        
    }

    // ------------------------------------------------------------#
    // Util
    // ------------------------------------------------------------#

    public static Vector3 StringToVector3 (string sVector) {
        if (sVector.StartsWith ("(") && sVector.EndsWith (")")) {
            sVector = sVector.Substring (1, sVector.Length - 2);
        }

        string[] sArray = sVector.Split (',');
        Vector3 result = new Vector3 (
            float.Parse (sArray[0]),
            float.Parse (sArray[1]),
            float.Parse (sArray[2]));
        return result;
    }

}