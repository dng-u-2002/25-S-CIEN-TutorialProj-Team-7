using Helpers;
using Photon.Pun;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class PlayerUIDrawer : MonoBehaviour
{
    [SerializeField] TMP_Text NickNameTextBox;
    [SerializeField] Transform RPSTextBoxBackground;
    [SerializeField] TMP_Text RPSTextBox;

    [SerializeField] Transform Character_WhieRabbit;
    [SerializeField] Transform Character_DownRabbit;
    [SerializeField] Transform Character_BlackRabbit;

    [SerializeField] Emoticon EmoticonDatas;



    string EmoticonTransformAnimationID;
    string EmoticonAlphaAnimationID;

    [SerializeField] RectTransform EmoticonSpriteContainer;
    [SerializeField] Image EmoticonSprite;

    IEnumerator _PlayEmoticon(SingleEmoticon emoticon)
    {
        float duration = 1.5f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            var sprite = emoticon.GetSpriteWithNormalizedTime(elapsedTime);
            if (sprite != null)
            {
                EmoticonSprite.sprite = sprite;
            }
            yield return null;
        }
        Quaternion originalRotation = EmoticonSpriteContainer.localRotation;
        float diffAngle = -10f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, diffAngle) * Quaternion.identity;

        var image = EmoticonSprite;


        EmoticonTransformAnimationID = ObjectMoveHelper.RotatebjectSlerp(EmoticonSpriteContainer, targetRotation, 0.12f, Helpers.ePosition.Local);
        EmoticonAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 0.0f, 0.12f);

        yield return new WaitForSeconds(0.12f);
        EmoticonSpriteContainer.gameObject.SetActive(false);
    }
    Coroutine EmotionPlayer;
    public virtual void PlayEmoticon(int index)
    {
        var emoticon = EmoticonDatas.Emoticons[index];
        ObjectMoveHelper.TryStop(EmoticonTransformAnimationID);
        ObjectMoveHelper.TryStop(EmoticonAlphaAnimationID);
        EmoticonSpriteContainer.gameObject.SetActive(true);

        Quaternion originalRotation = Quaternion.identity;
        float diffAngle = 10f;
        Quaternion startRotation = Quaternion.Euler(0, 0, diffAngle) * originalRotation;
        EmoticonSpriteContainer.localRotation = startRotation;

        var image = EmoticonSprite.GetComponentInChildren<Image>();
        var originalColor = image.color;
        originalColor.a = 0.2f;
        image.color = originalColor;

        EmoticonTransformAnimationID = ObjectMoveHelper.RotatebjectSlerp(EmoticonSpriteContainer, originalRotation, 0.12f, Helpers.ePosition.Local);
        EmoticonAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 1.0f, 0.12f);

        if (EmotionPlayer != null)
            StopCoroutine(EmotionPlayer);
        EmotionPlayer = StartCoroutine(_PlayEmoticon(emoticon));
    }

    public void MoveCardPosition2DeletedCardContainerPosition(int idx, float duration)
    {
        Target.ThisDeck.GetCard(idx).CardGameObject.MoveMovementTransformPosition(DeletedCardContainer.position, duration, ePosition.World);
    }
    public void MoveCardPosition_FromDeckPosition2LocalPosition(Card c, float duration)
    {
        if(c == null || c.CardGameObject == null)
        {
            Debug.LogError("Card or CardGameObject is null.");
            return;
        }
        if(Target.ThisDeck.GetCardsAsList().Contains(c) == false)
        {
            Debug.LogError("Card is not in the deck.");
            return;
        }
        c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
        c.CardGameObject.MoveMovementTransformPosition(Vector3.zero, duration, ePosition.Local);
    }
    public void MoveCardPosition_FromDeckPosition2LocalPosition(int idx, float duration)
    {
        Target.ThisDeck.GetCard(idx).CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
        Target.ThisDeck.GetCard(idx).CardGameObject.MoveMovementTransformPosition(Vector3.zero, duration, ePosition.Local);
    }

    public int Character { get; private set; }
    public void SetChatacter(int idx)
    {
        Character_WhieRabbit.gameObject.SetActive(false);
        Character_DownRabbit.gameObject.SetActive(false);
        Character_BlackRabbit.gameObject.SetActive(false);

        Character = idx;
        if (idx == 0)
        {
            Character_WhieRabbit.gameObject.SetActive(true);
        }
        else if (idx == 1)
        {
            Character_DownRabbit.gameObject.SetActive(true);
        }
        else
        {
            Character_BlackRabbit.gameObject.SetActive(true);
        }
    }

    protected string ObserverModeAnimationIDPosition;
    protected string ObserverModeAnimationIDScale;
    public void Go2OriginTransform()
    {
        ObjectMoveHelper.TryStop(ObserverModeAnimationIDPosition);
        ObjectMoveHelper.TryStop(ObserverModeAnimationIDScale);

        ObserverModeAnimationIDPosition = ObjectMoveHelper.MoveObjectSlerp(transform, OriginalPosition, 1.0f, ePosition.Local);
        ObserverModeAnimationIDScale = ObjectMoveHelper.ScaleObject(transform, OriginalScale, 1.0f);
        ShowGrayPanel(false);
    }
    public void SetOutCount(byte length)
    {
        length = (byte)(3 - length);
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

    public void ShowAllCardWithAnimatedIntervalDelay(float delay, float durationPerCard)
    {
        CardObjects.RemoveAll(item => item == null);
        IEnumerator SACWAID()
        {
            foreach (var cardObject in CardObjects)
            {
                if (cardObject != null)
                {
                    cardObject.SetFaceAnimated(true, 1.2f, durationPerCard);
                    yield return new WaitForSeconds(delay);
                }
            }
        }
        StartCoroutine(SACWAID());
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
    string RPSTextBoxAnimationID;
    string RPSTextBoxAlphaAnimationID;

    public void SetWordCloudTextBox(bool active, string text, float fontSize, bool animated = true)
    {
        if (active)
        {
            RPSTextBox.text = text;
            RPSTextBox.fontSize = fontSize;
        }
        else
        {
            RPSTextBox.text = string.Empty;
        }

        if (animated)
        {
            if (active)
            {
                ObjectMoveHelper.TryStop(RPSTextBoxAnimationID);
                ObjectMoveHelper.TryStop(RPSTextBoxAlphaAnimationID);
                RPSTextBoxBackground.gameObject.SetActive(true);

                Quaternion originalRotation = Quaternion.identity;
                float diffAngle = 10f;
                Quaternion startRotation = Quaternion.Euler(0, 0, diffAngle) * originalRotation;
                RPSTextBoxBackground.localRotation = startRotation;

                var image = RPSTextBoxBackground.GetComponentInChildren<Image>();
                var originalColor = image.color;
                originalColor.a = 0.2f;
                image.color = originalColor;

                RPSTextBoxAnimationID = ObjectMoveHelper.RotatebjectSlerp(RPSTextBoxBackground, originalRotation, 0.12f, Helpers.ePosition.Local);
                RPSTextBoxAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 1.0f, 0.12f);
            }
            else
            {
                RPSTextBoxBackground.gameObject.SetActive(true);

                Quaternion originalRotation = RPSTextBoxBackground.localRotation;
                float diffAngle = -10f;
                Quaternion targetRotation = Quaternion.Euler(0, 0, diffAngle) * Quaternion.identity;

                var image = RPSTextBoxBackground.GetComponentInChildren<Image>();


                RPSTextBoxAnimationID = ObjectMoveHelper.RotatebjectSlerp(RPSTextBoxBackground, targetRotation, 0.12f, Helpers.ePosition.Local);
                RPSTextBoxAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 0.0f, 0.12f);
            }
        }
        else
        {
            RPSTextBoxBackground.gameObject.SetActive(active);
        }
    }

    [SerializeField] Sprite RockSprite;
    [SerializeField] Sprite PaperSprite;
    [SerializeField] Sprite ScissorsSprite;
    [SerializeField] Image RPSImage;
    public void SetRPSTextBox(bool active, eRPS rps, bool ignoreSame = false, bool animated = true)
    {
        if (rps == eRPS.None)
        {
            active = false;
        }
            RPSTextBox.fontSize = 0.0f;
        RPSTextBox.text = "";

        if (active)
        {
            switch (rps)
            {
                case eRPS.Rock:
                    RPSImage.sprite = RockSprite;
                    RPSTextBox.text = "바위";
                    break;
                case eRPS.Paper:
                    RPSImage.sprite = PaperSprite;
                    RPSTextBox.text = "보";
                    break;
                case eRPS.Scissors:
                    RPSImage.sprite = ScissorsSprite;
                    RPSTextBox.text = "가위";
                    break;
            }
        }
        else
        {
            RPSTextBox.text = "";
        }

        if (animated)
        {
            if(active)
            {
                if ((ignoreSame == true))
                {
                    if (RPSTextBox.text == "가위" && rps == eRPS.Scissors)
                        return;
                    if (RPSTextBox.text == "바위" && rps == eRPS.Rock)
                        return;
                    if (RPSTextBox.text == "보" && rps == eRPS.Paper)
                        return;
                }
                ObjectMoveHelper.TryStop(RPSTextBoxAnimationID);
                ObjectMoveHelper.TryStop(RPSTextBoxAlphaAnimationID);
                RPSTextBoxBackground.gameObject.SetActive(true);

                Quaternion originalRotation = Quaternion.identity;
                float diffAngle = 10f;
                Quaternion startRotation = Quaternion.Euler(0, 0, diffAngle) * originalRotation;
                RPSTextBoxBackground.localRotation = startRotation;

                var image = RPSTextBoxBackground.GetComponentInChildren<Image>();
                var originalColor = image.color;
                originalColor.a = 0.2f;
                image.color = originalColor;


                RPSTextBoxAnimationID = ObjectMoveHelper.RotatebjectSlerp(RPSTextBoxBackground, originalRotation, 0.12f, Helpers.ePosition.Local);
                RPSTextBoxAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 0.0f, 0.12f);
                ObjectMoveHelper.ChangeAlpha(RPSImage, 1.0f, 0.12f);
            }
            else
            {
                RPSTextBoxBackground.gameObject.SetActive(true);

                Quaternion originalRotation = RPSTextBoxBackground.localRotation;
                float diffAngle = -10f;
                Quaternion targetRotation = Quaternion.Euler(0, 0, diffAngle) * Quaternion.identity;

                var image = RPSTextBoxBackground.GetComponentInChildren<Image>();


                RPSTextBoxAnimationID = ObjectMoveHelper.RotatebjectSlerp(RPSTextBoxBackground, targetRotation, 0.12f, Helpers.ePosition.Local);
                RPSTextBoxAlphaAnimationID = ObjectMoveHelper.ChangeAlpha(image, 0.0f, 0.12f);
                ObjectMoveHelper.ChangeAlpha(RPSImage, 0.0f, 0.12f);
            }
        }
        else
        {
            RPSTextBoxBackground.gameObject.SetActive(active);
        }
    }
    public void Initialize(Player target)
    {
        Target = target;
        Target.ThisDeck.OnCardAdded += OnCardAdded;
        Target.ThisDeck.OnCardRemoved += OnCardRemoved;
    }
    public void SetNickName()
    {
        Photon.Realtime.Player p = null;
        foreach(var s in PhotonNetwork.CurrentRoom.Players)
        {
            if (s.Value.ActorNumber == Target.ID)
                p = s.Value;
        }
        string nickName;
        if (p != null)
            nickName = p.NickName;
        else
        {
            //봇임
            //Target.ID
            if (Character == 0)
            {
                nickName = "계순이";
            }
            else if (Character == 1)
            {
                nickName = "달래";
            }
            else
            {
                nickName = "덕구";
            }
            //nickName = "Unknown";
        }
        if (nickName.Contains("Test User"))
        {
            if (Character == 0)
            {
                nickName = "계순이";
            }
            else if (Character == 1)
            {
                nickName = "달래";
            }
            else
            {
                nickName = "덕구";
            }
        }
        Debug.Log($"Setting nickname for player {Target.ID} to {nickName}");
        NickNameTextBox.text = nickName;
    }

    public string GetNickName()
    {
        return NickNameTextBox.text;
    }

    [SerializeField] public Player Target;

    [SerializeField] Image PlayerMainImage;
    [SerializeField] RectTransform[] OutcountImages;
    [SerializeField] TMP_Text OrderText;
    [SerializeField] TMP_Text InOutText;

    [SerializeField] protected List<CardObject> CardObjects;
    [SerializeField] protected HorizontalLayoutGroup CardContainer;

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
    public string GetOrderText()
    {
        return OrderText.text;
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
        OriginalPosition = transform.localPosition;
        OriginalScale = transform.localScale;
        CardObjects = new List<CardObject>();

        SetChatacter(UnityEngine.Random.Range(0, 3));

        EmoticonSpriteContainer.gameObject.SetActive(false);
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
                    //ObjectMoveHelper.ScaleObject(co.transform, Vector3.zero, 0.28f);
                    //DelayedFunctionHelper.InvokeDelayed(() =>
                    //{
                    //}, 0.3f);
                    CardObjects.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer.GetComponent<RectTransform>());
        }
    }
    public void UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer.GetComponent<RectTransform>());
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


            LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer.GetComponent<RectTransform>());
        }
    }
    public void UpdateCardsLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer.GetComponent<RectTransform>());
    }
    Vector3 OriginalPosition;
    Vector3 OriginalScale;
    public virtual void SetSpecialRuleMode()
    {

    }

    public virtual void SetSpecialRuleObserverMode()
    {

    }

    [SerializeField] public RectTransform DeletedCardContainer;

    internal void DeleteAnycard()
    {
        if (CardObjects.Count > 0)
        {
            CardObject lastCard = CardObjects[CardObjects.Count - 1];
            lastCard.transform.SetParent(DeletedCardContainer);
            lastCard.transform.localPosition = Vector3.zero; // Reset position to center of DeletedCardContainer
            lastCard.SetFace(false); // Set the card face to back
            LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer.GetComponent<RectTransform>());
        }
    }

    public virtual void ShowCard2Delete(Card c1)
    {
    }

    public virtual void RemoveCard2Delete()
    {
    }
}
