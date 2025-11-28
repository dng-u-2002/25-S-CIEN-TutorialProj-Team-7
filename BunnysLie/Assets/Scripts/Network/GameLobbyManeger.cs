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
using System.Collections;
using Steamworks;
using System.Text;

public class GameLobbyManager : MonoBehaviourPunCallbacks
{
    // ===== [LOCAL FRIEND MOCK - OPTIONS] =====
    [Header("로컬 친구 목 옵션")]
    [SerializeField] private bool useLocalFriendMock = true;      // 끄면 서버 연동 사용
    [SerializeField] private int mockInitialFriendCount = 6;      // 시작 더미 친구 수

    private LocalFriendService _localFriends;                     // 로컬 목 핸들


    #region ===== LOCAL FRIEND MOCK SERVICE =====
    void SetupLocalFriendMock()
    {
        if (!useLocalFriendMock) return;

        bool alreadyWired =
            ServerFetchFriends != null ||
            ServerAddFriend != null ||
            ServerRemoveFriend != null ||
            ServerInviteFriend != null ||
            ServerFetchActivities != null;

        // 서버 델리게이트가 이미 주입되어 있으면 그대로 사용
        if (alreadyWired) return;

        _localFriends = new LocalFriendService();
        _localFriends.Bootstrap(mockInitialFriendCount);

        // --- 서버 대행 델리게이트 주입 ---
        ServerFetchFriends = (onOk, onErr) =>
        {
            onOk?.Invoke(_localFriends.Snapshot());
        };

        ServerAddFriend = (code, onOk, onErr) =>
        {
            var added = _localFriends.Add(code);
            if (added != null) onOk?.Invoke(added);
            else onErr?.Invoke("ALREADY_EXISTS");
        };

        ServerRemoveFriend = (code, onOk, onErr) =>
        {
            if (_localFriends.Remove(code)) onOk?.Invoke();
            else onErr?.Invoke("NOT_FOUND");
        };

        ServerInviteFriend = (code, gameMode, onOk, onErr) =>
        {
            if (_localFriends.Invite(code, gameMode)) onOk?.Invoke();
            else onErr?.Invoke("UNAVAILABLE");
        };

        ServerFetchActivities = (onOk, onErr) =>
        {
            onOk?.Invoke(_localFriends.Tick());
        };
    }

    /// <summary>
    /// 네트워크 없이도 친구 목록/상태/초대 흐름을 흉내 내는 로컬 목 서비스
    /// </summary>
    class LocalFriendService
    {
        private readonly List<FriendData> _friends = new List<FriendData>();
        private readonly System.Random _rng = new System.Random();

        private readonly string[] _names = new[]
        {
        "하늘","바다","솔","별","루나","민호","지우","다연","준호","예린","소라","태민","라라","유나","수현","현우"
    };

        public void Bootstrap(int count)
        {
            _friends.Clear();
            for (int i = 0; i < count; i++)
            {
                var f = new FriendData
                {
                    nickname = _names[i % _names.Length] + _rng.Next(10, 99),
                    friendCode = $"FR-{_rng.Next(100000, 999999)}",
                    isOnline = _rng.NextDouble() < 0.8,
                    lastSeen = DateTime.Now,
                    currentActivity = FriendActivity.InLobby,
                    currentRoomName = ""
                };
                if (!f.isOnline) f.lastSeen = DateTime.Now.AddMinutes(-_rng.Next(1, 180));
                _friends.Add(f);
            }
        }

        public List<FriendData> Snapshot() => _friends.Select(Clone).ToList();

        public FriendData Add(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var normalized = code.Trim().ToUpper();

            if (_friends.Any(x => string.Equals(x.friendCode, normalized, StringComparison.OrdinalIgnoreCase)))
                return null; // 이미 존재

            var f = new FriendData
            {
                nickname = $"테스트{_rng.Next(100, 999)}",
                friendCode = normalized,
                isOnline = true,
                lastSeen = DateTime.Now,
                currentActivity = FriendActivity.InLobby,
                currentRoomName = ""
            };
            _friends.Add(f);
            return Clone(f);
        }

