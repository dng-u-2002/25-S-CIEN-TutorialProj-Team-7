using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Chat.Demo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class GameLobbyManager : MonoBehaviourPunCallbacks
{
    public static GameLobbyManager Instance { get; private set; }
    
    [Header("매칭 키 UI")]
    public Button privateRoomButton;
    public Button randomMatchingButton;
    public Button twoCardGameButton;
    public Button threeCardGameButton;
    public Button quickMatchButton;
    
    [Header("설정 키 UI")]
    public Button settingsButton;
    public Button soundButton;
    public Button notificationButton;
    public Button accountButton;
    public Button helpButton;
    public Button friendButton;
    public Button exitButton;
    
    [Header("팝업 패널들")]
    public GameObject privateRoomPanel;
    public GameObject settingsPanel;
    public GameObject soundPanel;
    public GameObject notificationPanel;
    public GameObject accountPanel;
    public GameObject helpPanel;
    public GameObject friendPanel;
    public GameObject exitConfirmPanel;
    public GameObject matchmakingPanel;
    public GameObject friendInvitePanel;
    public GameObject friendRemoveConfirmPanel;
    public GameObject messagePanel;
    
    [Header("비공개 방 UI")]
    public InputField roomCodeInput;
    public Button createPrivateRoomButton;
    public Button joinPrivateRoomButton;
    
    [Header("매칭 중 UI")]
    public Text matchmakingStatusText;
    public Button cancelMatchmakingButton;
    
    [Header("사운드 설정")]
    public Toggle soundToggle;
    public Text soundStatusText;
    
    [Header("알림 설정")]
    public Toggle pushNotificationToggle;
    
    [Header("계정 정보")]
    public Text nicknameText;
    public Text joinDateText;
    public Button googleLoginButton;
    public Button steamLoginButton;
    
    [Header("친구 시스템")]
    public InputField friendCodeInput;
    public Button addFriendButton;
    public Button inviteFriendButton;
    public Transform friendListContent;
    public GameObject friendItemPrefab;
    public Button refreshFriendListButton;
    
    [Header("친구 초대 UI")]
    public Text inviteTargetNameText;
    public Text inviteRoomInfoText;
    public Button sendInviteButton;
    public Button cancelInviteButton;
    
    [Header("친구 삭제 확인 UI")]
    public Text removeTargetNameText;
    public Button confirmRemoveButton;
    public Button cancelRemoveButton;
    
    [Header("메시지 UI")]
    public Text messageText;
    public Button messageOkButton;
    
    public enum GameType
    {
        TwoCard = 2,
        ThreeCard = 3
    }
    
    private GameType selectedGameType = GameType.TwoCard;
    private bool isMatchmaking = false;
    private List<FriendData> friendList = new List<FriendData>();
    private FriendData pendingInviteFriend;
    private FriendData pendingRemoveFriend;
    private Dictionary<string, FriendItem> friendItemDict = new Dictionary<string, FriendItem>();
    
    private float friendActivityUpdateInterval = 10f;
    private float lastFriendActivityUpdate = 0f;
    
    void Awake()
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
    
    void Start()
    {
        CheckLoginStatus();
        if (HTTPLoginManager.Instance != null && !HTTPLoginManager.Instance.IsLoggedIn()) return;
        InitializeUI();
        SetupEventListeners();
        ConnectToPhoton();
        LoadFriendList();
        StartFriendActivitySimulation();
    }
    
    void InitializeUI()
    {
        CloseAllPanels();
        
        bool soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        soundToggle.isOn = soundEnabled;
        UpdateSoundStatus(soundEnabled);
        
        pushNotificationToggle.isOn = PlayerPrefs.GetInt("PushNotification", 1) == 1;
        
        UpdateGameTypeSelection();
        UpdateAccountInfo();
    }
    
    void SetupEventListeners()
    {
        privateRoomButton.onClick.AddListener(() => ShowPrivateRoomPanel());
        randomMatchingButton.onClick.AddListener(() => StartRandomMatching());
        twoCardGameButton.onClick.AddListener(() => SelectGameType(GameType.TwoCard));
        threeCardGameButton.onClick.AddListener(() => SelectGameType(GameType.ThreeCard));
        quickMatchButton.onClick.AddListener(() => StartQuickMatch());
        
        createPrivateRoomButton.onClick.AddListener(() => CreatePrivateRoom());
        joinPrivateRoomButton.onClick.AddListener(() => JoinPrivateRoom());
        
        cancelMatchmakingButton.onClick.AddListener(() => CancelMatching());
        
        settingsButton.onClick.AddListener(() => ShowSettingsPanel());
        soundButton.onClick.AddListener(() => ShowSoundPanel());
        notificationButton.onClick.AddListener(() => ShowNotificationPanel());
        accountButton.onClick.AddListener(() => ShowAccountPanel());
        helpButton.onClick.AddListener(() => ShowHelpPanel());
        friendButton.onClick.AddListener(() => ShowFriendPanel());
        exitButton.onClick.AddListener(() => ShowExitConfirmation());
        
        soundToggle.onValueChanged.AddListener(SetSoundEnabled);
        pushNotificationToggle.onValueChanged.AddListener(SetPushNotification);
        
        googleLoginButton.onClick.AddListener(() => LoginWithGoogle());
        steamLoginButton.onClick.AddListener(() => LoginWithSteam());
        
        addFriendButton.onClick.AddListener(() => AddFriend());
        refreshFriendListButton.onClick.AddListener(() => RefreshFriendList());
        
        sendInviteButton.onClick.AddListener(() => SendFriendInvite());
        cancelInviteButton.onClick.AddListener(() => CancelFriendInvite());
        
        confirmRemoveButton.onClick.AddListener(() => ConfirmRemoveFriend());
        cancelRemoveButton.onClick.AddListener(() => CancelRemoveFriend());
        
        messageOkButton.onClick.AddListener(() => CloseMessage());
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
        UpdateFriendActivitySimulation();
    }
    
    void HandleBackButton()
    {
        if (AnyPanelOpen())
        {
            CloseAllPanels();
        }
        else
        {
            ShowExitConfirmation();
        }
    }
    
    void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        UpdateMyActivityStatus(FriendActivity.InLobby);
    }
    
    void SelectGameType(GameType gameType)
    {
        selectedGameType = gameType;
        UpdateGameTypeSelection();
        
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.Mode = (InGameManager.eGameMode)selectedGameType;
        }
    }
    
    void UpdateGameTypeSelection()
    {
        Color selectedColor = Color.green;
        Color normalColor = Color.white;
        
        twoCardGameButton.GetComponent<Image>().color = 
            selectedGameType == GameType.TwoCard ? selectedColor : normalColor;
        threeCardGameButton.GetComponent<Image>().color = 
            selectedGameType == GameType.ThreeCard ? selectedColor : normalColor;
    }
    
    void StartRandomMatching()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        isMatchmaking = true;
        ShowMatchmakingPanel();
        UpdateMyActivityStatus(FriendActivity.Matchmaking);
        
        var roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["IsPrivate"] = false;
        
        PhotonNetwork.JoinRandomRoom(roomProps, 3);
    }
    
    void StartQuickMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        isMatchmaking = true;
        ShowMatchmakingPanel();
        UpdateMyActivityStatus(FriendActivity.Matchmaking);
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        var roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["IsPrivate"] = false;
        
        var roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            IsVisible = true,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode", "IsPrivate" }
        };
        
        string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnJoinedRoom()
    {
        if (isMatchmaking)
        {
            isMatchmaking = false;
            UpdateMyActivityStatus(FriendActivity.InGame, PhotonNetwork.CurrentRoom.Name);
            
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.RoomID = PhotonNetwork.CurrentRoom.Name.GetHashCode();
            }
            
            if (matchmakingPanel != null) matchmakingPanel.SetActive(true);
            if (matchmakingStatusText != null)
                matchmakingStatusText.text = $"플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
            
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
            {
                CloseAllPanels();
                LoadGameScene();
            }
        }
    }
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            CloseAllPanels();
            LoadGameScene();
        }
        else
        {
            if (matchmakingStatusText != null)
                matchmakingStatusText.text = $"플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
        }
    }
    
    void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
    
    void CreatePrivateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        string roomCode = GenerateRoomCode();
        if (roomCodeInput != null) roomCodeInput.text = roomCode;
        
        var roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["IsPrivate"] = true;
        
        var roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            IsVisible = false,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode", "IsPrivate" }
        };
        
        PhotonNetwork.CreateRoom(roomCode, roomOptions);
    }
    
    void JoinPrivateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        string roomCode = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : "";
        if (string.IsNullOrEmpty(roomCode))
        {
            return;
        }
        
        PhotonNetwork.JoinRoom(roomCode);
    }
    
    string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return new string(code);
    }
    
    void CancelMatching()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        
        isMatchmaking = false;
        UpdateMyActivityStatus(FriendActivity.InLobby);
        CloseAllPanels();
    }
    
    void LoadFriendList()
    {
        friendList.Clear();
        string saved = PlayerPrefs.GetString("FriendList", "");
        if (!string.IsNullOrEmpty(saved))
        {
            foreach (var code in saved.Split(','))
            {
                if (string.IsNullOrWhiteSpace(code)) continue;
                friendList.Add(new FriendData
                {
                    nickname = "Friend_" + code,
                    friendCode = code,
                    isOnline = false,
                    lastSeen = DateTime.Now.AddMinutes(-UnityEngine.Random.Range(10, 120)),
                    currentActivity = FriendActivity.Idle
                });
            }
            UpdateFriendList();
            return;
        }
        AddSampleFriends();
        UpdateFriendList();
    }
    
    void AddSampleFriends()
    {
        string[] sampleNames = { "철수", "영희", "민수", "지영", "준호" };
        
        for (int i = 0; i < sampleNames.Length; i++)
        {
            FriendData friend = new FriendData
            {
                nickname = sampleNames[i],
                friendCode = "F" + (1000 + i).ToString(),
                isOnline = UnityEngine.Random.value > 0.3f,
                lastSeen = DateTime.Now.AddMinutes(-UnityEngine.Random.Range(1, 1440)),
                currentActivity = FriendActivity.Idle
            };
            
            if (friend.isOnline)
            {
                Array activities = System.Enum.GetValues(typeof(FriendActivity));
                friend.currentActivity = (FriendActivity)activities.GetValue(
                    UnityEngine.Random.Range(0, activities.Length));
                    
                if (friend.currentActivity == FriendActivity.InGame)
                {
                    friend.currentRoomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
                }
            }
            
            friendList.Add(friend);
        }
    }
    
    void AddFriend()
    {
        string friendCode = friendCodeInput != null ? friendCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(friendCode))
        {
            ShowMessage("친구 코드를 입력해주세요.");
            return;
        }
        
        if (friendList.Any(f => f.friendCode.Equals(friendCode, StringComparison.OrdinalIgnoreCase)))
        {
            ShowMessage("이미 추가된 친구입니다.");
            return;
        }
        
        FriendData newFriend = new FriendData
        {
            nickname = "Friend_" + friendCode,
            friendCode = friendCode,
            isOnline = UnityEngine.Random.value > 0.5f,
            lastSeen = DateTime.Now,
            currentActivity = FriendActivity.Idle
        };
        
        friendList.Add(newFriend);
        if (friendCodeInput != null) friendCodeInput.text = "";
        UpdateFriendList();
        
        ShowMessage($"{newFriend.nickname}님이 친구로 추가되었습니다.");
    }
    
    void RefreshFriendList()
    {
        foreach (var friend in friendList)
        {
            if (UnityEngine.Random.value < 0.3f)
            {
                friend.isOnline = !friend.isOnline;
                if (!friend.isOnline)
                {
                    friend.lastSeen = DateTime.Now;
                    friend.currentActivity = FriendActivity.Idle;
                    friend.currentRoomName = "";
                }
                else
                {
                    Array activities = System.Enum.GetValues(typeof(FriendActivity));
                    friend.currentActivity = (FriendActivity)activities.GetValue(
                        UnityEngine.Random.Range(0, activities.Length));
                }
            }
        }
        
        UpdateFriendList();
        ShowMessage("친구 목록이 새로고침되었습니다.");
    }
    
    void UpdateFriendList()
    {
        if (friendListContent != null)
        {
            foreach (Transform child in friendListContent)
            {
                Destroy(child.gameObject);
            }
        }
        friendItemDict.Clear();
        
        var sortedFriends = friendList.OrderByDescending(f => f.isOnline)
                                     .ThenBy(f => f.nickname)
                                     .ToList();
        
        foreach (FriendData friend in sortedFriends)
        {
            if (friendItemPrefab != null && friendListContent != null)
            {
                GameObject friendItem = Instantiate(friendItemPrefab, friendListContent);
                FriendItem friendItemScript = friendItem.GetComponent<FriendItem>();
                if (friendItemScript != null)
                {
                    friendItemScript.SetupFriend(friend);
                    friendItemDict[friend.friendCode] = friendItemScript;
                }
            }
        }
    }
    
    public void InviteFriendToGame(FriendData friend)
    {
        if (friend == null || !friend.isOnline)
        {
            ShowMessage("친구가 온라인 상태가 아닙니다.");
            return;
        }
        
        if (friend.currentActivity == FriendActivity.InGame)
        {
            ShowMessage("친구가 이미 게임 중입니다.");
            return;
        }
        
        if (friend.currentActivity == FriendActivity.Matchmaking)
        {
            ShowMessage("친구가 매칭 중입니다.");
            return;
        }
        
        ShowFriendInvitePanel(friend);
    }
    
    void ShowFriendInvitePanel(FriendData friend)
    {
        pendingInviteFriend = friend;
        
        if (inviteTargetNameText != null)
        {
            inviteTargetNameText.text = friend.nickname;
        }
        
        if (inviteRoomInfoText != null)
        {
            inviteRoomInfoText.text = $"게임 모드: {selectedGameType}\n함께 게임하시겠습니까?";
        }
        
        SafeSet(friendInvitePanel, true);
    }
    
    void SendFriendInvite()
    {
        if (pendingInviteFriend != null)
        {
            bool accepted = UnityEngine.Random.value < 0.7f;
            if (accepted)
            {
                ShowMessage($"{pendingInviteFriend.nickname}님이 초대를 수락했습니다!\n잠시 후 게임이 시작됩니다.");
                StartPrivateGameWithFriend(pendingInviteFriend);
            }
            else
            {
                ShowMessage($"{pendingInviteFriend.nickname}님이 초대를 거절했습니다.");
            }
        }
        
        SafeSet(friendInvitePanel, false);
        pendingInviteFriend = null;
    }
    
    void CancelFriendInvite()
    {
        SafeSet(friendInvitePanel, false);
        pendingInviteFriend = null;
    }
    
    void StartPrivateGameWithFriend(FriendData friend)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        string roomCode = GenerateRoomCode();
        
        var roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["IsPrivate"] = true;
        roomProps["InvitedFriend"] = friend.friendCode;
        
        var roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            IsVisible = false,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode", "IsPrivate" }
        };
        
        PhotonNetwork.CreateRoom(roomCode, roomOptions);
        
        friend.currentActivity = FriendActivity.InGame;
        friend.currentRoomName = roomCode;
        UpdateFriendItemDisplay(friend);
    }
    
    public void ShowFriendRemoveConfirmation(FriendData friend)
    {
        pendingRemoveFriend = friend;
        
        if (removeTargetNameText != null)
        {
            removeTargetNameText.text = $"{friend.nickname}님을 친구에서 삭제하시겠습니까?";
        }
        
        SafeSet(friendRemoveConfirmPanel, true);
    }
    
    void ConfirmRemoveFriend()
    {
        if (pendingRemoveFriend != null)
        {
            RemoveFriend(pendingRemoveFriend);
            ShowMessage($"{pendingRemoveFriend.nickname}님이 친구 목록에서 삭제되었습니다.");
        }
        
        SafeSet(friendRemoveConfirmPanel, false);
        pendingRemoveFriend = null;
    }
    
    void CancelRemoveFriend()
    {
        SafeSet(friendRemoveConfirmPanel, false);
        pendingRemoveFriend = null;
    }
    
    public void RemoveFriend(FriendData friend)
    {
        friendList.Remove(friend);
        SaveFriendList();
        UpdateFriendList();
    }
    
    void SaveFriendList()
    {
        string friendCodes = string.Join(",", friendList.Select(f => f.friendCode));
        PlayerPrefs.SetString("FriendList", friendCodes);
        PlayerPrefs.Save();
    }
    
    void StartFriendActivitySimulation()
    {
        lastFriendActivityUpdate = Time.time;
    }
    
    void UpdateFriendActivitySimulation()
    {
        if (Time.time - lastFriendActivityUpdate > friendActivityUpdateInterval)
        {
            SimulateFriendActivityChanges();
            lastFriendActivityUpdate = Time.time;
        }
    }
    
    void SimulateFriendActivityChanges()
    {
        foreach (var friend in friendList)
        {
            if (!friend.isOnline) continue;
            
            if (UnityEngine.Random.value < 0.2f)
            {
                Array activities = System.Enum.GetValues(typeof(FriendActivity));
                FriendActivity newActivity = (FriendActivity)activities.GetValue(
                    UnityEngine.Random.Range(0, activities.Length));
                
                string roomName = "";
                if (newActivity == FriendActivity.InGame)
                {
                    roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
                }
                
                friend.currentActivity = newActivity;
                friend.currentRoomName = roomName;
                
                UpdateFriendItemDisplay(friend);
            }
        }
    }
    
    void UpdateFriendItemDisplay(FriendData friend)
    {
        if (friendItemDict.ContainsKey(friend.friendCode))
        {
            friendItemDict[friend.friendCode].UpdateFriendData(friend);
        }
    }
    
    void UpdateMyActivityStatus(FriendActivity activity, string roomName = "")
    {
        var props = new ExitGames.Client.Photon.Hashtable
        {
            ["Activity"] = (int)activity,
            ["RoomName"] = roomName
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }
        else
        {
            Debug.Log(message);
        }
    }
    
    void CloseMessage()
    {
        SafeSet(messagePanel, false);
    }
    
    void ShowPrivateRoomPanel()
    {
        CloseAllPanels();
        SafeSet(privateRoomPanel, true);
    }
    
    void ShowMatchmakingPanel()
    {
        CloseAllPanels();
        SafeSet(matchmakingPanel, true);
        if (matchmakingStatusText != null) matchmakingStatusText.text = "매칭 중...";
    }
    
    void ShowSettingsPanel()
    {
        CloseAllPanels();
        SafeSet(settingsPanel, true);
    }
    
    void ShowSoundPanel()
    {
        CloseAllPanels();
        SafeSet(soundPanel, true);
    }
    
    void ShowNotificationPanel()
    {
        CloseAllPanels();
        SafeSet(notificationPanel, true);
    }
    
    void ShowAccountPanel()
    {
        CloseAllPanels();
        SafeSet(accountPanel, true);
        UpdateAccountInfo();
    }
    
    void ShowHelpPanel()
    {
        CloseAllPanels();
        SafeSet(helpPanel, true);
    }
    
    void ShowFriendPanel()
    {
        CloseAllPanels();
        SafeSet(friendPanel, true);
        UpdateFriendList();
    }
    
    void ShowExitConfirmation()
    {
        CloseAllPanels();
        SafeSet(exitConfirmPanel, true);
    }
    
    void CloseAllPanels()
    {
        SafeSet(privateRoomPanel, false);
        SafeSet(settingsPanel, false);
        SafeSet(soundPanel, false);
        SafeSet(notificationPanel, false);
        SafeSet(accountPanel, false);
        SafeSet(helpPanel, false);
        SafeSet(friendPanel, false);
        SafeSet(exitConfirmPanel, false);
        SafeSet(matchmakingPanel, false);
        SafeSet(friendInvitePanel, false);
        SafeSet(friendRemoveConfirmPanel, false);
        SafeSet(messagePanel, false);
    }
    
    bool AnyPanelOpen()
    {
        return (privateRoomPanel != null && privateRoomPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf) ||
               (soundPanel != null && soundPanel.activeSelf) ||
               (notificationPanel != null && notificationPanel.activeSelf) ||
               (accountPanel != null && accountPanel.activeSelf) ||
               (helpPanel != null && helpPanel.activeSelf) ||
               (friendPanel != null && friendPanel.activeSelf) ||
               (exitConfirmPanel != null && exitConfirmPanel.activeSelf) ||
               (matchmakingPanel != null && matchmakingPanel.activeSelf) ||
               (friendInvitePanel != null && friendInvitePanel.activeSelf) ||
               (friendRemoveConfirmPanel != null && friendRemoveConfirmPanel.activeSelf) ||
               (messagePanel != null && messagePanel.activeSelf);
    }
    
    void SafeSet(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
    
    void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt("SoundEnabled", enabled ? 1 : 0);
        UpdateSoundStatus(enabled);
        AudioListener.volume = enabled ? 1.0f : 0.0f;
    }
    
    void UpdateSoundStatus(bool enabled)
    {
        if (soundStatusText != null)
        {
            soundStatusText.text = enabled ? "켜짐" : "꺼짐";
        }
    }
    
    void SetPushNotification(bool enabled)
    {
        PlayerPrefs.SetInt("PushNotification", enabled ? 1 : 0);
    }
    
    void UpdateAccountInfo()
    {
        if (HTTPLoginManager.Instance != null && HTTPLoginManager.Instance.IsLoggedIn())
        {
            if (nicknameText != null) nicknameText.text = HTTPLoginManager.Instance.GetLoggedInUserName();
            if (joinDateText != null) joinDateText.text = PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
        }
        else
        {
            if (nicknameText != null) nicknameText.text = PhotonNetwork.NickName;
            if (joinDateText != null) joinDateText.text = PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
    
    void CheckLoginStatus()
    {
        if (HTTPLoginManager.Instance != null && !HTTPLoginManager.Instance.IsLoggedIn())
        {
            HTTPLoginManager.Instance.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }
    }
    
    public void OnLoginSuccess()
    {
        gameObject.SetActive(true);
        UpdateAccountInfo();
    }
    
    void LoginWithGoogle()
    {
    }
    
    void LoginWithSteam()
    {
    }
    
    public void ExitGame()
    {
        PlayerPrefs.Save();
        
        if (HTTPLoginManager.Instance != null)
        {
            HTTPLoginManager.Instance.Logout();
        }
        
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        
        Application.Quit();
    }
    
    public void CancelExit()
    {
        CloseAllPanels();
    }
}

public enum FriendActivity
{
    Idle,
    InLobby,
    Matchmaking,
    InGame
}

[System.Serializable]
public class FriendData
{
    public string nickname;
    public string friendCode;
    public bool isOnline;
    public DateTime lastSeen;
    public FriendActivity currentActivity = FriendActivity.Idle;
    public string currentRoomName = "";
}
