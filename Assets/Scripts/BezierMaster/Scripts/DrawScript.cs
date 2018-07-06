using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrawScript : NetworkBehaviour {

	int countTouch = 0;
	Vector3 prevPosition = Vector3.zero;
	Vector3 touchPoint = Vector3.zero;

	float coneDistance = 1.5f;

	//keep a list of curves that the player drew
	public List<GameObject> curvesDrew;

	void Start () {
		curvesDrew = new List<GameObject>();
	}
	
	// Update is called once per frame
	void Update () {
		
		//return because it's not the player's object
		if(!hasAuthority){
			return;
		}

        countTouch += 1;
        if (countTouch % 3 != 0)
            return;

        Ray raycast = Camera.main.ScreenPointToRay(Input.mousePosition);

        prevPosition = touchPoint;

        //inverse transform point transforms the coordinates to local instead of global
        touchPoint = transform.InverseTransformPoint(raycast.GetPoint(coneDistance));


        if (Vector3.Distance(prevPosition, touchPoint) > 0.05f)
        {
            //let server generate points
            //CmdUpdatePoints(touchPoint);


        }
    }
}