        public bool Remove(string code)
        {
            var f = _friends.FirstOrDefault(x => string.Equals(x.friendCode, code, StringComparison.OrdinalIgnoreCase));
            if (f == null) return false;
            _friends.Remove(f);
            return true;
        }

        public bool Invite(string code, int gameMode)
        {
            var f = _friends.FirstOrDefault(x => string.Equals(x.friendCode, code, StringComparison.OrdinalIgnoreCase));
            if (f == null) return false;
            if (!f.isOnline) return false;
            if (f.currentActivity == FriendActivity.Matchmaking || f.currentActivity == FriendActivity.InGame) return false;

            // 초대 → 매칭 중 상태로 전환(간이 시뮬레이션)
            f.currentActivity = FriendActivity.Matchmaking;
            f.currentRoomName = "";
            return true;
        }

        /// <summary>
        /// 10초마다 호출되는 ServerFetchActivities에 대응. 
        /// 약간의 랜덤으로 온라인/오프라인/활동상태를 바꿔서 변화를 만들어줌.
        /// </summary>
        public List<FriendData> Tick()
        {
            foreach (var f in _friends)
            {
                double r = _rng.NextDouble();

                // 5% 확률로 온라인/오프라인 토글
                if (r < 0.05)
                {
                    f.isOnline = !f.isOnline;
                    if (!f.isOnline)
                    {
                        f.currentActivity = FriendActivity.InLobby;
                        f.currentRoomName = "";
                        f.lastSeen = DateTime.Now;
                    }
                    else
                    {
                        f.currentActivity = FriendActivity.InLobby;
                    }
                }
                else if (f.isOnline)
                {
                    // 온라인이면 활동상태 약간 섞기
                    double a = _rng.NextDouble();

                    if (f.currentActivity == FriendActivity.Matchmaking && a < 0.40)
                    {
                        // 매칭 중 → 게임 중(초대 수락된 느낌)
                        f.currentActivity = FriendActivity.InGame;
                        f.currentRoomName = "Room_" + _rng.Next(1000, 9999);
                    }
                    else if (a < 0.10)
                    {
                        // 아무 때나 로비로 복귀
                        f.currentActivity = FriendActivity.InLobby;
                        f.currentRoomName = "";
                    }
                }
            }
            return Snapshot();
        }

        private static FriendData Clone(FriendData src)
        {
            return new FriendData
            {
                nickname = src.nickname,
                friendCode = src.friendCode,
                isOnline = src.isOnline,
                lastSeen = src.lastSeen,
                currentActivity = src.currentActivity,
                currentRoomName = src.currentRoomName
            };
        }
    }
    #endregion



    public static GameLobbyManager Instance { get; private set; }
    
    [Header("매칭 키 UI")]
    public Button PrevButton_ShowPrivateRoomButtons;
    public Button RealButton_EnterQuickMatchButton;
    public Button PrevButton_ShowModeSelectingButton;
    public Button RealButton_EnterRoomWithTwoCardGameButton;
    public Button RealButton_EnterRoomWithThreeCardGameButton;
    public Button RealButton_ShowPrivateCodeEnterPanelButton;
    public Button[] CancleBackButtons;

    public Transform ModeSelectButtons;
    public Transform RandomMatchingButtons;
    public Transform PrivateMatchingButtons;
    public Button[] ExitNowMatchButtons;

    [Header("설정 키 UI")]
    public Button settingsButton;
    //public Button soundButton;
    //public Button accountButton;
    //public Button friendButton;
    //public Button helpButton;
    public Button exitButton;
    
    [Header("팝업 패널들")]
    public GameObject privateRoomPanel;
    public GameObject settingsPanel;
    public GameObject exitConfirmPanel;
    public GameObject friendInvitePanel;
    public GameObject friendRemoveConfirmPanel;
    public GameObject messagePanel;
    
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
    //public Button inviteFriendButton;
    public Transform friendListContent;
    public GameObject friendItemPrefab;
    public Button refreshFriendListButton;
    
    [Header("친구 초대 UI")]
    public TMPro.TMP_Text inviteTargetNameText;
    public TMPro.TMP_Text inviteRoomInfoText;
    public Button sendInviteButton;
    public Button cancelInviteButton;
    
