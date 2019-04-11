using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    internal enum GameMode { OnePlayer, TwoPlayer }

    private GameMode currentGameMode;

    private int playerOneLives = 3;

    private int playerTwoLives = 3;

    private bool playerOneTurn = true;//false = player two's turn, true = player one

    private bool gameInProgress = false;

    private int levelCount;

    public int LevelCount {
        get { return levelCount; }
        set {
            levelCount = value;
            if (playerOneLives >= 1) PlayerStats.CurrentLevelP1 = value;//if player one is still alive, update their stats
            if (playerTwoLives >= 1) PlayerStats.CurrentLevelP2 = value;
        }
    }

    private int sceneCounter;//Need to use a seperate variable than levelCount to load scene cause after level 12 levels 8 though 12 really just loop

    private static bool created;

    private void Awake() {
        if (!created) {
            DontDestroyOnLoad(this.gameObject);
            created = true;
        }

        else {
            Destroy(this.gameObject);
        }
    }

    internal void OnDeath() {//called by PlayerControllers

        if (playerOneTurn) {
            playerOneLives--;
        }
        else {
            playerTwoLives--;
        }

        NextTurn();

    }

    private void NextTurn() {

        foreach (EnemyBehaviour enemy in FindObjectsOfType<EnemyBehaviour>()) {
            enemy.transform.position = enemy.StartPosition;
        }
        PlayerController player = FindObjectOfType<PlayerController>();
        player.transform.position = player.StartPosition;

        if (currentGameMode == GameMode.OnePlayer) {
            playerOneTurn = true;
        }
        else {

            if (playerOneTurn) {
                if (playerTwoLives >= 1) {
                    playerOneTurn = false;
                }
            }
            else {
                if (playerOneLives >= 1) {
                    playerOneTurn = true;
                }
            }
        }
    }

    internal void Pause() {
        foreach (EnemyBehaviour enemy in FindObjectsOfType<EnemyBehaviour>()) {
            enemy.Paused = true;
        }
        FindObjectOfType<PlayerController>().Paused = true;
    }

    internal void UnPause() {
        foreach (EnemyBehaviour enemy in FindObjectsOfType<EnemyBehaviour>()) {
            enemy.Paused = false;
        }
        FindObjectOfType<PlayerController>().Paused = false;
    }

    private void Update() {
        if (playerOneLives <= 0 && currentGameMode == GameMode.OnePlayer || playerTwoLives <= 0 && playerOneLives <= 0) {
            GameOver();
        }
    }

    internal void NewGame(GameMode gameMode) {

        PlayerStats.ClearRecentStats();

        if (!gameInProgress) {

            currentGameMode = gameMode;
            gameInProgress = true;
            NextLevel();

        }

    }

    private void NextLevel() {

        levelCount++;
        sceneCounter++;

        if (sceneCounter > 12) {//If we're on the last level
            sceneCounter = 8;//Loop back around to the 8th
        }

        SceneManager.LoadScene(sceneCounter);

    }

    private void GameOver() {//Trigger by animation? So player can finish death animation b4 being recenterered along with enemies

        gameInProgress = false;
        sceneCounter = 0;
        SceneManager.LoadScene(sceneCounter);//menu
        playerOneLives = 3;
        playerTwoLives = 3;

    }
}
