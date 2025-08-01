using Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    public enum eGameMode : byte
    {
        TwoCards = 2,
        ThreeCards = 3
    }

    public eGameMode Mode = eGameMode.TwoCards;
    public RectTransform DeckTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Player LocalPlayer;
    public Player[] RemotePlayers;

    public LocalPlayerUIDrawer LocalPlayerUIDrawer;
    public PlayerUIDrawer[] RemotePlayerUIDrawers;
    internal int RoomID;

    public void SetRemotePlayersID(List<int> ids, List<byte> characters)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            RemotePlayers[i].ID = ids[i];

            RemotePlayerUIDrawers[i].SetChatacter(characters[i]);
        }
    }
    public void SetLocalPlayerCharacter(byte idx)
    {
        LocalPlayerUIDrawer.SetChatacter(idx);
    }

    public int CalculateOutPlayerCount(Dictionary<int, Tuple<List<Card>, eIO>> status)
    {
        int outCount = 0;
        foreach (var stat in status)
        {
            if (stat.Value.Item2 == eIO.Out)
            {
                outCount++;
            }
        }
        return outCount;
    }
    public Tuple<int, int> CalculateSpecialRule(Dictionary<int, Tuple<List<Card>, eIO>> status)
    {
        List<int> outPlayers = new List<int>();
        List<int> inPlayers = new List<int>();
        foreach (var stat in status)
        {
            if (stat.Value.Item2 == eIO.Out)
            {
                outPlayers.Add(stat.Key);
            }
            else
            {
                inPlayers.Add(stat.Key);
            }
        }
        if (outPlayers.Count == 2)
            return new Tuple<int, int>(outPlayers[0], outPlayers[1]);
        else if(outPlayers.Count >= 3)
            return new Tuple<int, int>(-1, -1); // No special rule applies

        //outPlayers.Count == 1임이 보장
        int s1 = CalculateScore(status[inPlayers[0]].Item1);
        int s2 = CalculateScore(status[inPlayers[1]].Item1);

        if (s1 == s2)
        {
            return new Tuple<int, int>(inPlayers[0], inPlayers[1]); // Special rule applies
        }
        return new Tuple<int, int>(-1, -1); // No special rule applies
    }
    int CalculateScore(List<Card> cards)
    {
        if (cards.Count < 2)
        {
            Debug.LogWarning("Not enough cards to calculate score.");
            return 0;
        }
        var card1 = cards[0];
        var card2 = cards[1];
        if (card1.Value == card2.Value)
        {
            return 100 + card1.Value; // Special case for pairs
        }
        else
        {
            return (card1.Value + card2.Value) % 10; // Normal case
        }
    }
    public int CalculateLoser(Dictionary<int, Tuple<List<Card>, eIO>> status)
    {
        //무조건 Out이 1명이거나 0명
        int outCount = 0;
        foreach(var stat in status)
        {
            if(stat.Value.Item2 == eIO.Out)
            {
                outCount++;
            }
        }
        if (outCount >= 2)
            return -1;

        if(outCount == 0)
        {
            //세 명이 전부 In
            int lowestInScore = 1000;
            int lowerID = 01;
            foreach (var stat in status)
            {
                int score = CalculateScore(stat.Value.Item1);
                if (stat.Value.Item2 == eIO.In)
                {
                    if (lowestInScore > score)
                    {
                        lowestInScore = score;
                        lowerID = stat.Key; // Update the ID of the player with the lowest score
                    }
                }
            }
            return lowerID; // Return the ID of the player with the lowest score among those who are In
        }
        else
        {
            int lowestInScore = 1000;
            int lowerID = 01;
            foreach (var stat in status)
            {
                int score = CalculateScore(stat.Value.Item1);
                if (stat.Value.Item2 == eIO.In)
                {
                    if (lowestInScore > score)
                    {
                        lowestInScore = score;
                        lowerID = stat.Key; // Update the ID of the player with the lowest score
                    }
                }
            }

            //한 명이 Out
            int outPlayer = 0;
            foreach (var stat in status)
            {
                if (stat.Value.Item2 == eIO.Out)
                {
                    outPlayer = stat.Key; // Store the ID of the player who is Out
                }
            }
            int outScore = CalculateScore(status[outPlayer].Item1);

            if(outScore > lowestInScore)
            {
                return outPlayer; // Return the ID of the player who is Out if their score is higher than the lowest In score
            }
            else
            {
                return lowerID; // Return the ID of the player with the lowest In score
            }
        }
        return -1;
    }

    public void ShowRPSRoundResult_Selection(int first, int second, int third, eRPS f, eRPS s, eRPS t)
    {
        foreach (var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == first && first >= 0)
            {
                rp.SetRPSTextBox(true, f); // Set RPS text box for first player
            }
            else if (rp.Target.ID == second && second >= 0)
            {
                rp.SetRPSTextBox(true, s); // Set RPS text box for second player
            }
            else if (rp.Target.ID == third && third >= 0)
            {
                rp.SetRPSTextBox(true, t); // Set RPS text box for third player
            }
            else
            {
                rp.SetRPSTextBox(false, eRPS.None);
            }
        }

        if (LocalPlayer.ID == first && first >= 0)
        {
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false); // Hide panel on screen center for local player
            LocalPlayerUIDrawer.SetRPSTextBox(true, f, true); // Set RPS text box for local player
        }
        else if (LocalPlayer.ID == second && second >= 0)
        {
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false); // Hide panel on screen center for local player
            LocalPlayerUIDrawer.SetRPSTextBox(true, s, true); // Set RPS text box for local player
        }
        else if (LocalPlayer.ID == third && third >= 0)
        {
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false); // Hide panel on screen center for local player
            LocalPlayerUIDrawer.SetRPSTextBox(true, t, true); // Set RPS text box for local player
        }
        else
        {
            LocalPlayerUIDrawer.ShowPanelOnScreenCenter("기다리는 중...", 0);
            LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None); // Reset RPS text box for local player
        }
        LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
        if (LocalPlayer.IsOrderDetermined == true)
        {
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
            LocalPlayerUIDrawer.ShowPanelOnScreenCenter("기다리는 중...", 0);
            LocalPlayerUIDrawer.SetRPSButtonsActive(false);
        }
    }
    public void SetRPSResult_RPSSelectionOnly(int first, int second, int third, eRPS f, eRPS s, eRPS t)
    {
        foreach (var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == first && first >= 0)
            {
                rp.SetRPSTextBox(true, f); // Set RPS text box for first player
            }
            else if (rp.Target.ID == second && second >= 0)
            {
                rp.SetRPSTextBox(true, s); // Set RPS text box for second player
            }
            else if (rp.Target.ID == third && third >= 0)
            {
                rp.SetRPSTextBox(true, t); // Set RPS text box for third player
            }
            else
            {
                rp.SetRPSTextBox(false, eRPS.None);
            }
        }

        if (LocalPlayer.ID == first && first >= 0)
        {
            LocalPlayerUIDrawer.SetRPSTextBox(true, f); // Set RPS text box for local player
        }
        else if (LocalPlayer.ID == second && second >= 0)
        {
            LocalPlayerUIDrawer.SetRPSTextBox(true, s); // Set RPS text box for local player
        }
        else if (LocalPlayer.ID == third && third >= 0)
        {
            LocalPlayerUIDrawer.SetRPSTextBox(true, t); // Set RPS text box for local player
        }
        else
        {
            LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None); // Reset RPS text box for local player
        }
    }

    public int OutPlayerCount;
    public void ShowIOResult(List<int> ins, List<int> outs, bool isFinal)
    {
        OutPlayerCount = outs.Count;
        foreach (var rp in RemotePlayerUIDrawers)
        {
            if(ins.Contains(rp.Target.ID))
            {
                rp.Target.IO = eIO.In; // Set IO for remote player
                rp.SetIOText("IN");
            }
            else if (outs.Contains(rp.Target.ID))
            {
                rp.Target.IO = eIO.Out; // Set IO for remote player
                rp.SetIOText("OUT");
            }
            else
            {
                rp.SetIOText("");
            }
        }

        if (ins.Contains(LocalPlayer.ID))
        {
            LocalPlayer.IO = eIO.In; // Set IO for local player
            LocalPlayerUIDrawer.SetIOText("IN");
        }
        else if (outs.Contains(LocalPlayer.ID))
        {
            LocalPlayer.IO = eIO.Out; // Set IO for local player
            LocalPlayerUIDrawer.SetIOText("OUT");
        }
        else
        {
            LocalPlayerUIDrawer.SetIOText("");
        }
        LocalPlayerUIDrawer.SetIOButtonsActive(false); // Disable IO buttons after showing results
        LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None); // Reset RPS text box
        LocalPlayerUIDrawer.SetRPSButtonsActive(false);
        if(isFinal)
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
    }

    public void SetRPSResult(int first, int second, int third)
    {
        bool isAllPlayerRanked = true;
        if(first != -1 && second != -1 && third != -1)
        {
            isAllPlayerRanked = true;
        }
        else
        {
            isAllPlayerRanked = false;
        }

        foreach (var rp in RemotePlayerUIDrawers)
        {
            if(rp.Target.ID == first && first >= 0)
            {
                rp.SetOrderText(2); // First player
                rp.Target.Order = 2;
            }
            else if (rp.Target.ID == second && second >= 0)
            {
                rp.SetOrderText(1); // Second player
                rp.Target.Order = 1;
            }
            else if (rp.Target.ID == third && third >= 0)
            {
                rp.SetOrderText(0); // Third player
                rp.Target.Order = 0;
            }
            rp.SetRPSTextBox(false, eRPS.None); // Reset RPS text box for remote players
            rp.ShowGrayPanel(false);
        }

        bool isLocalPlayerRanked = false;
        if (LocalPlayer.ID == first && first >= 0)
        {
            isLocalPlayerRanked = true;
            LocalPlayerUIDrawer.SetOrderText(2); // First player
            LocalPlayer.Order = 2;
        }
        else if (LocalPlayer.ID == second && second >= 0)
        {
            isLocalPlayerRanked = true;
            LocalPlayerUIDrawer.SetOrderText(1); // Second player
            LocalPlayer.Order = 1;
        }
        else if (LocalPlayer.ID == third && third >= 0)
        {
            isLocalPlayerRanked = true;
            LocalPlayerUIDrawer.SetOrderText(0); // Third player
            LocalPlayer.Order = 0;
        }
        else
        {
            Debug.LogWarning("Local player is not ranked in RPS result.");
        }

        LocalPlayerUIDrawer.ShowGrayPanel(false); // Hide gray panel for local player
        LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None); // Reset RPS text box
        LocalPlayerUIDrawer.SetRPSButtonsActive(false);
        LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
    }

    public void ShowRPSResult_Order_Accumulated(int firstID, int firstOrder, int secondID, int secondOrder, int thirdID, int thirdOrder)
    {
        Debug.Log(firstID + " " + firstOrder + ", " + secondID + " " + secondOrder + ", " + thirdID + " " + thirdOrder);
        bool isAllPlayerRanked = true;
        if (firstOrder != 3 && secondOrder != 3 && thirdOrder != 3)
        {
            isAllPlayerRanked = true;
        }
        else
        {
            isAllPlayerRanked = false;
        }

        foreach (var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == firstID)
            {
                if(firstOrder == 0)
                {
                    rp.SetOrderText(0); // First player
                    rp.ShowGrayPanel(true);
                }
                else if (firstOrder == 1)
                {
                    rp.SetOrderText(1); // Second player
                    rp.ShowGrayPanel(true);
                }
                else if (firstOrder == 2)
                {
                    rp.SetOrderText(2); // Third player
                    rp.ShowGrayPanel(true);
                }
            }
            else if(rp.Target.ID == secondID)
            {
                if (secondOrder == 0)
                {
                    rp.SetOrderText(0); // First player
                    rp.ShowGrayPanel(true);
                }
                else if (secondOrder == 1)
                {
                    rp.SetOrderText(1); // Second player
                    rp.ShowGrayPanel(true);
                }
                else if (secondOrder == 2)
                {
                    rp.SetOrderText(2); // Third player
                    rp.ShowGrayPanel(true);
                }
            }
            else if (rp.Target.ID == thirdID)
            {
                if (thirdOrder == 0)
                {
                    rp.SetOrderText(0); // First player
                    rp.ShowGrayPanel(true);
                }
                else if (thirdOrder == 1)
                {
                    rp.SetOrderText(1); // Second player
                    rp.ShowGrayPanel(true);
                }
                else if (thirdOrder == 2)
                {
                    rp.SetOrderText(2); // Third player
                    rp.ShowGrayPanel(true);
                }
            }
            rp.SetRPSTextBox(false, eRPS.None); // Reset RPS text box for remote players
        }

        bool isLocalPlayerRanked = false;
        if(LocalPlayer.ID == firstID)
        {
            if(firstOrder == 0)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(0); // First player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (firstOrder == 1)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(1); // Second player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (firstOrder == 2)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(2); // Third player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
        }
        else if(LocalPlayer.ID == secondID)
        {
            if (secondOrder == 0)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(0); // First player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (secondOrder == 1)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(1); // Second player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (secondOrder == 2)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(2); // Third player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
        }
        else if(LocalPlayer.ID == thirdID)
        {
            if (thirdOrder == 0)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(0); // First player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (thirdOrder == 1)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(1); // Second player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
            else if (thirdOrder == 2)
            {
                isLocalPlayerRanked = true;
                LocalPlayerUIDrawer.SetOrderText(2); // Third player
                LocalPlayerUIDrawer.ShowGrayPanel(true);
            }
        }

            LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None);

        LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
        if (LocalPlayer.IsOrderDetermined == true)
        {
            LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(true);
            LocalPlayerUIDrawer.ShowPanelOnScreenCenter("기다리는 중...", 0);
            LocalPlayerUIDrawer.SetRPSButtonsActive(false);
            if (isAllPlayerRanked)
                LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
        }
        LocalPlayer.IsOrderDetermined = LocalPlayer.IsOrderDetermined | isLocalPlayerRanked;
    }

    public void StartGame()
    {

    }

    internal void SetAllCards(Dictionary<int, List<Card>> allCards)
    {
        foreach (var player in RemotePlayers)
        {
            if(allCards.TryGetValue(player.ID, out var cards))
            {
                player.ThisDeck.RemoveAllCards();
                player.ThisDeck.AddCards(cards.ToArray());
                foreach(var c in cards)
                {
                    c.CardGameObject.SetFace(false);
                }
            }
            else
            {
                Debug.LogWarning($"No cards found for player ID {player.ID}");
            }
        }

        //Dictionary<int, Tuple<List<Card>, eIO>> status = new Dictionary<int, Tuple<List<Card>, eIO>>();
        //foreach (var player in RemotePlayers)
        //{
        //    if (allCards.TryGetValue(player.ID, out var cards))
        //    {
        //        status[player.ID] = new Tuple<List<Card>, eIO>(cards, player.IO);
        //    }
        //}
        //status[LocalPlayer.ID] = new Tuple<List<Card>, eIO>(LocalPlayer.ThisDeck.GetCardsAsList(), LocalPlayer.IO);
    }
    internal void ShowAllCardsWithAnimation()
    {
        float interval = 0.2f;
        IEnumerator SACWA()
        {
            foreach (var rp in RemotePlayerUIDrawers)
            {
                rp.ShowAllCardWithAnimatedIntervalDelay(0.1f, 0.4f);
                yield return new WaitForSeconds(interval);
            }
            LocalPlayerUIDrawer.ShowAllCardWithAnimatedIntervalDelay(0.1f, 0.4f);
        }
        StartCoroutine(SACWA());
    }
    internal void ShowAllCards()
    {
        foreach (var rp in RemotePlayerUIDrawers)
        {
            rp.ShowAllCards();
        }
        LocalPlayerUIDrawer.ShowAllCards();
    }

    internal void ShowLoserOfThisRound(int loserId, byte count)
    {
        foreach (var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == loserId)
            {
                LocalPlayerUIDrawer.ShowPanelOnScreenCenter("패배: " + loserId + "번 플레이어", rp.Character + 2);
                rp.SetOutCount(count);
            }
        }
        if (LocalPlayer.ID == loserId)
        {
            LocalPlayerUIDrawer.SetOutCount(count);
        }
    }

    [SerializeField] AudioSource NormalBGM;
    [SerializeField] AudioSource SpecailBGM;
    internal void StartSpecialRule(int user1Id, int user2Id, List<Card> user1Cards, List<Card> user2Cards, System.Action onGo, System.Action<Card> onExchangeWithDeck, System.Action onExhangeWithOpponentButtonClicked, System.Action<Card> onExchangeWithOther)
    {
        NormalBGM.Stop();
        SpecailBGM.Play();
        List<Card> cards2Animated = new List<Card>();
        List<Card> cards2AnimatedLocal = new List<Card>();

        foreach (var rp in RemotePlayerUIDrawers)
        {
            rp.Target.ThisDeck.RemoveAllCards();
        }
        LocalPlayer.ThisDeck.RemoveAllCards();
        bool isLocalObserver = (user1Id == LocalPlayer.ID || user2Id == LocalPlayer.ID) ? false : true;
        if(isLocalObserver == true)
        {
            LocalPlayerUIDrawer.SetSpecialRuleObserverMode();
            foreach (var rp in RemotePlayerUIDrawers)
            {
                if (rp.Target.ID == user1Id)
                {
                    rp.Target.ThisDeck.AddCards(user1Cards.ToArray());
                    cards2Animated.AddRange(user1Cards);
                }
                else if (rp.Target.ID == user2Id)
                {
                    rp.Target.ThisDeck.AddCards(user2Cards.ToArray());
                    cards2Animated.AddRange(user2Cards);
                }
            }

            foreach (var c in cards2Animated)
            {
                c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
                c.CardGameObject.SetFace(false);
            }
            foreach (var c in cards2AnimatedLocal)
            {
                c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
                c.CardGameObject.SetFace(false);
            }

            IEnumerator SSR()
            {
                foreach (var c in cards2Animated)
                {
                    c.CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.4f, ePosition.Local);
                    yield return new WaitForSeconds(0.3f);
                }
            }
            StartCoroutine(SSR());
        }
        else
        {
            //모드 설정만 먼저(카드 에니메이션을 위함)
            foreach (var rp in RemotePlayerUIDrawers)
            {
                if (rp.Target.ID == user1Id)
                {
                    rp.SetSpecialRuleMode();
                }
                else if (rp.Target.ID == user2Id)
                {
                    rp.SetSpecialRuleMode();
                }
                else
                {
                    rp.SetSpecialRuleObserverMode();
                }
            }

            LocalPlayerUIDrawer.SetSpecialRuleMode();

            DelayedFunctionHelper.InvokeDelayed(() =>
            {
                foreach (var rp in RemotePlayerUIDrawers)
                {
                    if (rp.Target.ID == user1Id)
                    {
                        rp.Target.ThisDeck.AddCards(user1Cards.ToArray());
                        cards2Animated.AddRange(user1Cards);
                    }
                    else if (rp.Target.ID == user2Id)
                    {
                        rp.Target.ThisDeck.AddCards(user2Cards.ToArray());
                        cards2Animated.AddRange(user2Cards);
                    }
                }

                if (LocalPlayer.ID == user1Id)
                {
                    LocalPlayer.ThisDeck.AddCards(user1Cards.ToArray());
                    cards2AnimatedLocal.AddRange(user1Cards);
                    foreach (var c in user1Cards)
                        c.CardGameObject.SetFace(true);
                }
                else if (LocalPlayer.ID == user2Id)
                {
                    LocalPlayer.ThisDeck.AddCards(user2Cards.ToArray());
                    cards2AnimatedLocal.AddRange(user2Cards);
                    foreach (var c in user2Cards)
                        c.CardGameObject.SetFace(true);
                }

                foreach (var c in cards2Animated)
                {
                    c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
                    c.CardGameObject.SetFace(false);
                }
                foreach (var c in cards2AnimatedLocal)
                {
                    c.CardGameObject.SetMovementTransformPosition(InGameManager.Instance.DeckTransform.position);
                    c.CardGameObject.SetFace(false);
                }

                IEnumerator SSR()
                {
                    foreach (var c in cards2Animated)
                    {
                        c.CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.4f, ePosition.Local);
                        yield return new WaitForSeconds(0.3f);
                    }
                    foreach (var c in cards2AnimatedLocal)
                    {
                        c.CardGameObject.MoveMovementTransformPosition(Vector3.zero, 0.4f, ePosition.Local);
                        c.CardGameObject.SetFaceAnimated(true, 1.2f, 0.2f);
                        yield return new WaitForSeconds(0.3f);
                    }
                    LocalPlayerUIDrawer.SetSpecialRuleEvents(() =>
                    {
                        //go
                        onGo?.Invoke();
                    },
                    (card) =>
                    {
                        //exchange with deck
                        DelayedFunctionHelper.InvokeDelayed(() =>
                        {
                            onExchangeWithDeck?.Invoke(card);
                        }, 0.8f);
                    },
                    () =>
                    {
                        //exchange with opponent button clicked
                        onExhangeWithOpponentButtonClicked?.Invoke();
                    },
                    (card) =>
                    {
                        //exchange with opponent
                        onExchangeWithOther?.Invoke(card);
                    });
                    }
                    StartCoroutine(SSR());
            }, 1.1f);
         
        }
    }
    internal void StartSpecialRule_ThreeCardMode(Action<Card> selectCard2Delete, int user1Id, int user2Id, List<Card> user1Cards, List<Card> user2Cards, System.Action onGo, System.Action<Card> onExchangeWithDeck, System.Action onExhangeWithOpponentButtonClicked, System.Action<Card> onExchangeWithOther)
    {
        NormalBGM.Stop();
        SpecailBGM.Play();
        if (Mode != eGameMode.ThreeCards)
        {
            Debug.LogError("Cannot start special rule in ThreeCard mode when the game mode is not set to ThreeCards.");
            return;
        }

        foreach (var rp in RemotePlayerUIDrawers)
        {
            rp.Target.ThisDeck.RemoveAllCards();
        }
        LocalPlayer.ThisDeck.RemoveAllCards();
        bool isLocalObserver = (user1Id == LocalPlayer.ID || user2Id == LocalPlayer.ID) ? false : true;
        if (isLocalObserver == true)
        {
            LocalPlayerUIDrawer.SetSpecialRuleObserverMode();
            foreach (var rp in RemotePlayerUIDrawers)
            {
                if (rp.Target.ID == user1Id)
                {
                    rp.Target.ThisDeck.AddCards(user1Cards.ToArray());
                }
                else if (rp.Target.ID == user2Id)
                {
                    rp.Target.ThisDeck.AddCards(user2Cards.ToArray());
                }
            }
        }
        else
        {
            foreach (var rp in RemotePlayerUIDrawers)
            {
                if (rp.Target.ID == user1Id)
                {
                    rp.SetSpecialRuleMode();
                    rp.Target.ThisDeck.AddCards(user1Cards.ToArray());
                }
                else if (rp.Target.ID == user2Id)
                {
                    rp.SetSpecialRuleMode();
                    rp.Target.ThisDeck.AddCards(user2Cards.ToArray());
                }
                else
                {
                    rp.SetSpecialRuleObserverMode();
                }
            }

            LocalPlayerUIDrawer.SetSpecialRuleMode();
            if (LocalPlayer.ID == user1Id)
            {
                LocalPlayer.ThisDeck.AddCards(user1Cards.ToArray());
                foreach (var c in user1Cards)
                    c.CardGameObject.SetFace(true);
            }
            else if (LocalPlayer.ID == user2Id)
            {
                LocalPlayer.ThisDeck.AddCards(user2Cards.ToArray());
                foreach (var c in user2Cards)
                    c.CardGameObject.SetFace(true);
            }

            LocalPlayerUIDrawer.SelectCard2Delete((card) =>
            {
                // Card selected for deletion
                selectCard2Delete(card);
            });

            LocalPlayerUIDrawer.SetSpecialRuleEvents(() =>
            {
                //go
                onGo?.Invoke();
            },
            (card) =>
            {
                //exchange with deck
                onExchangeWithDeck?.Invoke(card);
            },
            () =>
            {
                //exchange with opponent button clicked
                onExhangeWithOpponentButtonClicked?.Invoke();
            },
            (card) =>
            {
                //exchange with opponent
                onExchangeWithOther?.Invoke(card);
            });
        }
    }

    internal void StartNextRound(byte reason)
    {
        foreach(var rp in RemotePlayerUIDrawers)
        {
            rp.SetRPSTextBox(false, eRPS.None);
            rp.SetOrderText(-1);
            rp.SetIOText("");
            rp.ShowGrayPanel(false);
            rp.Go2OriginTransform();
            rp.Target.ThisDeck.RemoveAllCards();
        }
        LocalPlayerUIDrawer.SetRPSTextBox(false, eRPS.None);
        LocalPlayerUIDrawer.SetOrderText(-1);
        LocalPlayerUIDrawer.SetIOText("");
        LocalPlayerUIDrawer.ShowGrayPanel(false);
        LocalPlayerUIDrawer.SetActivePanelOnScreenCenter(false);
        LocalPlayerUIDrawer.Go2OriginTransform();
        LocalPlayerUIDrawer.SpecialRuleButton_Go.gameObject.SetActive(false);
        LocalPlayerUIDrawer.SpecialRuleButton_ExchangeWithDeck.gameObject.SetActive(false);
        LocalPlayerUIDrawer.SpecialRuleButton_ExchangeWithOpponent.gameObject.SetActive(false);

        LocalPlayer.IsOrderDetermined = false;
        LocalPlayer.IO = eIO.None; // Reset IO for local player
        LocalPlayer.Order = -1;
        LocalPlayer.ThisDeck.RemoveAllCards(); // Clear local player's deck

        foreach (var player in RemotePlayers)
        {
            player.Order = -1;
            player.IO = eIO.None; // Reset IO for remote players
        }
        NormalBGM.Play();
        SpecailBGM.Stop();
    }

    internal void ShowRPSRoundResult(int round, int firstPlayerId, eRPS firstPlayerRPS, byte firstPlayerOrder, int secondPlayerId, eRPS secondPlayerRPS, byte secondPlayerOrder, int thirdPlayerId, eRPS thirdPlayerRPS, byte thirdPlayerOrder)
    {
        ShowRPSRoundResult_Selection(firstPlayerId, secondPlayerId, thirdPlayerId, firstPlayerRPS, secondPlayerRPS, thirdPlayerRPS);


        Helpers.DelayedFunctionHelper.InvokeDelayed(() =>
        {
            ShowRPSResult_Order_Accumulated(firstPlayerId, firstPlayerOrder, secondPlayerId, secondPlayerOrder, thirdPlayerId, thirdPlayerOrder);
        }, 1.5f);
    }

    internal void SomeoneSelectedCard2Delete(int playerId)
    {
        foreach(var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == playerId)
            {
                rp.DeleteAnycard();
            }
        }
    }

    internal void ShowCards2Delete(int id1, Card c1, int id2, Card c2, int id3, Card c3)
    {
        if(id1 == LocalPlayer.ID)
        {
            LocalPlayerUIDrawer.ShowCard2Delete(c1);
        }
        else if (id2 == LocalPlayer.ID)
        {
            LocalPlayerUIDrawer.ShowCard2Delete(c2);
        }
        else if (id3 == LocalPlayer.ID)
        {
            LocalPlayerUIDrawer.ShowCard2Delete(c3);
        }

        foreach (var rp in RemotePlayerUIDrawers)
        {
            if (rp.Target.ID == id1)
            {
                rp.ShowCard2Delete(c1);
            }
            else if (rp.Target.ID == id2)
            {
                rp.ShowCard2Delete(c2);
            }
            else if (rp.Target.ID == id3)
            {
                rp.ShowCard2Delete(c3);
            }
        }
    }

    internal void RemoveAllCard2Delete()
    {
        LocalPlayerUIDrawer.RemoveCard2Delete();
        foreach (var rp in RemotePlayerUIDrawers)
        {
            rp.RemoveCard2Delete();
        }
    }
}
