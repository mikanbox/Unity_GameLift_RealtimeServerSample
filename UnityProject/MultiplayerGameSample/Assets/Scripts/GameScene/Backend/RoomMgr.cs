using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Aws.GameLift.Realtime.Types;
using UnityEngine;

public enum MessageCode {
    PLAYER_POSITION_UPDATE = 200,
    PLAYER_POSITION_UPDATE_RESPONSE = 201,

    PLAYER_JOIN = 100,
    PLAYER_JOIN_RESPONSE = 101,

    // PLAYER_NAME = 102,
    PLAYER_NAME_RESPONSE = 103,

    PLAYER_JOIN_FINISHED = 104,

    START_GAME = 300,
    START_GAME_RESPONSE=301,
    END_GAME = 302,
    END_GAME_RESPONSE=303,

    DEBUG_MESSAGE = 1,
    DEBUG_MESSAGE_RESPONSE = 2
}

public class RoomMgr {
    RegionEndpoint regionEndpoint = RegionEndpoint.APNortheast1;
    string accessKeyId = "";
    string secretAccessKey = "";
    string gameLiftAliasId = "";



    public AmazonGameLiftClient gameLiftClient;
    public RealTimeClient realTimeClient;

    public string gameSessionId;
    public int playerpeerId = -1;
    private string playsersessionId;

    public UnityEngine.UI.Text DebugUI;

    public RoomMgr () {
        gameLiftClient = new AmazonGameLiftClient (accessKeyId, secretAccessKey, regionEndpoint);
    }

    public async void CreateRoom () {
        UnityEngine.Debug.Log ("CreateRoom");
        String roomName = Guid.NewGuid ().ToString ();

        var request = new CreateGameSessionRequest {
            AliasId = gameLiftAliasId,
            MaximumPlayerSessionCount = 20,
            Name = roomName
        };
        var response = await gameLiftClient.CreateGameSessionAsync (request);

        if (response.HttpStatusCode == HttpStatusCode.OK) {
            gameSessionId = response.GameSession.GameSessionId;
        }

        DebugUI.text = "CreateRoom: SessionID=" + gameSessionId;
        JoinRoom ();
    }

    public async void SearchRooms () {
        UnityEngine.Debug.Log ("SearchRooms");
        var response = await gameLiftClient.SearchGameSessionsAsync (new SearchGameSessionsRequest { AliasId = gameLiftAliasId, });

        if (response.HttpStatusCode == HttpStatusCode.OK) {
            if (response.GameSessions.Count > 0) {
                gameSessionId = response.GameSessions[0].GameSessionId;
            } else {
                UnityEngine.Debug.Log ("There is no room");
            }
        }

        DebugUI.text = "SearchRoom: SessionID=" + gameSessionId;
        JoinRoom ();
    }

    public async void JoinRoom () {
        string sessionId = gameSessionId;
        System.Random rnd = new System.Random ();

        UnityEngine.Debug.Log ("JoinRoom");
        var response = await gameLiftClient.CreatePlayerSessionAsync (new CreatePlayerSessionRequest {
            GameSessionId = sessionId,
                PlayerId = SystemInfo.deviceUniqueIdentifier,
        });

        var playerSession = response.PlayerSession;
        playsersessionId = playerSession.PlayerSessionId;
        var ClientudpPort = SearchAvailableUdpPort (33400, 33400 + 100);

        realTimeClient = new RealTimeClient (
            playerSession.DnsName,
            playerSession.Port,
            ClientudpPort,
            ConnectionType.RT_OVER_WS_UDP_UNSECURED,
            playerSession.PlayerSessionId,
            null);
        realTimeClient.OnDataReceivedCallback = ReceivedServerPushedDataCallback;

        DebugUI.text = DebugUI.text + "\n" + "Room Joined: playsersessionId=" + playsersessionId;
    }

    public void SendMessageToAll (string message = "test", int opcode = 1) {
        realTimeClient.SendMessage (opcode, DeliveryIntent.Fast, message);
    }

