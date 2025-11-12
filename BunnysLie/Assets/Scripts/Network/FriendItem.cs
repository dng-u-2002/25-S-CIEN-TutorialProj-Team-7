using System;
using UnityEngine;
using UnityEngine.UI;

public class FriendItem : MonoBehaviour
{
    [Header("UI Components")]
    public TMPro.TMP_Text nicknameText;
    public TMPro.TMP_Text statusText;
    public Button inviteButton3;
    public Button inviteButton2;
    public Image statusIndicator;
    public TMPro.TMP_Text activityText;
    
    [Header("Status Colors")]
    public Color onlineColor = Color.green;
    public Color offlineColor = Color.gray;
    public Color busyColor = Color.yellow;
    public Color inGameColor = Color.red;
    
    private FriendListManager.FriendInfo friendData;
    
    public void SetupFriend(FriendListManager.FriendInfo friend)
    {
        friendData = friend;
        UpdateFriendDisplay();
        SetupButtons();
    }
    
    void UpdateFriendDisplay()
    {
        //if (friendData == null) return;
        if (nicknameText != null) nicknameText.text = friendData.name;
        UpdateStatusAndActivity();
        UpdateStatusIndicator();
        if (inviteButton2 != null)
            inviteButton2.interactable = friendData.isPlayingThisGame;
        if (inviteButton3 != null)
            inviteButton3.interactable = friendData.isPlayingThisGame;
    }
    
    void UpdateStatusAndActivity()
    {
        if (statusText == null) return;

        //if (friendData.isOnline)
        //{
        switch (friendData.richStatus)
        {
            case "In Lobby":
                statusText.text = "로비";
                statusText.color = onlineColor;
                break;
            case "Playing":
                statusText.text = "게임중";
                statusText.color = inGameColor;
                break;
            case "In Matching":
                statusText.text = "매칭중";
                statusText.color = busyColor;
                break;
        }
        //if (activityText != null) activityText.text = GetActivityDetail();
        //}
        //else
        //{
        //    TimeSpan t = DateTime.Now - friendData.lastSeen;
        //    if (t.TotalMinutes < 60) statusText.text = $"{(int)t.TotalMinutes}분 전";
        //    else if (t.TotalHours < 24) statusText.text = $"{(int)t.TotalHours}시간 전";
        //    else statusText.text = $"{(int)t.TotalDays}일 전";
        //    statusText.color = offlineColor;
        //    if (activityText != null) activityText.text = "";
        //}
    }
    
    void UpdateStatusIndicator()
    {
        if (statusIndicator == null) return;
        if (friendData.isPlayingThisGame)
        {
            switch (friendData.richStatus)
            {
                case "In Lobby":
                    statusIndicator.color = onlineColor; break;
                case "Playing":
                    statusIndicator.color = inGameColor; break;
                case "In Matching":
                    statusIndicator.color = busyColor; break;
            }
        }
        else
        {
            statusIndicator.color = offlineColor;
        }
    }
    
    //string GetActivityDetail()
    //{
    //    switch (friendData.currentActivity)
    //    {
    //        case FriendActivity.InGame:
    //            return !string.IsNullOrEmpty(friendData.currentRoomName) ? $"방: {friendData.currentRoomName}" : "";
    //        case FriendActivity.Matchmaking:
    //            return "상대방을 찾는 중...";
    //        case FriendActivity.InLobby:
    //            return "게임을 시작할 수 있습니다";
    //        default:
    //            return "";
    //    }
    //}
    
    void SetupButtons()
    {
        if (inviteButton2 != null)
        {
            inviteButton2.onClick.RemoveAllListeners();
            inviteButton2.onClick.AddListener(() =>
            {
                GameLobbyManager.Instance.selectedGameType = eGameMode.TwoCards;
                OnInviteButtonClicked();
                });
        }
        if (inviteButton3 != null)
        {
            inviteButton3.onClick.RemoveAllListeners();
            inviteButton3.onClick.AddListener(() =>
            {
                GameLobbyManager.Instance.selectedGameType = eGameMode.ThreeCards;
                OnInviteButtonClicked();
            });
        }
    }
    
    void OnInviteButtonClicked()
    {
        //if (friendData == null || GameLobbyManager.Instance == null) return;
        //if (!friendData.isOnline) { ShowMessage("친구가 오프라인 상태입니다."); return; }
        //if (friendData.currentActivity == FriendActivity.InGame) { ShowMessage("친구가 게임 중입니다."); return; }
        //if (friendData.currentActivity == FriendActivity.Matchmaking) { ShowMessage("친구가 매칭 중입니다."); return; }
        //GameLobbyManager.Instance.InviteFriendToGame(friendData);
        FriendListManager.Instance.InviteFriend(friendData.id);
    }
    
    
    void ShowMessage(string message)
    {
        if (GameLobbyManager.Instance != null) GameLobbyManager.Instance.SendMessage("ShowMessage", message, SendMessageOptions.DontRequireReceiver);
        else Debug.Log(message);
    }
    
    public void UpdateFriendData(FriendListManager.FriendInfo updatedFriend)
    {
        friendData = updatedFriend;
        UpdateFriendDisplay();
    }
}
