using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour {


    [SerializeField]
    private int yGridlines;

    internal int YGridlines {
        get { return yGridlines; }
        private set { yGridlines = value; }
    }

    [SerializeField]
    private int xGridlines;

    internal int XGridlines {
        get { return xGridlines; }
        private set { xGridlines = value; }
    }

    [SerializeField]
    private float gridlineSpacing;

    internal float GridlineSpacing {
        get { return gridlineSpacing; }
        private set { gridlineSpacing = value; }
    }


    [SerializeField]
    private GameObject gridDebug;

    [SerializeField]
    private Transform preDigParent;

    [SerializeField]
    private Transform debugGridlineParent;

    internal Transform DebugGridlineParent {
        get { return debugGridlineParent; }
        private set { debugGridlineParent = value; }
    }

    [SerializeField]
    private Transform DigMaskParent;

    [SerializeField]
    private GameObject DigMaskPrefab;

    [SerializeField, Tooltip("How close a position has to be to another for it to be considered connected / walkable")]
    private float disconnectedThreshold;

    internal float DisconnectedThreshold {
        get { return disconnectedThreshold; }
        private set { disconnectedThreshold = value; }
    }


    internal List<Vector2> dugPositions { get; private set; }//necessary for AI pathfinding

    // Use this for initialization
    void Start () {
        dugPositions = new List<Vector2>();
        PlaceGridDebugMarkers();

        foreach (Transform preDig in preDigParent) {//Foreach child of preDigParent

            Vector2 nearestGridlines = GetNearestGridlines(preDig.position);

            DoDigAt(new Vector2(preDig.position.x, nearestGridlines.y));

            Destroy(preDig.gameObject);

        }
    }
	
    internal void DoDigAt(Vector2 position) {//Unsafe, could dig off grid
        if (position.y < -GridlineSpacing/8) {//If we're above ground, count it as dug but don't display a digMask 

            Instantiate(DigMaskPrefab, new Vector3(position.x, position.y, 1.5f), Quaternion.identity, DigMaskParent);
        }
        Instantiate(this.gridDebug, new Vector3(position.x, position.y, 10), Quaternion.identity).GetComponent<SpriteRenderer>().material.color = Color.red;
        dugPositions.Add(position);
    }

    internal bool IsAlreadyDug(Vector2 pos) {//if closer than digFrequency-x, we're probably re-digging
        foreach (Vector2 dig in dugPositions) {
            if (Vector2.Distance(dig, pos) < .5) {//TODO: Change hardcoded .5
                return true;
            }
        }
        return false;
    }

    internal bool IsConnected(Vector2 pos1, Vector2 pos2) {

        //Debug.Log(Vector2.Distance(pos1, pos2) + " < " + disconnectedThreshold);

        if (Vector2.Distance(pos1, pos2) < DisconnectedThreshold) {
            return true;
        }

        return false;

    }

    private void PlaceGridDebugMarkers() {

        Debug.Log("xGridlines: " + XGridlines + " yGridlines: " + YGridlines);

        for (float x = 0;x > XGridlines * -gridlineSpacing;x -= gridlineSpacing) {
            for (float y = 0;y > YGridlines * -gridlineSpacing;y -= gridlineSpacing) {
                GameObject newGridDebug = Instantiate(gridDebug, DebugGridlineParent);
                newGridDebug.transform.position = new Vector3(x, y, 2);
            }
        }
    }

    internal Vector2 GetNearestGridlines(Vector3 position) {

        Vector2 nearestGridlines = new Vector2(1000, 1000);

        for (float x = 0;x > XGridlines * -gridlineSpacing;x -= gridlineSpacing) {
            if (Mathf.Abs(x - position.x) < Mathf.Abs(nearestGridlines.x - position.x)) {//if the this gridline is closer to our position than the last one we found
                nearestGridlines.x = x;
            }
        }

        for (float y = 0;y > YGridlines * -gridlineSpacing;y -= gridlineSpacing) {
            if (Mathf.Abs(y - position.y) < Mathf.Abs(nearestGridlines.y - position.y)) {//if the this gridline is closer to our position than the last one we found
                nearestGridlines.y = y;
            }
        }

        //Debug.Log("Nearest gridline: " + nearestGridlines);

        return nearestGridlines;

    }
}