    [Header("친구 삭제 확인 UI")]
    public TMPro.TMP_Text removeTargetNameText;
    public Button confirmRemoveButton;
    public Button cancelRemoveButton;
    
    [Header("메시지 UI")]
    public TMPro.TMP_Text messageText;
    public Button messageOkButton;
    
    public eGameMode selectedGameType = eGameMode.TwoCards;
    private bool isMatchmaking = false;
    //private List<FriendData> friendList = new List<FriendData>();
    private FriendData pendingInviteFriend;
    private FriendData pendingRemoveFriend;
    //private Dictionary<string, FriendItem> friendItemDict = new Dictionary<string, FriendItem>();
    
    private float friendActivityUpdateInterval = 10f;
    private float lastFriendActivityUpdate = 0f;

    public Action<Action<List<FriendData>>, Action<string>> ServerFetchFriends;
    public Action<string, Action<FriendData>, Action<string>> ServerAddFriend;
    public Action<string, Action, Action<string>> ServerRemoveFriend;
    public Action<string, int, Action, Action<string>> ServerInviteFriend;
    public Action<Action<List<FriendData>>, Action<string>> ServerFetchActivities;

    [SerializeField] Button RuleBookButton;
    
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

        if(PlayerPrefs.HasKey("MasterVolume") == false)
        {
            PlayerPrefs.SetFloat("MasterVolume", 1.0f);
            PlayerPrefs.SetFloat("BGMVolume", 0.5f);
            PlayerPrefs.SetFloat("SFXVolume", 0.5f);
            PlayerPrefs.SetInt("IsActive_TextChat", 1);
            PlayerPrefs.SetInt("IsActive_VoiceChat", 1);
            PlayerPrefs.SetInt("Width", 1920);
            PlayerPrefs.SetInt("Height", 1080);
            PlayerPrefs.SetInt("FullScreen", 1);
            PlayerPrefs.Save();
        }
    }
    
    void Start()
    {
        CheckLoginStatus();
        InitializeUI();
        SetupEventListeners();
        ConnectToPhoton();


        //SetupLocalFriendMock();//Test

        //LoadFriendList();
        //StartFriendActivitySimulation();

        var friends = FriendListManager.Instance.GetFriends(true, false);
        Debug.Log($"[Friends] Count = {friends.Count}");
        foreach (var f in friends)
        {
            var sb = new StringBuilder();
            sb.Append($"{f.name} ({f.id}) state={f.state}");
            if (f.isPlayingThisGame) sb.Append(" | in THIS game");
            if (!string.IsNullOrEmpty(f.richStatus)) sb.Append($" | status='{f.richStatus}'");
            Debug.Log(sb.ToString());
        }
        refreshFriendListButton.onClick.AddListener(() => UpdateFriendList());
        UpdateFriendList();
        gameObject.SetActive(true);
        StartCoroutine(_LoopUpdateFriendList(1.5f));

        PresenceManager.Instance.SetStatus(FriendActivity.InLobby);
    }

    IEnumerator _LoopUpdateFriendList(float dt)
    {
        while(true)
        {
            yield return new WaitForSeconds(dt);
            UpdateFriendList();
        }
    }


    public void SetFriendPanelActive(bool active)
    {
        friendListContent.parent.gameObject.SetActive(active);
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

        SetFriendPanelActive(false);
    }
    void ShowPrivateMathcingButtons()
    {
        RandomMatchingButtons.gameObject.SetActive(false);
        PrivateMatchingButtons.gameObject.SetActive(true);
        ModeSelectButtons.gameObject.SetActive(false);
    }
    void SetupEventListeners()
    {
        PrevButton_ShowPrivateRoomButtons.onClick.AddListener(() => ShowPrivateMathcingButtons());
        RealButton_ShowPrivateCodeEnterPanelButton.onClick.AddListener(() => ShowPrivateRoomPanel());
        foreach(var bb in CancleBackButtons)
        {
            bb.onClick.AddListener(() => CloseAllPanels());
        }
        foreach (var bb in ExitNowMatchButtons)
        {
            bb.onClick.AddListener(()=>
            {
                //if (PhotonNetwork.InRoom == false)
                //    return;

                //방 안에 있을 때에만 작동
                CloseAllPanels();
            });
        }
        PrevButton_ShowModeSelectingButton.onClick.AddListener(() => ShowRandomMatchingModeButtons());
        RealButton_EnterRoomWithTwoCardGameButton.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.TwoCards);
            StartRandomMatching();
        });
        RealButton_EnterRoomWithThreeCardGameButton.onClick.AddListener(() =>
        {
            SelectGameType(eGameMode.ThreeCards);
            StartRandomMatching();
        });
        RealButton_EnterQuickMatchButton.onClick.AddListener(() => StartQuickMatch());

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

        settingsButton.onClick.AddListener(() => ShowSettingsPanel());
        //soundButton.onClick.AddListener(() => ShowSoundPanel());
        //accountButton.onClick.AddListener(() => ShowAccountPanel());
        //helpButton.onClick.AddListener(() => ShowHelpPanel());
        //friendButton.onClick.AddListener(() => ShowFriendPanel());
        exitButton.onClick.AddListener(() => ShowExitConfirmation());
        
        soundToggle.onValueChanged.AddListener(SetSoundEnabled);
        pushNotificationToggle.onValueChanged.AddListener(SetPushNotification);
        
        googleLoginButton.onClick.AddListener(() => LoginWithGoogle());
        steamLoginButton.onClick.AddListener(() => LoginWithSteam());
       
        
        sendInviteButton.onClick.AddListener(() => SendFriendInvite());
        cancelInviteButton.onClick.AddListener(() => CancelFriendInvite());
        
        //confirmRemoveButton.onClick.AddListener(() => ConfirmRemoveFriend());
        //cancelRemoveButton.onClick.AddListener(() => CancelRemoveFriend());
        
        messageOkButton.onClick.AddListener(() => CloseMessage());
        //inviteFriendButton.onClick.AddListener(() => InviteFriend());


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
                PresenceManager.Instance.SetStatus(FriendActivity.InGame);
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
            PhotonNetwork.GameVersion = "b25";
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
        UpdateMyActivityStatus(FriendActivity.InLobby);
    }


    void SelectGameType(eGameMode gameType)
    {
        selectedGameType = gameType;
        UpdateGameTypeSelection();
        
        if (InGameManager.Instance != null)
        {
            //InGameManager.Instance.Mode = (eGameMode)selectedGameType;
        }
    }
    
    void UpdateGameTypeSelection()
    {
        Color selectedColor = Color.green;
        Color normalColor = Color.white;
        
        RealButton_EnterRoomWithTwoCardGameButton.GetComponent<Image>().color = 
            selectedGameType == eGameMode.TwoCards ? selectedColor : normalColor;
        RealButton_EnterRoomWithThreeCardGameButton.GetComponent<Image>().color = 
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

        RealButton_EnterRoomWithTwoCardGameButton.GetComponent<Image>().color =
            normalColor;
        RealButton_EnterRoomWithThreeCardGameButton.GetComponent<Image>().color =
            normalColor;
    }
    
    void StartRandomMatching()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        isMatchmaking = true;
        UpdateMyActivityStatus(FriendActivity.Matchmaking);
        
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["RandomMode"] = true;

        RoomOptions roomOptions = new RoomOptions()
        {
            MaxPlayers = 3,
            CustomRoomProperties = roomProps,
            CustomRoomPropertiesForLobby = new string[] { "GameMode" }
        };
        
        PhotonNetwork.JoinRandomRoom(roomProps, 3);
        ShowReadyPanel();
    }
    
    void StartQuickMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return;
        }
        
        isMatchmaking = true;
        //ShowMatchmakingPanel();
        readyPanel.transform.localPosition = Vector3.zero;
        readyPanel.gameObject.SetActive(true);
        UpdateMyActivityStatus(FriendActivity.Matchmaking);
        selectedGameType = UnityEngine.Random.value > 0.5f ? eGameMode.TwoCards : eGameMode.ThreeCards;
        PhotonNetwork.JoinRandomRoom();
    }
    
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["GameMode"] = (int)selectedGameType;
        roomProps["RandomMode"] = false;
        
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
        PresenceManager.Instance.SetStatus(FriendActivity.Matchmaking);
        ModeSelectButtons.gameObject.SetActive(false);
        if (isMatchmaking)
        {
            isMatchmaking = false;
            CloseAllPanels();
            
            UpdateMyActivityStatus(FriendActivity.InGame, PhotonNetwork.CurrentRoom.Name);
            
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
                //readyPanelText.text = $"플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
            }
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
            if(PlayerCounterRunner != null)
                StopCoroutine(PlayerCounterRunner);
            ShowReadyPanel();
            PlayerCounterRunner = StartCoroutine(_ShowPlayerCounter());


            bool isPrivate = false;
            var data = PhotonNetwork.CurrentRoom.PropertiesListedInLobby;
            foreach(var d in data)
            {
                if(d == "IsPrivate")
                {
                    isPrivate = true;
                    break;
                }
            }
            if(isPrivate)
            {
                SetFriendPanelActive(true);
            }
            else
            {
                SetFriendPanelActive(false);
            }
        }
    }

    Coroutine PlayerCounterRunner;

    IEnumerator _ShowPlayerCounter()
    {
        if ((bool)PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("IsPrivate", out var priv) == true && (bool)priv == true)
        {
            while (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < 3)
            {
                readyPanelText.text = "방 코드 : " + PhotonNetwork.CurrentRoom.Name + $"\n플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            while (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount < 3)
            {
                readyPanelText.text =$"\n플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
                yield return new WaitForSeconds(1f);
            }
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
            //matchmakingStatusText.text = "방 코드 : " + PhotonNetwork.CurrentRoom.Name + $"\n플레이어 대기 중... ({PhotonNetwork.CurrentRoom.PlayerCount}/3)";
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
    
    public string CreatePrivateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            return string.Empty;
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
        ShowReadyPanel();
        return roomCode;
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
        UpdateMyActivityStatus(FriendActivity.InLobby);
        CloseAllPanels();
    }

    
    void UpdateFriendList()
    {
        foreach (Transform child in friendListContent)
        {
            Destroy(child.gameObject);
        }
        var friends = FriendListManager.Instance.GetFriends(true, false);
        //Debug.Log($"[Friends] Count = {friends.Count}");
        foreach (var f in friends)
        {
            var sb = new StringBuilder();
            sb.Append($"{f.name} ({f.id}) state={f.state}");
            if (f.isPlayingThisGame) sb.Append(" | in THIS game");
            if (!string.IsNullOrEmpty(f.richStatus)) sb.Append($" | status='{f.richStatus}'");
           // Debug.Log(sb.ToString());
        }

        var sortedFriends = friends.OrderByDescending(f => f.isPlayingThisGame)
                                     .ThenBy(f => f.name)
                                     .ToList();
        
        foreach (var friend in sortedFriends)
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

        friendInvitePanel.transform.localPosition = Vector3.zero;
        friendInvitePanel.SetActive(true);
    }
    
    void SendFriendInvite()
    {
        if (pendingInviteFriend != null)
        {
            if (ServerInviteFriend == null)
            {
                ShowMessage("친구 초대 연동이 설정되지 않았습니다");
            }
            else
            {
                ServerInviteFriend(
                    pendingInviteFriend.friendCode,
                    (int)selectedGameType,
                    () =>
                    {
                        ShowMessage($"{pendingInviteFriend.nickname}님에게 초대를 전송했습니다.");
                    },
                    err =>
                    {
                        ShowMessage("초대 전송 실패");
                    }
                );
            }
        }
        
        friendInvitePanel.SetActive(false);
        pendingInviteFriend = null;
    }
    
    void CancelFriendInvite()
    {
        friendInvitePanel.SetActive(false);
        pendingInviteFriend = null;
    }
    
    public void ShowFriendRemoveConfirmation(FriendData friend)
    {
        pendingRemoveFriend = friend;
        
        if (removeTargetNameText != null)
        {
            removeTargetNameText.text = $"{friend.nickname}님을 친구에서 삭제하시겠습니까?";
        }

        friendRemoveConfirmPanel.transform.localPosition = Vector3.zero;
        friendRemoveConfirmPanel.SetActive(true);
    }
    
    
    void CancelRemoveFriend()
    {
        friendRemoveConfirmPanel.SetActive(false);
        pendingRemoveFriend = null;
    }
    
    void SaveFriendList() {}
    
    void StartFriendActivitySimulation()
    {
        lastFriendActivityUpdate = Time.time;
    }
    
    void UpdateMyActivityStatus(FriendActivity activity, string roomName = "")
    {
        Debug.Log($"내 활동 상태 업데이트: {activity}");
        //FriendListManager.Instance.UpdateMyActivity(activity, roomName);
        PresenceManager.Instance.SetStatus(activity);
    }
    
    void ShowMessage(string message)
    {
        messagePanel.transform.localPosition = Vector3.zero;
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
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }
    
    void ShowPrivateRoomPanel()
    {
        CloseAllPanels();
        ModeSelectButtons.gameObject.SetActive(false);
        PrevButton_ShowPrivateRoomButtons.gameObject.SetActive(false);
        RealButton_EnterQuickMatchButton.gameObject.SetActive(false);
        PrevButton_ShowModeSelectingButton.gameObject.SetActive(false);
        privateRoomPanel.transform.localPosition = Vector3.zero;
        privateRoomPanel.SetActive(true);
        roomCodeInput.text = string.Empty;
    }
    

    public void ShowReadyPanel()
    {
        CloseAllPanels();
        readyPanel.transform.localPosition = Vector3.zero;
        readyPanel.gameObject.SetActive(true);
        readyPanelText.text = string.Empty;

        if (PlayerCounterRunner != null)
            StopCoroutine(PlayerCounterRunner);
    }
    
    void ShowSettingsPanel()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
        settingsPanel.transform.GetComponent<SettingPanel>().OnSettingButtonClicked();
    }
   
    
    
    //void ShowAccountPanel()
    //{
    //    CloseAllPanels();
    //    settingsPanel.SetActive(true);
    //    accountPanel.SetActive(true);
    //    UpdateAccountInfo();
    //}
    
    //void ShowFriendPanel()
    //{
    //    if(friendPanel.activeSelf == true)
    //    {
    //        CloseAllPanels();
    //        return;
    //    }
    //    CloseAllPanels();
    //    friendPanel.SetActive(true);
    //    UpdateFriendList();
    //}
    
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

        PrevButton_ShowPrivateRoomButtons.gameObject.SetActive(true);
        RealButton_EnterQuickMatchButton.gameObject.SetActive(true);
        PrevButton_ShowModeSelectingButton.gameObject.SetActive(true);
        ModeSelectButtons.gameObject.SetActive(true);

        privateRoomPanel.SetActive(false);
        settingsPanel.SetActive(false);
        exitConfirmPanel.SetActive(false);

        FindObjectOfType<RuleBook>()?.OffRuleBook();

        SetFriendPanelActive(false);

        if (friendInvitePanel != null) friendInvitePanel.SetActive(false);
        if (friendRemoveConfirmPanel != null) friendRemoveConfirmPanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);
    }
    
    bool AnyPanelOpen()
    {
        return privateRoomPanel.activeSelf || settingsPanel.activeSelf || 
               exitConfirmPanel.activeSelf || readyPanel.activeSelf ||
               (friendInvitePanel != null && friendInvitePanel.activeSelf) ||
               (friendRemoveConfirmPanel != null && friendRemoveConfirmPanel.activeSelf) ||
               (messagePanel != null && messagePanel.activeSelf);
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
        gameObject.SetActive(true);
        transform.GetChild(0).gameObject.SetActive(true);
        UpdateAccountInfo();

        //RuleBookButton.onClick.Invoke(); //시작 시 룰북 보여줌
        FindObjectOfType<RuleBook>().ShowRuleBook(0);
    }
    
    void LoginWithGoogle()
    {
    }
    
    void LoginWithSteam()
    {
    }

    private void OnApplicationQuit()
    {
        ExitGame();
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
    public FriendActivity currentActivity = FriendActivity.InLobby;
    public string currentRoomName = "";
}
