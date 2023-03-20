using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Clip
{
    public string name;
    public AudioClip[] clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource audioSource;
    public List<Clip> clips = new List<Clip>();

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        Actions.OnRecognition += OnRecognitionEvent;
    }

    private void OnDisable()
    {
        Actions.OnRecognition -= OnRecognitionEvent;
    }

    public void Play(string name)
    {
        Clip clip = clips.Find(x => x.name == name);
        if (clip != null)
        {
            audioSource.clip = clip.clip[Random.Range(0, clip.clip.Length)];
            audioSource.Play();
        }
    }

    private void OnRecognitionEvent(PatternDictionary patternDictionary)
    {
        Play("Swish");
    }
}
