using System;
using System.Collections.Generic;
using UnityEngine;

public class RockBehaviour : MonoBehaviour {

    [SerializeField]
    private float fallSpeed;

    [SerializeField]
    private float fallDelay;

    [SerializeField, Tooltip("How close something needs to be to the rock to be considered directly under it.")]
    private float triggerRange;

    internal float TriggerRange {
        get { return triggerRange; }
        private set { triggerRange = value; }
    }


    private bool isFalling = false;
    private float fallStartTime;
    private LevelManager levelManager;
    private PlayerController playerController;
    private EnemyBehaviour[] enemies;
    private List<EnemyBehaviour> squashedEnemies;//A list of squashed enemies, we need this so we know who to kill once the rock shatters

    // Use this for initialization
    void Start () {
        enemies = FindObjectsOfType<EnemyBehaviour>();
        squashedEnemies = new List<EnemyBehaviour>();
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
                            squashedEnemies.Add(enemy);
                        }

                    }
                }

                bool noneBelow = true;

                foreach (Vector2 pos in levelManager.dugPositions) {
                    if (IsBelow(pos, TriggerRange)) {
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y - (fallSpeed * Time.deltaTime), this.transform.position.z);
                        noneBelow = false;
                    }
                }

                if (noneBelow) {

                    foreach (EnemyBehaviour enemy in enemies) {
                        enemy.currentlyFallingRocks.Remove(this);
                    }

                    foreach (EnemyBehaviour enemy in squashedEnemies) {
                        enemy.Die(2);
                    }

                    DoShatterEffect();
                    isFalling = false;

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
