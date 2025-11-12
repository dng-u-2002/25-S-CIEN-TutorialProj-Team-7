using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    //InGameManager.Instance.GetUserIDs(), InGameManager.Instance.GetAllCards(), InGameManager.Instance.LocalPlayer.ID, InGameManager.Instance.Mode, RoundCounter

    static bool IsAlreadyOnceLoseExceptSpecialRule = false; //한 라운드에서라도 진 적이 있는가?
    static bool IsAlreadyOnceLoseIncludeSpecialRule = false; //한 라운드에서라도 진 적이 있는가?

    static bool IsExchangedCardWithOpponent = false;
    public static void ResetValuesPerGame()
    {
        IsAlreadyOnceLoseExceptSpecialRule = false;
        IsAlreadyOnceLoseIncludeSpecialRule = false;
    }
    public static void ResetValuesPerRound()
    {
        RPSDrawCount = 0;
        IsExchangedCardWithOpponent = true;
    }
    public static void OnWinTotalGame()
    {
        var users = InGameManager.Instance.GetUserIDs();
        var cards = InGameManager.Instance.GetAllCards();
        var localId = InGameManager.Instance.LocalPlayer.ID;
        var mode = InGameManager.Instance.Mode;
        var rnd = InGameManager.Instance.IsRandomMode;
        var round = InGameUser_PUN.Instance.RoundCounter;

        //한 라운드도 지지 않고 승리하기
        if(IsAlreadyOnceLoseIncludeSpecialRule == false)
        {
            AchievementManager.Unlock(AchievementId.WinNoLoss);
        }

        if(mode == eGameMode.TwoCards)
        {
            AchievementManager.AddToStat(AchievementId.Play2Mode5Games, 1, (AchievementId.Play2Mode5Games, 5));
        }
        if(mode == eGameMode.ThreeCards)
        {
            AchievementManager.AddToStat(AchievementId.Play3Mode5Games, 1, (AchievementId.Play3Mode5Games, 5));
        }
        if(rnd)
        {
            AchievementManager.AddToStat(AchievementId.PlayRandomMode5Games, 1, (AchievementId.PlayRandomMode5Games, 5));
        }
    }
    public static void OnWinRoundGame()
    {
        var users = InGameManager.Instance.GetUserIDs();
        var cards = InGameManager.Instance.GetAllCards();
        var localId = InGameManager.Instance.LocalPlayer.ID;
        var mode = InGameManager.Instance.Mode;
        var round = InGameUser_PUN.Instance.RoundCounter;

        var localCard = cards[localId];

        //10별 족보로 라운드 승리하기
        if (localCard[0].Value == 9 && localCard[1].Value == 9)
        {
            AchievementManager.Unlock(AchievementId.WinPairTen);
        }

        //0달 족보로 라운드 승리하기
        if (localCard[0].Value != localCard[1].Value && (localCard[0].Value + localCard[1].Value + 2) % 10 == 0)
        {
            AchievementManager.Unlock(AchievementId.WinZeroMoon);
        }

        if(InGameManager.Instance.LocalPlayerUIDrawer.GetIOText() == ConstStrings.Text_In) //내가 인
        {
            int loserScore = 0;
            //상대가 아웃
            if(InGameManager.Instance.RemotePlayerUIDrawers[0].GetIOText() == ConstStrings.Text_Out)
            {
                loserScore = InGameServer_PUN.CalculateScore(InGameManager.Instance.RemotePlayerUIDrawers[0].Target.ThisDeck.GetCardsAsList());
            }
            else if(InGameManager.Instance.RemotePlayerUIDrawers[1].GetIOText() == ConstStrings.Text_Out)
            {
                loserScore = InGameServer_PUN.CalculateScore(InGameManager.Instance.RemotePlayerUIDrawers[1].Target.ThisDeck.GetCardsAsList());
            }

            int localScore = InGameServer_PUN.CalculateScore(cards[localId]);
            if(localScore > loserScore)
            {
                AchievementManager.Unlock(AchievementId.WinRoundBiggerCardsThanOutPlayer);
            }
        }
    }
    public static void OnLoseTotalGame()
    {
        var mode = InGameManager.Instance.Mode;
        if (mode == eGameMode.TwoCards)
        {
            AchievementManager.AddToStat(AchievementId.Play2Mode5Games, 1, (AchievementId.Play2Mode5Games, 5));
        }
        if (mode == eGameMode.ThreeCards)
        {
            AchievementManager.AddToStat(AchievementId.Play3Mode5Games, 1, (AchievementId.Play3Mode5Games, 5));
        }
    }
    static int RPSDrawCount = 0;
    public static void OnDrawRPS()
    {
        RPSDrawCount++;
        if(RPSDrawCount >= 5)
        {
            AchievementManager.Unlock(AchievementId.Draw5RPS);
        }
    }
    public static void OnExchangeCardWithOpponent()
    {
        IsExchangedCardWithOpponent = true;
    }
    public static void OnLoseRoundGame()
    {
        IsAlreadyOnceLoseExceptSpecialRule = true;
    }


    public static void OnWinSpecialGame()
    {
        AchievementManager.AddToStat(AchievementId.WinSpecialRule5Times, 1, (AchievementId.WinSpecialRule5Times, 5));


        if(IsExchangedCardWithOpponent == true)
        {
            AchievementManager.Unlock(AchievementId.WinSpecialRuleAfterExhangeCardWithOpponent);
        }
    }
    public static void OnLoseSpecialGame()
    {
        IsAlreadyOnceLoseExceptSpecialRule = true;
        IsAlreadyOnceLoseIncludeSpecialRule = true;

    }

    public static void OnEndTotalGame()
    {

    }

}
