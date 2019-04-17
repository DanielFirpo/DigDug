using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

    [SerializeField]
    private AudioClip music;

    [SerializeField]
    private AudioClip death;

    private AudioSource source;

    // Use this for initialization
    private void Start () {
        source = GetComponent<AudioSource>();
        source.PlayOneShot(music);
	}

    internal void PlayDeath() {
        source.Pause();
        source.PlayOneShot(death);
    }

    internal void PlayMusic() {
        source.UnPause();
    }

    internal void PauseMusic() {
        source.Pause();
    }

}
