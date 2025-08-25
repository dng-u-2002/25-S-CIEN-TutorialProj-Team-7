using System;
using System.Collections.Generic;
using Photon.Chat.Demo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;

public class GameLobbyManager : MonoBehaviourPunCallbacks
{
    public static GameLobbyManager Instance { get; private set; }
    
    [Header("매칭 키 UI")]
    public Button privateRoomButton;
    public Button quickMatchButton;
    public Button selectRandomMatchingModeButton;
    //public Button randomMatchingButton;
    public Button twoCardGameButton;
    public Button threeCardGameButton;
    public Button enterPrivateGameButton;
    public Button[] BackButtons;

    public Transform ModeSelectButtons;
    public Transform RandomMatchingButtons;
    public Transform PrivateMatchingButtons;

    [Header("설정 키 UI")]
    public Button settingsButton;
    public Button soundButton;
    public Button accountButton;
    public Button helpButton;
    public Button exitButton;
    
    [Header("팝업 패널들")]
    public GameObject privateRoomPanel;
    public GameObject settingsPanel;
    public GameObject soundPanel;
    public GameObject accountPanel;
    public GameObject helpPanel;
    public GameObject friendPanel;
    public GameObject exitConfirmPanel;
    public GameObject matchmakingPanel;
    public GameObject readyPanel;
    public TMPro.TMP_Text readyPanelText;

    [Header("비공개 방 UI")]
    public TMPro.TMP_InputField roomCodeInput;
    public Button createPrivateRoomButton_TwoCards;
    public Button createPrivateRoomButton_ThreeCards;
    public Button joinPrivateRoomButton;
    
    [Header("매칭 중 UI")]
    public TMPro.TMP_Text matchmakingStatusText;
    public Button cancelMatchmakingButton;
    public Button cancelMatchmakingButton2;

    [Header("사운드 설정")]
    public Toggle soundToggle;
    public TMPro.TMP_Text soundStatusText;
    
    [Header("알림 설정")]
    public Toggle pushNotificationToggle;

    [Header("계정 정보")]
    public AccountSettingPanel AccountPanel;
    public Button googleLoginButton;
    public Button steamLoginButton;
    
    [Header("친구 시스템")]
    public TMPro.TMP_InputField friendCodeInput;
    public Button addFriendButton;
    public Button inviteFriendButton;
    public Transform friendListContent;
    public GameObject friendItemPrefab;
    
