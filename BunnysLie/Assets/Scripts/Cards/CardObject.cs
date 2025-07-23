using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class CardObject : MonoBehaviour
{
    Card ThisCard;
    public bool IsFront = false;

    [SerializeField] private Image FrontSprite;
    [SerializeField] private Image BackSprite;
    [SerializeField] Button SelectButton;
    [SerializeField] Transform SelectionBackground;
    private void Awake()
    {
        SelectButton.interactable = false;
        SelectionBackground.gameObject.SetActive(false);
    }
    public void ActiveSelection(bool active, System.Action<Card> onClick)
    {
        if (SelectButton != null)
        {
            SelectButton.interactable = active;
            SelectionBackground.gameObject.SetActive(active);
            SelectButton.onClick.RemoveAllListeners();
            SelectButton.gameObject.SetActive(active);
            if(active == true)
            {
                SelectButton.onClick.AddListener(() =>
                {
                    if (onClick != null)
                    {
                        onClick.Invoke(ThisCard);
                    }
                });
            }
        }

    }
    public Card GetCard()
    {
        return ThisCard;
    }
    public void SetCard(Card c)
    {
        ThisCard = c;
        c.CardGameObject = this;
        var sprite = CardImages.Instance.GetSprite(c.Type, c.Value);
        if (sprite != null)
        {
            FrontSprite.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("Card sprite not found for the given card.");
        }
    }

    public void SetFace(bool isFront)
    {
        if(isFront)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private void Update()
    {
        Vector3 dir = transform.forward;
        if(Vector3.Dot(new Vector3(0, 0, 1), dir) <0)
        {
            IsFront = true;
        }
        else
        {
            IsFront = false;
        }
        if (IsFront)
        {
                FrontSprite.gameObject.SetActive(true);
        }
        else
        {
            FrontSprite.gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
    }
}