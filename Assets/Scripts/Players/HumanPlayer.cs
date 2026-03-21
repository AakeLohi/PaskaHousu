using System.Collections.Generic;
using UnityEngine;

public class HumanPlayer : Player
{
    public CardHandDisplay cardHandDisplay;

    public void PlaySelectedCard(Card cardToPlay)
    {
        if (hasPlayedThisTurn)
        {
            Debug.Log("Not players turn");
            return;
        }
        Debug.Log($"PlaySelectedCard called on {playerName} id={GetInstanceID()} handCount={hand.Count}");

        bool ok = TryPlay(cardToPlay);
        if (!ok)
        {
            Debug.Log("Play invalid. Choose different card or draw.");
        }

        // refresh UI after action
        if (cardHandDisplay != null)
            cardHandDisplay.DisplayCards();

        EndTurn();
    }

    public void PlaySelectedCards(List<Card> cardsToPlay)
    {
        if (hasPlayedThisTurn)
        {
            Debug.Log("Not players turn");
            return;
        }

        if (cardsToPlay == null || cardsToPlay.Count == 0) return;

        // ensure all same value
        int value = cardsToPlay[0].cardValue;
        foreach (Card c in cardsToPlay)
        {
            if (c.cardValue != value)
            {
                Debug.Log("Cannot play cards of different values together!");
                return;
            }
        }

        // play each card, mark turn done on last card
        for (int i = 0; i < cardsToPlay.Count; i++)
        {
            bool isLast = (i == cardsToPlay.Count - 1);
            TryPlay(cardsToPlay[i], isLast);

        }
        if (cardHandDisplay != null)
            cardHandDisplay.DisplayCards();

        EndTurn();
    }

    public override void TakeTurn(Card topCard)
    {
        if (cardHandDisplay != null)
            cardHandDisplay.DisplayCards();
    }
}
