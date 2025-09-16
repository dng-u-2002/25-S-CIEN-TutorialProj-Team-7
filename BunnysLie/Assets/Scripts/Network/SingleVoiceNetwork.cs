using Photon.Voice.PUN;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleVoiceNetwork : MonoBehaviour
{
    void Awake()
    {
        var dups = FindObjectsOfType<PunVoiceClient>();
        if (dups.Length > 1) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

}
