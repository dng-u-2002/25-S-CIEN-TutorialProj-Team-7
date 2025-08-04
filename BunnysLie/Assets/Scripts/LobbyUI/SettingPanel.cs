using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    bool _IsViewing = false;
    bool IsViewing
    {
        get
        {
            return _IsViewing;
        }
        set
        {
            _IsViewing = false;
            Panel.gameObject.SetActive(value);
        }
    }

    [SerializeField] Button SettingButton;

    Transform NowSelectedButton;
    Vector3 OriginalButtonScale;
    [SerializeField] float SelectedButtonScaleFactor;

    [SerializeField] Button SoundSettingButton;
    [SerializeField] Button FriendButton;
    [SerializeField] Button HelperButton;

    [SerializeField] RectTransform SoundSettingView;
    [SerializeField] RectTransform FriendView;
    [SerializeField] RectTransform HelperView;

    [Header("Sound Setting Sliders/Buttons")]
    [SerializeField] Slider MasterVolumeSlider;
    [SerializeField] Slider BGMVolumeSlider;
    [SerializeField] Slider SFXVolumeSlider;

    [SerializeField] Button MasterVolumeActiveButton; bool Flag_MVAB;
    [SerializeField] Button BGMVolumeActiveButton; bool Flag_BVAB;
    [SerializeField] Button SFXVolumeActiveButton; bool Flag_SVAB;

    [SerializeField] Button VoiceChatActiveButton; bool Flag_VCAB;
    [SerializeField] Button TextChatActiveButton; bool Flag_TCAB;

    void ResetNowButton2NormalScale()
    {
        if (NowSelectedButton == null)
        {
            HelperButton.transform.localScale = OriginalButtonScale;
            SoundSettingButton.transform.localScale = OriginalButtonScale;
            FriendButton.transform.localScale = OriginalButtonScale;
            return;
        }
        NowSelectedButton.localScale = OriginalButtonScale;
    }

    void DisableAllViews()
    {
        SoundSettingView.gameObject.SetActive(false);
        HelperView.gameObject.SetActive(false);
        FriendView.gameObject.SetActive(false);
    }


    public void OnButtonClicked_SoundSetting()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = SoundSettingButton.transform;
        SoundSettingButton.transform.localScale = OriginalButtonScale * SelectedButtonScaleFactor;
        SoundSettingView.gameObject.SetActive(true);
    }

    public void OnButtonClicked_Friend()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = FriendButton.transform;
        FriendButton.transform.localScale = OriginalButtonScale * SelectedButtonScaleFactor;
        FriendView.gameObject.SetActive(true);
    }

    public void OnButtonClicked_Helper()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = HelperButton.transform;
        HelperButton.transform.localScale = OriginalButtonScale * SelectedButtonScaleFactor;
        HelperView.gameObject.SetActive(true);
    }

    private void Start()
    {
        IsViewing = false;

        OriginalButtonScale = SoundSettingButton.transform.localScale;

        SettingButton.onClick.AddListener(() =>
        {
            OnSettingButtonClicked();
        });


        SoundSettingButton.onClick.AddListener(() =>
        {
            OnButtonClicked_SoundSetting();
        });
        FriendButton.onClick.AddListener(() =>
        {
            OnButtonClicked_Friend();
        });
        HelperButton.onClick.AddListener(() =>
        {
            OnButtonClicked_Helper();
        });

        /*
         *     [SerializeField] Button MasterVolumeActiveButton; bool Flag_MVAB;
    [SerializeField] Button BGMVolumeActiveButton; bool Flag_BVAB;
    [SerializeField] Button SFXVolumeActiveButton; bool Flag_SVAB;

    [SerializeField] Button VoiceChatActiveButton; bool Flag_VCAB;
    [SerializeField] Button TextChatActiveButton; bool Flag_TCAB;
         */

        MasterVolumeActiveButton.onClick.AddListener(() =>
        {
            Flag_MVAB = !Flag_MVAB;
            if(Flag_MVAB)
            {
                MasterVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "On";
            }
            else
            {
                MasterVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "Off";
            }
        });
        BGMVolumeActiveButton.onClick.AddListener(() =>
        {
            Flag_BVAB = !Flag_BVAB;
            if (Flag_BVAB)
            {
                BGMVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "On";
            }
            else
            {
                BGMVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "Off";
            }
        });
        SFXVolumeActiveButton.onClick.AddListener(() =>
        {
            Flag_SVAB = !Flag_SVAB;
            if (Flag_SVAB)
            {
                SFXVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "On";
            }
            else
            {
                SFXVolumeActiveButton.GetComponentInChildren<TMP_Text>().text = "Off";
            }
        });
        VoiceChatActiveButton.onClick.AddListener(() =>
        {
            Flag_VCAB = !Flag_VCAB;
            if (Flag_VCAB)
            {
                VoiceChatActiveButton.GetComponentInChildren<TMP_Text>().text = "On";
            }
            else
            {
                VoiceChatActiveButton.GetComponentInChildren<TMP_Text>().text = "Off";
            }
        });
        TextChatActiveButton.onClick.AddListener(() =>
        {
            Flag_TCAB = !Flag_TCAB;
            if (Flag_TCAB)
            {
                TextChatActiveButton.GetComponentInChildren<TMP_Text>().text = "On";
            }
            else
            {
                TextChatActiveButton.GetComponentInChildren<TMP_Text>().text = "Off";
            }
        });

        OnButtonClicked_SoundSetting();
    }

    [SerializeField] RectTransform Panel;
    public void OnSettingButtonClicked()
    {
        IsViewing = !IsViewing;


        if(IsViewing)
        {

        }
        else
        {

        }
    }
}
