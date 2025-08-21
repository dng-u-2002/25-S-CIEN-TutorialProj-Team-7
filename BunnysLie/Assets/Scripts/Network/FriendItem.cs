using System;
using UnityEngine;
using UnityEngine.UI;

public class FriendItem : MonoBehaviour
{
    [Header("UI Components")]
    public Text nicknameText;
    public Text statusText;
    public Button inviteButton;
    public Button removeButton;
    public Image statusIndicator;
    
    [Header("Status Colors")]
    public Color onlineColor = Color.green;
    public Color offlineColor = Color.gray;
    
    private FriendData friendData;
    
    public void SetupFriend(FriendData friend)
    {
        friendData = friend;
        UpdateFriendDisplay();
        SetupButtons();
    }
    
    void UpdateFriendDisplay()
    {
        if (friendData == null) return;
        
        // 닉네임 설정
        if (nicknameText != null)
        {
            nicknameText.text = friendData.nickname;
        }
        
        // 상태 텍스트 설정
        if (statusText != null)
        {
            if (friendData.isOnline)
            {
                statusText.text = "온라인";
                statusText.color = onlineColor;
            }
            else
            {
                TimeSpan timeSinceLastSeen = DateTime.Now - friendData.lastSeen;
                if (timeSinceLastSeen.TotalMinutes < 60)
                {
                    statusText.text = $"{(int)timeSinceLastSeen.TotalMinutes}분 전";
                }
                else if (timeSinceLastSeen.TotalHours < 24)
                {
                    statusText.text = $"{(int)timeSinceLastSeen.TotalHours}시간 전";
                }
                else
                {
                    statusText.text = $"{(int)timeSinceLastSeen.TotalDays}일 전";
                }
                statusText.color = offlineColor;
            }
        }
        
        // 상태 인디케이터 설정
        if (statusIndicator != null)
        {
            statusIndicator.color = friendData.isOnline ? onlineColor : offlineColor;
        }
        
        // 초대 버튼 상태 설정
        if (inviteButton != null)
        {
            inviteButton.interactable = friendData.isOnline;
        }
    }
    
    void SetupButtons()
    {
        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.onClick.AddListener(OnInviteButtonClicked);
        }
        
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }
    }
    
    void OnInviteButtonClicked()
    {
        if (friendData != null && GameLobbyManager.Instance != null)
        {
            GameLobbyManager.Instance.InviteFriendToGame(friendData);
        }
    }
    
    void OnRemoveButtonClicked()
    {
        if (friendData != null && GameLobbyManager.Instance != null)
        {
            // 삭제 확인 다이얼로그를 표시할 수도 있습니다
            GameLobbyManager.Instance.RemoveFriend(friendData);
        }
    }
    
    // 친구 데이터 업데이트 (온라인 상태 변경 등)
    public void UpdateFriendData(FriendData updatedFriend)
    {
        friendData = updatedFriend;
        UpdateFriendDisplay();
    }
    
    // 친구가 온라인/오프라인 상태가 변경될 때 호출
    public void SetOnlineStatus(bool isOnline)
    {
        if (friendData != null)
        {
            friendData.isOnline = isOnline;
            if (!isOnline)
            {
                friendData.lastSeen = DateTime.Now;
            }
            UpdateFriendDisplay();
        }
    }
}