using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    List<Card> Cards;

    public Deck()
    {
        Cards = new List<Card>();
    }

    public int CardCount
    {
        get { return Cards.Count; }
    }

    public List<Card> GetCardsAsList()
    {
        return new List<Card>(Cards);
    }

    public Card GetCardByTypeAndValue(Card.CardType type, byte value)
    {
        foreach (var card in Cards)
        {
            if (card.Type == type && card.Value == value)
            {
                return card;
            }
        }
        Debug.LogWarning($"No card found with type {type} and value {value}.");
        return null;
    }
    public Card GetCard(int idx)
    {
        if (idx >= 0 && idx < Cards.Count)
        {
            return Cards[idx];
        }
        else
        {
            Debug.LogWarning($"Attempted to access card at index {idx}, but it is out of range.");
            return null;
        }
    }

    public void AddCards(Card[] cards)
    {
        if (cards != null && cards.Length > 0)
        {
            foreach (var card in cards)
            {
                AddCard(card);
            }
        }
        else
        {
            Debug.LogWarning("Attempted to add null or empty array of cards to the deck.");
        }
    }

    public Action<Card> OnCardAdded;
    public Action<Card> OnCardRemoved;

    public void AddCard(Card card)
    {
        if (card != null)
        {
            Cards.Add(card);
            OnCardAdded?.Invoke(card);
        }
        else
        {
            Debug.LogWarning("Attempted to add a null card to the deck.");
        }
    }

    public void RemoveCard(Card card)
    {
        if (card != null && Cards.Contains(card))
        {
            OnCardRemoved?.Invoke(card);
            Cards.Remove(card);
        }
        else
        {
            Debug.LogWarning("Attempted to remove a null card or a card not in the deck.");
        }
    }

    public void RemoveAllCards()
    {
        for(int i = 0; i < Cards.Count; i++)
        {

            RemoveCard(Cards[i]);
            i--;
        }
    }
}
