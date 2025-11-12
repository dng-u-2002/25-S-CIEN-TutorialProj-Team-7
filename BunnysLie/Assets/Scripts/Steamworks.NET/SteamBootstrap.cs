using UnityEngine;
using Steamworks;

public class SteamBootstrap : MonoBehaviour
{
    public static bool Initialized { get; private set; }

    void Awake()
    {
        if (Initialized) { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        try
        {
            if (!Packsize.Test() || !DllCheck.Test())
                Debug.LogError("[Steam] Steamworks.NET packsize/dll mismatch.");

            Initialized = SteamAPI.Init();
            Debug.Log($"[Steam] Init: {Initialized}, AppId={SteamUtils.GetAppID().m_AppId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Steam] Init failed: {e.Message}");
            Initialized = false;
        }
    }

    void Update()
    {
        if (Initialized) SteamAPI.RunCallbacks();
    }

    void OnApplicationQuit()
    {
        if (Initialized) { SteamAPI.Shutdown(); Initialized = false; }
    }
}
