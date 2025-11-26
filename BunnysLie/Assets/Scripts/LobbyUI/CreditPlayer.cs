using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreditPlayer : MonoBehaviour
{
    [SerializeField] Image Credit;
    [SerializeField] float Duration;
    [SerializeField] Vector2 Diff;
    private void Start()
    {
        Credit.gameObject.SetActive(false);
    }
    public void Play(System.Action onEnd)
    {
        StartCoroutine(_Play(onEnd));
    }

    IEnumerator _Play(System.Action onEnd)
    {
        Credit.gameObject.SetActive(true);
        Helpers.ObjectMoveHelper.ChangeAlpha(Credit, 1.0f, 1.0f);
        yield return new WaitForSeconds(0.5f);
        Vector2 startPos = Credit.rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Diff;
        float elapsed = 0f;
        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);
            Credit.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        Credit.rectTransform.anchoredPosition = endPos;

        yield return new WaitForSeconds(1.5f);

        onEnd?.Invoke();
        Helpers.ObjectMoveHelper.ChangeAlpha(Credit, 0.0f, 1.0f);
        yield return new WaitForSeconds(1.0f);

        Credit.rectTransform.anchoredPosition = startPos;
        Credit.gameObject.SetActive(false);
    }
}
