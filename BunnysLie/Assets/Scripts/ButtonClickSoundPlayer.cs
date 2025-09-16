using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonClickSoundPlayer : MonoBehaviour
{
    AudioSource ClickSound;
    public static ButtonClickSoundPlayer Instance { get; private set; }
    public static void Play()
    {
        if (Instance != null)
        {
            Instance.ClickSound.Play();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ClickSound = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
