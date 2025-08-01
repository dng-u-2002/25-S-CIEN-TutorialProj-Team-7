using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CardDeck : MonoBehaviour
{
    [SerializeField] List<CardObject> Cards;

    // ī�� �ϳ��� Ƣ����� �Ÿ�
    [SerializeField] float popDistance = 2f;
    // �� �� Ƣ����ų� ���ƿ��� �� �ɸ��� �ð�
    [SerializeField] float popDuration = 0.1f;
    // ī�� �� �ִϸ��̼� ���� �ð� �� (��)
    [SerializeField] float popInterval = 0.05f;

    //private void Start()
    //{
    //    PlayShuffleAnimation();
    //}

    /// <summary>
    /// �� ����(���־�) �ִϸ��̼� ����
    /// </summary>
    public void PlayShuffleAnimation()
    {
        StartCoroutine(_PlayShuffleAnimation());
    }

    private IEnumerator _PlayShuffleAnimation()
    {
        // �ִϸ��̼� �� ����ġ ����
        Vector3[] originalPositions = Cards.Select(c => c.transform.position).ToArray();

        // �� ī�庰�� StartCoroutine ȣ�� (������� �ð����� �ΰ�)
        for (int i = Cards.Count - 1; i >= 0; i--)
        {
            Transform t = Cards[i].transform;
            Vector3 origin = originalPositions[i];
            // i * popInterval ��ŭ ��� �� �� �ִϸ��̼� ����
            StartCoroutine(PopCard(t, origin, (Cards.Count - 1 - i) * popInterval));
        }

        // ������ ī�尡 ������ ���ƿ� ������ ���
        float totalTime = popInterval * (Cards.Count - 1) + popDuration * 2;
        yield return new WaitForSeconds(totalTime);
    }

    /// <summary>
    /// ���� ī�� �� �ִϸ��̼�: delay �Ŀ� �˾ƿ� �� �˹�
    /// </summary>
    private IEnumerator PopCard(Transform t, Vector3 origin, float delay)
    {
        // ���� ����
        yield return new WaitForSeconds(delay);

        // 1) �������� Ƣ�����
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / popDuration);
            // �ε巯�� easing
            float eased = Mathf.SmoothStep(0f, 1f, ratio);
            t.position = origin + Vector3.left * popDistance * eased;
            yield return null;
        }
        t.position = origin + Vector3.left * popDistance;

        // 2) ����ġ�� ���ƿ���
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
