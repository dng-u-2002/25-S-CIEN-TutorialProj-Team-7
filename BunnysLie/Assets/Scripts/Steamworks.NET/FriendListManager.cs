using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class FriendListManager : MonoBehaviour
{
    public class FriendInfo
    {
        public CSteamID id;
        public string name;
        public EPersonaState state;
        public bool isPlayingThisGame;   // 오직 현재 이 앱을 플레이 중인지로 판별
        public string richStatus;        // RP "status"
    }

    public static FriendListManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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
            if (onlyOnline && state == EPersonaState.k_EPersonaStateOffline) continue;

            // 최신 Rich Presence 요청
            //SteamFriends.RequestFriendRichPresence(fid);

            // "현재 이 앱 플레이 중인지"만 판단
            bool playingThis = false;
            FriendGameInfo_t gi;
            if (SteamFriends.GetFriendGamePlayed(fid, out gi))
            {
                Debug.Log(gi.m_gameID.AppID());
                playingThis = (gi.m_gameID.AppID() == myApp);
            }

            if (onlyPlayingThisGame && !playingThis) continue;

            list.Add(new FriendInfo
            {
                id = fid,
                name = SteamFriends.GetFriendPersonaName(fid),
                state = state,
                isPlayingThisGame = playingThis,
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
