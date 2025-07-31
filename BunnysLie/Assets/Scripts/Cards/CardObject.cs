using Helpers;
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

    [SerializeField] Transform FaceTransform;
    [SerializeField] Transform MovementTransform;

    [SerializeField] AudioSource FlipSound;
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
            FaceTransform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            FaceTransform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    public void SetMovementTransformPosition(Vector3 position)
    {
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDPosition);
        MovementTransform.position = position;
    }
    public void SetMovementTransformScale(Vector3 scale)
    {
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDScale);
        MovementTransform.localScale = scale;
    }
    string MovementTransformAnimationIDPosition;
    public Vector3 GetMoverPosition()
    {
        return MovementTransform.position;
    }
    public Vector3 GetMoverScale()
    {
        return MovementTransform.localScale;
    }
    public void SetMoverDefaultTransform()
    {
        MovementTransform.localPosition = Vector3.zero;
        MovementTransform.localRotation = Quaternion.identity;
        MovementTransform.localScale = Vector3.one;
    }
    public void MoveMovementTransformPosition(Vector3 target, float duration, Helpers.ePosition positionType = Helpers.ePosition.World)
    {
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDPosition);
        MovementTransformAnimationIDPosition = Helpers.ObjectMoveHelper.MoveObject(MovementTransform, target, duration, positionType);
    }
    public void MoveMovementTransformScale(Vector3 target, float duration)
    {
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDScale);
        MovementTransformAnimationIDScale = Helpers.ObjectMoveHelper.ScaleObject(MovementTransform, target, duration);
    }

    string MovementTransformAnimationIDRotation;
    string MovementTransformAnimationIDScale;
    public void SetFaceAnimated(bool isFront, float scaleFactor, float duration)
    {
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDRotation);
        Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDScale);

        Vector3 originScale = MovementTransform.localScale;
        MovementTransformAnimationIDScale = Helpers.ObjectMoveHelper.ScaleObject(MovementTransform, originScale * scaleFactor, duration/2);
        DelayedFunctionHelper.InvokeDelayed(() =>
        {
            MovementTransformAnimationIDRotation = Helpers.ObjectMoveHelper.RotatebjectSlerp(FaceTransform, isFront ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0), duration, Helpers.ePosition.World);
        }, duration * 0.4f);

        DelayedFunctionHelper.InvokeDelayed(() =>
        {
            Helpers.ObjectMoveHelper.TryStop(MovementTransformAnimationIDScale);
            MovementTransformAnimationIDScale = Helpers.ObjectMoveHelper.ScaleObject(MovementTransform, originScale, duration / 2);
        }, duration / 2);
    }

    bool preFrontFlag = false;
    private void Update()
    {
        Vector3 dir = FaceTransform.forward;
        if(Vector3.Dot(new Vector3(0, 0, 1), dir) <0)
        {
            IsFront = true;
        }
        else
        {
            IsFront = false;
        }
        if(preFrontFlag != IsFront)
        {
            FlipSound.Play();
        }
        if (IsFront)
        {
                FrontSprite.gameObject.SetActive(true);
        }
        else
        {
            FrontSprite.gameObject.SetActive(false);
        }
        preFrontFlag = IsFront;
    }

    private void OnValidate()
    {
    }
}