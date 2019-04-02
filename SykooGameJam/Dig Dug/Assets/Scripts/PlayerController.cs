using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField]
    private float speed = 1;

    private Vector3 lastDigLoc;

    [SerializeField, Tooltip("How frequently a \"dig\" is done while the player is walking, in distance, not time.")]
    private float digFrequency = .2f;

    [SerializeField]
    private float digFrequencyWhileTurning = .2f;

    private LevelManager levelManager;

    //Need this variable to keep track of whether or not the player was previously off and moving towards a gridline, so that 
    //when they finally get to the gridline we can immediately spawn a dig sprite otherwise the tunnel corner looks shitty 
    private bool wasOffDesiredGridline;

    void Start () {
        levelManager = FindObjectOfType<LevelManager>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        if (Input.GetKey(KeyCode.UpArrow)) {
            if (IsOnXGridline(transform.position.x)) {
                MoveUp();
                return;
            }
            else {//Not on gridline, can't move in this direction, we need to detour
                MoveTowardsClosestXGridline();
                return;
            }
        }

        if (Input.GetKey(KeyCode.RightArrow)) {
            if (IsOnYGridline(transform.position.y)) {
                MoveRight();
                return;
            }
            else {
                MoveTowardsClosestYGridline();
                return;
            }
        }

        if (Input.GetKey(KeyCode.DownArrow)) {
            if (IsOnXGridline(transform.position.x)) {
                MoveDown();
                return;
            }
            else {//Not on gridline, can't move in this direction, we need to detour
                MoveTowardsClosestXGridline();
                return;
            }
        }

        if (Input.GetKey(KeyCode.LeftArrow)) {
            if (IsOnYGridline(transform.position.y)) {
                MoveLeft();
                return;
            }
            else {
                MoveTowardsClosestYGridline();
                return;
            }
        }

    }

    private void Dig(float frequencyDist) {

        if (Vector3.Distance(this.transform.position, lastDigLoc) >= frequencyDist) {
            if (!levelManager.IsAlreadyDug(transform.position)) {

                levelManager.DoDigAt(transform.position);
                lastDigLoc = this.transform.position;

            }
        }

    }

    private void MoveUp() {
        this.transform.Translate(0, speed * Time.fixedDeltaTime, 0);
        Dig(digFrequency);
    }

    private void MoveDown() {
        this.transform.Translate(0, -speed * Time.fixedDeltaTime, 0);
        Dig(digFrequency);
    }

    private void MoveRight() {
        this.transform.Translate(-speed * Time.fixedDeltaTime, 0, 0);
        Dig(digFrequency);
    }

    private void MoveLeft() {
        this.transform.Translate(speed * Time.fixedDeltaTime, 0, 0);
        Dig(digFrequency);
    }

    private void MoveTowardsClosestXGridline() {

        wasOffDesiredGridline = true;

        if (DirectionsTowardsNearestGridlines(this.transform.position).x > 0) {//Closest gridline is to the right
            MoveRight();
        }
        else {//to the left
            MoveLeft();
        }
    }

    private void MoveTowardsClosestYGridline() {

        wasOffDesiredGridline = true;

        if (DirectionsTowardsNearestGridlines(this.transform.position).y > 0) {//Closest gridline is upwards sdasdasd
            MoveUp();
        }
        else {
            MoveDown();
        }
    }

    private bool IsOnYGridline(float y) {//Is the player on the appropriate Y value (+-.01f) to start moving on X? The player can only move up and down on certain grid lines
        for (float i = 0; i > levelManager.YGridlines * -levelManager.GridlineSpacing; i -= levelManager.GridlineSpacing) {
            if (Mathf.Abs(y - i) <= .06f) {//some room for error

                if (wasOffDesiredGridline) {
                    Dig(0);//Do a dig immediately
                    Debug.Log("doing corner dig");
                }

                wasOffDesiredGridline = false;

                return true;
            }
        }
        return false;
    }

    private bool IsOnXGridline(float x) {//Is the player on the appropriate X value (+-.01f) to start moving on Y? The player can only move up and down on certain grid lines

        for (float i = 0; i > levelManager.XGridlines * -levelManager.GridlineSpacing; i -= levelManager.GridlineSpacing) {
            if (Mathf.Abs(x - i) <= .06f) {//some room for error

                if (wasOffDesiredGridline) {
                    Dig(0);//Do a dig immediately
                    Debug.Log("doing corner dig");
                }

                wasOffDesiredGridline = false;

                return true;
            }
        }
        return false;
    }

    public Vector2 DirectionsTowardsNearestGridlines(Vector3 position) {

        Vector2 directions = new Vector2();

        Vector2 nearestGridlines = levelManager.GetNearestGridlines(position);

        if (position.x - nearestGridlines.x < 0) {
            directions.x = -1;//direction towards nearest x is up
        }
        else {
            directions.x = 1;//direction towards nearest x is down
        }

        if (position.y - nearestGridlines.y < 0) {
            directions.y = 1;//direction towards nearest y is right
        }
        else {
            directions.y = -1;//direction towards nearest y is left
        }

        return directions;

    }

}
