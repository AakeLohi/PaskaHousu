using UnityEngine;
using System.Collections.Generic;

public class CardPileDisplay : MonoBehaviour
{
    [Header("Renderers")]
    public SpriteRenderer cardPileSprite;   // base pile sprite
    public SpriteRenderer topCardSprite;    // top card sprite (optional)

    [Header("Pile Sprites")]
    public Sprite[] pileStates; // states array, index 0..N-1

    [Header("Top Card Offsets")]
    public Vector3[] topCardOffsets; // optional offsets per state

    [Header("Fallbacks")]
    public Sprite defaultTopSprite; // optional card-back or placeholder when top sprite missing
    public bool showBackIfMissing = true;

    // true -> show play pile. false -> show draw pile.
    public bool displayPlayPileOrDrawPile = true;

    // public router
    public void UpdatePileDisplay()
    {
        if (displayPlayPileOrDrawPile)
            UpdatePlayPileDisplay();
        else
            UpdateDrawPileDisplay();
    }

    // Draw pile rules:
    // - state 0 only when draw pile is empty
    // - otherwise map 1..52 to indices 1..(pileStates.Length-1)
    public void UpdateDrawPileDisplay()
    {
        if (CardManager.Instance == null)
        {
            ClearAll();
            return;
        }

        List<Card> draw = CardManager.Instance.cards;
        int count = (draw == null) ? 0 : draw.Count;

        // empty draw pile -> state 0, no top card
        if (count == 0)
        {
            SetPileSpriteSafe(0);
            DisableTopCard();
            return;
        }

        // There is at least one card. We must pick an index in range 1..(pileStates.Length-1)
        int stateIndex = 1; // fallback
        if (pileStates != null && pileStates.Length > 1)
        {
            float frac = Mathf.Clamp01((float)count / 52f); // 0..1
            int maxIndex = pileStates.Length - 1;           // highest index
            // map 0..1 -> 1..maxIndex
            stateIndex = 1 + Mathf.FloorToInt(frac * (maxIndex - 1 + 0.0001f));
            stateIndex = Mathf.Clamp(stateIndex, 1, maxIndex);
        }
        else
        {
            // If no enough states, fall back to last available
            stateIndex = Mathf.Clamp(stateIndex, 0, (pileStates != null ? pileStates.Length - 1 : 0));
        }

        SetPileSpriteSafe(stateIndex);

        // Show top card (if any)
        Card top = draw[draw.Count - 1];
        SetTopCardFromCard(top, stateIndex);
    }

    // Play pile rules:
    // - if empty -> no pile sprite, no top card
    // - if exactly 1 card -> use state 0 and show top card
    // - if >=2 cards -> map 2..52 to indices 1..(pileStates.Length-1)
    public void UpdatePlayPileDisplay()
    {
        if (CardManager.Instance == null)
        {
            ClearAll();
            return;
        }

        List<Card> play = CardManager.Instance.playPile;
        int count = (play == null) ? 0 : play.Count;

        // empty play pile -> nothing shown
        if (count == 0)
        {
            if (cardPileSprite != null) cardPileSprite.sprite = null;
            if (topCardSprite != null) { topCardSprite.sprite = null; topCardSprite.enabled = false; }
            return;
        }

        // exactly 1 card -> use state 0 and show top
        if (count == 1)
        {
            SetPileSpriteSafe(0);
            Card topSingle = play[0];
            SetTopCardFromCard(topSingle, 0);
            return;
        }

        // 2 or more cards -> map to indices 1..(pileStates.Length-1)
        int stateIndex = 1;
        if (pileStates != null && pileStates.Length > 1)
        {
            float frac = Mathf.Clamp01((float)count / 52f); // 0..1
            int maxIndex = pileStates.Length - 1;
            stateIndex = 1 + Mathf.FloorToInt(frac * (maxIndex - 1 + 0.0001f));
            stateIndex = Mathf.Clamp(stateIndex, 1, maxIndex);
        }
        else
        {
            stateIndex = Mathf.Clamp(stateIndex, 0, (pileStates != null ? pileStates.Length - 1 : 0));
        }

        SetPileSpriteSafe(stateIndex);

        // top is last card
        Card top = play[play.Count - 1];
        SetTopCardFromCard(top, stateIndex);
    }

    // helpers -------------------------------------------------------

    private void ClearAll()
    {
        if (cardPileSprite != null) cardPileSprite.sprite = null;
        if (topCardSprite != null) { topCardSprite.sprite = null; topCardSprite.enabled = false; }
    }

    private void SetPileSpriteSafe(int index)
    {
        if (cardPileSprite == null) return;
        if (pileStates == null || pileStates.Length == 0)
        {
            cardPileSprite.sprite = null;
            return;
        }

        int safe = Mathf.Clamp(index, 0, pileStates.Length - 1);
        cardPileSprite.sprite = pileStates[safe];
    }

    private void DisableTopCard()
    {
        if (topCardSprite == null) return;
        topCardSprite.sprite = null;
        topCardSprite.enabled = false;
    }

    // set topCardSprite from Card, with fallback to defaultTopSprite if missing and allowed.
    // offsetIndex chooses topCardOffsets entry (optional).
    private void SetTopCardFromCard(Card topCard, int offsetIndex)
    {
        if (topCardSprite == null) return;

        Sprite topSprite = null;
        if (topCard != null && CardDataHandler.Instance != null)
            topSprite = CardDataHandler.Instance.GetSpritePreferPlaced(topCard.cardValue, topCard.cardHouse);

        if (topSprite == null && showBackIfMissing && defaultTopSprite != null)
            topSprite = defaultTopSprite;

        topCardSprite.sprite = topSprite;
        topCardSprite.enabled = (topSprite != null);

        if (topCardOffsets != null && topCardOffsets.Length > 0)
        {
            int safe = Mathf.Clamp(offsetIndex, 0, topCardOffsets.Length - 1);
            topCardSprite.transform.localPosition = topCardOffsets[safe];
        }
    }
}
