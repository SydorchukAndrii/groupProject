using System.Collections.Generic;
using UnityEngine;
using GwentLogic;

public class PlayerManager : MonoBehaviour
{
    [Header("Configuration")]
    // Drag and drop your CardData ScriptableObjects here in the Inspector
    public List<CardData> startingDeck = new List<CardData>();

    [Header("Current State (Zones)")]
    public List<CardData> deck = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("References")]
    public BoardManager boardManager;

    void Start()
    {
        InitializeDeck();
        DrawStartingHand();
    }

    // Copies the starting deck to the active deck and shuffles it
    private void InitializeDeck()
    {
        deck = new List<CardData>(startingDeck);
        ShuffleDeck();
    }

    // Fisher-Yates shuffle algorithm
    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
        Debug.Log("Deck shuffled.");
    }

    // Logic for drawing a single card from the deck to the hand
    public void DrawCard()
    {
        if (deck.Count > 0)
        {
            CardData drawnCard = deck[0]; // Take the top card
            deck.RemoveAt(0);             // Remove it from the deck
            hand.Add(drawnCard);          // Add it to the player's hand

            Debug.Log($"Drawn card: {drawnCard.cardName}");
        }
        else
        {
            Debug.Log("Deck is empty!");
        }
    }

    // Draws 10 cards at the beginning of the match
    private void DrawStartingHand()
    {
        Debug.Log("Drawing starting hand...");
        for (int i = 0; i < 10; i++)
        {
            DrawCard();
        }
    }

    void Update()
    {
        // PRESS SPACE TO PLAY THE FIRST CARD IN HAND
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (hand.Count > 0)
            {
                // Граємо найпершу карту з руки
                boardManager.PlayCard(this, hand[0]);
            }
            else
            {
                Debug.Log("Hand is empty, cannot play any more cards.");
            }
        }
    }
}