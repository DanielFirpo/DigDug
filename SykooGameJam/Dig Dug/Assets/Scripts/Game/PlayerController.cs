using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController: MonoBehaviour {

    internal enum FacingDirection { Up, Down, Left, Right }

    internal FacingDirection CurrentlyFacing { get; private set; }

    [SerializeField]
    private float speed = 1;

    private Vector3 lastDigLoc;

    [SerializeField, Tooltip("How frequently a \"dig\" is done while the player is walking, in distance, not time.")]
    private float digFrequency = .2f;

    private LevelManager levelManager;
    private Weapon weapon;

    internal Animator animator { get; private set; }

    [SerializeField]
    private bool shouldDie;//The animator wants us to die now

    private bool hasDied;

    [SerializeField]
    private bool isDying;//manipulated by animator, true while death animation is playing

    private float lastDigTime;

    public Vector3 StartPosition { get; private set; }//could just be a constant Vector2 but this is cooler
    public bool Paused { get; internal set; }

    private GameManager gameManager;

    //Need this variable to keep track of whether or not the player was previously off and moving towards a gridline, so that 
    //when they finally get to the gridline we can immediately spawn a dig sprite otherwise the tunnel corner looks shitty 
    private bool wasOffDesiredGridline;

    void Start() {
        CurrentlyFacing = FacingDirection.Down;
        levelManager = FindObjectOfType<LevelManager>();
        weapon = FindObjectOfType<Weapon>();
        animator = GetComponent<Animator>();
        StartPosition = transform.position;
        gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update () {

        if (Paused) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            animator.Rebind();
            animator.SetTrigger("AttackStart");
            weapon.Attack();
        }

        if (weapon.attackInProgress) {//if we're attacking, don't move
            return;
        }

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

    public void Dying() {//called by animator. Pause at start of death animation
        gameManager.SetPaused(true);
    }

    public void Died() {//called by animator. unpause and kill the player now that the death animation has ended
        gameManager.SetPaused(false);
        gameManager.OnDeath();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Enemy")) {
            animator.SetTrigger("Die");
            //FindObjectOfType<GameManager>().OnDeath();//TODO: GET IN STARRT TO LAZY RN
        }
    }

    private void Dig(float frequencyDist) {

        if (Vector3.Distance(this.transform.position, lastDigLoc) >= frequencyDist) {
            if (!levelManager.IsAlreadyDug(transform.position)) {

                levelManager.DoDigAt(transform.position);
                lastDigLoc = this.transform.position;


                animator.SetTrigger("Dig");

            }
        }

    }

    private void MoveUp() {
        if (transform.position.y < 0) {
            this.transform.Translate(0, speed * Time.deltaTime, 0);
            animator.SetTrigger("Run");
            Dig(digFrequency);
        }
    }

    private void MoveDown() {
        if (transform.position.y > (levelManager.YGridlines - 1) * -levelManager.GridlineSpacing) {
            this.transform.Translate(0, -speed * Time.deltaTime, 0);
            animator.SetTrigger("Run");
            Dig(digFrequency);
        }
    }

    private void MoveRight() {
        if (transform.position.x > (levelManager.XGridlines - 1) * -levelManager.GridlineSpacing) {
            this.transform.Translate(-speed * Time.deltaTime, 0, 0);
            animator.SetTrigger("Run");
            Dig(digFrequency);
        }
    }

    private void MoveLeft() {
        if (transform.position.x < 0) {
            this.transform.Translate(speed * Time.deltaTime, 0, 0);
            animator.SetTrigger("Run");
            Dig(digFrequency);
        }
    }

    //internal Vector3 FacingDirectionToEuler() {
       // if (CurrentlyFacing == FacingDirection.Up) {
            
        //}
    //}

        

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

    internal Vector2 DirectionsTowardsNearestGridlines(Vector3 position) {

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
