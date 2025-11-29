using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using Photon.Pun;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Tooltip("로비 최대 인원 (게임 세션 인원과 무관).")]
    public int MaxMembers = 50; // 필요에 맞게 조정

    public CSteamID CurrentLobby { get; private set; } = CSteamID.Nil;
    public bool InLobby => CurrentLobby.IsValid();

    // 로비 생성 후 초대해야 하는 친구를 임시로 보관하는 큐
    private readonly Queue<CSteamID> _pendingInvites = new Queue<CSteamID>();

    // Events
    public delegate void LobbyEvent(CSteamID lobbyId);
    public event LobbyEvent OnLobbyCreatedOK;
    public event LobbyEvent OnLobbyEntered;
    public event LobbyEvent OnLobbyMembersChanged;
    public event LobbyEvent OnLobbyFull;

    // Steam callbacks
    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<LobbyEnter_t> _lobbyEnter;
    protected Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    protected Callback<GameLobbyJoinRequested_t> _joinRequested;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);

        if (PresenceManager.Instance != null)
            PresenceManager.Instance.OnJoinPresenceRequested += HandleJoinPresence;
    }

    // === Public API ===

    public void CreateFriendsOnlyLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxMembers);
    }

    public void CreateInviteOnlyLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxMembers);
    }

    public void LeaveLobby()
    {
        if (!InLobby) return;
        SteamMatchmaking.LeaveLobby(CurrentLobby);
        CurrentLobby = CSteamID.Nil;
        PresenceManager.Instance?.ClearAll();
    }

    public void InviteFriendsOverlay()
    {
        if (!InLobby) { Debug.LogWarning("[Lobby] No lobby."); return; }
        
        SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobby);
    }

    /// <summary>로비가 없으면 생성 -> 생성 완료 시 초대 전송.</summary>
    public void InviteFriendDirect(CSteamID friendId)
    {
        if (!InLobby)
        {
            Debug.Log("[Lobby] No lobby. Create (Invite-Only) then enqueue invite.");
            _pendingInvites.Enqueue(friendId);
            CreateInviteOnlyLobby();
            return;
        }
        bool ok = SteamMatchmaking.InviteUserToLobby(CurrentLobby, friendId);
        Debug.Log($"[Lobby] Invite {friendId} -> {ok}");
    }

    public List<CSteamID> GetMembers()
    {
        var list = new List<CSteamID>();
        if (!InLobby) return list;
        int n = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
        for (int i = 0; i < n; i++)
            list.Add(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i));
        return list;
    }

    // === Internal Handlers ===

    private void OnLobbyCreated(LobbyCreated_t e)
    {
        if (e.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[Lobby] Create failed: {e.m_eResult}");
            _pendingInvites.Clear();
            return;
        }

        string id;
        if(PhotonNetwork.InRoom == false)
            id = FindFirstObjectByType<GameLobbyManager>().CreatePrivateRoom();
        else
            id = PhotonNetwork.CurrentRoom.Name;
        CurrentLobby = new CSteamID(e.m_ulSteamIDLobby);
        Debug.Log($"[Lobby] Created: {CurrentLobby}");

        SteamMatchmaking.SetLobbyData(CurrentLobby, "mode", "cards");
        SteamMatchmaking.SetLobbyData(CurrentLobby, "id", id);
        SteamMatchmaking.SetLobbyData(CurrentLobby, "build", Application.version);
        SteamMatchmaking.SetLobbyJoinable(CurrentLobby, true);

        //PresenceManager.Instance?.SetStatus(FriendActivity.InLobby);
        //PresenceManager.Instance?.SetConnect($"lobby:{CurrentLobby.m_SteamID}");
        //PresenceManager.Instance?.SetPlayerGroup(CurrentLobby.ToString());

        // 로비가 생겼으니 대기열의 초대 처리
        while (_pendingInvites.Count > 0)
            InviteFriendDirect(_pendingInvites.Dequeue());

        OnLobbyCreatedOK?.Invoke(CurrentLobby);
    }

    private void OnLobbyEnter(LobbyEnter_t e)
    {
        CurrentLobby = new CSteamID(e.m_ulSteamIDLobby);
        Debug.Log($"[Lobby] Entered: {CurrentLobby}");

        var id = SteamMatchmaking.GetLobbyData(CurrentLobby, "id");

        // FindFirstObjectByType<GameLobbyManager>().jo
        if(id != string.Empty)
        {
            Debug.Log(id);
            if (PhotonNetwork.InRoom == false)
                PhotonNetwork.JoinRoom(id);
            else if (PhotonNetwork.CurrentRoom.Name != id)
            {
                GameLobbyManager.PendedRoomCode = id;
                PhotonNetwork.LeaveRoom();
                //PhotonNetwork.JoinRoom(id);
            }
        }

        //PresenceManager.Instance?.SetStatus(FriendActivity.InLobby);
        //PresenceManager.Instance?.SetConnect($"lobby:{CurrentLobby.m_SteamID}");
        //PresenceManager.Instance?.SetPlayerGroup(CurrentLobby.ToString());

        OnLobbyEntered?.Invoke(CurrentLobby);
        CheckFull();
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t e)
    {
        var lobbyId = new CSteamID(e.m_ulSteamIDLobby);
        if (!InLobby || lobbyId != CurrentLobby) return;

        OnLobbyMembersChanged?.Invoke(CurrentLobby);
        CheckFull();
    }

    private void OnJoinRequested(GameLobbyJoinRequested_t e)
    {
        Debug.Log($"[Lobby] Join requested by overlay. Lobby={e.m_steamIDLobby}");
        //if (PhotonNetwork.InRoom)
        //{
        //    Debug.Log("Already In Room");
        //    return;
        //}
        SteamMatchmaking.JoinLobby(e.m_steamIDLobby);
    }

    private void HandleJoinPresence(string connect)
    {
        // "lobby:<ulong>"
        if (string.IsNullOrEmpty(connect)) return;
        const string prefix = "lobby:";
        if (!connect.StartsWith(prefix)) return;

        if (ulong.TryParse(connect.Substring(prefix.Length), out var raw))
        {
            var lobbyId = new CSteamID(raw);
            Debug.Log($"[Lobby] Presence connect → JoinLobby {lobbyId}");
            SteamMatchmaking.JoinLobby(lobbyId);
        }
    }

    private void CheckFull()
    {
        if (!InLobby) return;
        int members = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
        Debug.Log($"[Lobby] Members: {members}/{MaxMembers}");
        if (members >= MaxMembers) OnLobbyFull?.Invoke(CurrentLobby);
    }
}
