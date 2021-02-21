'use strict';

// The Realtime server session object
var session;
var logger;


var ACTIVE_PLAYER_NUM = 0;
var SESSION_START_TIME;
const TICK_TIME = 1000;
const MIN_TIME_UNTIL_CLOSE_SESSION = 120;


///////////////////////////////////////////////////////////////////////////////
// Initialization Tasks 
///////////////////////////////////////////////////////////////////////////////
function init(rtSession) {
    session = rtSession;
    logger = session.getLogger();
}

// A simple tick loop example
function SessionTimeOutTimer() {
    const elapsedTime = getTimeInS() - SESSION_START_TIME;
    logger.info("Tick... " + elapsedTime + " ACTIVE_PLAYER_NUM: " + ACTIVE_PLAYER_NUM);

    if ((ACTIVE_PLAYER_NUM == 0) && (elapsedTime > MIN_TIME_UNTIL_CLOSE_SESSION)) {
        CloseGameSession();
    } else {
        setTimeout(SessionTimeOutTimer, TICK_TIME);
    }
}



async function CloseGameSession() {
    const outcome = await session.processEnding();
    logger.info("All players disconnected. Completed process ending with: " + outcome);
    process.exit(0);
}

function onStartGameSession(gameSession) {
    logger.info(`[onStartGameSession]`);
    SESSION_START_TIME = getTimeInS();
    SessionTimeOutTimer();
}

function onProcessStarted(args) {
    logger.info(`[onProcessStarted]`);
    return true;
}

function onProcessTerminate() {
    logger.info(`[onProcessTerminate]`);
}






///////////////////////////////////////////////////////////////////////////////
// Group Tasks 
///////////////////////////////////////////////////////////////////////////////
function onPlayerJoinGroup(groupId, peerId) {
    return true;
}

function onPlayerLeaveGroup(groupId, peerId) {
    return true;
}

function onSendToPlayer(gameMessage) {
    return true;
}

function onSendToGroup(gameMessage) {
    logger.info(`[onSendToGroup]`);
    return true;
}


///////////////////////////////////////////////////////////////////////////////
// RoomCreation And Join Tasks 
///////////////////////////////////////////////////////////////////////////////
const MAX_ROOM_PLAYER_NUM = 6;
let PLAYERIDs = [];
var USERNAMEs = {}

function onPlayerConnect(connectMsg) {
    logger.info(`[onPlayerConnect]`);
    if (PLAYERIDs.length >= MAX_ROOM_PLAYER_NUM) {
        return false;
    }
    return true;
}


function onPlayerAccepted(player) {
    logger.info(`[onPlayerAccepted]`);

    if (PLAYERIDs.length >= MAX_ROOM_PLAYER_NUM) {
        return;
    }else{
        // player.peerId : ゲームセッション(ルーム)参加時にクライアントに割当て　 ルーム内固有 ID (1,2,3...)
        // player.playerSessionId : GameLiftAPI : CreatePlayerSession の返り値で取得した ID (形式は不定)
        PLAYERIDs.push(player.peerId);
        ACTIVE_PLAYER_NUM++;
        SendStringToAll(PLAYER_JOIN_RESPONSE, String(player.playerSessionId), player.peerId);
        
    }
}

function onPlayerDisconnect(peerId) {
    logger.info(`[onPlayerDisconnect]`);
    ACTIVE_PLAYER_NUM--;

    PLAYERIDs.some(function(v, i){
        if (v==peerId) PLAYERIDs.splice(i,1);    
    });

    if (ACTIVE_PLAYER_NUM == 0) {
        CloseGameSession();
    }
}


///////////////////////////////////////////////////////////////////////////////
// Game code
///////////////////////////////////////////////////////////////////////////////

const PLAYER_POSITION_UPDATE = 200;
const PLAYER_POSITION_UPDATE_RESPONSE = 201;

const PLAYER_JOIN =100;
const PLAYER_JOIN_RESPONSE = 101;

const PLAYER_NAME = 102;
const PLAYER_NAME_RESPONSE = 103;

const PLAYER_JOIN_FINISHED = 104;

const START_GAME = 300;
const START_GAME_RESPONSE=301;
const END_GAME = 302;
const END_GAME_RESPONSE=303;


function onMessage(gameMessage) {
    logger.info(`[onMessage] ` + gameMessage.opCode + "  payload="+gameMessage.payload);

    switch (gameMessage.opCode) {
        case PLAYER_POSITION_UPDATE: {
            SendStringToAll(PLAYER_POSITION_UPDATE_RESPONSE, gameMessage.payload, gameMessage.sender);
            break;
        }
        case PLAYER_JOIN_FINISHED: {                
            USERNAMEs[gameMessage.sender] = gameMessage.payload;
            SendStringToAll(PLAYER_NAME_RESPONSE, gameMessage.payload, gameMessage.sender);
            Object.keys(USERNAMEs).map(key => 
                SendStringToAll(PLAYER_NAME_RESPONSE, USERNAMEs[key], key)
                );


            break;
        }
        case START_GAME: {
            SendStringToAll(START_GAME_RESPONSE, gameMessage.payload, gameMessage.sender);
            break;
        }
        case END_GAME: {
            SendStringToAll(END_GAME_RESPONSE, gameMessage.payload, gameMessage.sender);
            break;
        }
    }
}




///////////////////////////////////////////////////////////////////////////////
// Game code
///////////////////////////////////////////////////////////////////////////////


// Return true if the process is healthy
function onHealthCheck() {
    return true;
}

exports.ssExports = {
    init: init,
    onProcessStarted: onProcessStarted,
    onStartGameSession: onStartGameSession,
    onProcessTerminate: onProcessTerminate,
    onPlayerConnect: onPlayerConnect,
    onPlayerAccepted: onPlayerAccepted,
    onPlayerDisconnect: onPlayerDisconnect,
    onPlayerJoinGroup: onPlayerJoinGroup,
    onPlayerLeaveGroup: onPlayerLeaveGroup,
    onSendToPlayer: onSendToPlayer,
    onSendToGroup: onSendToGroup,
    onMessage: onMessage,
    onHealthCheck: onHealthCheck
};


///////////////////////////////////////////////////////////////////////////////
// Utility functions
///////////////////////////////////////////////////////////////////////////////

// Calculates the current time in seconds
function getTimeInS() {
    return Math.round(new Date().getTime() / 1000);
}

function SendStringToAll(opCode, stringToSend, senderid) {
    let gameMessage = session.newTextGameMessage(opCode, senderid, stringToSend);

    session.getLogger().info("[app] SendStringToAll: DefaultGroupId " + session.getAllPlayersGroupId());
    session.sendGroupMessage(gameMessage, session.getAllPlayersGroupId());
}



function SendStringToClients(opCode, stringToSend, targetUserIds) {
    session.getLogger().info("[app] SendStringToClient: targetUserIds = " + targetUserIds.toString() + " opCode = " + opCode + " stringToSend = " + stringToSend);

    let gameMessage = session.newTextGameMessage(opCode, session.getServerId(), stringToSend);

    let targetUserIdArrayLen = targetUserIds.length;
    for (let index = 0; index < targetUserIdArrayLen; ++index) {
        session.getLogger().info("[app] SendStringToClient: sendMessageT " + gameMessage.toString() + " " + targetUserIds[index].toString());
        session.sendMessage(gameMessage, targetUserIds[index]);
    };

}

