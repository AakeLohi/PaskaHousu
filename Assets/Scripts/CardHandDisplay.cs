using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardHandDisplay : MonoBehaviour
{
    public GameObject cardButtonPrefab;
    public HumanPlayer humanPlayer; // assign in inspector
    public GameObject playMultipleCardsButton;
    public GameObject drawButton; // assign in inspector

    // cache for created buttons (aligned with lastHand by index)
    private readonly List<GameObject> cardButtons = new List<GameObject>();
    private readonly List<Card> lastHand = new List<Card>();
    private readonly List<CardButtonUI> selectedButtons = new List<CardButtonUI>();

    public float onTurnCardPosY;
    public float offTurnCardPosY;
    private Vector3 originalPos;

    // animated cards list (kept as-is)
    private class AnimatedCard
    {
        public GameObject obj;
        public Vector3 start;
        public Vector3 target;
        public float duration;
        public float elapsed;
    }
    private readonly List<AnimatedCard> animatedCards = new List<AnimatedCard>();

    [SerializeField] private Vector3 cardPileTargetPosition;
    [SerializeField] private float animationSpeed = 1000f;

    void Start()
    {
        originalPos = this.gameObject.transform.localPosition;
    }

    void Update()
    {
        // raise or lower the whole hand depending on turn
        if (IsPlayersTurn())
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos + Vector3.up * onTurnCardPosY, 15f * Time.deltaTime);
        else
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos + Vector3.up * offTurnCardPosY, 15f * Time.deltaTime);

        // ensure all existing buttons approach scale 1 (pop-in/new ones should start at Vector3.zero)
        for (int i = 0; i < cardButtons.Count; i++)
        {
            var cb = cardButtons[i];
            if (cb == null) continue;
            cb.transform.localScale = Vector3.Lerp(cb.transform.localScale, Vector3.one, 15f * Time.deltaTime);
        }

        // existing fly-away animations
        for (int i = animatedCards.Count - 1; i >= 0; i--)
        {
            var ac = animatedCards[i];
            ac.elapsed += Time.deltaTime;
            float t = ac.duration <= 0f ? 1f : Mathf.Clamp01(ac.elapsed / ac.duration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            if (ac.obj != null)
            {
                ac.obj.transform.localPosition = Vector3.Lerp(ac.start, ac.target, s);
                ac.obj.transform.localScale = Vector3.one * (1f - 0.5f * t);
                var img = ac.obj.GetComponent<Image>();
                if (img != null)
                    img.color = new Color(1f, 1f, 1f, 1f - t);
            }

            if (t >= 1f)
            {
                if (ac.obj != null) Destroy(ac.obj);
                animatedCards.RemoveAt(i);
            }
        }
    }

    // Public: rebuild the hand UI. Call after any hand change.
    public void DisplayCards()
    {
        if (humanPlayer == null) return;
        var hand = humanPlayer.hand;
        if (hand == null) return;

        // Prepare UI controls
        if (playMultipleCardsButton != null) playMultipleCardsButton.SetActive(false);
        if (drawButton != null) drawButton.SetActive(false);

        Card top = CardManager.Instance != null ? CardManager.Instance.GetTopCard() : null;
        int count = hand.Count;
        if (count == 0)
        {
            // If hand became empty, clear and destroy any child buttons that are still children here.
            for (int i = cardButtons.Count - 1; i >= 0; i--)
            {
                var go = cardButtons[i];
                if (go != null && go.transform.parent == transform) Destroy(go);
            }
            cardButtons.Clear();
            lastHand.Clear();
            selectedButtons.Clear();
            return;
        }

        // Build a map of which old slots were used
        bool[] usedOld = new bool[lastHand.Count];
        var newButtons = new List<GameObject>(count);

        float radius = 300f;
        float angleStep = 10f;
        float startAngle = -angleStep * (count - 1) / 2f;

        int playableCount = 0;

        // For each card in the new hand, try to find an existing slot with the same Card reference.
        for (int i = 0; i < count; i++)
        {
            var card = hand[i];
            GameObject reusedGo = null;
            int foundIndex = -1;

            // look for a matching card in lastHand that hasn't been used yet
            for (int j = 0; j < lastHand.Count; j++)
            {
                if (usedOld[j]) continue;
                if (ReferenceEquals(lastHand[j], card))
                {
                    // reuse this slot
                    if (j < cardButtons.Count)
                        reusedGo = cardButtons[j];
                    foundIndex = j;
                    break;
                }
            }

            // if we found an existing GameObject but it was destroyed, null it
            if (reusedGo != null && reusedGo == null) reusedGo = null;

            // position & rotation calculations
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad) * 2f, Mathf.Cos(rad) * 2f, 0f) * radius;
            Vector3 finalLocalPos = new Vector3(offset.x, (offset.y - 580f) / Mathf.Max(1, count), offset.z);
            Quaternion rotation = Quaternion.Euler(0f, 0f, ((finalLocalPos.x * -0.01f) * 25f) / Mathf.Max(1, count));

            GameObject go;

            if (reusedGo != null)
            {
                // reuse existing button
                go = reusedGo;
                usedOld[foundIndex] = true;

                // re-parent if needed (some reused objects might have been animated/deparented; re-parent them back)
                if (go.transform.parent != transform)
                    go.transform.SetParent(transform, false);

                // update transform immediately
                go.transform.localPosition = finalLocalPos;
                go.transform.localRotation = rotation;

                // keep existing scale (so already-visible cards don't pop)
            }
            else
            {
                // create a new card button
                go = Instantiate(cardButtonPrefab, transform);

                // start small so the Update() lerp gives pop-in
                go.transform.localScale = Vector3.zero;

                go.transform.localPosition = finalLocalPos;
                go.transform.localRotation = rotation;
            }

            // ensure CardButtonUI exists and is up-to-date
            var btnUI = go.GetComponent<CardButtonUI>() ?? go.AddComponent<CardButtonUI>();
            btnUI.Initialize();
            btnUI.card = card;
            btnUI.button = go.GetComponent<Button>();

            // set image if available
            var img = go.GetComponent<Image>();
            if (img != null && card.cardSprite != null) img.sprite = card.cardSprite;

            // set interactability based on playability
            bool playable = IsCardPlayable(card, top);
            btnUI.SetInteractable(playable);
            if (playable) playableCount++;

            // reset and assign listener
            btnUI.button.onClick.RemoveAllListeners();
            btnUI.button.onClick.AddListener(() => OnCardClicked(btnUI));

            newButtons.Add(go);
        }

        // Clean up old buttons that were not reused.
        for (int j = 0; j < lastHand.Count; j++)
        {
            if (j < cardButtons.Count && !usedOld[j])
            {
                var oldGo = cardButtons[j];
                if (oldGo == null) continue;
                // If it is still a child of this display, destroy it.
                // If it was deparented (for animation), leave it alone.
                if (oldGo.transform.parent == transform)
                    Destroy(oldGo);
            }
        }

        // Replace caches
        cardButtons.Clear();
        cardButtons.AddRange(newButtons);

        // clear selection (safer) and update lastHand snapshot
        selectedButtons.Clear();
        lastHand.Clear();
        lastHand.AddRange(hand);

        // draw button if no playable cards but there are cards
        if (playableCount == 0 && drawButton != null && count != 0 && IsPlayersTurn())
        {
            drawButton.SetActive(true);
            var db = drawButton.GetComponent<Button>();
            db.onClick.RemoveAllListeners();
            db.onClick.AddListener(() =>
            {
                if (!IsPlayersTurn()) return;
                humanPlayer.DrawPlayPile(CardManager.Instance);
                DisplayCards();
            });
        }
        else
        {
            drawButton.SetActive(false);
        }
    }

    private bool IsCardPlayable(Card card, Card top)
    {
        if (card == null) return false;

        // special always-playable values
        if (card.cardValue == 0) return true;
        if (card.cardValue == 1) return true;
        if (card.cardValue == 2) return true;
        if (card.cardValue == 10) return true;
        if (top == null) return true;

        if (top.cardValue == 1) return (card.cardValue == 1 || card.cardValue == 0 || card.cardValue == 10 || card.cardValue == 2);
        if (top.cardValue == 2) return true;

        return card.cardValue >= top.cardValue;
    }

    // returns true if humanPlayer is the current player according to GameManager.CurrentPlayer
    private bool IsPlayersTurn()
    {
        var gm = GameManager.Instance;
        if (gm == null) return true; // be permissive if no game manager found
        return ReferenceEquals(gm.currentPlayer, humanPlayer) && !humanPlayer.hasPlayedThisTurn;
    }

    private void OnCardClicked(CardButtonUI clicked)
    {
        if (clicked == null || clicked.card == null) return;

        // Disallow interacting when not player's turn
        if (!IsPlayersTurn()) return;

        // only allow playable buttons
        if (!clicked.IsInteractable()) return;

        // if selecting a different value, clear old selection
        if (selectedButtons.Count > 0 && clicked.card.cardValue != selectedButtons[0].card.cardValue)
        {
            foreach (var b in selectedButtons) b.SetHighlight(false);
            selectedButtons.Clear();
        }

        // toggle selection
        if (selectedButtons.Contains(clicked))
        {
            clicked.SetHighlight(false);
            selectedButtons.Remove(clicked);
        }
        else
        {
            clicked.SetHighlight(true);
            selectedButtons.Add(clicked);
        }

        // auto-play if this is the only duplicate of that value
        int duplicates = humanPlayer.hand.Count(c => c.cardValue == clicked.card.cardValue);
        if (selectedButtons.Count == 1 && duplicates == 1 && IsPlayersTurn())
        {
            StartCardPlayedAnimation(new GameObject[] { clicked.gameObject });

            humanPlayer.PlaySelectedCard(clicked.card);

            // tidy up selection & UI
            clicked.SetHighlight(false);
            selectedButtons.Clear();
            if (playMultipleCardsButton != null) playMultipleCardsButton.SetActive(false);

            // refresh UI after play (successful or fallback draw)
            DisplayCards();
            return;
        }

        // if there are other playable duplicates, show multi-play button
        Card top = CardManager.Instance != null ? CardManager.Instance.GetTopCard() : null;
        int playableDuplicates = humanPlayer.hand.Count(c => c.cardValue == clicked.card.cardValue && IsCardPlayable(c, top));
        if (playMultipleCardsButton != null) playMultipleCardsButton.SetActive(playableDuplicates > 1);
    }

    // called by UI button
    public void OnPlayMultipleButton()
    {
        if (!IsPlayersTurn()) return;
        if (selectedButtons.Count == 0) return;

        var cardsToPlay = selectedButtons.Select(b => b.card).ToList();
        Card top = CardManager.Instance != null ? CardManager.Instance.GetTopCard() : null;
        if (!IsBatchPlayable(cardsToPlay, top))
        {
            foreach (var b in selectedButtons) b.SetHighlight(false);
            selectedButtons.Clear();
            if (playMultipleCardsButton != null) playMultipleCardsButton.SetActive(false);
            return;
        }

        // animate AFTER attempting plays so UI destruction doesn't remove animated objects
        var uiObjects = selectedButtons.Select(b => b.gameObject).ToArray();
        StartCardPlayedAnimation(uiObjects);

        humanPlayer.PlaySelectedCards(cardsToPlay);

        foreach (var b in selectedButtons) b.SetHighlight(false);
        selectedButtons.Clear();
        if (playMultipleCardsButton != null) playMultipleCardsButton.SetActive(false);

        DisplayCards();
    }

    private bool IsBatchPlayable(List<Card> batch, Card initialTop)
    {
        if (batch == null || batch.Count == 0) return false;
        int value = batch[0].cardValue;
        if (batch.Any(c => c.cardValue != value)) return false;

        Card simulatedTop = initialTop;
        foreach (var c in batch)
        {
            if (!IsCardPlayable(c, simulatedTop)) return false;
            if (c.cardValue == 10) simulatedTop = null;
            else simulatedTop = c;
        }
        return true;
    }

    public void StartCardPlayedAnimation(GameObject[] cardObjects)
    {
        if (cardObjects == null || cardObjects.Length == 0) return;
        foreach (var go in cardObjects)
        {
            if (go == null) continue;
            // move out of this parent so DisplayCards won't destroy it
            go.transform.SetParent(this.gameObject.transform.parent);
            Vector3 startPos = go.transform.localPosition;
            Vector3 targetPos = cardPileTargetPosition;
            float dist = Vector3.Distance(startPos, targetPos);
            float duration = animationSpeed <= 0f ? 0.2f : Mathf.Max(0.02f, dist / animationSpeed);

            animatedCards.Add(new AnimatedCard()
            {
                obj = go,
                start = startPos,
                target = targetPos,
                duration = duration,
                elapsed = 0f
            });
        }
    }
}


public class CardButtonUI : MonoBehaviour
{
    public Card card;
    public Button button;
    public TextMeshProUGUI text;
    private Image img;

    public Vector3 originalLocalPos;
    private Color originalColor;
    private bool interactable = true;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        img = GetComponent<Image>();
        originalLocalPos = transform.localPosition;
        if (img != null)
            originalColor = Color.white;
    }

    public void SetHighlight(bool highlight)
    {
        Vector3 lift = new Vector3(0f, 30f, 0f);
        transform.localPosition = highlight ? originalLocalPos + lift : originalLocalPos;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (button != null)
            button.interactable = value;

        if (img != null)
            img.color = value ? originalColor : Color.gray;
    }

    public bool IsInteractable() => interactable;
}
