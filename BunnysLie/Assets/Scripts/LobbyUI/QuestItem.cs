using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestItem : MonoBehaviour
{

    [SerializeField] TMP_Text QuestNameText;
    [SerializeField] TMP_Text QuestCounterText;

    public void SetTexts(string name, string counter)
    {
        QuestNameText.text = name;
        QuestCounterText.text = counter;
    }
}
