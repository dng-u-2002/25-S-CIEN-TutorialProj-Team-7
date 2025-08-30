using UnityEngine;

[ExecuteAlways]
public class FixedAspectLetterbox : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] Vector2 aspect = new Vector2(16, 9); // 원하는 비율

    int lastW, lastH;

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        Apply();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float target = aspect.x / aspect.y;
        float window = (float)lastW / lastH;

        if (window > target) // 창이 더 넓으면: 좌우 필러박스
        {
            float width = target / window;
            targetCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else // 창이 더 높으면: 상하 레터박스
        {
            float height = window / target;
            targetCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }
}
