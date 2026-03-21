using System.Collections.Generic;
using UnityEngine;

public abstract class Player : MonoBehaviour
{
    public string playerName;

    public string turnText = "Your turn"; 
    public List<Card> hand = new List<Card>();

    public int handsCleared = 0;

    [HideInInspector] public bool hasPlayedThisTurn = false;
    [HideInInspector] public bool extraTurn = false;

    public virtual void AddCard(Card card)
    {
        if (card != null) hand.Add(card);
    }

    public virtual void RemoveCard(Card card)
    {
        if (card != null && hand.Contains(card)) hand.Remove(card);
    }

    public void DrawCard(CardManager manager)
    {
        if (manager == null || manager.cards == null || manager.cards.Count == 0) return;
        Card drawn = manager.DrawTopCard();
        if (drawn != null) AddCard(drawn);
    }

    // Player.cs (only the DrawPlayPile method changed)
    public void DrawPlayPile(CardManager manager)
    {
        if (manager == null) return;

        // get the play pile safely via CardManager
        var taken = CardManager.Instance.TakePlayPile(); // returns list
        if (taken == null || taken.Count == 0) return;

        foreach (var c in taken)
            AddCard(c);

        EndTurn();
    }


    // Called by GameManager to begin a turn
    public void StartTurn(Card topCard)
    {
        hasPlayedThisTurn = false;
        extraTurn = false;
        TakeTurn(topCard);
    }

    public void EndTurn()
    {
        hasPlayedThisTurn = true;
    }

    // Implementation decides how to play. Must set hasPlayedThisTurn when done.
    public abstract void TakeTurn(Card topCard);

    public bool TryPlay(Card card, bool markTurnDone = true)
    {
        bool success = card != null && CardManager.Instance.PlayCard(card, this);

        if (success)
        {
            RemoveCard(card);
            return true;
        }
        else
        {
            // If play fails, draw the entire play pile
            DrawPlayPile(CardManager.Instance);
        }
        return success;
    }

}
