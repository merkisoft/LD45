using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundSound : MonoBehaviour {
    public AudioSource clip;

    void Start() {
        clip = GetComponent<AudioSource>();
        clip.volume = 0.01f;
    }

    void Update() {
        clip.volume = Mathf.Clamp(clip.volume + Time.deltaTime / 100f, 0, .7f);
    }
}