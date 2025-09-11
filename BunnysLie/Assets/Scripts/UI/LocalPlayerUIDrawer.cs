using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Helpers;
using Photon.Voice.Unity;

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

    [SerializeField] Button MicButton;
    bool IsMicActive = true;


    [SerializeField] Sprite GreenClock;
    [SerializeField] Sprite YellowClock;
    [SerializeField] Sprite RedClock;
    [SerializeField] Image ClockImage;
    [SerializeField] TMP_Text ClockSecondCounterText;

    Coroutine ClockRunner;

    Action KeyboardCallback;
    Action KeyboardCallback_SelectCard;

    public void StopClock()
    {
        if (ClockRunner != null)
        {
            StopCoroutine(ClockRunner);
            ClockRunner = null;
        }
        ClockImage.gameObject.SetActive(false);
    }
    public void StartClock_30s(System.Action onEnd)
    {
        if(ClockRunner != null)
        {
            StopCoroutine(ClockRunner);
        }
        ClockImage.gameObject.SetActive(true);
        ClockRunner = StartCoroutine(_StartClock_30s(onEnd));
    }

    public void StartClock_10s(System.Action onEnd)
    {
        if (ClockRunner != null)
        {
            StopCoroutine(ClockRunner);
        }
        ClockImage.gameObject.SetActive(true);
        ClockRunner = StartCoroutine(_StartClock_10s(onEnd));
    }

    IEnumerator _StartClock_10s(System.Action onEnd)
    {
        float totalTime = 10.0f;
        float elapsedTime = 0.0f;
        while (elapsedTime < totalTime)
        {
            elapsedTime += Time.deltaTime;
            float leftTime = totalTime - elapsedTime;
            if (leftTime < 2.0f)
            {
                ClockImage.sprite = RedClock;
            }
            else if (leftTime < 5.0f)
            {
                ClockImage.sprite = YellowClock;
            }
            else
            {
                ClockImage.sprite = GreenClock;
            }
            ClockSecondCounterText.text = Mathf.CeilToInt(leftTime).ToString();
            yield return null;
        }
        onEnd?.Invoke();
        yield return new WaitForSeconds(0.5f);
        ClockImage.gameObject.SetActive(false);
    }
    IEnumerator _StartClock_30s(System.Action onEnd)
    {
        float totalTime = 30.0f;
        float elapsedTime = 0.0f;
        while (elapsedTime < totalTime)
        {
            elapsedTime += Time.deltaTime;
            float leftTime = totalTime - elapsedTime;
            if (leftTime < 5.0f)
            {
                ClockImage.sprite = RedClock;
            }
            else if (leftTime < 15.0f)
            {
                ClockImage.sprite = YellowClock;
            }
            else
            {
                ClockImage.sprite = GreenClock;
            }
            ClockSecondCounterText.text = Mathf.CeilToInt(leftTime).ToString();
            yield return null;
        }
        onEnd?.Invoke();
        yield return new WaitForSeconds(0.5f);
        ClockImage.gameObject.SetActive(false);
    }







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
    public void ReStart30sClock2GoInSpecialRule()
    {
        StopClock();
        StartClock_30s(() =>
        {
            SpecialRuleButton_Go.onClick.Invoke();
        });
    }
    void Callback_SpecialRuleActionByKeyboard()
    {
        var chat = FindObjectOfType<Chating>();
        if(chat != null)
        {
            if (chat.IsFocusing == true)
                return;
        }
        if (SpecialRuleButton_ExchangeWithDeck.interactable && SpecialRuleButton_ExchangeWithDeck.gameObject.activeInHierarchy)
        {
            if(Input.GetKeyDown(KeyCode.I))
            {
                SpecialRuleButton_ExchangeWithDeck.onClick.Invoke();
            }
        }
        if(SpecialRuleButton_ExchangeWithOpponent.interactable && SpecialRuleButton_ExchangeWithOpponent.gameObject.activeInHierarchy)
        {
            if(Input.GetKeyDown(KeyCode.O))
            {
                SpecialRuleButton_ExchangeWithOpponent.onClick.Invoke();
            }
        }
        if(SpecialRuleButton_Go.interactable && SpecialRuleButton_Go.gameObject.activeInHierarchy)
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                SpecialRuleButton_Go.onClick.Invoke();
            }
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

        AlreadyExchangedWithDeck = false;
        AlreadyExchangedWithOpponent = false;

        ReStart30sClock2GoInSpecialRule();
        SpecialRuleButton_Go.onClick.AddListener(() =>
        {
            onGo?.Invoke();
            SpecialRuleButton_Go.interactable = (false);
            SpecialRuleButton_ExchangeWithDeck.interactable = false;
            SpecialRuleButton_ExchangeWithOpponent.interactable = false;
        });
        ExchangeWithDeck = (card => onExchangeWithDeck?.Invoke(card));
        ExchangeWithOpponent = (card => onExchangeWithOpponent?.Invoke(card));

        KeyboardCallback = Callback_SpecialRuleActionByKeyboard;

        SpecialRuleButton_ExchangeWithDeck.onClick.AddListener(() =>
        {
            InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, ConstStrings.TextCloud_Card2Exchange, 40.0f, true);
            InGameManager.Instance.PlayButtonClickSound();
            SetActiveAllSpecialRuleButtons(false);
            AlreadyExchangedWithDeck = true;
            SpecialRuleButton_ExchangeWithDeck.interactable = false;

            SelectCard2Exchange((card) =>
            {
                ExchangeWithDeck?.Invoke(card);
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, 0.0f, true);
            });
        });
        SpecialRuleButton_ExchangeWithOpponent.onClick.AddListener(() =>
        {
            InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, ConstStrings.TextCloud_Card2Exchange, 40.0f, true);
            InGameManager.Instance.PlayButtonClickSound();
            SetActiveAllSpecialRuleButtons(false);
            AlreadyExchangedWithOpponent = true;
            SpecialRuleButton_ExchangeWithOpponent.interactable = false;

            onExhangeWithOpponentButtonClicked?.Invoke();
            SelectCard2Exchange((card) =>
            {
                ExchangeWithOpponent?.Invoke(card);
                ShowPanelOnScreenCenter(ConstStrings.Message_WaitingOpponent, 0);
                StopClock();
                InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(false, string.Empty, 0.0f, true);
            });
        });
    }
    private void Update()
    {
        KeyboardCallback?.Invoke();
        KeyboardCallback_SelectCard?.Invoke();
    }
    // public Card Card2Exchange;
    public void SelectCard2Exchange(System.Action<Card> onSelected)
    {
        InGameManager.Instance.LocalPlayerUIDrawer.SetWordCloudTextBox(true, ConstStrings.TextCloud_Card2Exchange, 40.0f, true);
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
    void Callback_SelectCard()
    {
        var chat = FindObjectOfType<Chating>();
        if (chat != null)
        {
            if (chat.IsFocusing == true)
                return;
        }

        //얘는 직접 가져와야 함(교환을 하거나 하면 CardObjects의 Index와 Child Index가 맞지 않게 됨)
        if (Input.GetKeyDown(KeyCode.Alpha1) && CardObjects[0].SelectButton.interactable)
        {
            CardContainer.transform.GetChild(0).transform.GetComponent<CardObject>().SelectButton.onClick.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && CardObjects[1].SelectButton.interactable)
        {
            CardContainer.transform.GetChild(1).transform.GetComponent<CardObject>().SelectButton.onClick.Invoke();
        }
        if (CardObjects.Count >= 3 && Input.GetKeyDown(KeyCode.Alpha3))
        {
            if(CardObjects[2].SelectButton.interactable)
            {
                CardContainer.transform.GetChild(2).transform.GetComponent<CardObject>().SelectButton.onClick.Invoke();
            }
        }
    }
    public void SelectCard2Delete(System.Action<Card> onSelected)
    {
        SetWordCloudTextBox(true, ConstStrings.TextCloud_Card2Delete, 43.7f,  true);
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
        RPSButton_P.transform.parent.gameObject.SetActive(false);
        InOutButton_In.transform.parent.gameObject.SetActive(false);
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

        MicButton.onClick.AddListener(() =>
        {
            IsMicActive = !IsMicActive;
            if (IsMicActive)
            {
                FindObjectOfType<Recorder>().TransmitEnabled = true;
            }
            else
            {
                FindObjectOfType<Recorder>().TransmitEnabled = false;
            }
        });
        IsMicActive = PlayerPrefs.GetInt("IsActive_VoiceChat", 1) == 1;
        if (IsMicActive)
        {
            FindObjectOfType<Recorder>().TransmitEnabled = true;
        }
        else
        {
            FindObjectOfType<Recorder>().TransmitEnabled = false;
        }
        StopClock();
        KeyboardCallback_SelectCard = Callback_SelectCard;
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
