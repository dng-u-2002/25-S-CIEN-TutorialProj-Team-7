using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
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
            _IsViewing = value;
            gameObject.SetActive(value); 
        }
    }

    [SerializeField] Button SettingButton;

    Transform NowSelectedButton;
    [SerializeField] float SelectedButtonScaleFactor;

    [SerializeField] Button SoundSettingButton;
    [SerializeField] Button CreditButton;
    [SerializeField] Button QuestButton;
    [SerializeField] Button AccountButton;
    [SerializeField] Button HotKeyButton;
    [SerializeField] Button FriendButton;

    [SerializeField] RectTransform SoundSettingView;
    [SerializeField] RectTransform CreditView;
    [SerializeField] RectTransform QuestView;
    [SerializeField] RectTransform AccountView;
    [SerializeField] RectTransform HotKeyView;
    [SerializeField] RectTransform FriendView;

    void ResetNowButton2NormalScale()
    {
        if (NowSelectedButton == null)
        {
            SoundSettingButton.image.color = UnSelectedButtonColor;
            CreditButton.image.color = UnSelectedButtonColor;
            QuestButton.image.color = UnSelectedButtonColor;
            AccountButton.image.color = UnSelectedButtonColor;
            HotKeyButton.image.color = UnSelectedButtonColor;
            FriendButton.image.color = UnSelectedButtonColor;
            SoundSettingButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            CreditButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            QuestButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            AccountButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            HotKeyButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            FriendButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
            return;
        }
        NowSelectedButton.GetComponent<Image>().color = UnSelectedButtonColor;
        NowSelectedButton.GetComponentInChildren<TMP_Text>(true).color = UnSelectedButtonColor_Text;
    }
    [SerializeField] Color SelectedButtonColor;
    [SerializeField] Color UnSelectedButtonColor;
    [SerializeField] Color SelectedButtonColor_Text;
    [SerializeField] Color UnSelectedButtonColor_Text;
    void MakeButtonUIAsSelected(Button b)
    {
        b.image.color = SelectedButtonColor;
        b.GetComponentInChildren<TMP_Text>(true).color = SelectedButtonColor_Text;
    }

    void DisableAllViews()
    {
        SoundSettingView.gameObject.SetActive(false);
        CreditView.gameObject.SetActive(false);
        QuestView.gameObject.SetActive(false);
        AccountView.gameObject.SetActive(false);
        HotKeyView.gameObject.SetActive(false);
        FriendView.gameObject.SetActive(false);
    }


    public void OnButtonClicked_SoundSetting()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = SoundSettingButton.transform;
        MakeButtonUIAsSelected(SoundSettingButton);
        SoundSettingView.gameObject.SetActive(true);
        SoundSettingView.transform.GetComponentInChildren<InGameSettingPanel>().IsOn = true;
    }
    public void OnButtonClicked_Friend()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = HotKeyButton.transform;
        MakeButtonUIAsSelected(HotKeyButton);
        FriendView.gameObject.SetActive(true);
    }
    public void OnButtonClicked_Helper()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = HotKeyButton.transform;
        MakeButtonUIAsSelected(HotKeyButton);
        HotKeyView.gameObject.SetActive(true);
    }
    public void OnButtonClicked_Quest()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = QuestButton.transform;
        MakeButtonUIAsSelected(QuestButton);
        QuestView.gameObject.SetActive(true);
    }
    public void OnButtonClicked_Credit()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = CreditButton.transform;
        MakeButtonUIAsSelected(CreditButton);
        CreditView.gameObject.SetActive(true);
        AchievementManager.Unlock(AchievementId.WatchCredit);
    }
    public void OnButtonClicked_Account()
    {
        ResetNowButton2NormalScale();
        DisableAllViews();
        NowSelectedButton = AccountButton.transform;
        MakeButtonUIAsSelected(AccountButton);
        AccountView.gameObject.SetActive(true);
    }
    private void Awake()
    {
        //SettingButton.onClick.AddListener(() =>
        //{
        //    OnSettingButtonClicked();
        //});


        SoundSettingButton.onClick.AddListener(OnButtonClicked_SoundSetting);
        CreditButton.onClick.AddListener(OnButtonClicked_Credit);
        QuestButton.onClick.AddListener(OnButtonClicked_Quest);
        AccountButton.onClick.AddListener(OnButtonClicked_Account);
        HotKeyButton.onClick.AddListener(OnButtonClicked_Helper);
        FriendButton.onClick.AddListener(OnButtonClicked_Friend);
    }

    public void OnSettingButtonClicked()
    {
        gameObject.SetActive(true);
        //IsViewing = true;
        SoundSettingButton.onClick.Invoke();
        //EditorApplication.isPaused = true;
    }
}
