using UnityEngine;

/// <summary>
/// Singleton that holds card sprites and their "placed" variations.
/// Use:
///   CardDataHandler.Instance.GetSprite(value, house)
///   CardDataHandler.Instance.GetPlacedSprite(value, house)
/// value: 0 = Joker, 1..13 = Ace..King
/// </summary>
public class CardDataHandler : MonoBehaviour
{
    public static CardDataHandler Instance { get; private set; }

    [Header("Card Sprites (normal)")]
    [Tooltip("For non-Joker houses supply 13 sprites in order: index 0 = Ace, index 12 = King")]
    public Sprite[] spades;
    public Sprite[] clubs;
    public Sprite[] diamonds;
    public Sprite[] hearts;

    [Tooltip("Joker sprites. index 0 used for value 0 (Joker).")]
    public Sprite[] jokers;

    [Header("Card Sprites (placed variation)")]
    [Tooltip("Same indexing rules as normal sprites.")]
    public Sprite[] spadesPlaced;
    public Sprite[] clubsPlaced;
    public Sprite[] diamondsPlaced;
    public Sprite[] heartsPlaced;
    public Sprite[] jokersPlaced;

    // Internal 2D caches for fast lookup [value, houseIndex]
    // value range: 0..13 (0 = Joker, 1..13 = Ace..King)
    public Sprite[,] cardSprites;
    public Sprite[,] placedCardSprites;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSprites();
    }

    private void InitSprites()
    {
        int maxValue = 14; // indices 0..13
        int houseCount = System.Enum.GetValues(typeof(CardHouse)).Length;

        cardSprites = new Sprite[maxValue, houseCount];
        placedCardSprites = new Sprite[maxValue, houseCount];

        // Fill per house
        FillHouse(CardHouse.Spades, spades, spadesPlaced, false);
        FillHouse(CardHouse.Clubs, clubs, clubsPlaced, false);
        FillHouse(CardHouse.Diamonds, diamonds, diamondsPlaced, false);
        FillHouse(CardHouse.Hearts, hearts, heartsPlaced, false);

        // Jokers are special: they map to value 0 (and optionally further indices)
        FillHouse(CardHouse.Joker, jokers, jokersPlaced, true);
    }

    /// <summary>
    /// If isJokerHouse is false: normal[] should be 13 items where index 0 = Ace (value 1).
    /// This maps normal[i] -> cardSprites[i+1, houseIndex].
    /// If isJokerHouse is true: normal[0] -> cardSprites[0, houseIndex].
    /// </summary>
    private void FillHouse(CardHouse house, Sprite[] normal, Sprite[] placed, bool isJokerHouse)
    {
        int houseIndex = (int)house;
        int maxValue = cardSprites.GetLength(0);

        if (normal != null)
        {
            if (isJokerHouse)
            {
                // Map jokers to value 0..n-1 (typically only index 0 used)
                for (int i = 0; i < Mathf.Min(normal.Length, maxValue); i++)
                {
                    cardSprites[i, houseIndex] = normal[i];
                }
            }
            else
            {
                // Map normal[0] -> value 1 (Ace), normal[12] -> value 13 (King)
                int limit = Mathf.Min(normal.Length, maxValue - 1); // leave index 0 for Joker
                for (int i = 0; i < limit; i++)
                {
                    int targetValue = i + 1;
                    if (targetValue < maxValue)
                        cardSprites[targetValue, houseIndex] = normal[i];
                }
            }
        }

        if (placed != null)
        {
            if (isJokerHouse)
            {
                for (int i = 0; i < Mathf.Min(placed.Length, maxValue); i++)
                {
                    placedCardSprites[i, houseIndex] = placed[i];
                }
            }
            else
            {
                int limit = Mathf.Min(placed.Length, maxValue - 1);
                for (int i = 0; i < limit; i++)
                {
                    int targetValue = i + 1;
                    if (targetValue < maxValue)
                        placedCardSprites[targetValue, houseIndex] = placed[i];
                }
            }
        }
    }

    public Sprite GetSprite(int value, CardHouse house)
    {
        if (cardSprites == null) return null;
        if (value < 0 || value >= cardSprites.GetLength(0)) return null;
        return cardSprites[value, (int)house];
    }

    public Sprite GetPlacedSprite(int value, CardHouse house)
    {
        if (placedCardSprites == null) return null;
        if (value < 0 || value >= placedCardSprites.GetLength(0)) return null;
        return placedCardSprites[value, (int)house];
    }

    public Sprite GetSpritePreferPlaced(int value, CardHouse house)
    {
        var placed = GetPlacedSprite(value, house);
        if (placed != null) return placed;
        return GetSprite(value, house);
    }

    public bool HasPlacedVariant(int value, CardHouse house)
    {
        return GetPlacedSprite(value, house) != null;
    }
}
