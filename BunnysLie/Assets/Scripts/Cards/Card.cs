using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card
{
    public enum CardType : byte
    {
        Tree,
        Cake,
        Dummy
    }
    public byte Value;

    public CardType Type = CardType.Tree;

    public Card(CardType type, byte value)
    {
        Type = type;
        Value = value;
    }
    public CardObject CardGameObject;
}
