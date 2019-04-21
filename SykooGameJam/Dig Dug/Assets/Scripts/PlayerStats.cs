
using UnityEngine;

public static class PlayerStats {

    private static readonly string highScoreKey = "highscore";
    private static readonly string highLevelKey = "highlevel";

    private static bool recordStatsLoaded = false;

    internal static readonly int PointsPerDig = 1;
    internal static readonly int PointsPerKill = 500;
    internal static readonly int PointsPerSquash = 1000;

    private static int highScore;
    private static int highLevel;

    internal static int HighScore {
        get {
            if (recordStatsLoaded) {
                return highScore;
            }
            else {
                LoadRecordStats();
                return highScore;
            }
        }
        private set {
            highScore = value;
            PlayerPrefs.SetInt(highScoreKey, value);
        }
    }

    internal static int HighLevel {
        get {
            if (recordStatsLoaded) {
                return highLevel;
            }
            else {
                LoadRecordStats();
                return highLevel;
            }
        }
        private set {
            highLevel = value;
            Debug.Log("Our new high level is " + value);
            PlayerPrefs.SetInt(highLevelKey, value);
        }
    }

    private static int currentScoreP1;

    internal static int CurrentScoreP1 {
        get { return currentScoreP1; }
        set { currentScoreP1 = value; if (currentScoreP1 > HighScore) HighScore = currentScoreP1; }
    }

    private static int currentLevel;

    internal static int CurrentLevel {
        get { return currentLevel; }
        set { currentLevel = value; if (currentLevel > HighLevel) HighLevel = currentLevel; }
    }

    private static int currentScoreP2;

    internal static int CurrentScoreP2 {
        get { return currentScoreP2; }
        set { currentScoreP2 = value; if (currentScoreP2 > HighScore) HighScore = currentScoreP2; }
    }

    internal static void LoadRecordStats() {

        HighScore = PlayerPrefs.GetInt(highScoreKey);
        HighLevel = PlayerPrefs.GetInt(highLevelKey);

        recordStatsLoaded = true;

    }

    internal static void ClearRecentStats() {//NOT highscores!

        CurrentLevel = 0;

        CurrentScoreP1 = 0;
        CurrentScoreP2 = 0;

    }

}
