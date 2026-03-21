using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class AIPlayer : Player
{
    public UnityEvent onDrawPile;
    public UnityEvent onPlayCard;
    public UnityEvent onPlayTen;

    [Header("Delays (seconds)")]
    public float drawDelay = 0.6f;
    public float playDelay = 0.6f;

    public override void TakeTurn(Card topCard)
    {
        StartCoroutine(TakeTurnRoutine(topCard));
    }

    private IEnumerator TakeTurnRoutine(Card topCard)
    {
        // Build playable list
        List<Card> playable = new List<Card>();
        foreach (Card c in new List<Card>(hand))
        {
            if (IsPlayableAgainstTop(c, topCard))
                playable.Add(c);
        }

        yield return new WaitForSeconds(Random.Range(0.1f, 2f));

        // Case: no playable -> draw
        if (playable.Count == 0)
        {
            onDrawPile.Invoke();
            yield return new WaitForSeconds(drawDelay);
            DrawPlayPile(CardManager.Instance);
            EndTurn();
            yield break;
        }

        // Pick best group
        var bestGroup = playable
            .GroupBy(c => c.cardValue)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First()
            .ToList();

        // Single card
        if (bestGroup.Count == 1)
        {
            Card chosen = playable.OrderBy(c => c.cardValue).First();
            onPlayCard.Invoke();
            yield return new WaitForSeconds(playDelay);
            TryPlay(chosen);
            EndTurn();
            yield break;
        }

        // Multiple cards
        onPlayCard.Invoke();
        for (int i = 0; i < bestGroup.Count; i++)
        {
            yield return new WaitForSeconds(playDelay);
            bool isLast = (i == bestGroup.Count - 1);
            bool ok = TryPlay(bestGroup[i], isLast);
            if (!ok) yield break;
        }
        EndTurn();
    }

    private bool IsPlayableAgainstTop(Card card, Card top)
    {
        if (card == null) return false;
        if (card.cardValue == 0) return true; // Joker
        if (card.cardValue == 2) return true;
        if (card.cardValue == 10) return true;
        if (top == null) return true;
        if (top.cardValue == 1)
            return (card.cardValue == 1 || card.cardValue == 0 || card.cardValue == 10 || card.cardValue == 2);
        if (top.cardValue == 2) return true;
        return card.cardValue >= top.cardValue;
    }
}