    // ------------------------------------------------------------#
    // CallBack
    // ------------------------------------------------------------#
    public void ReceivedServerPushedDataCallback (object sender, Aws.GameLift.Realtime.Event.DataReceivedEventArgs e) {

        string message = System.Text.Encoding.Default.GetString (e.Data);
        int opcode = int.Parse (e.OpCode.ToString ());
        int senderid = int.Parse (e.Sender.ToString ());

        UnityEngine.Debug.Log ($"[server-sent] OnDataReceived - Sender: {e.Sender} OpCode: " + opcode + " Data: " + message);

        switch (opcode) {
            case (int) MessageCode.PLAYER_POSITION_UPDATE_RESPONSE:
                if (playerpeerId != -1) {
                    if (senderid != playerpeerId) {
                        if (GameMgr._GameMgr.otherplayerlist.IndexOf (senderid) != -1) {
                            GameMgr._GameMgr.UpdateOtherUserPosition (senderid, message);
                        }
                    }
                }
                break;

            case (int) MessageCode.PLAYER_JOIN_RESPONSE:
                if (message == playsersessionId) {
                    playerpeerId = senderid;
                    GameMgr._GameMgr.changePlayerColor (senderid);
                    GameMgr._GameMgr.AddOtherPlayerName (true,message,senderid);
                    GameMgr._GameMgr.AddUserNameToObject(true,senderid,message);
                    SendMessageToAll (GameMgr._GameMgr.userName, (int) MessageCode.PLAYER_JOIN_FINISHED);
                }

                DebugUI.text = DebugUI.text + "\n" + "PLAYER_JOIN_RESPONSE " + senderid;
                break;


            case (int) MessageCode.PLAYER_NAME_RESPONSE:
                if (playerpeerId != -1) {
                    if (senderid != playerpeerId) {
                        if (GameMgr._GameMgr.otherplayerlist.IndexOf (senderid) == -1) {
                            GameMgr._GameMgr.AddOtherUser (senderid);
                        }
                        if (!GameMgr._GameMgr.otherplayerNameList.ContainsKey (senderid) ) {
                            GameMgr._GameMgr.AddOtherPlayerName (false,message, senderid);
                            GameMgr._GameMgr.AddUserNameToObject(false,senderid,message);
                        }
                    }
                }
                DebugUI.text = DebugUI.text + "\n" + "PLAYER_NAME_RESPONSE " + senderid +" "+ message;
                break;
            
            case (int) MessageCode.START_GAME_RESPONSE:
                DebugUI.text = DebugUI.text + "\n" + "GetStartGameResponse " + senderid +" "+ message;
                GameMgr._GameMgr.GetStartGameResponse();
                break;
            case (int) MessageCode.END_GAME_RESPONSE:
                DebugUI.text = DebugUI.text + "\n" + "GetStartGameResponse " + senderid +" "+ message;
                GameMgr._GameMgr.GetEndGameResponse();
                break;
            
        }
    }

    // ------------------------------------------------------------#
    // Util
    // ------------------------------------------------------------#
    int SearchAvailableUdpPort (int from = 1024, int to = ushort.MaxValue) {
        from = Mathf.Clamp (from, 1, ushort.MaxValue);
        to = Mathf.Clamp (to, 1, ushort.MaxValue);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        var set = LsofUdpPorts (from, to);
#else
        var set = GetActiveUdpPorts ();
#endif
        for (int port = from; port <= to; port++)
            if (!set.Contains (port))
                return port;
        return -1;
    }

    HashSet<int> LsofUdpPorts (int from, int to) {
        var set = new HashSet<int> ();
        string command = string.Join (" | ",
            $"lsof -nP -iUDP:{from.ToString()}-{to.ToString()}",
            "sed -E 's/->[0-9.:]+$//g'",
            @"grep -Eo '\d+$'");
        var process = Process.Start (new ProcessStartInfo {
            FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
        });
        if (process != null) {
            process.WaitForExit ();
            var stream = process.StandardOutput;
            while (!stream.EndOfStream)
                if (int.TryParse (stream.ReadLine (), out int port))
                    set.Add (port);
        }
        return set;
    }

    HashSet<int> GetActiveUdpPorts () {
        return new HashSet<int> (IPGlobalProperties.GetIPGlobalProperties ()
            .GetActiveUdpListeners ().Select (listener => listener.Port));
    }
}