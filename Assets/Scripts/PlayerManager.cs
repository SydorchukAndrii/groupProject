using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System
#endif
using GwentLogic;

public class PlayerManager : MonoBehaviour
{
    [Header("Configuration")]
    public List<CardData> startingDeck = new List<CardData>();

    [Header("Player Settings")]
    public bool isPlayer1; // Check this in the Inspector for Player 1, uncheck for Player 2
    public bool hasPassed = false; // Has the player passed this round?

    [Header("Current State (Zones)")]
    public List<CardData> deck = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("References")]
    public BoardManager boardManager;
    public GameManager gameManager; // Reference to the GameManager

    void Start()
    {
        InitializeDeck();
        DrawStartingHand();
    }

    private void InitializeDeck()
    {
        deck = new List<CardData>(startingDeck);
        // Set ownership
        foreach (CardData card in deck)
        {
            card.owner = this;
        }
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
        Debug.Log($"{(isPlayer1 ? "Player 1" : "Player 2")} deck shuffled.");
    }

    public void DrawCard()
    {
        if (deck.Count > 0)
        {
            CardData drawnCard = deck[0];
            deck.RemoveAt(0);
            hand.Add(drawnCard);
        }
    }

    private void DrawStartingHand()
    {
        for (int i = 0; i < 10; i++)
        {
            DrawCard();
        }
    }

    void Update()
    {
        // 1. If the player has already passed, they can't do anything
        if (hasPassed) return;

        // 2. Determine if it is currently this player's turn
        bool isMyTurn = (isPlayer1 && gameManager.currentState == GameState.Player1Turn) ||
                        (!isPlayer1 && gameManager.currentState == GameState.Player2Turn);

        // 3. If it's not my turn, ignore input
        if (!isMyTurn) return;

        // 4. Input detection for playing a card and passing
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return;

        // P1 uses Space to play, P uses to Pass
        // P2 uses Enter to play, L uses to Pass
        bool playKey = isPlayer1 ? Keyboard.current.spaceKey.wasPressedThisFrame : Keyboard.current.enterKey.wasPressedThisFrame;
        bool passKey = isPlayer1 ? Keyboard.current.pKey.wasPressedThisFrame : Keyboard.current.lKey.wasPressedThisFrame;

        if (playKey)
        {
            PlayFirstCardInHand();
        }
        else if (passKey)
        {
            PassTurn();
        }
#else
        // Legacy Input Manager fallback (Project Settings -> Player -> Active Input Handling)
        bool playKey = isPlayer1 ? Input.GetKeyDown(KeyCode.Space) : Input.GetKeyDown(KeyCode.Return);
        bool passKey = isPlayer1 ? Input.GetKeyDown(KeyCode.P) : Input.GetKeyDown(KeyCode.L);

        if (playKey)
        {
            PlayFirstCardInHand();
        }
        else if (passKey)
        {
            PassTurn();
        }
#endif
    }

    private void PlayFirstCardInHand()
    {
        if (hand.Count > 0)
        {
            boardManager.PlayCard(this, hand[0]);

            // In Gwent, after playing one card, your turn ends
            gameManager.EndTurn();
        }
        else
        {
            Debug.Log($"{(isPlayer1 ? "Player 1" : "Player 2")} hand is empty!");
        }
    }

    private void PassTurn()
    {
        hasPassed = true;
        Debug.Log($"{(isPlayer1 ? "Player 1" : "Player 2")} has PASSED the round.");
        gameManager.EndTurn();
    }
}