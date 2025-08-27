using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Helpers;

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


    [SerializeField] public AudioSource RPSSelectSound;
    [SerializeField] public AudioSource IOSelectSound;
    [SerializeField] public AudioSource POSCSelectSound;



    [SerializeField] RectTransform EmotionPanel;

    [SerializeField] RectTransform EmoticonPanel0;
    [SerializeField] RectTransform EmoticonPanel1;
    public void SetEmoticonPanelRange(int range)
    {
        if(range == 0)
        {
            EmoticonPanel0.gameObject.SetActive(true);
            EmoticonPanel1.gameObject.SetActive(false);
        }
        else if(range == 1)
        {
            EmoticonPanel0.gameObject.SetActive(false);
            EmoticonPanel1.gameObject.SetActive(true);
        }
        else
        {
            EmoticonPanel0.gameObject.SetActive(false);
            EmoticonPanel1.gameObject.SetActive(false);
        }
    }
    public override void PlayEmoticon(int index)
    {
        base.PlayEmoticon(index);
        SetActiveEmoticonPanel(false);
        FindObjectOfType<InGameUser_PUN>().SendLocalEmoticonData2Server(index);
    }
    public void OnEmotioconShowButtonClicked()
    {
        EmotionPanel.gameObject.SetActive(!EmotionPanel.gameObject.activeSelf);
        EmotionPanel.transform.GetChild(0).SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SetEmoticonPanelRange(0);
    }
    public void SetActiveEmoticonPanel(bool flag)
    {
        EmotionPanel.gameObject.SetActive(flag);
    }

    public void ShowPanelOnScreenCenterWithButtons(string text, int background, string leftButtonText, string rightButtonText, Action onLeftButtonClick, Action onRightButtonClick)
    {
        if (PanelOnScreenCenterWithButtons != null)
        {
            SetPOSCBackground(background);
            PanelOnScreenCenterWithButtons.gameObject.SetActive(true);
            POSCWBText.text = text;
            POSCWBButton_Left.GetComponentInChildren<TMP_Text>().text = leftButtonText;
            POSCWBButton_Right.GetComponentInChildren<TMP_Text>().text = rightButtonText;
            POSCWBButton_Left.onClick.RemoveAllListeners();
            POSCWBButton_Right.onClick.RemoveAllListeners();
            POSCWBButton_Left.onClick.AddListener(() =>
            {
                onLeftButtonClick?.Invoke();
                POSCSelectSound.Play();
            });
            POSCWBButton_Right.onClick.AddListener(() =>
            {
                onRightButtonClick?.Invoke();
                POSCSelectSound.Play();
            });
        }
    }
    public void SetActivePanelOnScreenCenterWithButtons(bool active)
    {
        if (PanelOnScreenCenterWithButtons != null)
        {
            PanelOnScreenCenterWithButtons.gameObject.SetActive(active);
        }
    }
    [SerializeField] List<Sprite> POSCBakcgrounds;
    public void SetPOSCBackground(int idx)
    {
        PanelOnScreenCenter.GetComponent<Image>().sprite = POSCBakcgrounds[idx];
        PanelOnScreenCenterWithButtons.GetComponent<Image>().sprite = POSCBakcgrounds[idx];
    }
    public Action<Card> ExchangeWithDeck;
    public Action<Card> ExchangeWithOpponent;
    public void ShowPanelOnScreenCenter(string text, int background)
    {
        if (PanelOnScreenCenter != null)
        {
            PanelOnScreenCenter.gameObject.SetActive(true);
            SetPOSCBackground(background);
            POSCText.text = text;
        }
    }
    public void ActivateGoButton()
    {
        if (SpecialRuleButton_Go != null)
        {
            SpecialRuleButton_Go.interactable = true;
        }
    }
    public void RollBackSpecialRuleExchangeButtons()
    {
        SpecialRuleButton_ExchangeWithDeck.interactable = !AlreadyExchangedWithDeck;
        SpecialRuleButton_ExchangeWithOpponent.interactable = !AlreadyExchangedWithOpponent;
    }
    public void SetActiveAllSpecialRuleButtons(bool flag)
    {
        SpecialRuleButton_Go.interactable = flag;
        SpecialRuleButton_ExchangeWithDeck.interactable = flag;
        SpecialRuleButton_ExchangeWithOpponent.interactable = flag;
    }
    public bool AlreadyExchangedWithOpponent = false;
    bool AlreadyExchangedWithDeck = false;
    public void SetSpecialRuleEvents(Action onGo, Action<Card> onExchangeWithDeck, Action onExhangeWithOpponentButtonClicked, Action<Card> onExchangeWithOpponent)
    {
        SpecialRuleButton_Go.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithDeck.onClick.RemoveAllListeners();
        SpecialRuleButton_ExchangeWithOpponent.onClick.RemoveAllListeners();
        SpecialRuleButton_Go.interactable = true;
        SpecialRuleButton_ExchangeWithDeck.interactable = true;
        SpecialRuleButton_ExchangeWithOpponent.interactable = true;

        AlreadyExchangedWithDeck = false;
        AlreadyExchangedWithOpponent = false;

        SpecialRuleButton_Go.onClick.AddListener(() =>
        {
            onGo?.Invoke();
            SpecialRuleButton_Go.interactable = (false);
            SpecialRuleButton_ExchangeWithDeck.interactable = false;
            SpecialRuleButton_ExchangeWithOpponent.interactable = false;
        });
        ExchangeWithDeck = (card => onExchangeWithDeck?.Invoke(card));
        ExchangeWithOpponent = (card => onExchangeWithOpponent?.Invoke(card));

        SpecialRuleButton_ExchangeWithDeck.onClick.AddListener(() =>
        {
            InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, "어떤 카드를\n교환할까?", 40.0f, true);
            InGameManager.Instance.PlayButtonClickSound();
            SetActiveAllSpecialRuleButtons(false);
            AlreadyExchangedWithDeck = true;

            SelectCard2Exchange((card) =>
            {
                ExchangeWithDeck?.Invoke(card);
                SpecialRuleButton_ExchangeWithDeck.interactable = false;
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, 0.0f, true);
            });
        });
        SpecialRuleButton_ExchangeWithOpponent.onClick.AddListener(() =>
        {
            InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, "어떤 카드를\n교환할까?", 40.0f, true);
            InGameManager.Instance.PlayButtonClickSound();
            SetActiveAllSpecialRuleButtons(false);
            AlreadyExchangedWithOpponent = true;

            onExhangeWithOpponentButtonClicked?.Invoke();
            SelectCard2Exchange((card) =>
            {
                ExchangeWithOpponent?.Invoke(card);
                ShowPanelOnScreenCenter("상대의 응답을 기다리는중...", 0);
                SpecialRuleButton_ExchangeWithOpponent.interactable = false;
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, 0.0f, true);
            });
        });
    }
   // public Card Card2Exchange;
    public void SelectCard2Exchange(System.Action<Card> onSelected)
    {
        InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, "어떤 카드를\n교환할까?", 40.0f, true);
        foreach (var c in CardObjects)
        {
            c.ActiveSelection(true, (card) =>
            {
                //Card2Exchange = card;
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, 0.0f, true);
                card.CardGameObject.MoveMovementTransformScale(Vector3.one * 1.1f, 0.2f);
                card.CardGameObject.MoveMovementTransformPosition(new Vector3(0, 50, 0), 0.2f, ePosition.Local);
                DelayedFunctionHelper.InvokeDelayed(() =>
                {
                    onSelected?.Invoke(card);

                }, 0.5f);
                foreach (var oc in CardObjects)
                {
                    oc.ActiveSelection(false, null); // Disable selection for all other cards
                }
            });
        }
    }
    public void SelectCard2Delete(System.Action<Card> onSelected)
    {
        SetWordCloudTextBox(true, "어떤 카드를 버릴까?", 43.7f,  true);
        foreach (var c in CardObjects)
        {
            c.ActiveSelection(true, (card) =>
            {
                //"어떤 카드를 버릴까?" 제거
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, -1, true);

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
        SetActiveEmoticonPanel(false);
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
        realCard.CardGameObject.SetFaceAnimated(true, 1.2f, 0.2f);
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
        //CardObject c = card.CardGameObject;
        //c.transform.SetParent(DeletedCardContainer);
        //c.transform.localPosition = Vector3.zero; // Reset position to center of DeletedCardContainer
        //c.SetFace(true); // Set the card face to back
    }

    internal void SetAllCards2DefaultState()
    {
        foreach(var c in CardObjects)
        {
            c.SetMoverDefaultTransform();
            c.ActiveSelection(false, null);
        }
    }

    internal void ChangeCardSibilingIndicesAndKeepMoverPosition(Card c1, Card c2)
    {
        int idx1 = c1.CardGameObject.transform.GetSiblingIndex();
        int idx2 = c2.CardGameObject.transform.GetSiblingIndex();

        Vector3 originPosition1 = c1.CardGameObject.GetMoverPosition();
        Vector3 originPosition2 = c2.CardGameObject.GetMoverPosition();

        c1.CardGameObject.transform.SetSiblingIndex(idx2);
        c2.CardGameObject.transform.SetSiblingIndex(idx1);
        //레이아웃을 업데이트해야 함.
        UpdateLayout();

        c1.CardGameObject.SetMovementTransformPosition(originPosition1);
        c2.CardGameObject.SetMovementTransformPosition(originPosition2);
    }
}
