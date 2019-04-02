using System.Collections;
using System.Timers;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class StopPlayingOnRecompile {

    private static Timer waitForCompilation = new Timer();
    private static Timer restartPlaymodeLater = new Timer();

    static StopPlayingOnRecompile() {

        waitForCompilation.Elapsed += new ElapsedEventHandler(CheckForCompletion);
        waitForCompilation.Interval = 100;

        restartPlaymodeLater.Elapsed += new ElapsedEventHandler(StartPlaymodeLater);
        restartPlaymodeLater.Interval = 1000;

        //Since InitializeOnLoad is called when unity starts AND every time you hit play, we will unsubscribe and resubscribe to avoid duplicates.
        //Might not be needed to do since EditorApplication.update might be cleared on every InitializeOnLoad call?
        EditorApplication.update -= StopPlayingIfRecompiling;
        EditorApplication.update += StopPlayingIfRecompiling;
    }

    static void StopPlayingIfRecompiling() {
        if (EditorApplication.isCompiling && EditorApplication.isPlaying) {
            EditorApplication.isPlaying = false;

            waitForCompilation.Enabled = true;
        }
    }

    static void StartPlaymodeLater(object source, ElapsedEventArgs e) {
        Debug.Log("Restarting playmode");
        EditorApplication.isPlaying = true;
        restartPlaymodeLater.Enabled = false;
    }

    private static void CheckForCompletion(object source, ElapsedEventArgs e) {

        Debug.Log("Timer working.");

        if (!EditorApplication.isCompiling) {
            restartPlaymodeLater.Enabled = true;
            waitForCompilation.Enabled = false;
        }
    }
}