    private eGameMode selectedGameType = eGameMode.TwoCards;
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
    void ShowPrivateMathcingButtons()
    {
        RandomMatchingButtons.gameObject.SetActive(false);
        PrivateMatchingButtons.gameObject.SetActive(true);
        ModeSelectButtons.gameObject.SetActive(false);
    }
    void SetupEventListeners()
    {
        privateRoomButton.onClick.AddListener(() => ShowPrivateMathcingButtons());
        enterPrivateGameButton.onClick.AddListener(() => ShowPrivateRoomPanel());
        foreach(var bb in BackButtons)
        {
            bb.onClick.AddListener(() => CloseAllPanels());
        }
        //randomMatchingButton.onClick.AddListener(() => StartRandomMatching());
        selectRandomMatchingModeButton.onClick.AddListener(() => ShowRandomMatchingModeButtons());
        twoCardGameButton.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.TwoCards);
            StartRandomMatching();
        });
        threeCardGameButton.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.ThreeCards);
            StartRandomMatching();
        });
        quickMatchButton.onClick.AddListener(() => StartQuickMatch());

        createPrivateRoomButton_TwoCards.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.TwoCards);
            CreatePrivateRoom();
        });
        createPrivateRoomButton_ThreeCards.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.ThreeCards);
            CreatePrivateRoom();
        });
        joinPrivateRoomButton.onClick.AddListener(() => JoinPrivateRoom());
        
        cancelMatchmakingButton.onClick.AddListener(() => CancelMatching());
        cancelMatchmakingButton2.onClick.AddListener(() => CancelMatching());

        settingsButton.onClick.AddListener(() => ShowSoundPanel());
        soundButton.onClick.AddListener(() => ShowSoundPanel());
        accountButton.onClick.AddListener(() => ShowAccountPanel());
        helpButton.onClick.AddListener(() => ShowHelpPanel());
        //friendButton.onClick.AddListener(() => ShowFriendPanel());
        exitButton.onClick.AddListener(() => ShowExitConfirmation());
        
        soundToggle.onValueChanged.AddListener(SetSoundEnabled);
        pushNotificationToggle.onValueChanged.AddListener(SetPushNotification);
        
        googleLoginButton.onClick.AddListener(() => LoginWithGoogle());
        steamLoginButton.onClick.AddListener(() => LoginWithSteam());
        
        addFriendButton.onClick.AddListener(() => AddFriend());
        inviteFriendButton.onClick.AddListener(() => InviteFriend());


        RandomMatchingButtons.gameObject.SetActive(false);
        PrivateMatchingButtons.gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(false);
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


        if(PhotonNetwork.InRoom)
        {
            if(PhotonNetwork.CurrentRoom.PlayerCount >= 3 && IsSceneLoadingStarted == false)
            {
                Debug.Log("방에 충분한 플레이어가 있습니다. 게임 시작 중...");
                LoadGameScene();
            }
        }
    }

    bool IsSceneLoadingStarted;
    
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
            PhotonNetwork.GameVersion = "0.1.0";
            PhotonNetwork.AutomaticallySyncScene = false;

            // 고정 리전 지정 (둘 다 같은 값으로!)
            var app = PhotonNetwork.PhotonServerSettings.AppSettings;
            app.FixedRegion = "asia";      // 필요 시 "kr" 등 팀이 쓰는 실제 코드로 통일
                                           // UDP가 막힌 환경 회피 (선택)
            app.Protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        Debug.Log("로비에 참가함");
    }


    void SelectGameType(eGameMode gameType)
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
            selectedGameType == eGameMode.TwoCards ? selectedColor : normalColor;
        threeCardGameButton.GetComponent<Image>().color = 
            selectedGameType == eGameMode.ThreeCards ? selectedColor : normalColor;
    }

    void ShowRandomMatchingModeButtons()
    {
        CloseAllPanels();

        RandomMatchingButtons.gameObject.SetActive(true);
        PrivateMatchingButtons.gameObject.SetActive(false);
        ModeSelectButtons.gameObject.SetActive(false);

        Color selectedColor = Color.green;
        Color normalColor = Color.white;

        twoCardGameButton.GetComponent<Image>().color =
            normalColor;
        threeCardGameButton.GetComponent<Image>().color =
            normalColor;
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
        base.OnJoinRandomFailed(returnCode, message);
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        
        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode" }
        };
        string roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);
        Debug.Log($"매칭 실패, 새로운 방 생성: {roomName}");
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }
    
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
            CloseAllPanels();
        ModeSelectButtons.gameObject.SetActive(false);
        if (isMatchmaking)
        {
            isMatchmaking = false;

        }
        Debug.Log(($"방에 참가함: {PhotonNetwork.CurrentRoom.Name}"));

        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.RoomID = PhotonNetwork.CurrentRoom.Name.GetHashCode();
        }
        readyPanel.transform.localPosition = Vector3.zero;
        readyPanel.gameObject.SetActive(true);
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            //LoadGameScene();
        }
        else
        {
            StartCoroutine(_ShowPlayerCounter());
        }
    }

    IEnumerator _ShowPlayerCounter()
    {
        while (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < 3)
        {
            readyPanelText.text = "방 코드 : " + PhotonNetwork.CurrentRoom.Name + $"\n플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
            yield return new WaitForSeconds(1f);
        }
    }


    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            //LoadGameScene();
        }
        else
        {
            matchmakingStatusText.text = "방 코드 : " + PhotonNetwork.CurrentRoom.Name + $"\n플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
        }
    }
    
    void LoadGameScene()
    {
        IsSceneLoadingStarted = true;
        //if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MainPlayScene");
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
        Debug.Log($"비공개 방 참가 시도: {roomCode}");
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
            Debug.Log("방에서 나감");
        }
        isMatchmaking = false;
        CloseAllPanels();
    }
    
    void ShowPrivateRoomPanel()
    {
        CloseAllPanels();
        ModeSelectButtons.gameObject.SetActive(false);
        privateRoomButton.gameObject.SetActive(false);
        quickMatchButton.gameObject.SetActive(false);
        selectRandomMatchingModeButton.gameObject.SetActive(false);
        privateRoomPanel.transform.localPosition = Vector3.zero;
        privateRoomPanel.SetActive(true);
    }
    
    void ShowMatchmakingPanel()
    {
        CloseAllPanels();
        ModeSelectButtons.gameObject.SetActive(false);
        matchmakingPanel.transform.localPosition = Vector3.zero;
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
        settingsPanel.SetActive(true);
        soundPanel.SetActive(true);
    }
    
    
    void ShowAccountPanel()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
        accountPanel.SetActive(true);
        UpdateAccountInfo();
    }
    
    void ShowHelpPanel()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
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

    /*
     * 
    public Button privateRoomButton;
    public Button quickMatchButton;
    public Button selectRandomMatchingModeButton;
     */
    void CloseAllPanels()
    {
        readyPanel.gameObject.SetActive(false);
        RandomMatchingButtons.gameObject.SetActive(false);
        PrivateMatchingButtons.gameObject.SetActive(false);

        ModeSelectButtons.gameObject.SetActive(true);

        privateRoomPanel.SetActive(false);
        settingsPanel.SetActive(false);
        soundPanel.SetActive(false);
        accountPanel.SetActive(false);
        helpPanel.SetActive(false);
        friendPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);
        matchmakingPanel.SetActive(false);
    }
    
    bool AnyPanelOpen()
    {
        return privateRoomPanel.activeSelf || settingsPanel.activeSelf || 
               soundPanel.activeSelf ||
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
            AccountPanel.SetText(HTTPLoginManager.Instance.GetLoggedInUserName(),
                PlayerPrefs.GetString("LastLoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        }
        else
        {
            AccountPanel.SetText(PhotonNetwork.NickName, PlayerPrefs.GetString("JoinDate", DateTime.Now.ToString("yyyy-MM-dd")));
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
        Debug.Log("C");
        gameObject.SetActive(true);
        transform.GetChild(0).gameObject.SetActive(true);
        UpdateAccountInfo();
        Debug.Log("B");
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