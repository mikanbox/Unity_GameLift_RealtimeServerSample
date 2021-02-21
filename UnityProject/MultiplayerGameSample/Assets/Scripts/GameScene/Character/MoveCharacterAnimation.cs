using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCharacterAnimation : MonoBehaviour {

    public float direction = 0;
    private int directionisRight = 0;
    private int directionisLeft = 180;
    private bool isdirectionright = true;
    private float maxTurnTime = 0.5f;


    private int leftjumptime = 0;
    private int maxJumpTime =6;


    void FixedUpdate() {
        if (isdirectionright==true && direction>0){
            direction-=15;
            if (direction <=0)direction=0;
            this.transform.localRotation = Quaternion.Euler(0,direction,0);
        }else if (isdirectionright==false && direction < 180){
            direction+=15;
            if (direction>=180)direction=180;
            this.transform.localRotation = Quaternion.Euler(0,direction,0);
        }

        if (leftjumptime>0){
            if (leftjumptime > maxJumpTime /2){
                this.transform.position += new Vector3(0,0.05f,0);
            }else{
                this.transform.position += new Vector3(0,-0.05f,0);
            }
            leftjumptime--;
        }

    }

    public void TurnCharacter(bool isright){
        if (isright != isdirectionright){
            isdirectionright = isright;
        }
    }

    public void JumpCharacter(){
        if (leftjumptime==0){
            leftjumptime = maxJumpTime;
        }
    }


}
