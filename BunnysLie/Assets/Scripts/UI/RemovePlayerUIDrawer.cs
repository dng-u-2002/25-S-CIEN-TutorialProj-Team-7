using Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RemovePlayerUIDrawer : PlayerUIDrawer
{
    [SerializeField] Button MuteButton;
    [SerializeField] Sprite NormalSpeaker;
    [SerializeField] Sprite MutedSpeaker;

    bool _NowMuteState = false;
    bool NowMuteState
    {
        get
        {
            return _NowMuteState;
        }

        set
        {
            _NowMuteState = value;
            if(_NowMuteState == false)
            {
                MuteButton.image.sprite = NormalSpeaker;
            }
            else
            {
                MuteButton.image.sprite = MutedSpeaker;
            }
        }
    }
    protected override void Start()
    {
        base.Start();
        NowMuteState = false;

        MuteButton.onClick.AddListener(() =>
        {
            InGameManager.Instance.PlayButtonClickSound();
            NowMuteState = !NowMuteState;
        });

        CardContainer.transform.localScale = Vector3.one * 1.12f;
        CardContainer.transform.localPosition += new Vector3(40, 0, 0);
    }
    public override void SetSpecialRuleMode()
    {
        base.SetSpecialRuleMode();

        ObjectMoveHelper.TryStop(ObserverModeAnimationIDPosition);
        ObjectMoveHelper.TryStop(ObserverModeAnimationIDScale);

        ObserverModeAnimationIDPosition = ObjectMoveHelper.MoveObject(transform, new Vector3(-196, 350, 0), 1.0f, ePosition.Local);
        ObserverModeAnimationIDScale = ObjectMoveHelper.ScaleObject(transform, Vector3.one * 0.9f, 1.0f);
    }
    public override void ShowCard2Delete(Card cardData)
    {
        DeletedCardContainer.transform.GetComponentInChildren<CardObject>().SetCard(cardData);
        DeletedCardContainer.transform.GetComponentInChildren<CardObject>().SetFace(true); // Set the card face to back
    }
    public override void RemoveCard2Delete()
    {
        var card = DeletedCardContainer.transform.GetComponentInChildren<CardObject>();
        if (card != null)
        {
            Target.ThisDeck.RemoveCard(card.GetCard());
        }
        if(card != null)
        {
            Destroy(card.gameObject);
        }
    }

    public override void SetSpecialRuleObserverMode()
    {
        base.SetSpecialRuleObserverMode();
        ObjectMoveHelper.TryStop(ObserverModeAnimationIDPosition);
        ObjectMoveHelper.TryStop(ObserverModeAnimationIDScale);

        ObserverModeAnimationIDPosition = ObjectMoveHelper.MoveObject(transform, new Vector3(736, 359, 0), 1.0f, ePosition.Local);
        ObserverModeAnimationIDScale= ObjectMoveHelper.ScaleObject(transform, Vector3.one * 0.7f, 1.0f);
    }
}
