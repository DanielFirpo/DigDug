using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    [SerializeField]
    private GameObject readyDialog;

    [SerializeField]
    private GameObject gameOverDialog;

    [SerializeField]
    private GameObject playerDialogHeader;

    [SerializeField]
    private Text playerDialogNumber;

    [SerializeField]
    private GameObject statsSidebar;

    [SerializeField]
    private Text sidebarPlayerOneScore;

    [SerializeField]
    private Text sidebarPlayerTwoScore;

    [SerializeField]
    private Text sidebarLevelNumber;

    [SerializeField]
    private Text sidebarHighScore;

    [SerializeField]
    private GameObject sidebarPlayerOneLivesParent;

    [SerializeField]
    private GameObject sidebarPlayerTwoLivesParent;

    [SerializeField]
    private GameObject[] sidebarPlayerOneLives;

    [SerializeField]
    private GameObject[] sidebarPlayerTwoLives;

    private Queue dialogQueue;//Sometimes we want to trigger more than one dialog, but we don't want them to play at the same time so we have this queue.

    internal enum GameMode { OnePlayer, TwoPlayer }

    private GameMode currentGameMode;

    private int playerOneLives = 3;

    private int playerTwoLives = 3;

    private bool playerOneTurn = true;//false = player two's turn, true = player one

    private bool gameInProgress = false;

    private object currentDialogInProgress;

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

    internal bool HasGameEnded {
        get {
            return playerOneLives <= 0 && currentGameMode == GameMode.OnePlayer || playerTwoLives <= 0 && playerOneLives <= 0;
        }
    }


    private void Awake() {
        if (!created) {
            DontDestroyOnLoad(this.gameObject);
            created = true;
        }

        else {
            Destroy(this.gameObject);
        }
    }

    private void Start() {
        dialogQueue = new Queue();
    }

    private void Update() {

        if (gameInProgress) {//statsSidebar

            UpdateSidebar();

        }
        else {
            statsSidebar.gameObject.SetActive(false);
        }

        if (currentDialogInProgress == null && dialogQueue.Count >= 1) {//play next dialog

            currentDialogInProgress = dialogQueue.Dequeue();

            if (currentDialogInProgress is ReadyDialog) {
                ReadyDialog dialog = currentDialogInProgress as ReadyDialog;
                dialog.DoDialog();
            }
            else {
                GameOverDialog dialog = currentDialogInProgress as GameOverDialog;
                dialog.DoDialog();
            }
        }
    }

    private void UpdateSidebar() {

        statsSidebar.SetActive(true);

        sidebarHighScore.text = PlayerStats.HighScore.ToString();
        sidebarLevelNumber.text = levelCount.ToString();
        sidebarPlayerOneScore.text = PlayerStats.CurrentScoreP1.ToString();
        sidebarPlayerOneScore.text = PlayerStats.CurrentScoreP2.ToString();

        if (playerOneTurn) {

            sidebarPlayerTwoLivesParent.SetActive(false);
            sidebarPlayerOneLivesParent.SetActive(true);

            switch (playerOneLives) {

                case 0:
                    sidebarPlayerOneLives[0].SetActive(false);
                    sidebarPlayerOneLives[1].SetActive(false);
                    sidebarPlayerOneLives[2].SetActive(false);
                    break;
                case 1:
                    sidebarPlayerOneLives[0].SetActive(true);
                    sidebarPlayerOneLives[1].SetActive(false);
                    sidebarPlayerOneLives[2].SetActive(false);
                    break;
                case 2:
                    sidebarPlayerOneLives[0].SetActive(true);
                    sidebarPlayerOneLives[1].SetActive(true);
                    sidebarPlayerOneLives[2].SetActive(false);
                    break;
                        case 3:
                    sidebarPlayerOneLives[0].SetActive(true);
                    sidebarPlayerOneLives[1].SetActive(true);
                    sidebarPlayerOneLives[2].SetActive(true);
                    break;
            }
        }
        else {
            sidebarPlayerOneLivesParent.SetActive(false);
            sidebarPlayerTwoLivesParent.SetActive(true);

            switch (playerTwoLives) {

                case 0:
                    sidebarPlayerTwoLives[0].SetActive(false);
                    sidebarPlayerTwoLives[1].SetActive(false);
                    sidebarPlayerTwoLives[2].SetActive(false);
                    break;
                case 1:
                    sidebarPlayerTwoLives[0].SetActive(true);
                    sidebarPlayerTwoLives[1].SetActive(false);
                    sidebarPlayerTwoLives[2].SetActive(false);
                    break;
                case 2:
                    sidebarPlayerTwoLives[0].SetActive(true);
                    sidebarPlayerTwoLives[1].SetActive(true);
                    sidebarPlayerTwoLives[2].SetActive(false);
                    break;
                case 3:
                    sidebarPlayerTwoLives[0].SetActive(true);
                    sidebarPlayerTwoLives[1].SetActive(true);
                    sidebarPlayerTwoLives[2].SetActive(true);
                    break;
            }

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

        if (playerOneLives <= 0 || playerTwoLives <= 0) {
            currentDialogInProgress = new GameOverDialog(4f, this);//There shouldn't be a dialog playing already, so do an immediate game over dialog. I would dialogQueue.enqueue here but the turn changes and by the time it's dequeued it will say the wrong player has game overed... This queue system could use some polish tbh
            GameOverDialog dialog = currentDialogInProgress as GameOverDialog;//TODO change dialogs to actual classes with inheritance to avoid this bs (interface prob)
            dialog.DoDialog();
        }

        if (currentGameMode == GameMode.OnePlayer) {
            playerOneTurn = true;
        }
        else {

            if (playerOneTurn) {
                if (playerTwoLives >= 1) {
                    PlayerStats.CurrentLevelP2 = levelCount;
                    playerOneTurn = false;
                }
            }
            else {
                if (playerOneLives >= 1) {
                    PlayerStats.CurrentLevelP1 = levelCount;
                    playerOneTurn = true;
                }
            }
        }

        if (!HasGameEnded) {
            dialogQueue.Enqueue(new ReadyDialog(3f, this));
        }

    }

    internal void SetPaused(bool paused) {
        foreach (EnemyBehaviour enemy in FindObjectsOfType<EnemyBehaviour>()) {
            enemy.Paused = paused;
        }
        FindObjectOfType<PlayerController>().Paused = paused;
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

    private void ReverseGameOverDialog() {
        SetGameOverDialog(false);
        SetPaused(false);
        currentDialogInProgress = null;

        if (HasGameEnded) {
            EndGame();
        }
    }

    private void ReverseReadyDialog() {
        SetReadyDialog(false);
        SetPaused(false);
        currentDialogInProgress = null;
    }

    private void GameOver() {//Do game over stuff before ending game
        dialogQueue.Enqueue(new GameOverDialog(3f, this));
    }

    private void EndGame() {

        SetPaused(false);
        gameInProgress = false;
        sceneCounter = 0;
        SceneManager.LoadScene(sceneCounter);//menu
        playerOneLives = 3;
        playerTwoLives = 3;

    }

    private void SetReadyDialog(bool enabled) {

        int currentPlayerNumber;

        if (playerOneTurn)
            currentPlayerNumber = 1;
        else
            currentPlayerNumber = 2;

        playerDialogNumber.text = currentPlayerNumber.ToString();
        playerDialogHeader.SetActive(enabled);
        readyDialog.SetActive(enabled);
    }

    private void SetGameOverDialog(bool enabled) {

        int currentPlayerNumber;

        if (playerOneTurn)
            currentPlayerNumber = 1;
        else
            currentPlayerNumber = 2;

        playerDialogNumber.text = currentPlayerNumber.ToString();
        playerDialogHeader.SetActive(enabled);
        gameOverDialog.SetActive(enabled);

    }

    private class ReadyDialog {

        private float displayDuration;
        private GameManager gameManager;

        public ReadyDialog(float displayDuration, GameManager gameManager) {
            this.displayDuration = displayDuration;
            this.gameManager = gameManager;
        }

        internal void DoDialog() {
            gameManager.SetPaused(true);
            gameManager.SetReadyDialog(true);
            gameManager.Invoke(nameof(ReverseReadyDialog), displayDuration);
        }

    }

    private class GameOverDialog {

        private float displayDuration;
        private GameManager gameManager;

        public GameOverDialog(float displayDuration, GameManager gameManager) {
            this.displayDuration = displayDuration;
            this.gameManager = gameManager;
        }

        internal void DoDialog() {
            gameManager.SetPaused(true);
            gameManager.SetGameOverDialog(true);
            gameManager.Invoke(nameof(ReverseGameOverDialog), displayDuration);
        }
    }
}
