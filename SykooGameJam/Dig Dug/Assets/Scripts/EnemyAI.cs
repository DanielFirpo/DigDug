using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour {

    /// <summary>
    ///idle = walk until hitting wall, then turn around. Each wall hit has a chance to trigger ghosting
    ///ghost = turn to ghost form and travel to the nearest tunnel, favoring ones in the direction of the player
    ///chase = find the closest pastPlayerLoc to the player and move to it
    ///Attack = only if Fygar, stops for a moment to breath fire
    ///flee = if rock is falling, and our y gridline is less than the rock's y gridline, and on the same x gridline... flee. speed*fleeSpeedMultiplier and run whichever way is further from the rock.
    /// </summary>
    private enum Goal { Idle, Ghost, Chase, Attack, Flee }

    private Goal currentGoal = Goal.Idle;

    private readonly Vector2 up = new Vector2(0, 1000);
    private readonly Vector2 down = new Vector2(0, -1000);
    private readonly Vector2 left = new Vector2(1000, 0);
    private readonly Vector2 right = new Vector2(-1000, 0);

    [SerializeField]
    private Vector2 travelDirection;

    [SerializeField]
    private float speed;

    [SerializeField]
    private float fleeSpeedMultiplier;

    private Vector2 travelTarget;//The target we must reach before finding another (Easiest way to restrict movement to gridlines, shouldn't cut corners either if PlayerController digFreq. is high enough)

    private Vector2 TravelTarget {
        get { return travelTarget; }
        set { travelTarget = value; progressingTowardsTarget = true;  }
    }

    private bool progressingTowardsTarget = false;

    private float idleTime;//How long we've been idling, need this cause we don't want to ghost too soon, we want to idle for a bit before ghosting

    private float lastDirectionDistance;//The distance between the last walkable position and the travelDirection, used to determine when to change directions

    private LevelManager levelManager;
    private PlayerController playerController;

    //Need this cause if we didn't move since we last changed direction, we're going to think we're not making progress so change directions again (causing a loop of direction changes and never moving anywhere)
    //private bool hasMovedSinceLastDirectionChange = true;

    // Use this for initialization
    void Start () {

        idleTime = Time.time;

        levelManager = FindObjectOfType<LevelManager>();
        playerController = FindObjectOfType<PlayerController>();

    }//TODO: 1. Not new random, new direction with walkable positions. DONE 2. Don't let player dig already dug positions. DONE 3. Restrict enemy movement along gridlines.


	// Update is called once per frame
	void Update () {

        if (currentGoal == Goal.Idle) {

            //PrintDirection(travelDirection);

            List<Vector2> walkablePositions = FindWalkablePositions();
            Vector2 bestPostion = new Vector2(900000, 900000);//some crazy far away best position so this will never be the best

            if (walkablePositions.Count > 0) {

                if (!progressingTowardsTarget) {//If we're not already moving towards a target, lets find a new one.

                    foreach (Vector2 pos in walkablePositions) {
                        if (DistanceToDirection(pos, travelDirection) < DistanceToDirection(bestPostion, travelDirection)) {//found new best walkable position, which is closest to the direction we want to be traveling in
                            bestPostion = pos;
                        }
                    }

                    if (Vector2.Distance(bestPostion, travelDirection) > lastDirectionDistance) {//We're starting to have to move farther away from our desired direction, lets travel a different direction so we don't get stuck in a loop walking farther and closer over and over

                        if (Time.time - idleTime > 10) {//if we've been idling for more than 10 seconds
                            int rand = Random.Range(1, 5);//25% chance to ghost upon hitting a wall
                            if (rand == 1) {
                                Debug.Log(rand + " == 1, GHOSTING");
                                TravelTarget = new Vector2(playerController.transform.position.x, playerController.transform.position.y);
                                currentGoal = Goal.Ghost;
                                return;
                            }
                        }

                        lastDirectionDistance = 0;//We need to clear this value because we're now working with a new direction, which we could be farther away from, and we'll try to go back if so

                        travelDirection = FindNewOptimalDirection();

                    }

                    lastDirectionDistance = Vector2.Distance(bestPostion, travelDirection);

                    TravelTarget = bestPostion;

                }

            }
            else {//somehow we got stuck (probably will never happen) lets ghost out
                Debug.Log("No walkable positions");
                currentGoal = Goal.Ghost;
                return;
            }

            MoveTowardsTarget();

        }
        else if (currentGoal == Goal.Ghost) {

            MoveTowardsTarget();
            Debug.Log("Moving towards travelTarget: " + TravelTarget);

            if (!progressingTowardsTarget) {
                Debug.Log("WE MADE IT! EXPERT GHOSTERS. Now lets chase.");
                currentGoal = Goal.Chase;
                return;
            }

        }
        else if (currentGoal == Goal.Chase) {

        }
        else if (currentGoal == Goal.Attack) {

        }
        else if (currentGoal == Goal.Flee) {

        }
        else {

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

    private Vector2 FindNewOptimalDirection() {//find any direction that has walkable positions
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

                int randIndex = Random.Range(0, possibleDirections.Count);
                Debug.Log("RAND INDEX: " + randIndex);
                if (possibleDirections.Count >= 1) {
                    return possibleDirections[randIndex];//choose one direction randomly to avoid AI behaviour loop
                }

            }
        }
        Debug.LogError("Could not find a walkable direction. This should never happen, so something went wrong. Defaulting to UP");
        return up;
    }

    private void MoveTowardsTarget() {

        this.transform.position = Vector3.MoveTowards(transform.position, new Vector3(TravelTarget.x, TravelTarget.y, transform.position.z), speed*Time.deltaTime);

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

    private Vector2 ClosestToPlayer(List<Vector2> locs) {
        return new Vector2();
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
