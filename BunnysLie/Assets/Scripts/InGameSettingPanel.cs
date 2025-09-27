using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.Unity.Demos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class InGameSettingPanel : MonoBehaviour
{
    [SerializeField] Button SettingButton;
    [SerializeField] Button SettingXButton;

    [SerializeField] RectTransform Container;
    bool _IsOn;
    public bool IsOn
    {
        get { return _IsOn; }
        set { _IsOn = value; 
                Container.gameObject.SetActive(_IsOn);
        }
    }
    [SerializeField] AudioMixer Mixer;
    [SerializeField] Sprite NormalSpeaker;
    [SerializeField] Sprite MutedSpeaker;
    [SerializeField] Sprite NormalHeadset;
    [SerializeField] Sprite MutedHeadset;
    [SerializeField] Sprite NormalChat;
    [SerializeField] Sprite MutedChat;
    [Header("Sound Setting Sliders/Buttons")]
    [SerializeField] Slider MasterVolumeSlider;
    [SerializeField] Slider BGMVolumeSlider;
    [SerializeField] Slider SFXVolumeSlider;
    [SerializeField] Slider VoiceChatVolumeSlider;

    [SerializeField] Button MasterVolumeActiveButton; bool Flag_MVAB;
    bool IsActive_MasterVolume
    {
        get
        {
            return Flag_MVAB;
        }

        set
        {
            Flag_MVAB = value;
            if (Flag_MVAB == true)
            {
                MasterVolumeActiveButton.image.sprite = NormalSpeaker;
            }
            else
            {
                MasterVolumeActiveButton.image.sprite = MutedSpeaker;
            }
        }
    }
    [SerializeField] Button BGMVolumeActiveButton; bool Flag_BVAB;
    bool IsActive_BGMVolume
    {
        get
        {
            return Flag_BVAB;
        }

        set
        {
            Flag_BVAB = value;
            if (Flag_BVAB == true)
            {
                BGMVolumeActiveButton.image.sprite = NormalSpeaker;
            }
            else
            {
                BGMVolumeActiveButton.image.sprite = MutedSpeaker;
            }
        }
    }
    [SerializeField] Button SFXVolumeActiveButton; bool Flag_SVAB;
    bool IsActive_SFXVolume
    {
        get
        {
            return Flag_SVAB;
        }

        set
        {
            Flag_SVAB = value;
            if (Flag_SVAB == true)
            {
                SFXVolumeActiveButton.image.sprite = NormalSpeaker;
            }
            else
            {
                SFXVolumeActiveButton.image.sprite = MutedSpeaker;
            }
        }
    }

    [SerializeField] Button VoiceChatActiveButton; bool Flag_VCAB;
    bool IsActive_VoiceChat
    {
        get
        {
            return Flag_VCAB;
        }

        set
        {
            Flag_VCAB = value;
            if (Flag_VCAB == true)
            {
                VoiceChatActiveButton.image.sprite = NormalHeadset;
                //FindObjectOfType<Recorder>().TransmitEnabled = true;
                //Mixer.SetFloat("VoiceChatVolume", 10.0f);
            }
            else
            {
                VoiceChatActiveButton.image.sprite = MutedHeadset;
                //FindObjectOfType<Recorder>().TransmitEnabled = false;
                //Mixer.SetFloat("VoiceChatVolume", -80.0f);
            }
        }
    }
    [SerializeField] Button TextChatActiveButton; bool Flag_TCAB;
    bool IsActive_TextChat
    {
        get
        {
            return Flag_TCAB;
        }

        set
        {
            Flag_TCAB = value;
            if (Flag_TCAB == true)
            {
                TextChatActiveButton.image.sprite = NormalChat;
            }
            else
            {
                TextChatActiveButton.image.sprite = MutedChat;
            }
        }
    }

    [SerializeField] Button LeaveGameButton;
    [SerializeField] TMPro.TMP_Dropdown ResolutionSelector;
    [SerializeField] Toggle FullScreenToggle;

    private void Awake()
    {

        //IsActive_VoiceChat = PlayerPrefs.GetInt("IsActive_VoiceChat", 1) == 1;


        if(SettingButton != null)
        {
            SettingButton.onClick.AddListener(() =>
            {
                IsOn = !IsOn;
                InGameManager.Instance.PlayButtonClickSound();
            });
        }
        if(SettingXButton != null)
        {
            SettingXButton.onClick.AddListener(() =>
            {
                IsOn = false;
                InGameManager.Instance.PlayButtonClickSound();
            });
        }

        MasterVolumeSlider.onValueChanged.AddListener((value01) =>
        {
            value01 = Mathf.Clamp01(value01); // 범위 제한

            // 0은 -80dB(거의 무음), 1은 0dB(원본 크기)
            float dB;
            if (value01 <= 0.0001f) // 0에 가까우면 무음 처리
            {
                dB = -80f;
                IsActive_MasterVolume = false;
            }
            else
            {
                dB = Mathf.Log10(value01) * 20f + 1; // 선형 dB 변환
                IsActive_MasterVolume = true;
            }

            Mixer.SetFloat("MasterVolume", dB);
            PlayerPrefs.SetFloat("MasterVolume", value01); // 플레이어 설정 저장
            PlayerPrefs.Save();
        });
        BGMVolumeSlider.onValueChanged.AddListener((value01) =>
        {
            value01 = Mathf.Clamp01(value01); // 범위 제한

            // 0은 -80dB(거의 무음), 1은 0dB(원본 크기)
            float dB;
            if (value01 <= 0.0001f) // 0에 가까우면 무음 처리
            {
                dB = -80f;
                IsActive_BGMVolume = false;
            }
            else
            {
                dB = Mathf.Log10(value01) * 20f + 1; // 선형 dB 변환
                IsActive_BGMVolume = true;
            }

            Mixer.SetFloat("BGMVolume", dB);
            PlayerPrefs.SetFloat("BGMVolume", value01); // 플레이어 설정 저장
            PlayerPrefs.Save();
        });
        SFXVolumeSlider.onValueChanged.AddListener((value01) =>
        {
            value01 = Mathf.Clamp01(value01); // 범위 제한

            // 0은 -80dB(거의 무음), 1은 0dB(원본 크기)
            float dB;
            if (value01 <= 0.0001f) // 0에 가까우면 무음 처리
            {
                dB = -80f;
                IsActive_SFXVolume = false;
            }
            else
            {
                dB = Mathf.Log10(value01) * 20f + 1; // 선형 dB 변환
                IsActive_SFXVolume = true;
            }

            Mixer.SetFloat("SFXVolume", dB);
            PlayerPrefs.SetFloat("SFXVolume", value01); // 플레이어 설정 저장
            PlayerPrefs.Save();
        });
        VoiceChatVolumeSlider.onValueChanged.AddListener((value01) =>
        {
            value01 = Mathf.Clamp01(value01); // 범위 제한

            // 0은 -80dB(거의 무음), 1은 0dB(원본 크기)
            float dB;
            if (value01 <= 0.0001f) // 0에 가까우면 무음 처리
            {
                dB = -80f;
                IsActive_VoiceChat = false;
            }
            else
            {
                dB = Mathf.Log10(value01) * 20f + 8; // 선형 dB 변환
                IsActive_VoiceChat = true;
            }

            Mixer.SetFloat("VoiceChatVolume", dB);
            PlayerPrefs.SetFloat("VoiceChatVolume", value01); // 플레이어 설정 저장
            PlayerPrefs.Save();
        });


        MasterVolumeActiveButton.onClick.AddListener(() =>
        {
            if(IsActive_MasterVolume)
                MasterVolumeSlider.value = 0.0f;
            else
                MasterVolumeSlider.value = 0.1f;

            ButtonClickSoundPlayer.Play();
        });
        BGMVolumeActiveButton.onClick.AddListener(() =>
        {
            if (IsActive_BGMVolume)
                BGMVolumeSlider.value = 0.0f;
            else
                BGMVolumeSlider.value = 0.1f;
            ButtonClickSoundPlayer.Play();
        });
        SFXVolumeActiveButton.onClick.AddListener(() =>
        {
            if (IsActive_SFXVolume)
                SFXVolumeSlider.value = 0.0f;
            else
                SFXVolumeSlider.value = 0.1f;
            ButtonClickSoundPlayer.Play();
        });
        VoiceChatActiveButton.onClick.AddListener(() =>
        {
            if (IsActive_VoiceChat)
                VoiceChatVolumeSlider.value = 0.0f;
            else
                VoiceChatVolumeSlider.value = 0.1f;
            //PlayerPrefs.SetInt("IsActive_VoiceChat", IsActive_VoiceChat ? 1 : 0); // 플레이어 설정 저장
            //PlayerPrefs.Save();
            ButtonClickSoundPlayer.Play();
        });
        TextChatActiveButton.onClick.AddListener(() =>
        {
            IsActive_TextChat = !IsActive_TextChat;
            PlayerPrefs.SetInt("IsActive_TextChat", IsActive_TextChat ? 1 : 0); // 플레이어 설정 저장
            PlayerPrefs.Save();
            ButtonClickSoundPlayer.Play();

            FindObjectOfType<Chating>(true)?.gameObject.SetActive(IsActive_TextChat);
        });
        //int width = PlayerPrefs.GetInt("Width", 1920);
        //int height = PlayerPrefs.GetInt("Height", 1080);
        ResolutionSelector.onValueChanged.AddListener((selection) =>
        {
            var texts = ResolutionSelector.options[selection].text.Split('x');
            texts[0] = texts[0].Remove(0, texts[0].IndexOf('(') + 1);
            texts[1] = texts[1].TrimEnd(')');

            int width = int.Parse(texts[0]);
            int height = int.Parse(texts[1]);
            PlayerPrefs.SetInt("Width", width);
            PlayerPrefs.SetInt("Height", height);
            PlayerPrefs.Save();
            Screen.SetResolution(width, height, PlayerPrefs.GetInt("FullScreen", 1) == 1);
        });

        FullScreenToggle.onValueChanged.AddListener((full) =>
        {
            int width = PlayerPrefs.GetInt("Width");
            int height = PlayerPrefs.GetInt("Height");
            PlayerPrefs.SetInt("FullScreen", full ? 1 : 0);
            PlayerPrefs.Save();
            Screen.SetResolution(width, height, full);
        });

        ResolutionSelector.onValueChanged.Invoke(ResolutionSelector.options.IndexOf(ResolutionSelector.options.Find((o) => o.text.Contains(PlayerPrefs.GetInt("Width", 1920).ToString()) && o.text.Contains(PlayerPrefs.GetInt("Height", 1080).ToString()))));
        FullScreenToggle.SetValue(PlayerPrefs.GetInt("FullScreen", 1) == 1);
        Container.localPosition = new Vector3(0, 0);

        LeaveGameButton.onClick.AddListener(() =>
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("Lobby");
            ButtonClickSoundPlayer.Play();
        });
        MasterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        BGMVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        SFXVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        VoiceChatVolumeSlider.value = PlayerPrefs.GetFloat("VoiceChatVolume", 0.5f);
        IsActive_TextChat = PlayerPrefs.GetInt("IsActive_TextChat", 1) == 1;
        IsOn = false;
    }

    private void Start()
    {

    }
}
