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
    private bool progressingTowardsTarget = false;

    private float lastDirectionDistance;//The distance between the last walkable position and the travelDirection, used to determine when to change directions

    private LevelManager levelManager;

    //Need this cause if we didn't move since we last changed direction, we're going to think we're not making progress so change directions again (causing a loop of direction changes and never moving anywhere)
    //private bool hasMovedSinceLastDirectionChange = true;

    // Use this for initialization
    void Start () {

        levelManager = FindObjectOfType<LevelManager>();

    }//TODO: 1. Not new random, new direction with walkable positions. DONE 2. Don't let player dig already dug positions. DONE 3. Restrict enemy movement along gridlines.


	// Update is called once per frame
	void Update () {

        if (currentGoal == Goal.Idle) {

            //PrintDirection(travelDirection);

            List<Vector2> walkablePositions = FindWalkablePositions();
            Vector2 bestPostion = new Vector2(900000, 900000);//some crazy far away best position so this will never be the best

            if (walkablePositions.Count > 0) {

                if (!progressingTowardsTarget) {//If we're not already moving towards a target, lets find a new one.

                    progressingTowardsTarget = true;

                    foreach (Vector2 pos in walkablePositions) {
                        if (DistanceToDirection(pos, travelDirection) < DistanceToDirection(bestPostion, travelDirection)) {//found new best walkable position, which is closest to the direction we want to be traveling in
                            bestPostion = pos;
                        }
                    }

                    if (Vector2.Distance(bestPostion, travelDirection) > lastDirectionDistance) {//We're starting to have to move farther away from our desired direction, lets travel a different direction so we don't get stuck in a loop walking farther and closer over and over

                        lastDirectionDistance = 0;//We need to clear this value because we're now working with a new direction, which we could be farther away from, and we'll try to go back if so

                        travelDirection = FindNewOptimalDirection();

                    }

                    lastDirectionDistance = Vector2.Distance(bestPostion, travelDirection);

                    travelTarget = bestPostion;

                }

            }
            else {//somehow we got stuck (probably will never happen) lets ghost out
                Debug.Log("No walkable positions");
                currentGoal = Goal.Ghost;
                return;
            }

            MoveTowards(travelTarget);

            if (Vector2.Distance(transform.position, travelTarget) < .05f) {//if we've reached our travelTarget
                progressingTowardsTarget = false;
            }

        }
        else if (currentGoal == Goal.Ghost) {

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

    private float DistanceToDirection(Vector2 pos, Vector2 direction) {

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

                int randIndex = Random.Range(0, possibleDirections.Count - 1);
                Debug.Log("RAND INDEX: " + randIndex);
                if (possibleDirections.Count >= 1) {
                    return possibleDirections[randIndex];//choose one direction randomly to avoid AI behaviour loop
                }

            }
        }
        Debug.LogError("Could not find a walkable direction. This should never happen, so something went wrong. Defaulting to UP");
        return up;
    }

    private void MoveTowards(Vector2 pos) {

        this.transform.position = Vector3.MoveTowards(transform.position, new Vector3(pos.x, pos.y, transform.position.z), speed*Time.deltaTime);
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
