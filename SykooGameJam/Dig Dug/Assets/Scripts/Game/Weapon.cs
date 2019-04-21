using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour {

    private Animator attackAnimator;

    private float timeAtLastAttack;

    [SerializeField]
    private float attackDuration;

    internal bool attackInProgress { get; private set; }

    private PlayerController playerController;

    private EnemyBehaviour target;

    [SerializeField]
    private float attackCooldown;

    internal bool casting { get; private set; }

    [SerializeField]
    private float attackSpeed;

    private LevelManager levelManager;

    [SerializeField]
    private Transform attackTrigger;

    [SerializeField]
    private SpriteRenderer weaponVisuals;//The weapon's visuals

    [SerializeField]
    private SpriteRenderer hitRenderer;//current sprite being shown by the animator

    // Use this for initialization
    void Start () {
        attackAnimator = GetComponent<Animator>();
        playerController = FindObjectOfType<PlayerController>();
        levelManager = FindObjectOfType<LevelManager>();
    }
	
	// Update is called once per frame
	void Update () {

        if (casting) {

            attackTrigger.position = (attackTrigger.transform.position + (this.transform.forward * attackSpeed * Time.deltaTime));
            bool hasHitWall = true;

            foreach (Vector2 pos in levelManager.dugPositions) {
                if (levelManager.IsConnected(attackTrigger.transform.position, pos, levelManager.DisconnectedThreshold/1.5f)) {
                    hasHitWall = false;
                }
            }

            if (hasHitWall) {
                casting = false;
                attackAnimator.SetTrigger("StopAttack");
                attackTrigger.transform.position = transform.position;//reset the attack trigger to be cast again
                attackInProgress = false;
            }

        }

        if (target != null) {
            playerController.animator.SetTrigger("Inflate");
        }

        if (Time.time - timeAtLastAttack > attackDuration) {
            StopAttack();
        }

	}

    internal void StopAttack() {
        attackInProgress = false;
        target = null;
        attackTrigger.transform.position = transform.position;
        hitRenderer.sprite = null;
        casting = false;
    }

    internal void Attack() {

        if (Time.time - timeAtLastAttack > attackCooldown) {

            timeAtLastAttack = Time.time;

            if (!attackInProgress) {
                attackInProgress = true;
                target = null;
                BeginTriggerCast();
            }
            else {
                if (target != null) {
                    target.Inflation += 1;
                    attackInProgress = true;
                    playerController.animator.SetTrigger("Inflate");
                }
            }
        }
    }

    private void BeginTriggerCast() {//Sends the weapon out in the direction the player is facing

        casting = true;
        attackAnimator.ResetTrigger("StopAttack");//Have to reset StopAttack for some reason, I guess there's a bug where triggers get stuck on
        attackAnimator.SetTrigger("StartAttack");

        Debug.Log("Casting");

    }

    private void OnTriggerEnter2D(Collider2D other) {//PlayAttack animation sends out a trigger to represent the weapon, lets see if we hit any enemies
        if (other.CompareTag("Enemy")) {
            if (target == null) {
                target = other.GetComponent<EnemyBehaviour>();
                if (target == null) {
                    target = other.transform.parent.GetComponent<EnemyBehaviour>();
                }
                target.Inflation += 1;
                hitRenderer.sprite = weaponVisuals.sprite;
                hitRenderer.transform.position = weaponVisuals.transform.position;//Mimic the attack animation except in a frozen state
                attackAnimator.SetTrigger("StopAttack");
                casting = false;
            }
        }
    }

}
