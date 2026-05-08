using System.Collections.Generic;
using UnityEngine;
using GwentLogic;

public class BoardManager : MonoBehaviour
{
    [Header("Player 1 Board Rows")]
    public List<CardData> meleeRow = new List<CardData>();
    public List<CardData> rangedRow = new List<CardData>();
    public List<CardData> siegeRow = new List<CardData>();

    [Header("Player 1 Scores")]
    public int meleeScore = 0;
    public int rangedScore = 0;
    public int siegeScore = 0;
    public int totalScore = 0;

    // Method to play a card from the player's hand to the board
    public void PlayCard(PlayerManager player, CardData card)
    {
        // 1. Check if the card is actually in the player's hand
        if (!player.hand.Contains(card))
        {
            Debug.LogWarning("Cannot play this card: It is not in the hand!");
            return;
        }

        // 2. Remove the card from the hand
        player.hand.Remove(card);

        // 3. Place the card in the correct row based on its allowedRow property
        switch (card.allowedRow)
        {
            case CardRow.Melee:
                meleeRow.Add(card);
                break;
            case CardRow.Ranged:
                rangedRow.Add(card);
                break;
            case CardRow.Siege:
                siegeRow.Add(card);
                break;
            case CardRow.Any:
                // For MVP backend test, default "Any" to Melee. 
                // Later, the UI will let the player choose the row.
                meleeRow.Add(card);
                break;
            default:
                Debug.LogWarning("This card type cannot be played on the unit board.");
                return; // Stop execution if it's weather or special (for now)
        }

        Debug.Log($"Successfully played {card.cardName} to the {card.allowedRow} row.");

        // 4. Recalculate scores after the board state changes
        CalculateScores();
    }

    // Calculates the score for all rows
    public void CalculateScores()
    {
        meleeScore = CalculateRowScore(meleeRow);
        rangedScore = CalculateRowScore(rangedRow);
        siegeScore = CalculateRowScore(siegeRow);

        totalScore = meleeScore + rangedScore + siegeScore;

        Debug.Log($"Scores updated. Total Score: {totalScore}");
    }

    // Helper method to sum up the base power of cards in a specific row
    private int CalculateRowScore(List<CardData> row)
    {
        int score = 0;
        foreach (CardData card in row)
        {
            score += card.basePower;
        }
        return score;
    }
}