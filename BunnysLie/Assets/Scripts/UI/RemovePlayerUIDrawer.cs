using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemovePlayerUIDrawer : PlayerUIDrawer
{
    public override void SetSpecialRuleMode()
    {
        base.SetSpecialRuleMode();
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(-196, 350);
        rectTransform.localScale = Vector3.one * 0.9f;
    }

    public override void SetSpecialRuleObserverMode()
    {
        base.SetSpecialRuleObserverMode();
        var rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(736, 359);
        rectTransform.localScale = Vector3.one * 0.7f;
    }
}
