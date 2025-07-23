using UnityEngine;

public class CardImages : MonoBehaviour
{
    public static CardImages Instance { get; private set; }
    public Sprite[] CardSprites_Tree; // Array to hold card sprites
    public Sprite[] CardSprites_Cake; // 2D array to hold card sprites for each type
    public Sprite Dummy;

    private void Awake()
    {
        // Ensure that there is only one instance of CardImages
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep this object across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public Sprite GetSprite(Card.CardType type, byte value)
    {
        if (type == Card.CardType.Tree && value < CardSprites_Tree.Length)
        {
            return CardSprites_Tree[value];
        }
        else if (type == Card.CardType.Cake && value < CardSprites_Cake.Length)
        {
            return CardSprites_Cake[value];
        }
        else  if(type == Card.CardType.Dummy)
        {
            return Dummy;
        }
        else
        {
            Debug.LogWarning($"Card sprite not found for type: {type}, value: {value}");
            return null; // Return null if sprite is not found
        }
    }
}