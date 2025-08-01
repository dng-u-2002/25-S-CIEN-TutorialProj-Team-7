using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CardDeck : MonoBehaviour
{
    [SerializeField] List<CardObject> Cards;

    // 카드 하나가 튀어나오는 거리
    [SerializeField] float popDistance = 2f;
    // 한 번 튀어나오거나 돌아오는 데 걸리는 시간
    [SerializeField] float popDuration = 0.1f;
    // 카드 간 애니메이션 시작 시간 차 (초)
    [SerializeField] float popInterval = 0.05f;

    //private void Start()
    //{
    //    PlayShuffleAnimation();
    //}

    /// <summary>
    /// 덱 셔플(비주얼) 애니메이션 시작
    /// </summary>
    public void PlayShuffleAnimation()
    {
        StartCoroutine(_PlayShuffleAnimation());
    }

    private IEnumerator _PlayShuffleAnimation()
    {
        // 애니메이션 전 원위치 저장
        Vector3[] originalPositions = Cards.Select(c => c.transform.position).ToArray();

        // 각 카드별로 StartCoroutine 호출 (순서대로 시간차를 두고)
        for (int i = Cards.Count - 1; i >= 0; i--)
        {
            Transform t = Cards[i].transform;
            Vector3 origin = originalPositions[i];
            // i * popInterval 만큼 대기 후 팝 애니메이션 실행
            StartCoroutine(PopCard(t, origin, (Cards.Count - 1 - i) * popInterval));
        }

        // 마지막 카드가 완전히 돌아올 때까지 대기
        float totalTime = popInterval * (Cards.Count - 1) + popDuration * 2;
        yield return new WaitForSeconds(totalTime);
    }

    /// <summary>
    /// 개별 카드 팝 애니메이션: delay 후에 팝아웃 → 팝백
    /// </summary>
    private IEnumerator PopCard(Transform t, Vector3 origin, float delay)
    {
        // 시작 지연
        yield return new WaitForSeconds(delay);

        // 1) 왼쪽으로 튀어나오기
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / popDuration);
            // 부드러운 easing
            float eased = Mathf.SmoothStep(0f, 1f, ratio);
            t.position = origin + Vector3.left * popDistance * eased;
            yield return null;
        }
        t.position = origin + Vector3.left * popDistance;

        // 2) 원위치로 돌아오기
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / popDuration);
            float eased = Mathf.SmoothStep(0f, 1f, ratio);
            t.position = Vector3.Lerp(
                origin + Vector3.left * popDistance,
                origin,
                eased
            );
            yield return null;
        }
        t.position = origin;
    }
}
