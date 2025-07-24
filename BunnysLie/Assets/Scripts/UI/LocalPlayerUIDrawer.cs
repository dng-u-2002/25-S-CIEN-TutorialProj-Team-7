using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LocalPlayerUIDrawer : PlayerUIDrawer
{
    [SerializeField] public Button RPSButton_R;
    [SerializeField] public Button RPSButton_P;
    [SerializeField] public Button RPSButton_S;

    [SerializeField] public Button InOutButton_In;
    [SerializeField] public Button InOutButton_Out;

    [SerializeField] Transform PanelOnScreenCenter;
    [SerializeField] TMP_Text POSCText;

    [SerializeField] public Button SpecialRuleButton_Go;
    [SerializeField] public Button SpecialRuleButton_ExchangeWithDeck;
    [SerializeField] public Button SpecialRuleButton_ExchangeWithOpponent;

    [SerializeField] Transform PanelOnScreenCenterWithButtons;
    [SerializeField] TMP_Text POSCWBText;
    [SerializeField] Button POSCWBButton_Left;
    [SerializeField] Button POSCWBButton_Right;

    public void ShowPanelOnScreenCenterWithButtons(string text, string leftButtonText, string rightButtonText, Action onLeftButtonClick, Action onRightButtonClick)
    {
        if (PanelOnScreenCenterWithButtons != null)
        {
            PanelOnScreenCenterWithButtons.gameObject.SetActive(true);
            POSCWBText.text = text;
            POSCWBButton_Left.GetComponentInChildren<TMP_Text>().text = leftButtonText;
            POSCWBButton_Right.GetComponentInChildren<TMP_Text>().text = rightButtonText;
            POSCWBButton_Left.onClick.RemoveAllListeners();
            POSCWBButton_Right.onClick.RemoveAllListeners();
            POSCWBButton_Left.onClick.AddListener(() => onLeftButtonClick?.Invoke());
            POSCWBButton_Right.onClick.AddListener(() => onRightButtonClick?.Invoke());
        }
    }
    public void SetActivePanelOnScreenCenterWithButtons(bool active)
    {
        if (PanelOnScreenCenterWithButtons != null)
        {
            PanelOnScreenCenterWithButtons.gameObject.SetActive(active);
        }
    }

    public Action<Card> ExchangeWithDeck;
    public Action<Card> ExchangeWithOpponent;
    public void ShowPanelOnScreenCenter(string text)
    {
        if (PanelOnScreenCenter != null)
        {
            PanelOnScreenCenter.gameObject.SetActive(true);
            POSCText.text = text;
        }
    }
    public void SetSpecialRuleEvents(Action onGo, Action<Card> onExchangeWithDeck, Action onExhangeWithOpponentButtonClicked, Action<Card> onExchangeWithOpponent)
    {
        SpecialRuleButton_Go.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithDeck.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithOpponent.onClick.RemoveAllListeners();
        SpecialRuleButton_Go.interactable = true;
        SpecialRuleButton_ExchangeWithDeck.interactable = true;
        SpecialRuleButton_ExchangeWithOpponent.interactable = true;

        SpecialRuleButton_Go.onClick.AddListener(() =>
        {
            onGo?.Invoke();
            SpecialRuleButton_Go.interactable = (false);
        });
        ExchangeWithDeck = (card => onExchangeWithDeck?.Invoke(card));
        ExchangeWithOpponent = (card => onExchangeWithOpponent?.Invoke(card));

        SpecialRuleButton_ExchangeWithDeck.onClick.AddListener(() =>
        {
            SelectCard2Exchange((card) =>
            {
                ExchangeWithDeck?.Invoke(card);
                SpecialRuleButton_ExchangeWithDeck.interactable = false;
            });
        });
        SpecialRuleButton_ExchangeWithOpponent.onClick.AddListener(() =>
        {
            onExhangeWithOpponentButtonClicked?.Invoke();
            SelectCard2Exchange((card) =>
            {
                ExchangeWithOpponent?.Invoke(card);
                ShowPanelOnScreenCenter("상대의 응답을 기다리는중...");
                SpecialRuleButton_ExchangeWithOpponent.interactable = false;
            });
        });
    }
   // public Card Card2Exchange;
    public void SelectCard2Exchange(System.Action<Card> onSelected)
    {
        foreach(var c in CardObjects)
        {
            c.ActiveSelection(true, (card) =>
            {
                //Card2Exchange = card;
                onSelected?.Invoke(card);
                foreach (var oc in CardObjects)
                {
                    oc.ActiveSelection(false, null); // Disable selection for all other cards
                }
            });
        }
    }
    public void SelectCard2Delete(System.Action<Card> onSelected)
    {
        foreach (var c in CardObjects)
        {
            c.ActiveSelection(true, (card) =>
            {
                //Card2Exchange = card;
                onSelected?.Invoke(card);
                foreach (var oc in CardObjects)
                {
                    oc.ActiveSelection(false, null); // Disable selection for all other cards
                }
            });
        }
    }
    public override void SetSpecialRuleMode()
    {
        base.SetSpecialRuleMode();
        SpecialRuleButton_Go.gameObject.SetActive(true);
        SpecialRuleButton_ExchangeWithDeck.gameObject.SetActive(true);
        SpecialRuleButton_ExchangeWithOpponent.gameObject.SetActive(true);
        SpecialRuleButton_Go.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithDeck.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithOpponent.onClick.RemoveAllListeners();
    }
    public void SetActivePanelOnScreenCenter(bool active)
    {
        if (PanelOnScreenCenter != null)
        {
            PanelOnScreenCenter.gameObject.SetActive(active);
        }
    }


    protected override void Start()
    {
        base.Start();
        SetRPSButtonsActive(false); 
        SetIOButtonsActive(false);
        SetActivePanelOnScreenCenter(false);
        SpecialRuleButton_Go.gameObject.SetActive(false);
        SpecialRuleButton_ExchangeWithDeck.gameObject.SetActive(false);
        SpecialRuleButton_ExchangeWithOpponent.gameObject.SetActive(false);
        SetActivePanelOnScreenCenterWithButtons(false);
    }

    public void RemoveAllListenersFromRPSButtons()
    {
        RPSButton_R.onClick.RemoveAllListeners();
        RPSButton_P.onClick.RemoveAllListeners();
        RPSButton_S.onClick.RemoveAllListeners();
    }

    public void SetRPSButtonsActive(bool active)
    {
        RPSButton_R.interactable = active;
        RPSButton_P.interactable = active;
        RPSButton_S.interactable = active;
    }

    public void SetIOButtonsActive(bool active)
    {
        InOutButton_In.interactable = active;
        InOutButton_Out.interactable = active;
    }
    public void RemoveAllListenersFromIOButtons()
    {
        InOutButton_In.onClick.RemoveAllListeners();
        InOutButton_Out.onClick.RemoveAllListeners();
    }
    public override void ShowCard2Delete(Card cardData)
    {
        Card realCard = null;
        foreach (var c in CardObjects)
        {
            if (c.GetCard().Type == cardData.Type && c.GetCard().Value == cardData.Value)
            {
                realCard = c.GetCard();
                break;
            }
        }
        realCard.CardGameObject.SetFace(true); // Set the card face to back
    }
    public override void RemoveCard2Delete()
    {
        var card = DeletedCardContainer.transform.GetComponentInChildren<CardObject>();
        if (card != null)
        {
            Target.ThisDeck.RemoveCard(card.GetCard());
        }
    }
    internal void SelectedCard2Delete(Card card)
    {
        CardObject c = card.CardGameObject;
        c.transform.SetParent(DeletedCardContainer);
        c.transform.localPosition = Vector3.zero; // Reset position to center of DeletedCardContainer
        c.SetFace(true); // Set the card face to back
    }
}
