using UnityEngine;

public enum CursorState { Default, Hover, Pressed, Disabled }

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor Textures")]
    [SerializeField] Texture2D defaultCursor;
    [SerializeField] Texture2D hoverCursor;
    [SerializeField] Texture2D pressedCursor;
    [SerializeField] Texture2D disabledCursor;

    [Header("Hotspots (픽셀 좌표, 이미지 좌상단=0,0)")]
    [SerializeField] Vector2 defaultHotspot;
    [SerializeField] Vector2 hoverHotspot;
    [SerializeField] Vector2 pressedHotspot;
    [SerializeField] Vector2 disabledHotspot;

    CursorState current;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        SetState(CursorState.Default);
    }

    public void SetState(CursorState state)
    {
        if (current == state) return;
        current = state;

        Texture2D tex = defaultCursor;
        Vector2 hot = defaultHotspot;

        switch (state)
        {
            case CursorState.Hover: tex = hoverCursor; hot = hoverHotspot; break;
            case CursorState.Pressed: tex = pressedCursor; hot = pressedHotspot; break;
            case CursorState.Disabled: tex = disabledCursor; hot = disabledHotspot; break;
        }

        Cursor.SetCursor(tex, hot, CursorMode.ForceSoftware); // Auto면 가능하면 하드웨어 커서 사용
    }

    // 포커스 잃거나 비활성화될 때 기본으로 복귀(커서가 걸리는 상황 방지)
    void OnApplicationFocus(bool focus) { if (!focus) SetState(CursorState.Default); }
    void OnDisable() { if (Instance == this) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }
}
