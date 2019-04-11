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

    // Use this for initialization
    void Start () {
        attackAnimator = GetComponent<Animator>();
        playerController = FindObjectOfType<PlayerController>();
	}
	
	// Update is called once per frame
	void Update () {

        if (target != null) {
            playerController.animator.SetTrigger("Inflate");
        }

        this.transform.rotation = playerController.transform.rotation;

        if (Time.time - timeAtLastAttack > attackDuration) {
            attackInProgress = false;
            target = null;
        }

	}

    internal void Attack() {

        if (Time.time - timeAtLastAttack > attackCooldown) {

            timeAtLastAttack = Time.time;

            if (!attackInProgress) {
                attackInProgress = true;
                target = null;
                attackAnimator.SetTrigger("StartAttack");
            }
            else {
                if (target != null) {
                    target.Inflation += 1;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {//PlayAttack animation sends out a trigger to represent the weapon, lets see if we hit any enemies
        if (other.CompareTag("Enemy")) {
            if (target == null) {
                target = other.GetComponent<EnemyBehaviour>();
                target.Inflation += 1;
            }
        }
    }

}
