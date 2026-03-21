using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Player> players = new List<Player>();
    public float turnDelay = 0.5f;

    private Queue<Player> turnQueue = new Queue<Player>();

    public UnityEvent onTurnChange;

    public UnityEvent onHandCleared;

    public UnityEvent onGameEnd;

    public TextMeshProUGUI turnText;

    public Player currentPlayer;

    public GameObject endScreen;

    public GameObject environment;

    public int handsClearedToWin = 3;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        environment.SetActive(true);
        endScreen.SetActive(false);
        Invoke("StartGame", 3f);
    }

    void EndGame()
    {
        endScreen.SetActive(true);
        environment.SetActive(false);

    }

    public void StartGame()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogError("CardManager missing");
            return;
        }

        CardManager.Instance.InitCards();
        CardManager.Instance.ShuffleCards(CardManager.Instance.cards);

        // Deal 6 cards to each player
        for (int i = 0; i < 6; i++)
        {
            foreach (var player in players)
            {
                player.DrawCard(CardManager.Instance);
            }
        }

        // GameManager.cs excerpt in Start()
        Card first = CardManager.Instance.DrawTopCard();
        if (first != null)
        {
            // use CardManager method which invokes events
            CardManager.Instance.StartPlayPile(first);
            Debug.Log($"Starting card: {first.cardValue} of {first.cardHouse}");
        }


        onTurnChange.Invoke();

        // Setup turn order
        turnQueue.Clear();
        foreach (var player in players)
        {
            turnQueue.Enqueue(player);
        }

        StartCoroutine(RunTurns());
    }

    IEnumerator RunTurns()
    {
        while (true)
        {
            if (turnQueue.Count == 0) yield break;

            Player current = turnQueue.Dequeue();
            currentPlayer = current;
            Debug.Log($"RunTurns: current = {current.playerName} id={current.GetInstanceID()} hasPlayed={current.hasPlayedThisTurn}");
            turnQueue.Enqueue(current);

            Card topCard = CardManager.Instance.GetTopCard();
            current.StartTurn(topCard);
            if (turnText != null) turnText.text = current.turnText;
            onTurnChange.Invoke();

            // Wait until player finishes their action
            while (!current.hasPlayedThisTurn)
            {
                Debug.Log("Waiting for" + current.name + " to play...");
                yield return new WaitForSeconds(0.1f);
            }

            // If the draw pile isn't empty, draw 3 cards; otherwise player wins
            if (current.hand.Count == 0)
            {
                if (CardManager.Instance.cards.Count > 0)
                {
                    int drawCount = Mathf.Min(3, CardManager.Instance.cards.Count);
                    for (int i = 0; i < drawCount; i++)
                        current.DrawCard(CardManager.Instance);
                    Debug.Log($"{current.playerName} emptied hand but draw pile exists. Drew {drawCount} cards.");
                    current.handsCleared += 1;
                    onHandCleared.Invoke();

                    if (current.handsCleared >= handsClearedToWin)
                    {
                        Debug.Log($"{current.playerName} WINS!");
                        Invoke("EndGame", 6f);
                        onGameEnd.Invoke();
                        yield break;
                    }
                }
                else
                {
                    Debug.Log($"{current.playerName} WINS!");
                    Invoke("EndGame", 6f);
                    onGameEnd.Invoke();
                    yield break;
                }
            }

            // Handle extra turn
            if (current.extraTurn)
            {
                current.extraTurn = false;
                if (turnQueue.Count > 0)
                {
                    Player next = turnQueue.Dequeue();
                    turnQueue.Enqueue(next);
                }
            }

            yield return new WaitForSeconds(turnDelay);
            onTurnChange.Invoke();
        }
    }
}
