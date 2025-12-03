using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreditPlayer : MonoBehaviour
{
    [SerializeField] Image Credit;
    [SerializeField] AudioSource A1;
    [SerializeField] AudioSource A2;
    [SerializeField] float Duration;
    [SerializeField] Vector2 Diff;
    Coroutine C;
    private void Start()
    {
        Credit.gameObject.SetActive(false);
    }
    public void Play(System.Action onEnd)
    {
        if (C != null)
            return;
        C = StartCoroutine(_Play(onEnd));
    }

    IEnumerator LerpVolume(AudioSource audioSource, float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }
    private void Awake()
    {
        StartPos = Credit.rectTransform.anchoredPosition;
    }
    Vector3 StartPos;
    IEnumerator _Play(System.Action onEnd)
    {
        Credit.rectTransform.anchoredPosition = StartPos;
        Helpers.ObjectMoveHelper.ChangeAlpha(Credit, 0.0f, 0.0f);
        Credit.gameObject.SetActive(true);
        yield return null;
        Helpers.ObjectMoveHelper.ChangeAlpha(Credit, 1.0f, 1.0f);
        yield return new WaitForSeconds(0.5f);
        Vector2 startPos = Credit.rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Diff;
        float elapsed = 0f;
        A1.Play();
        A1.volume = 0.0f;
        StartCoroutine(LerpVolume(A1, 1.0f, 1.0f));
        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            if(elapsed >= 9.0f && A1.isPlaying == true)
            {
                A1.Stop();
                A2.volume = 1.0f;
                A2.Play();
            }
            float t = Mathf.Clamp01(elapsed / Duration);
            Credit.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        Credit.rectTransform.anchoredPosition = endPos;
        StartCoroutine(LerpVolume(A2, 0.0f, 1.0f));
        yield return new WaitForSeconds(1.5f);

        onEnd?.Invoke();
        Helpers.ObjectMoveHelper.ChangeAlpha(Credit, 0.0f, 1.0f);
        yield return new WaitForSeconds(1.0f);

        Credit.rectTransform.anchoredPosition = startPos;
        Credit.gameObject.SetActive(false);
    }
}
