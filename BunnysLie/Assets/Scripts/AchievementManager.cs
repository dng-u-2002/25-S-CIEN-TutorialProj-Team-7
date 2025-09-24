// AchievementManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public enum AchievementId
{
    WinNoLoss,     // 한 라운드도 안 지고 최종 승리
    WinPairTen,    // 10별 페어로 라운드 승리
    WinZeroMoon,   // 0달 족보로 라운드 승리
    Play2Mode5Games, //2장 모드 5번 플레이
    Play3Mode5Games, //3장 모드 5번 플레이
    WinSpecialRule5Times, //스페셜 룰 5번 플레이
    WinRoundBiggerCardsThanOutPlayer, //퇴청한 상대보다 높은 패로 참가해 라운드 승리하기
    WatchCredit, //크레딧 보기
    Draw5RPS, //가위바위보 5번 연속 무승부
    WinSpecialRuleAfterExhangeCardWithOpponent //1 vs 1 룰에서 상대와 교환 후 승리하기
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private Callback<UserStatsReceived_t> _cbStatsReceived;
    private Callback<UserStatsStored_t> _cbStatsStored;
    private Callback<UserAchievementStored_t> _cbAchStored;

    private bool _statsReady = false;
    private readonly Queue<AchievementId> _pendingUnlocks = new();

    // Steamworks 파트너 페이지의 API Name과 반드시 1:1 매칭
    private static readonly Dictionary<AchievementId, string> Ach = new()
    {
        { AchievementId.WinNoLoss,        "ACH_WIN_NO_LOSS" },
        { AchievementId.WinPairTen,       "ACH_WIN_PAIR_10STAR" },
        { AchievementId.WinZeroMoon,      "ACH_WIN_ZERO_MOON" },
        { AchievementId.Play2Mode5Games,      "ACH_PLAY_2MODE_3GAMES" },
        { AchievementId.Play3Mode5Games,      "ACH_PLAY_3MODE_3GAMES" },
        { AchievementId.WinSpecialRule5Times,      "ACH_WIN_SPC_5" },
        { AchievementId.WinRoundBiggerCardsThanOutPlayer,      "ACH_WIN_BIGGERCARDS_THANOUTPLAYER" },
        { AchievementId.WatchCredit,      "WATCH_CREDIT" },
        { AchievementId.Draw5RPS,      "DRAW_RPS_5TIMES" },
        { AchievementId.WinSpecialRuleAfterExhangeCardWithOpponent,      "WIN_SPECAILGAME_EXHANGE_OPPONENT" }
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        if (!SteamManager.Initialized) { Debug.LogWarning("Steam not initialized"); return; }

        _cbStatsReceived = Callback<UserStatsReceived_t>.Create(OnStatsReceived);
        _cbStatsStored = Callback<UserStatsStored_t>.Create(OnStatsStored);
        _cbAchStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

        // 1) 업적/스탯을 서버에서 로드 (이후에야 SetAchievement 가능)
        SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
        //SteamUserStats.RequestCurrentStats(); // 반드시 선행! :contentReference[oaicite:4]{index=4}
    }

    private void OnStatsReceived(UserStatsReceived_t p)
    {
        if ((ulong)p.m_nGameID == SteamUtils.GetAppID().m_AppId && p.m_eResult == EResult.k_EResultOK)
        {
            _statsReady = true;
            // 대기열 처리
            while (_pendingUnlocks.Count > 0) TryUnlock(_pendingUnlocks.Dequeue());
        }
    }

    private void OnStatsStored(UserStatsStored_t p) { /* 필요시 로그 */ }
    private void OnAchievementStored(UserAchievementStored_t p) { /* 필요시 로그 */ }

    public static void Unlock(AchievementId id)
    {
        if (Instance == null) return;
        if (!Instance._statsReady) { Instance._pendingUnlocks.Enqueue(id); return; }
        Instance.TryUnlock(id);
    }

    private void TryUnlock(AchievementId id)
    {
        if (!Ach.TryGetValue(id, out var api)) return;

        bool already;
        SteamUserStats.GetAchievement(api, out already);
        if (already) return;

        if (SteamUserStats.SetAchievement(api))
        {
            SteamUserStats.StoreStats(); // 서버 반영 트리거 (중요) :contentReference[oaicite:5]{index=5}
        }
    }

    // 진행형 업적(팝업 + 진행도 표시)
    public static void IndicateProgress(AchievementId id, int cur, int max)
    {
        if (Instance == null || !Instance._statsReady) return;
        if (!Ach.TryGetValue(id, out var api)) return;
        // 진행 팝업 (RequestCurrentStats 성공 후에만 true) :contentReference[oaicite:6]{index=6}
        SteamUserStats.IndicateAchievementProgress(api, (uint)cur, (uint)max);
    }

    public static void AddToStat(AchievementId statApiEnum, int delta, (AchievementId ach, int target)? gate = null)
    {
        if (Instance == null || !Instance._statsReady) return;
        string statApiName = Ach[statApiEnum];
        SteamUserStats.GetStat(statApiName, out int v);
        v += delta;
        SteamUserStats.SetStat(statApiName, v);
        SteamUserStats.StoreStats();

        if (gate.HasValue && v >= gate.Value.target)
        {
            Unlock(gate.Value.ach);
            IndicateProgress(gate.Value.ach, gate.Value.target, gate.Value.target);
        }
        else if (gate.HasValue)
        {
            IndicateProgress(gate.Value.ach, v, gate.Value.target);
        }
    }
    // 스탯 보조: 누적용 정수 스탯 예시
    public static void AddToStat(string statApiName, int delta, (AchievementId ach, int target)? gate = null)
    {
        if (Instance == null || !Instance._statsReady) return;
        SteamUserStats.GetStat(statApiName, out int v);
        v += delta;
        SteamUserStats.SetStat(statApiName, v);
        SteamUserStats.StoreStats();

        if (gate.HasValue && v >= gate.Value.target)
        {
            Unlock(gate.Value.ach);
            IndicateProgress(gate.Value.ach, gate.Value.target, gate.Value.target);
        }
        else if (gate.HasValue)
        {
            IndicateProgress(gate.Value.ach, v, gate.Value.target);
        }
    }

    // 테스트용 리셋(개발 중에만 사용)
    public static void Clear(AchievementId id)
    {
        if (Instance == null || !Instance._statsReady) return;
        if (!Ach.TryGetValue(id, out var api)) return;
        SteamUserStats.ClearAchievement(api);
        SteamUserStats.StoreStats();
    }
}