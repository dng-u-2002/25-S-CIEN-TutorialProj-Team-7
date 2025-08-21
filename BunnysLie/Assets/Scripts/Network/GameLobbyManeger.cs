using System;
using System.Collections.Generic;
using Photon.Chat.Demo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

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
    
    public enum GameType
    {
        TwoCard = 2,
        ThreeCard = 3
    }
    
    private GameType selectedGameType = GameType.TwoCard;
    private bool isMatchmaking = false;
    private List<FriendData> friendList = new List<FriendData>();
    
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
        InitializeUI();
        SetupEventListeners();
        ConnectToPhoton();
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
        inviteFriendButton.onClick.AddListener(() => InviteFriend());
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
        
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }
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
        Debug.Log("로비에 참가함");
    }
    
    void SelectGameType(GameType gameType)
    {
        selectedGameType = gameType;
        UpdateGameTypeSelection();
        
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.Mode = (eGameMode)selectedGameType;
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
        
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode" }
        };
        
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
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode" }
        };
        
        string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnJoinedRoom()
    {
        if (isMatchmaking)
        {
            isMatchmaking = false;
            CloseAllPanels();
            
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.RoomID = PhotonNetwork.CurrentRoom.Name.GetHashCode();
            }
            
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
            {
                LoadGameScene();
            }
            else
            {
                matchmakingStatusText.text = $"플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
            }
        }
    }
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            LoadGameScene();
        }
        else
        {
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
        roomCodeInput.text = roomCode;
        
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["IsPrivate"] = true;
        
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
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
        
        string roomCode = roomCodeInput.text.Trim().ToUpper();
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
        CloseAllPanels();
    }
    
    void ShowPrivateRoomPanel()
    {
        CloseAllPanels();
        privateRoomPanel.SetActive(true);
    }
    
    void ShowMatchmakingPanel()
    {
        CloseAllPanels();
        matchmakingPanel.SetActive(true);
        matchmakingStatusText.text = "매칭 중...";
    }
    
    void ShowSettingsPanel()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
    }
    
    void ShowSoundPanel()
    {
        CloseAllPanels();
        soundPanel.SetActive(true);
    }
    
    void ShowNotificationPanel()
    {
        CloseAllPanels();
        notificationPanel.SetActive(true);
    }
    
    void ShowAccountPanel()
    {
        CloseAllPanels();
        accountPanel.SetActive(true);
        UpdateAccountInfo();
    }
    
    void ShowHelpPanel()
    {
        CloseAllPanels();
        helpPanel.SetActive(true);
    }
    
    void ShowFriendPanel()
    {
        CloseAllPanels();
        friendPanel.SetActive(true);
        UpdateFriendList();
    }
    
    void ShowExitConfirmation()
    {
        CloseAllPanels();
        exitConfirmPanel.SetActive(true);
    }
    
    void CloseAllPanels()
    {
        privateRoomPanel.SetActive(false);
        settingsPanel.SetActive(false);
        soundPanel.SetActive(false);
        notificationPanel.SetActive(false);
        accountPanel.SetActive(false);
        helpPanel.SetActive(false);
        friendPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);
        matchmakingPanel.SetActive(false);
    }
    
    bool AnyPanelOpen()
    {
        return privateRoomPanel.activeSelf || settingsPanel.activeSelf || 
               soundPanel.activeSelf || notificationPanel.activeSelf ||
               accountPanel.activeSelf || helpPanel.activeSelf ||
               friendPanel.activeSelf || exitConfirmPanel.activeSelf ||
               matchmakingPanel.activeSelf;
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
            nicknameText.text = HTTPLoginManager.Instance.GetLoggedInUserName();
            joinDateText.text = PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
        }
        else
        {
            nicknameText.text = PhotonNetwork.NickName;
            joinDateText.text = PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("yyyy-MM-dd"));
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
        Debug.Log("Google 로그인 시도");
    }
    
    void LoginWithSteam()
    {
        Debug.Log("Steam 로그인 시도");
    }
    
    void AddFriend()
    {
        string friendCode = friendCodeInput.text.Trim();
        if (!string.IsNullOrEmpty(friendCode))
        {
            FriendData newFriend = new FriendData
            {
                nickname = "Friend_" + friendCode,
                friendCode = friendCode,
                isOnline = UnityEngine.Random.value > 0.5f,
                lastSeen = DateTime.Now
            };
            
            friendList.Add(newFriend);
            friendCodeInput.text = "";
            UpdateFriendList();
        }
    }
    
    void InviteFriend()
    {
        Debug.Log("친구 초대");
    }
    
    void UpdateFriendList()
    {
        foreach (Transform child in friendListContent)
        {
            Destroy(child.gameObject);
        }
        
        foreach (FriendData friend in friendList)
        {
            if (friendItemPrefab != null)
            {
                GameObject friendItem = Instantiate(friendItemPrefab, friendListContent);
                FriendItem friendItemScript = friendItem.GetComponent<FriendItem>();
                if (friendItemScript != null)
                {
                    friendItemScript.SetupFriend(friend);
                }
            }
        }
    }
    
    public void RemoveFriend(FriendData friend)
    {
        friendList.Remove(friend);
        UpdateFriendList();
    }
    
    public void InviteFriendToGame(FriendData friend)
    {
        if (friend.isOnline)
        {
            Debug.Log($"게임 초대: {friend.nickname}");
        }
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

[System.Serializable]
public class FriendData
{
    public string nickname;
    public string friendCode;
    public bool isOnline;
    public DateTime lastSeen;
}