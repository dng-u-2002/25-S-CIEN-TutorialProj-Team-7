using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SingleEmoticon
{
    public Sprite[] Images;
    public float TimeInterval;


    public Sprite GetSprite(float elapsedTime)
    {
        int imageCount = Images.Length;
        if (imageCount == 0)
        {
            return null;
        }

        int index = Mathf.FloorToInt(elapsedTime / TimeInterval) % imageCount;
        if (index < 0 || index >= imageCount)
        {
            return null;
        }
        return Images[index];
    }
    public Sprite GetSpriteWithNormalizedTime(float elapsedTime)
    {
        int imageCount = Images.Length;
        if (imageCount == 0)
        {
            return null;
        }

        int index = (Mathf.FloorToInt(elapsedTime / TimeInterval) % imageCount) % imageCount;
        if (index < 0)
        {
            return null;
        }
        return Images[index];
    }
    public float TotalTime
    {
        get
        {
            return TimeInterval * Images.Length;
        }
    }

}


public class Emoticon : MonoBehaviour
{
    [SerializeField] public SingleEmoticon[] Emoticons;
}
