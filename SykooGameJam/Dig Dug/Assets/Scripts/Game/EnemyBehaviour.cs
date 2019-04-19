using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyBehaviour: MonoBehaviour {

    #region InstanceVars
    [SerializeField]
    private EnemyType enemyType;

    [SerializeField]
    private Vector2 travelDirection;

    [SerializeField]
    private float speed;

    [SerializeField]
    private Vector2 randomIdleDurationRange;

    [SerializeField]
    private float ghostSpeedMultiplier;

    [SerializeField]
    private float fleeSpeedMultiplier;

    [SerializeField, Range(.1f, 8f)]
    private float fygarAttackRange;

    private float fygarAttackFrequency = 10;

    [SerializeField]
    private float fygarAttackDuration;

    [SerializeField]
    private float deflationSpeed;

    [SerializeField]
    private Sprite squashedSprite;


    /// <summary>
    ///The plan (might turn out a little different):
    ///idle = walk until hitting wall, then turn around. Each wall hit has a chance to trigger ghosting
    ///ghost = turn to ghost form and travel to the nearest tunnel, favoring ones in the direction of the player
    ///chase = find the closest pastPlayerLoc to the player and move to it
    ///Attack = only if Fygar, stops for a moment to breath fire
    ///flee = if rock is falling, and our y gridline is less than the rock's y gridline, and on the same x gridline... flee. speed*fleeSpeedMultiplier and run whichever way is further from the rock.
    /// </summary>
    internal enum Goal { Idle, Ghost, Chase, Attack, Flee }

    internal Goal CurrentGoal { get; private set; } = Goal.Idle;

    private enum FacingDirection { Left, Right }

    private FacingDirection currentlyFacing;

    private enum EnemyType { Fygar, Pooka }

    private int inflation = 0;

    internal int Inflation {
        get { return inflation; }
        set {
            timeSinceLastDeflation = Time.time;
            if (value < 0)
                inflation = 0;
            else
            inflation = value;
        }
    }
    //how many times the player has pumped up this enemy

    private readonly Vector2 upDirection = new Vector2(0, 1000);
    private readonly Vector2 downDirection = new Vector2(0, -1000);

    private readonly Vector2 leftDirection = new Vector2(1000, 0);
    private readonly Vector2 rightDirection = new Vector2(-1000, 0);

    private Vector3 fleePos;//Where we're currently running from

    private Vector2 travelTarget;//The target we must reach before finding another (Easiest way to restrict movement to gridlines, shouldn't cut corners either if PlayerController digFreq. is high enough and LevelManager DisconnectedThreshold is low enough)

    private Vector2 TravelTarget {
        get { return travelTarget; }
        set { travelTarget = value; progressingTowardsTarget = true;  }
    }

    private bool progressingTowardsTarget = false;

    private float idleTime;//How long we've been idling, need this cause we don't want to ghost too soon, we want to idle for a bit before ghosting
    private Vector2 ghostStartingPosition;//where we began ghosting, need this cause we don't want to stop ghosting after reaching the tunnel we just started ghosting from.

    private float lastDirectionDistance;//The distance between the last walkable position and the travelDirection, used to determine when to change directions

    private LevelManager levelManager;
    private PlayerController playerController;

    private Vector2 nullVector = new Vector2(Mathf.Infinity, Mathf.Infinity);//Kind of a work around here to have a default Vector2 value, like a null value.

    private List<Vector2> previousTravelTargets;//We need this to prevent back tracking

    private bool isSquashed = false;

    internal bool isDying { get; private set; } = false;

    internal List<RockBehaviour> currentlyFallingRocks { get; private set; }

    private float fleeStartTime;

    private float timeSinceLastDeflation;

    private float fygarLastAttackTime;

    private GameManager gameManager;

    public Vector3 startPosition;//Used to return enemies to their starting positions after the player dies

    public bool Paused { get; internal set; }

    private Animator animator;

    private bool attackQueued = false;

    private bool doneAttacking;

    private float idleDuration;

    private Vector2 lastPositon;//Used to determine which direction we're walking in

    #endregion

    // Use this for initialization
    void Start () {

        animator = GetComponent<Animator>();

        gameManager = FindObjectOfType<GameManager>();

        idleTime = Time.time;

        idleDuration += UnityEngine.Random.Range(randomIdleDurationRange.x, randomIdleDurationRange.y);//make sure they all don't attack at once

        Debug.Log(name + ": I'll idle for " + randomIdleDurationRange);

        fleeStartTime = Time.time;

        levelManager = FindObjectOfType<LevelManager>();
        playerController = FindObjectOfType<PlayerController>();

        currentlyFallingRocks = new List<RockBehaviour>();
        previousTravelTargets = new List<Vector2>();
        previousTravelTargets.Add(nullVector);
        previousTravelTargets.Add(nullVector);//to prevent index out of bounds errors before the list has been populated

        startPosition = this.transform.position;

    }//TODO: 1. Not new random, new direction with walkable positions. DONE 2. Don't let player dig already dug positions. DONE 3. Restrict enemy movement along gridlines. DONE

    #region Update
    // Update is called once per frame
    void Update () {

        if (Paused) {
            return;
        }

        SetFacing();

        RotateToMatchFacing();

        if (Time.time - timeSinceLastDeflation > deflationSpeed) {//slowly deflate
            Inflation -= 1;
            //Debug.Log("Deflating");
        }

        animator.SetInteger("Inflation", Inflation);//if Inflation is greater than 0 the Animator will set the appropriate inflation sprite

        if (Inflation == 4) {

            if (isDying == false) {
                if (gameManager.PlayerOneTurn) {
                    PlayerStats.CurrentScoreP1 += PlayerStats.PointsPerKill;
                }
                else {
                    PlayerStats.CurrentScoreP2 += PlayerStats.PointsPerKill;
                }

                Die(1f);
            }

        }

        if (isSquashed || Inflation > 0) {//if we're squashed or inflated freeze enemy behaviour
            return;
        }


        foreach (RockBehaviour rock in currentlyFallingRocks) {
            if (rock) {
                if (rock.transform.position.y > transform.position.y && Mathf.Abs(rock.transform.position.x - transform.position.x) < rock.TriggerRange) {
                    if (Time.time - fleeStartTime > 5) {
                        Debug.Log("Starting flee");
                        fleePos = rock.transform.position;
                        CurrentGoal = Goal.Flee;
                        fleeStartTime = Time.time;
                    }
                }
            }
        }

        if (CurrentGoal == Goal.Idle) {

            animator.SetTrigger("Run");

            //PrintDirection(travelDirection);

            List<Vector2> walkablePositions = FindWalkablePositions();
            Vector2 bestPosition = FindBestIdleTarget(walkablePositions);

            if (walkablePositions.Count > 0) {

                if (!progressingTowardsTarget) {//If we're not already moving towards a target, lets find a new one.

                    if (Vector2.Distance(bestPosition, travelDirection) > lastDirectionDistance) {//We're starting to have to move farther away from our desired direction, lets travel a different direction so we don't get stuck in a loop walking farther and closer over and over
                        if (Time.time - idleTime > idleDuration) {//if we've been idling for more than idleDuration seconds
                            Debug.Log("Since Time.time (" + Time.time + ") minus the time we started idling (" + idleTime + ") is greater than idle duration (" + randomIdleDurationRange + ") we are going to stop idling");
                            int rand = Random.Range(1, 5);//25% chance to ghost/chase upon hitting a wall
                            if (rand == 1) {
                                StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
                                fygarLastAttackTime = Time.time;
                                return;
                            }
                        }

                        lastDirectionDistance = 0;//We need to clear this value because we're now working with a new direction, which we could be farther away from, and we'll try to go back if so

                        travelDirection = FindNewIdleDirection();

                    }

                    lastDirectionDistance = Vector2.Distance(bestPosition, travelDirection);

                    TravelTarget = bestPosition;

                }

            }
            else {//somehow we got stuck (probably will never happen) lets ghost out
                Debug.Log("No walkable positions " + name);
                StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
                return;
            }

            MoveTowardsTarget();

        }
        else if (CurrentGoal == Goal.Ghost) {

            animator.ResetTrigger("Run");
            animator.SetTrigger("Ghost");

            MoveTowardsTarget();

            if (!progressingTowardsTarget || (!IsInTunnel().Equals(nullVector) && Vector2.Distance(IsInTunnel(), ghostStartingPosition) > 1.5)) {//if we've made it to the target, or we're in a tunnel that is not the tunnel where we started, chase
                TravelTarget = FindBestChaseTarget(FindWalkablePositions());
                CurrentGoal = Goal.Chase;
                return;
            }

        }
        else if (CurrentGoal == Goal.Chase) {

            animator.SetTrigger("Run");

            if (enemyType == EnemyType.Fygar) {
                if (Time.time - fygarLastAttackTime > fygarAttackFrequency) {
                    fygarLastAttackTime = Time.time;
                    fygarAttackFrequency = UnityEngine.Random.Range(5, 15);//TODO: SerializeField constants
                    CurrentGoal = Goal.Attack;
                    attackQueued = true;
                    doneAttacking = true;
                }
            }

            if (!progressingTowardsTarget) {
                TravelTarget = FindBestChaseTarget(FindWalkablePositions());
                if (TravelTarget == nullVector || Vector2.Distance(previousTravelTargets[previousTravelTargets.Count-1], playerController.transform.position) > Vector2.Distance(this.transform.position, playerController.transform.position)) {//we hit a dead end or we're starting to move further from the player, lets ghost to the player

                    StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
                    return;
                }
            }
            else if (!new Vector2(TravelTarget.x, TravelTarget.y).Equals(nullVector)){//Double check we're not moving towards infinity, cause it can happen when we come out of ghost mode and have nowhere to move to (shouldn't happen unless there's a single unconnected predig we land on after ghosting)
                MoveTowardsTarget();
            }
            else {
                StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
                return;
            }

        }
        else if (CurrentGoal == Goal.Attack) {

            if (doneAttacking) {
                Debug.Log("doneAttacking: should attack:" + attackQueued);
                if (attackQueued) {//We haven't already attacked during this goal
                    DoAttack();
                }
            }

            if (!attackQueued && doneAttacking) {
                Debug.Log("We're goddamn done");
                    CurrentGoal = Goal.Chase;
                    return;
            }
        }
        else if (CurrentGoal == Goal.Flee) {

            animator.SetTrigger("Run");

            if (Time.time - fleeStartTime < 4) {

                TravelTarget = FindNewFleeTarget();

                Debug.Log("Currently falling rocks: " + currentlyFallingRocks.Count);
                if (TravelTarget.Equals(nullVector) || currentlyFallingRocks.Count == 0) {//No where left to run, lets just chase from here on TODO: This is never the case, fix
                    Debug.Log("No where left to flee, chasing now");
                    TravelTarget = FindBestChaseTarget(FindWalkablePositions());
                    CurrentGoal = Goal.Chase;
                    return;
                }

                MoveTowardsTarget();

            }
            else {
                TravelTarget = FindBestChaseTarget(FindWalkablePositions());
                CurrentGoal = Goal.Chase;
                return;
            }
        }

	}
    #endregion

    private void SetFacing() {
        if (DistanceToDirection(lastPositon, leftDirection) >= DistanceToDirection(this.transform.position, leftDirection)) {
            currentlyFacing = FacingDirection.Left;
        }
        else {
            currentlyFacing = FacingDirection.Right;
        }
    }

    private void RotateToMatchFacing() {
        if (currentlyFacing == FacingDirection.Right) {
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            transform.localScale = new Vector3(-8, 8, 1);
        }
        else {//left
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            transform.localScale = new Vector3(8, 8, 1);
        }
    }

    private void StopAttacking() {//called by animator after the attack animation finishes
        Debug.Log("Done Attacking");
        doneAttacking = true;
    }

    private void DoAttack() {

        Debug.Log("Our goal is to ATTACK!");
        doneAttacking = false;
        animator.SetTrigger("Attack");
        attackQueued = false;

    }


    internal void ResetBehaviour() {
        transform.position = startPosition;
        idleTime = Time.time;
        CurrentGoal = Goal.Idle;
        lastDirectionDistance = 0;
        Debug.Log("Reseting behaviour");
    }

    internal void Squash() {//Rocks can call this to make the enemy appear crushed and kill them
        if (!isSquashed) {
            animator.SetTrigger("Squashed");
            isSquashed = true;
            Die(2f);
        }
    }

    private void StartGhosting(Vector2 target) {
        TravelTarget = target;
        CurrentGoal = Goal.Ghost;
        ghostStartingPosition = ClosestPosition(levelManager.dugPositions);
    }

    internal void Die(float later) {
        isDying = true;
        Destroy(this.gameObject, later);
        gameManager.OnEnemyDeath();
    }

    private Vector2 IsInTunnel() {//Checks if we're in a tunnel, and returns the tunnel's point we're closest to if so. If not, returns nullVector.
        Vector2 closestTunnelPoint = ClosestPosition(levelManager.dugPositions);
        if (Vector2.Distance(this.transform.position, closestTunnelPoint) < levelManager.DisconnectedThreshold/2) {//we're in a tunnel
            return closestTunnelPoint;
        }
        else {
            return nullVector;
        }
    }

    private bool HasReachedTarget() {
        if (Vector2.Distance(transform.position, TravelTarget) < .05f) {//if we've reached our travelTarget
            return true;
        }
        return false;
    }

    private float DistanceToDirection(Vector2 pos, Vector2 direction) {//Gets the distance from pos to direction without knowing which direction direction is

        if (direction == upDirection || direction == downDirection) {
            return Distance(pos.y, direction.y);
        }
        else {//direction is right or left
            return Distance(pos.x, direction.x);
        }

    }

    private Vector2 FindNewIdleDirection() {//find any direction that has walkable positions
        foreach (Vector2 potentialPos in levelManager.dugPositions) {
            if (levelManager.IsConnected(this.transform.position, potentialPos)) {

                List<Vector2> possibleDirections = new List<Vector2>();

                if (travelDirection != upDirection && Distance(potentialPos.y, upDirection.y) < Distance(transform.position.y, upDirection.y)) {//if... fuck idk how to explain this
                    possibleDirections.Add(upDirection);
                }
                if (travelDirection != downDirection && Distance(potentialPos.y, downDirection.y) < Distance(transform.position.y, downDirection.y)) {
                    possibleDirections.Add(downDirection);
                }
                if (travelDirection != leftDirection && Distance(potentialPos.x, leftDirection.x) < Distance(transform.position.x, leftDirection.x)) {
                    possibleDirections.Add(leftDirection);
                }
                if (travelDirection != rightDirection && Distance(potentialPos.x, rightDirection.x) < Distance(transform.position.x, rightDirection.x)) {
                    possibleDirections.Add(rightDirection);
                }

                int randIndex = UnityEngine.Random.Range(0, possibleDirections.Count);
                if (possibleDirections.Count >= 1) {
                    return possibleDirections[randIndex];//choose one direction randomly to avoid AI behaviour loop
                }

            }
        }
        Debug.LogError("Could not find a walkable direction. This should never happen, so something went wrong. Defaulting to UP");
        return upDirection;
    }

    private Vector2 FindNewFleeTarget() {//find any direction that has walkable positions

        foreach (Vector2 potentialPos in levelManager.dugPositions) {
            if (levelManager.IsConnected(this.transform.position, potentialPos)) {
                if (Vector3.Distance(potentialPos, fleePos) > Vector3.Distance(transform.position, fleePos)) {
                    if (ClosestPosition(levelManager.dugPositions) != potentialPos) {
                        return potentialPos;
                    }
                }

            }
        }

        return nullVector;

    }

    private Vector2 FindBestIdleTarget(List<Vector2> walkablePositions) {

        Vector2 bestPostion = nullVector;//nullVector will never be best since it's infinite

        foreach (Vector2 pos in walkablePositions) {
            if (DistanceToDirection(pos, travelDirection) < DistanceToDirection(bestPostion, travelDirection)) {//found new best walkable position, which is closest to the direction we want to be traveling in
                bestPostion = pos;
            }
        }

        return bestPostion;

    }

    private Vector2 FindBestChaseTarget(List<Vector2> walkablePositions) {//can return nullStruct if we are, for example, stuck in a dead end tunnel.

        Vector2 bestPostion = nullVector;//nullVector will never be best since it's infinite

        foreach (Vector2 pos in walkablePositions) {
            if (Vector2.Distance(pos, playerController.transform.position) < Vector2.Distance(bestPostion, playerController.transform.position) && pos != previousTravelTargets[previousTravelTargets.Count-2]) {//found new best walkable position, which is closest to the direction we want to be traveling in. COunt-2 = not the current but previous past position
                bestPostion = pos;
            }
        }

        previousTravelTargets.Add(bestPostion);//new previous target
        return bestPostion;

    }

    private void MoveTowardsTarget() {

        lastPositon = transform.position;

        if (CurrentGoal == Goal.Ghost) {
            this.transform.position = Vector3.MoveTowards(transform.position, new Vector3(TravelTarget.x, TravelTarget.y, transform.position.z), speed * ghostSpeedMultiplier * Time.deltaTime);
        }
        else if (CurrentGoal == Goal.Flee) {
            this.transform.position = Vector3.MoveTowards(transform.position, new Vector3(TravelTarget.x, TravelTarget.y, transform.position.z), speed * fleeSpeedMultiplier * Time.deltaTime);
        }
        else {
            this.transform.position = Vector3.MoveTowards(transform.position, new Vector3(TravelTarget.x, TravelTarget.y, transform.position.z), speed * Time.deltaTime);
        }

        if (HasReachedTarget() == true) {
            progressingTowardsTarget = false;
        }

    }

    private List<Vector2> FindWalkablePositions() {//foreach position if is connected with this.position == walkable
        List<Vector2> walkable = new List<Vector2>();
        foreach (Vector2 digPos in levelManager.dugPositions) {
            if (levelManager.IsConnected(digPos, new Vector2(transform.position.x, transform.position.y))) {
                walkable.Add(digPos);
            }
        }

        Vector2 closest = new Vector2(10000, 10000);
        foreach (Vector2 pos in walkable) {//Discard the dig pos the enemy is already standing on.
            if (Vector2.Distance(pos, transform.position) < Vector2.Distance(closest, transform.position)) {
                closest = pos;
            }
        }

        walkable.Remove(closest);

        return walkable;
    }

    private Vector2 ClosestPosition(List<Vector2> locs) {
        Vector2 closest = nullVector;
        foreach (Vector2 loc in locs) {
            if (Vector2.Distance(loc, transform.position) < Vector2.Distance(closest, this.transform.position)) {
                closest = loc;
            }
        }
        return closest;
    }

    private void PrintDirection(Vector2 dir) {
        if (dir == upDirection) {
            Debug.Log("<color=green>UP</color>");
        }
        if (dir == downDirection) {
            Debug.Log("<color=green>DOWN</color>");
        }
        if (dir == leftDirection) {
            Debug.Log("<color=green>LEFT</color>");
        }
        if (dir == rightDirection) {
            Debug.Log("<color=green>RIGHT</color>");
        }
    }

    private float Distance(float num1, float num2) {
        return Mathf.Abs(num1 - num2);
    }
}
