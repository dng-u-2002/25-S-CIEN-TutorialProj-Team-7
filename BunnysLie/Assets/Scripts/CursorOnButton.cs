using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CursorOnButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Button btn;
    bool pointerInside;

    void Awake() => btn = GetComponent<Button>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        CursorManager.Instance.SetState(btn.interactable ? CursorState.Hover : CursorState.Disabled);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        CursorManager.Instance.SetState(CursorState.Default);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (btn.interactable)
            CursorManager.Instance.SetState(CursorState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 버튼 위에 계속 있으면 Hover, 아니면 Default
        CursorManager.Instance.SetState(pointerInside && btn.interactable ? CursorState.Hover : CursorState.Default);
    }

    // 버튼이 비활성화되는 순간에도 커서를 정리
    void OnDisable()
    {
        if (CursorManager.Instance) CursorManager.Instance.SetState(CursorState.Default);
    }
}
