using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Text;

public class FriendListManager : MonoBehaviour
{
    public enum FriendState
    {
        //얘네 순서대로 친구 목록에 뜸(값이 큰거)
        PlayingThisGame = 2,
        HiddenOrUnknown = 1,
        NotPlayingThisGame = 0
    }
    public class FriendInfo
    {
        public CSteamID id;
        public string name;
        public EPersonaState state;
        public FriendState gameState;   // 오직 현재 이 앱을 플레이 중인지로 판별
        public string richStatus;        // RP "status"
    }

    public static FriendListManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // 임의 채널 번호 (0이면 기본 채널)
    private const int CHANNEL = 1;

    private void OnEnable()
    {
        // 콜백 등록
        _p2pSessionRequest =
            Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
        _p2pSessionConnectFail =
            Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);

        // 릴레이 허용 (기본값 true지만, 명시적으로 써도 됨)
        SteamGameServerNetworking.AllowP2PPacketRelay(true);
    }
    private Callback<P2PSessionRequest_t> _p2pSessionRequest;
    private Callback<P2PSessionConnectFail_t> _p2pSessionConnectFail;

    private void OnP2PSessionRequest(P2PSessionRequest_t data)
    {
        CSteamID remote = data.m_steamIDRemote;

        Debug.Log($"[Server] P2P 세션 요청: {remote}");

        // 예: 로비에 있는 유저만 허용하는 식의 체크를 원래는 해야 함.
        // 여기서는 귀찮으니 일단 전부 수락.
        bool ok = SteamGameServerNetworking.AcceptP2PSessionWithUser(remote);
        Debug.Log($"[Server] AcceptP2PSessionWithUser({remote}) = {ok}");
    }

    private void OnP2PSessionConnectFail(P2PSessionConnectFail_t data)
    {
        Debug.LogWarning(
            $"[Server] P2P 연결 실패: {data.m_steamIDRemote}, 에러코드={data.m_eP2PSessionError}");
    }
    private void Update()
    {
        // 들어온 패킷이 있는지 확인
        uint packetSize;
        while (SteamGameServerNetworking.IsP2PPacketAvailable(out packetSize, CHANNEL))
        {
            byte[] buffer = new byte[packetSize];
            uint bytesRead;
            CSteamID remote;

            if (SteamGameServerNetworking.ReadP2PPacket(
                    buffer,
                    packetSize,
                    out bytesRead,
                    out remote,
                    CHANNEL))
            {
                // UTF-8 문자열로 변환
                string msg = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                Debug.Log($"[Server] {remote} 로부터 받은 메시지: {msg}");


                if(msg.StartsWith("ERP")) //rEsponse Rich Presence
                {
                    //친구의 Rich Presence 응답
                    string rpKey = msg.Substring(3); // "steam_display" 등
                    string rpValue = msg[3].ToString();
                    Debug.Log($"[Server] {remote} 의 Rich Presence '{rpKey}' = '{rpValue}'");
                    //예시: 클라이언트에게 RP 값 전송
                    CachedStates[remote] = int.Parse(rpValue);
                }
            }
        }
    }

    /// <summary>
    /// 특정 클라이언트에게 문자열 메시지 전송
    /// </summary>
    public void SendToClient(CSteamID clientId, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        uint length = (uint)data.Length;

        // Reliable로 전송 (필요에 따라 Unreliable 등으로 바꿔도 됨)
        bool ok = SteamGameServerNetworking.SendP2PPacket(
            clientId,
            data,
            length,
            EP2PSend.k_EP2PSendReliable,
            CHANNEL
        );

        Debug.Log($"[Server] SendP2PPacket to {clientId}, len={length}, result={ok}");
    }

    /// <summary>
    /// 필요 시 세션/채널 정리
    /// </summary>
    public void CloseClient(CSteamID clientId)
    {
        // 특정 채널만 닫기
        SteamGameServerNetworking.CloseP2PChannelWithUser(clientId, CHANNEL);
        // 전체 세션 닫기
        SteamGameServerNetworking.CloseP2PSessionWithUser(clientId);
    }

    Dictionary<CSteamID, int> CachedStates = new();
    /// <summary>
    /// onlyOnline: 온라인 친구만 가져올지
    /// onlyPlayingThisGame: "현재 이 앱을 플레이 중인 친구만" 필터링할지
    /// </summary>
    public List<FriendInfo> GetFriends(bool onlyOnline, bool onlyPlayingThisGame)
    {
        var list = new List<FriendInfo>();
        int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        AppId_t myApp = SteamUtils.GetAppID();

        for (int i = 0; i < count; i++)
        {
            var fid = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            var state = SteamFriends.GetFriendPersonaState(fid);
            Debug.Log($"{SteamFriends.GetFriendPersonaName(fid)} / {state}");
            if (onlyOnline && state == EPersonaState.k_EPersonaStateOffline) continue;
            // 최신 Rich Presence 요청
            //SteamFriends.RequestFriendRichPresence(fid);

            SendToClient(fid, "QRP"); // rQuest Rich Presence

            // "현재 이 앱 플레이 중인지"만 판단
            bool playingThis = false;
            FriendGameInfo_t gi;
            FriendState s = FriendState.HiddenOrUnknown;

            if(state == EPersonaState.k_EPersonaStateOffline) //스팀 앱이 꺼져 있는 경우
            {
                //근데 위에서 막아놔서 어차피 안 들어옴
            }
            else //스팀 앱이 켜져 있는 경우
            {
                if (SteamFriends.GetFriendGamePlayed(fid, out gi))
                {
                    //비공개 계정은 현재 무슨 게임 중인지도 알려주지 않으므로...
                    //여기 들어오면 일단은 비공개 계정은 아님(공개 계정임)
                    Debug.Log($"Game : {SteamFriends.GetFriendPersonaName(fid)} / {gi.m_gameID.AppID()}");
                    playingThis = (gi.m_gameID.AppID() == myApp);


                    if (playingThis)
                    {
                        //확실하게 우리 게임 중
                        s = FriendState.PlayingThisGame;
                    }
                    else
                    {
                        //확실하게 다른 게임 중
                        s = FriendState.NotPlayingThisGame;
                    }
                }
                else
                {
                    //여기선 경우가 2개인데, 아예 비공개 계정이거나 아무 게임도 하고 있지 않은 경우임
                    //근데 비공개 계정인지 확인하는 그런 함수도 없고, Rich Presence로 우회하려고 해도 비공개 계정은 RP에 그냥 값이 없어서 안됨
                    //현재 우리 게임을 플레이하지 않음 -> RP == emtpy
                    //비공개 계정임 -> RP == empty
                    s = FriendState.HiddenOrUnknown;
                }
            }


            if (onlyPlayingThisGame && !playingThis) continue;

            list.Add(new FriendInfo
            {
                id = fid,
                name = SteamFriends.GetFriendPersonaName(fid),
                state = state,
                gameState = s,
                richStatus = SteamFriends.GetFriendRichPresence(fid, "steam_display"),
            });
        }

        return list;
    }

    /// <summary>
    /// 특정 친구에게 직접 초대(로비 없으면 생성 후 대기열 처리).
    /// </summary>
    public void InviteFriend(CSteamID friendId)
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[Friends] LobbyManager missing.");
            return;
        }
        LobbyManager.Instance.InviteFriendDirect(friendId);
    }
}
