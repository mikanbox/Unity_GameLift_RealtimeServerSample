using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MoveCharacter : MonoBehaviour {

    public Tilemap tilemap;
    private MoveCharacterAnimation charaAnimationMgr;
    public bool isplayer = false;
    private Vector3 pastpos;

    void Start() {
        pastpos = this.transform.position;
        charaAnimationMgr = this.transform.GetChild(0).GetComponent<MoveCharacterAnimation>();
    }

    void Update() {
        Vector3 PlayerWorldPos = this.transform.position;

        float inputx = 0;
        float inputy = 0;
        if (isplayer) {
            (inputx, inputy) = InputMoveDirectionFromKey();
        } else {
            (inputx, inputy) = (this.transform.position.x - pastpos.x, this.transform.position.y - pastpos.y);
            pastpos = this.transform.position;
        }



        Vector3 PlayerWorldPosNext = PlayerWorldPos + new Vector3(inputx * 0.03f, inputy * 0.03f);
        if (inputx != 0) {
            charaAnimationMgr.TurnCharacter(inputx > 0);
        }
        if (inputy != 0 || inputx != 0) charaAnimationMgr.JumpCharacter();


        
        if (isplayer) {
            if (IsNextTileMovable(PlayerWorldPosNext)) {
                this.transform.position = PlayerWorldPosNext;
            }
        }
    }


    private (float, float) InputMoveDirectionFromKey() {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        return (x, y);
    }

    private bool IsNextTileMovable(Vector3 clickPosition) {
        Vector3Int PlayerMapPosNext = tilemap.WorldToCell(clickPosition); // ワールド座標からセル座標を取得
        return tilemap.GetColliderType(PlayerMapPosNext) != Tile.ColliderType.None;
    }


}


