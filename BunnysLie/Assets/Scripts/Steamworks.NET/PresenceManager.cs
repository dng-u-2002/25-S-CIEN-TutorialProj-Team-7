using UnityEngine;
using Steamworks;

public class PresenceManager : MonoBehaviour
{
    public static PresenceManager Instance { get; private set; }

    protected Callback<GameRichPresenceJoinRequested_t> _rpJoinRequested;
    private Callback<FriendRichPresenceUpdate_t> _cbRichPresenceUpdate;
    public delegate void JoinPresenceRequested(string connect);
    public event JoinPresenceRequested OnJoinPresenceRequested;

    void Awake() 
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _rpJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnRichPresenceJoinRequested);
        _cbRichPresenceUpdate = Callback<FriendRichPresenceUpdate_t>.Create(OnFriendRichPresenceUpdate);
    }
    void OnFriendRichPresenceUpdate(FriendRichPresenceUpdate_t param)
    {
        var fid = (CSteamID)param.m_steamIDFriend;
        int keyCount = SteamFriends.GetFriendRichPresenceKeyCount(fid);

        Debug.Log($"[RP] Update from {SteamFriends.GetFriendPersonaName(fid)}, keys: {keyCount}");

        for (int i = 0; i < keyCount; i++)
        {
            string key = SteamFriends.GetFriendRichPresenceKeyByIndex(fid, i);
            string val = SteamFriends.GetFriendRichPresence(fid, key);
            Debug.Log($"[RP] {fid}  {key} = '{val}'");
            GameLobbyManager.Instance.UpdateFriendRichPresence(fid, val);
        }

    }
    public void SetStatus(FriendActivity status)
    {
        /*
         * 		"#Status_InLobby"	 "로비"
		"#Status_Matchmaking"	"매칭중"
		"#Status_InGame"	"게임중"
         */
        switch (status)
        {
            case FriendActivity.InLobby:
                SteamFriends.SetRichPresence("steam_display", "#Status_InLobby");
                break;
            case FriendActivity.Matchmaking:
                SteamFriends.SetRichPresence("steam_display", "#Status_Matchmaking");
                break;
            case FriendActivity.InGame:
                SteamFriends.SetRichPresence("steam_display", "#Status_InGame");
                break;
        }
    }
    public void SetConnect(string connect) => SteamFriends.SetRichPresence("connect", connect);
    public void SetPlayerGroup(string groupId) => SteamFriends.SetRichPresence("steam_player_group", groupId);
    public void ClearAll() => SteamFriends.ClearRichPresence();

    private void OnRichPresenceJoinRequested(GameRichPresenceJoinRequested_t e)
    {
        string conn = e.m_rgchConnect; // e.g., "lobby:123456"
        Debug.Log($"[Presence] Join requested: '{conn}' from {e.m_steamIDFriend}");
        OnJoinPresenceRequested?.Invoke(conn);
    }
}
