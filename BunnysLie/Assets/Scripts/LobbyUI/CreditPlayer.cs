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
    [SerializeField] GameObject exitGuideText;
    Coroutine C;
    private void Start()
    {
        Credit.gameObject.SetActive(false);
    }
    
    private void Update()
    {
        // 크레딧이 재생 중일 때 ESC를 누르면 종료
        if (C != null && Input.GetKeyDown(KeyCode.Escape))
        {
            StopCredit();
        }
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
        AudioSource lobbyBgm = GameObject.Find("LobbyBGMPlayer")?.GetComponent<AudioSource>();
        if (lobbyBgm != null) StartCoroutine(LerpVolume(lobbyBgm, 0.0f, 1.0f)); // 로비 BGM 끔
        
        if (exitGuideText != null) exitGuideText.SetActive(true);
        
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

        if (lobbyBgm != null) StartCoroutine(LerpVolume(lobbyBgm, 1.0f, 1.0f)); // 로비 BGM 켬
        
        if (exitGuideText != null) exitGuideText.SetActive(false);
        
        Credit.rectTransform.anchoredPosition = startPos;
        Credit.gameObject.SetActive(false);
        
        C = null;
    }
    
    public void StopCredit()
    {
        if (C != null)
        {
            StopCoroutine(C); // 실행 중인 크레딧 코루틴 중단
            C = null;
        }

        // 1. 크레딧 사운드 정지 및 UI 정리
        if (A1 != null) A1.Stop();
        if (A2 != null) A2.Stop();
        if (exitGuideText != null) exitGuideText.SetActive(false); // 문구 끄기

        // 2. 로비 BGM 복구 (아까 추가한 로직이 있다면)
        AudioSource lobbyBgm = GameObject.Find("LobbyBGMPlayer")?.GetComponent<AudioSource>();
        if (lobbyBgm != null)
        {
            lobbyBgm.volume = 1.0f; // 즉시 복구하거나 LerpVolume 실행
            if (!lobbyBgm.isPlaying) lobbyBgm.Play();
        }

        // 3. UI 초기화
        Credit.gameObject.SetActive(false);
        Credit.rectTransform.anchoredPosition = StartPos;
    
        Debug.Log("크레딧 재생이 강제 중단되었습니다.");
    }
}
