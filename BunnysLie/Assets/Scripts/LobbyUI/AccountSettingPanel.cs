using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccountSettingPanel : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text NickNameText;
    [SerializeField] TMPro.TMP_Text LastLoginTimeText;


    public void SetText(string nickName, string lastLoginTime)
    {
        NickNameText.text = nickName;
        LastLoginTimeText.text = lastLoginTime;
    }
}
