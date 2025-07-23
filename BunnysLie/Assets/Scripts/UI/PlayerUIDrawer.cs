using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIDrawer : MonoBehaviour
{
    [SerializeField] Transform RPSTextBoxBackground;
    [SerializeField] TMP_Text RPSTextBox;

    public void Go2OriginTransform()
    {
        transform.position = OriginalPosition;
        transform.localScale = OriginalScale;
        ShowGrayPanel(false);
    }
    public void SetOutCount(byte length)
    {
        for (int i = 0; i < OutcountImages.Length; i++)
        {
            if (i < length)
            {
                OutcountImages[i].gameObject.SetActive(true);
            }
            else
            {
                OutcountImages[i].gameObject.SetActive(false);
            }
        }
    }
    public void ShowAllCards()
    {
        CardObjects.RemoveAll(item => item == null);
        foreach (var cardObject in CardObjects)
        {
            if(cardObject != null)
            { 
                cardObject.SetFace(true);
            }
        }
    }
    public void SetRPSTextBox(bool active, eRPS rps)
    {
        if (rps == eRPS.None)
            active = false;
        RPSTextBoxBackground.gameObject.SetActive(active);

        if (active)
        {
            switch (rps)
            {
                case eRPS.Rock:
                    RPSTextBox.text = "바위";
                    break;
                case eRPS.Paper:
                    RPSTextBox.text = "보";
                    break;
                case eRPS.Scissors:
                    RPSTextBox.text = "가위";
                    break;
            }
        }
        else
        {
            RPSTextBox.text = "";
        }
    }
    public void Initialize(Player target)
    {
        Target = target;
        Target.ThisDeck.OnCardAdded += OnCardAdded;
        Target.ThisDeck.OnCardRemoved += OnCardRemoved;
    }

    [SerializeField] public Player Target;

    [SerializeField] Image PlayerMainImage;
    [SerializeField] RectTransform[] OutcountImages;
    [SerializeField] TMP_Text OrderText;
    [SerializeField] TMP_Text InOutText;

    [SerializeField] protected List<CardObject> CardObjects;
    [SerializeField] HorizontalLayoutGroup CardContainer;

    [SerializeField] CardObject CardPrefab;

    [SerializeField] Transform FrontPanel;

    public string GetIOText()
    {
        return InOutText.text;
    }
    public void SetIOText(string text)
    {
        InOutText.text = text;
    }
    public void SetOrderText(int order)
    {
        OrderText.text = $"{order + 1}";
    }

    public void ShowGrayPanel(bool active)
    {
        FrontPanel.gameObject.SetActive(active);
    }
    protected virtual void Start()
    {
        if (Target != null)
        {
            Initialize(Target);
        }
        foreach (var oc in OutcountImages)
        {
            oc.gameObject.SetActive(false);
        }
        SetRPSTextBox(false, eRPS.Rock); // Reset RPS text box
        SetIOText("");
        ShowGrayPanel(false);
        SetOutCount(3);
        OriginalPosition = transform.position;
        OriginalScale = transform.localScale;
        CardObjects = new List<CardObject>();
    }

    void OnCardRemoved(Card c)
    {
        if (CardContainer != null)
        {
            for(int i = 0; i < CardObjects.Count; i++)
            {
                CardObject co = CardObjects[i];
                if (co.GetCard().Equals(c))
                {
                    Destroy(co.gameObject);
                    CardObjects.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
        }
    }
    void OnCardAdded(Card c)
    {
        if (CardContainer != null)
        {
            CardObject newCardObject = Instantiate(CardPrefab, CardContainer.transform);
            newCardObject.SetCard(c);
            newCardObject.SetFace(false);
            newCardObject.gameObject.SetActive(true);
            CardObjects.Add(newCardObject);
        }
    }
    Vector3 OriginalPosition;
    Vector3 OriginalScale;
    public virtual void SetSpecialRuleMode()
    {

    }

    public virtual void SetSpecialRuleObserverMode()
    {

    }

    [SerializeField] protected RectTransform DeletedCardContainer;

    internal void DeleteAnycard()
    {
        if (CardObjects.Count > 0)
        {
            CardObject lastCard = CardObjects[CardObjects.Count - 1];
            lastCard.transform.SetParent(DeletedCardContainer);
            lastCard.transform.localPosition = Vector3.zero; // Reset position to center of DeletedCardContainer
            lastCard.SetFace(false); // Set the card face to back
        }
    }

    public virtual void ShowCard2Delete(Card c1)
    {
    }

    public virtual void RemoveCard2Delete()
    {
    }
}
