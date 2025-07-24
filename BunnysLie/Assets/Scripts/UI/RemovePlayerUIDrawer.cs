using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemovePlayerUIDrawer : PlayerUIDrawer
{
    public override void SetSpecialRuleMode()
    {
        base.SetSpecialRuleMode();
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(-196, 350);
        rectTransform.localScale = Vector3.one * 0.9f;
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
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(736, 359);
        rectTransform.localScale = Vector3.one * 0.7f;
    }
}
