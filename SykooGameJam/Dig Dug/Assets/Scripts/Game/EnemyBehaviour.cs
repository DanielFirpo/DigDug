using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyBehaviour: MonoBehaviour {

    [SerializeField]
    private EnemyType enemyType;

    [SerializeField]
    private Vector2 travelDirection;

    [SerializeField]
    private float speed;

    [SerializeField]
    private float idleDuration;

    [SerializeField]
    private float fleeSpeedMultiplier;

    [SerializeField, Range(.1f, 8f)]
    private float fygarAttackRange;

    [SerializeField]
    private float fygarAttackFrequency;

    [SerializeField]
    private float fygarAttackDuration;

    [SerializeField]
    private float deflationSpeed;

    [SerializeField]
    private Sprite ghostSprite;

    [SerializeField]
    private Sprite enemySprite;

    [SerializeField]
    private Sprite inflated1;

    [SerializeField]
    private Sprite inflated2;

    [SerializeField]
    private Sprite inflated3;

    [SerializeField]
    private Sprite inflated4;

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

    private Vector3 fleePos;//Where we're currently running from
    internal Goal CurrentGoal { get; private set; } = Goal.Idle;

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

    private readonly Vector2 up = new Vector2(0, 1000);
    private readonly Vector2 down = new Vector2(0, -1000);

    private readonly Vector2 left = new Vector2(1000, 0);
    private readonly Vector2 right = new Vector2(-1000, 0);

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

    private SpriteRenderer spriteRenderer;

    private bool isSquashed = false;

    internal List<RockBehaviour> currentlyFallingRocks { get; private set; }

    private float fleeStartTime;

    private float timeSinceLastDeflation;

    private float fygarLastAttackTime;

    public Vector3 StartPosition { get; internal set; }//Used to return enemies to their starting positions after the player dies

    public bool Paused { get; internal set; }



    //Need this cause if we didn't move since we last changed direction, we're going to think we're not making progress so change directions again (causing a loop of direction changes and never moving anywhere)
    //private bool hasMovedSinceLastDirectionChange = true;

    // Use this for initialization
    void Start () {

        idleTime = Time.time;

        fleeStartTime = Time.time;

        levelManager = FindObjectOfType<LevelManager>();
        playerController = FindObjectOfType<PlayerController>();

        currentlyFallingRocks = new List<RockBehaviour>();
        previousTravelTargets = new List<Vector2>();
        previousTravelTargets.Add(nullVector);
        previousTravelTargets.Add(nullVector);//to prevent index out of bounds errors before the list has been populated

        spriteRenderer = GetComponent<SpriteRenderer>();

        StartPosition = this.transform.position;

    }//TODO: 1. Not new random, new direction with walkable positions. DONE 2. Don't let player dig already dug positions. DONE 3. Restrict enemy movement along gridlines.


	// Update is called once per frame
	void Update () {

        if (Paused) {
            return;
        }

        if (Time.time - timeSinceLastDeflation > deflationSpeed) {//slowly deflate
            Inflation -= 1;
            //Debug.Log("Deflating");
        }

        SetInflationSprite();

        if (isSquashed || Inflation > 0) {//if we're squashed or inflated freeze enemy behaviour
            return;
        }

        foreach (RockBehaviour rock in currentlyFallingRocks) {
            if (rock.transform.position.y > transform.position.y && Mathf.Abs(rock.transform.position.x - transform.position.x) < rock.TriggerRange) {
                if (Time.time - fleeStartTime > 5) {
                    Debug.Log("Starting flee");
                    fleePos = rock.transform.position;
                    CurrentGoal = Goal.Flee;
                    fleeStartTime = Time.time;
                }
            }
        }

        if (CurrentGoal == Goal.Ghost) {
            spriteRenderer.sprite = ghostSprite;
        }
        else {
            spriteRenderer.sprite = enemySprite;
        }

        if (CurrentGoal == Goal.Idle) {

            //PrintDirection(travelDirection);

            List<Vector2> walkablePositions = FindWalkablePositions();
            Vector2 bestPosition = FindBestIdleTarget(walkablePositions);

            if (walkablePositions.Count > 0) {

                if (!progressingTowardsTarget) {//If we're not already moving towards a target, lets find a new one.

                    if (Vector2.Distance(bestPosition, travelDirection) > lastDirectionDistance) {//We're starting to have to move farther away from our desired direction, lets travel a different direction so we don't get stuck in a loop walking farther and closer over and over

                        if (Time.time - idleTime > idleDuration) {//if we've been idling for more than idleDuration seconds
                            int rand = UnityEngine.Random.Range(1, 5);//25% chance to ghost/chase upon hitting a wall
                            if (rand == 1) {
                                StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
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
                Debug.Log("No walkable positions");
                StartGhosting(new Vector2(playerController.transform.position.x, playerController.transform.position.y));
                return;
            }

            MoveTowardsTarget();

        }
        else if (CurrentGoal == Goal.Ghost) {

            MoveTowardsTarget();

            if (!progressingTowardsTarget || (!IsInTunnel().Equals(nullVector) && Vector2.Distance(IsInTunnel(), ghostStartingPosition) > 1.5)) {//if we've made it to the target, or we're in a tunnel that is not the tunnel where we started, chase
                TravelTarget = FindBestChaseTarget(FindWalkablePositions());
                CurrentGoal = Goal.Chase;
                return;
            }

        }
        else if (CurrentGoal == Goal.Chase) {

            if (enemyType == EnemyType.Fygar) {
                if (Time.time - fygarLastAttackTime > fygarAttackFrequency) {
                    if (Vector3.Distance(transform.position, playerController.transform.position) < fygarAttackRange) {
                        CurrentGoal = Goal.Attack;
                        return;
                    }
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

            //Debug.Log("Attacking");
            DoAttack();

            if (Time.time - fygarLastAttackTime >= fygarAttackDuration) {//take dah chill pill mon an chess dem again
                CurrentGoal = Goal.Chase;
                return;
            }

        }
        else if (CurrentGoal == Goal.Flee) {

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

    private void SetInflationSprite() {
        switch (Inflation) {//TODO: Replace scale effect with actual inflation sprites
            case 0:
                spriteRenderer.sprite = enemySprite;
                break;
            case 1:
                spriteRenderer.sprite = inflated1;
                break;
            case 2:
                spriteRenderer.sprite = inflated2;
                break;
            case 3:
                spriteRenderer.sprite = inflated3;
                break;
            case 4:
                spriteRenderer.sprite = inflated4;
                Die(1f);
                break;
            default:
                Inflation = 4;
                break;
        }
    }

    internal void Squash() {//Rocks can call this to make the enemy appear crushed
        if (!isSquashed) {
            spriteRenderer.sprite = squashedSprite;
            isSquashed = true;
        }
    }

    private void DoAttack() {
        fygarLastAttackTime = Time.time;
    }

    private void StartGhosting(Vector2 target) {
        TravelTarget = target;
        CurrentGoal = Goal.Ghost;
        ghostStartingPosition = ClosestPosition(levelManager.dugPositions);
    }

    internal void Die(float later) {
        Destroy(this.gameObject, later);
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

        if (direction == up || direction == down) {
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

                if (travelDirection != up && Distance(potentialPos.y, up.y) < Distance(transform.position.y, up.y)) {//if... fuck idk how to explain this
                    possibleDirections.Add(up);
                }
                if (travelDirection != down && Distance(potentialPos.y, down.y) < Distance(transform.position.y, down.y)) {
                    possibleDirections.Add(down);
                }
                if (travelDirection != left && Distance(potentialPos.x, left.x) < Distance(transform.position.x, left.x)) {
                    possibleDirections.Add(left);
                }
                if (travelDirection != right && Distance(potentialPos.x, right.x) < Distance(transform.position.x, right.x)) {
                    possibleDirections.Add(right);
                }

                int randIndex = UnityEngine.Random.Range(0, possibleDirections.Count);
                if (possibleDirections.Count >= 1) {
                    return possibleDirections[randIndex];//choose one direction randomly to avoid AI behaviour loop
                }

            }
        }
        Debug.LogError("Could not find a walkable direction. This should never happen, so something went wrong. Defaulting to UP");
        return up;
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
        if (CurrentGoal == Goal.Flee) {
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
        if (dir == up) {
            Debug.Log("<color=green>UP</color>");
        }
        if (dir == down) {
            Debug.Log("<color=green>DOWN</color>");
        }
        if (dir == left) {
            Debug.Log("<color=green>LEFT</color>");
        }
        if (dir == right) {
            Debug.Log("<color=green>RIGHT</color>");
        }
    }

    private float Distance(float num1, float num2) {
        return Mathf.Abs(num1 - num2);
    }
}
