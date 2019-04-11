
public static class PlayerStats {


    internal static int HighScore { get; private set; }
    internal static int HighLevel { get; private set; }

    private static int currentScoreP1;

    internal static int CurrentScoreP1 {
        get { return currentScoreP1; }
        set { currentScoreP1 = value; if (currentScoreP1 > HighScore) HighScore = currentScoreP1; }
    }

    private static int currentLevelP1;

    internal static int CurrentLevelP1 {
        get { return currentLevelP1; }
        set { currentLevelP1 = value; if (currentLevelP1 > HighLevel) HighLevel = currentLevelP1; }
    }

    private static int currentScoreP2;

    internal static int CurrentScoreP2 {
        get { return currentScoreP2; }
        set { currentScoreP2 = value; if (currentScoreP2 > HighScore) HighScore = currentScoreP2; }
    }

    private static int currentLevelP2;

    internal static int CurrentLevelP2 {
        get { return currentLevelP2; }
        set { currentLevelP2 = value; if (currentLevelP2 > HighLevel) HighLevel = currentLevelP2; }
    }

    internal static void ClearRecentStats() {//NOT highscores!

        CurrentLevelP1 = 0;
        CurrentLevelP2 = 0;

        CurrentScoreP1 = 0;
        CurrentScoreP2 = 0;

    }


}
