using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSelector : MonoBehaviour
{

    [SerializeField] RectTransform MatchingSelectButtons;
    [SerializeField] RectTransform ModeSelectButtons_FromFastMatching;
    [SerializeField] RectTransform ModeSelectButtons_FromOnlineMatching;

    private void Start()
    {
        OffAllModeSelectButtons();
        MatchingSelectButtons.gameObject.SetActive(true);
    }

    void OffAllModeSelectButtons()
    {
        MatchingSelectButtons.gameObject.SetActive(false);
        ModeSelectButtons_FromFastMatching.gameObject.SetActive(false);
        ModeSelectButtons_FromOnlineMatching.gameObject.SetActive(false);
    }

    public void OnClickMatchingButton_Fast()
    {
        OffAllModeSelectButtons();
        ModeSelectButtons_FromFastMatching.gameObject.SetActive(true);
    }
    public void OnClickMatchingButton_Online()
    {
        OffAllModeSelectButtons();
        ModeSelectButtons_FromOnlineMatching.gameObject.SetActive(true);
    }

    public void OnClickSelectMode_TwoCardInFastMatching()
    {

    }
    public void OnClickSelectMode_ThreeCardFastMatching()
    {

    }

    public void OnClickSelectMode_AnyCardFastMatching()
    {

    }

    public void OnClickSelectMode_TwoCardInOnlineMatching()
    {

    }
    public void OnClickSelectMode_ThreeCardOnlineMatching()
    {

    }
}
