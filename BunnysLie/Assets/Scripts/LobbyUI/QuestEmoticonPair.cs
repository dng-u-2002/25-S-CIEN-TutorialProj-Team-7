using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestEmoticonPair : MonoBehaviour
{
    [SerializeField] Image EmoticonImage;
    [SerializeField] AchievementId AchId;
    [SerializeField] Image BlockPanel;
    [SerializeField] TMP_Text Name;
    [SerializeField] TMP_Text Description;

    //private void Awake()
    //{
    //    StartCoroutine(_Sync());
    //}
    private void Update()
    {
        Set();
    }
    IEnumerator _Sync()
    {
        while(true)
        {
            yield return new WaitForSeconds(1.0f);
            Set();
        }
    }

    public bool IsUnlocked()
    {
        return AchievementManager.IsUnLocked(AchId);
    }


    void Set()
    {
        var unlocked = AchievementManager.IsUnLocked(AchId);
        Debug.Log($"{AchId} : {unlocked}");
        BlockPanel.gameObject.SetActive(!unlocked);
    }
}
