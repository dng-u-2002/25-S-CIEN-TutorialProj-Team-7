using Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemovePlayerUIDrawer : PlayerUIDrawer
{
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
