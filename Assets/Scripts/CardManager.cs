using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum CardHouse
{
    Spades,
    Clubs,
    Diamonds,
    Hearts,
    Joker
};

public class Card
{
    public int cardValue; // 1-13 (1 = Ace, 10 = ten, 2 = two, 0 = Joker)
    public CardHouse cardHouse;
    public Sprite cardSprite;

    public Card(int value, CardHouse house, Sprite sprite)
    {
        cardValue = value;
        cardHouse = house;
        cardSprite = sprite;
    }
}


public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    // private piles. Only CardManager can change these.
    public List<Card> cards = new List<Card>(); // draw pile
    public List<Card> playPile = new List<Card>();

    // Events invoked whenever a pile is modified.
    public UnityEvent OnPlayPileChanged = new UnityEvent();
    public UnityEvent OnDrawPileChanged = new UnityEvent();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Read-only views for other systems
    public IReadOnlyList<Card> Cards => cards.AsReadOnly();
    public IReadOnlyList<Card> PlayPile => playPile.AsReadOnly();

    public void InitCards()
    {
        if (cards == null) cards = new List<Card>();
        else cards.Clear();

        // Cache handler reference once
        var data = CardDataHandler.Instance;

        for (int i = 1; i <= 13; i++)
        {
            Sprite s;

            // Spades
            s = data != null ? data.GetSprite(i, CardHouse.Spades) : null;
            cards.Add(new Card(i, CardHouse.Spades, s));

            // Clubs
            s = data != null ? data.GetSprite(i, CardHouse.Clubs) : null;
            cards.Add(new Card(i, CardHouse.Clubs, s));

            // Diamonds
            s = data != null ? data.GetSprite(i, CardHouse.Diamonds) : null;
            cards.Add(new Card(i, CardHouse.Diamonds, s));

            // Hearts
            s = data != null ? data.GetSprite(i, CardHouse.Hearts) : null;
            cards.Add(new Card(i, CardHouse.Hearts, s));
        }

        // 2 jokers (value 0)
        Sprite jokerSprite = data != null ? data.GetSprite(0, CardHouse.Joker) : null;
        cards.Add(new Card(0, CardHouse.Joker, jokerSprite));
        cards.Add(new Card(0, CardHouse.Joker, jokerSprite));

        // notify draw pile changed
        OnDrawPileChanged.Invoke();
    }

    public void ShuffleCards()
    {
        ShuffleCards(cards);
    }

    public void ShuffleCards(List<Card> cardsToShuffle)
    {
        if (cardsToShuffle == null || cardsToShuffle.Count <= 1) return;

        for (int i = cardsToShuffle.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = cardsToShuffle[i];
            cardsToShuffle[i] = cardsToShuffle[randomIndex];
            cardsToShuffle[randomIndex] = temp;
        }

        OnDrawPileChanged.Invoke();
    }

    // draw top card from draw pile. invokes OnDrawPileChanged
    public Card DrawTopCard()
    {
        if (cards == null || cards.Count == 0) return null;
        Card c = cards[0];
        cards.RemoveAt(0);
        OnDrawPileChanged.Invoke();
        return c;
    }

    // peek top card on play pile
    public Card GetTopCard()
    {
        if (playPile == null || playPile.Count == 0) return null;
        return playPile[playPile.Count - 1];
    }

    // Start the play pile with a card without validation. Use at game start.
    public void StartPlayPile(Card card)
    {
        if (card == null) return;
        if (playPile == null) playPile = new List<Card>();
        playPile.Add(card);
        OnPlayPileChanged.Invoke();
    }

    // Attempt to play a card. Validates rules, mutates playPile, fires OnPlayPileChanged.
    // Returns true on success.
    public bool PlayCard(Card card, Player owner)
    {
        if (card == null || owner == null) return false;
        if (playPile == null) playPile = new List<Card>();

        Card top = GetTopCard();

        // validity check
        if (top != null && top.cardValue == 1)
        {
            if (card.cardValue != 1 && card.cardValue != 0 && card.cardValue != 10 && card.cardValue != 2)
                return false;
        }
        else
        {
            if (top != null && card.cardValue != 1 && card.cardValue != 0 && card.cardValue != 10 && card.cardValue != 2)
            {
                if (top.cardValue != 2 && card.cardValue < top.cardValue)
                    return false;
            }
        }

        // special cards
        if (card.cardValue == 2)
        {
            playPile.Add(card);
            owner.extraTurn = true;
            OnPlayPileChanged.Invoke();
            return true;
        }

        if (card.cardValue == 10)
        {
            playPile.Clear();
            owner.extraTurn = true;
            OnPlayPileChanged.Invoke();
            return true;
        }

        // normal add
        playPile.Add(card);
        OnPlayPileChanged.Invoke();

        // check for 4-of-a-kind anywhere in pile
        int valuePlayed = card.cardValue;
        int sameCount = 0;
        for (int i = 0; i < playPile.Count; i++)
        {
            if (playPile[i].cardValue == valuePlayed)
                sameCount++;
        }

        if (sameCount >= 4)
        {
            playPile.Clear();
            owner.extraTurn = true;
            OnPlayPileChanged.Invoke();
        }

        return true;
    }


    // Return copy of play pile and clear it. Invokes OnPlayPileChanged.
    public List<Card> TakePlayPile()
    {
        if (playPile == null) playPile = new List<Card>();
        var taken = new List<Card>(playPile);
        playPile.Clear();
        OnPlayPileChanged.Invoke();
        return taken;
    }

    // Transfer entire play pile into player's hand and clear pile. Invokes OnPlayPileChanged.
    public void TransferPlayPileTo(Player player)
    {
        if (player == null) return;
        if (playPile == null || playPile.Count == 0) return;

        foreach (var c in playPile)
            player.AddCard(c);

        playPile.Clear();
        OnPlayPileChanged.Invoke();
    }

    // Utility: add specific card to top without invoking rules (useful for setup).
    public void ForceAddToPlayPile(Card card)
    {
        if (card == null) return;
        if (playPile == null) playPile = new List<Card>();
        playPile.Add(card);
        OnPlayPileChanged.Invoke();
    }
}
