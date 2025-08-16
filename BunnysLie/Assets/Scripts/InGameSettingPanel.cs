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
    bool IsOn
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
            }
            else
            {
                VoiceChatActiveButton.image.sprite = MutedHeadset;
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

    private void Awake()
    {
        SettingButton.onClick.AddListener(() =>
        {
            IsOn = !IsOn;
            InGameManager.Instance.PlayButtonClickSound();
        });
        SettingXButton.onClick.AddListener(() =>
        {
            IsOn = false;
            InGameManager.Instance.PlayButtonClickSound();
        });

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
        });


        MasterVolumeActiveButton.onClick.AddListener(() =>
        {
            if(IsActive_MasterVolume)
                MasterVolumeSlider.value = 0.0f;
            else
                MasterVolumeSlider.value = 0.1f;

            InGameManager.Instance.PlayButtonClickSound();
        });
        BGMVolumeActiveButton.onClick.AddListener(() =>
        {
            if (IsActive_BGMVolume)
                BGMVolumeSlider.value = 0.0f;
            else
                BGMVolumeSlider.value = 0.1f;
            InGameManager.Instance.PlayButtonClickSound();
        });
        SFXVolumeActiveButton.onClick.AddListener(() =>
        {
            if (IsActive_SFXVolume)
                SFXVolumeSlider.value = 0.0f;
            else
                SFXVolumeSlider.value = 0.1f;
            InGameManager.Instance.PlayButtonClickSound();
        });
        VoiceChatActiveButton.onClick.AddListener(() =>
        {
            IsActive_VoiceChat = !IsActive_VoiceChat;
            InGameManager.Instance.PlayButtonClickSound();
        });
        TextChatActiveButton.onClick.AddListener(() =>
        {
            IsActive_TextChat = !IsActive_TextChat;
            InGameManager.Instance.PlayButtonClickSound();
        });



        MasterVolumeSlider.value = 1.0f;
        BGMVolumeSlider.value = 0.5f;
        SFXVolumeSlider.value = 0.5f;
        Container.localPosition = new Vector3(0, 0);
        IsOn = false;
    }
}
