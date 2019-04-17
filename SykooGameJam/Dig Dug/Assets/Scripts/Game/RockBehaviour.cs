using System;
using System.Collections.Generic;
using UnityEngine;

public class RockBehaviour : MonoBehaviour {

    [SerializeField]
    private float fallSpeed;

    [SerializeField]
    private float fallDelay;

    [SerializeField, Tooltip("How long after hitting a tunnel floor the rock will disapear")]
    private float destroyDelay;

    [SerializeField, Tooltip("How close something needs to be to the rock to be considered directly under it.")]
    private float triggerRange;

    private GameManager gameManager;

    internal float TriggerRange {
        get { return triggerRange; }
        private set { triggerRange = value; }
    }

    private bool isFalling = false;

    private float fallStartTime;

    private LevelManager levelManager;

    private PlayerController playerController;

    private EnemyBehaviour[] enemies;

    // Use this for initialization
    void Start () {
        gameManager = FindObjectOfType<GameManager>();
        enemies = FindObjectsOfType<EnemyBehaviour>();
        levelManager = FindObjectOfType<LevelManager>();
        playerController = FindObjectOfType<PlayerController>();
    }
	
	void Update () {//TODO: Rock accelleration and fix fluctuating falling speed

        if (isFalling) {

            if (Time.time - fallStartTime > fallDelay) {
                foreach (EnemyBehaviour enemy in enemies) {
                    if (enemy) {//Make sure that the enemy hasn't been destroyed since the start of the scene (we only get enemies once, in Start())

                        if (!enemy.currentlyFallingRocks.Contains(this)) {
                            enemy.currentlyFallingRocks.Add(this);
                        }

                        if (IsBelow(enemy.transform.position, TriggerRange / 2)) {//OOF
                            enemy.Squash();
                            enemy.transform.parent = this.transform;
                            if (gameManager.PlayerOneTurn) {
                                PlayerStats.CurrentScoreP1 += PlayerStats.PointsPerSquash;
                            }
                            else {
                                PlayerStats.CurrentScoreP2 += PlayerStats.PointsPerSquash;
                            }
                        }

                    }
                }

                bool noneBelow = true;

                foreach (Vector2 pos in levelManager.dugPositions) {
                    if (IsBelow(pos, TriggerRange)) {
                        noneBelow = false;  
                    }
                }

                if (IsBelow(playerController.transform.position, TriggerRange / 2)) {
                    playerController.StartDeath();
                    noneBelow = true;//hit the player, lets stop falling or the rock will continue to fall and kill enemies while they're paused
                }

                if (!noneBelow) {
                    this.transform.Translate(0, -(fallSpeed * Time.deltaTime), 0);
                }
                else {

                    foreach (EnemyBehaviour enemy in enemies) {
                        enemy.currentlyFallingRocks.Remove(this);
                    }

                    isFalling = false;
                    Destroy(GetComponent<BoxCollider2D>());
                    Destroy(this.gameObject, destroyDelay);

                }
            }
        }
        else {

            foreach (Vector2 pos in levelManager.dugPositions) {
                if (IsBelow(pos, TriggerRange)) {
                    if (playerController.CurrentlyFacing != PlayerController.FacingDirection.Up) {//If the player isn't staring upwards at the rock
                        isFalling = true;
                        fallStartTime = Time.time;
                        DoWiggleEffect();
                    }
                }
            }

        }
	}

    private void DoWiggleEffect() {
        //throw new NotImplementedException();
    }

    private void DoShatterEffect() {
        //throw new NotImplementedException();
    }

    internal bool IsBelow(Vector2 position, float range) {//Is this point below this rock (and closer than range)?
        if (position.y < transform.position.y && Mathf.Abs(position.y - transform.position.y) <= range && Mathf.Abs(position.x - transform.position.x) <= range) {//if we're indirectly under this rock (+- range)
            return true;
        }
        return false;
    }
}
