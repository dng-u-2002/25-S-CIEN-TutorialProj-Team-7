using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmoticonLocker : MonoBehaviour
{
    [SerializeField] AchievementId TargetAchievement;

    private void Start()
    {
        StartCoroutine(_Start());
    }
    IEnumerator _Start()
    {
        var btn = GetComponent<Button>();
        while (true)
        {
            if (AchievementManager.IsUnLocked(TargetAchievement))
            {
                btn.interactable = true;
            }
            else
            {
                btn.interactable = false;
            }
            yield return new WaitForSeconds(2.0f);
        }
    }
}
