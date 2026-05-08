using System.Collections.Generic;
using UnityEngine;
using GwentLogic;

public class BoardManager : MonoBehaviour
{
    [Header("Player 1 Board")]
    public List<CardData> p1Melee = new List<CardData>();
    public List<CardData> p1Ranged = new List<CardData>();
    public List<CardData> p1Siege = new List<CardData>();
    public int p1TotalScore = 0;

    [Header("Player 2 Board")]
    public List<CardData> p2Melee = new List<CardData>();
    public List<CardData> p2Ranged = new List<CardData>();
    public List<CardData> p2Siege = new List<CardData>();
    public int p2TotalScore = 0;

    public void PlayCard(PlayerManager player, CardData card)
    {
        if (!player.hand.Contains(card)) return;

        player.hand.Remove(card);

        // Place card on the correct side of the board based on who played it
        if (player.isPlayer1)
        {
            PlaceCardInRow(card, p1Melee, p1Ranged, p1Siege);
        }
        else
        {
            PlaceCardInRow(card, p2Melee, p2Ranged, p2Siege);
        }

        CalculateScores();
    }

    private void PlaceCardInRow(CardData card, List<CardData> melee, List<CardData> ranged, List<CardData> siege)
    {
        switch (card.allowedRow)
        {
            case CardRow.Melee: melee.Add(card); break;
            case CardRow.Ranged: ranged.Add(card); break;
            case CardRow.Siege: siege.Add(card); break;
            case CardRow.Any: melee.Add(card); break; // Default to melee for now
        }
        Debug.Log($"Card {card.cardName} placed in {card.allowedRow} row.");
    }

    public void CalculateScores()
    {
        p1TotalScore = CalculateRowScore(p1Melee) + CalculateRowScore(p1Ranged) + CalculateRowScore(p1Siege);
        p2TotalScore = CalculateRowScore(p2Melee) + CalculateRowScore(p2Ranged) + CalculateRowScore(p2Siege);

        Debug.Log($"Scores -> P1: {p1TotalScore} | P2: {p2TotalScore}");
    }

    private int CalculateRowScore(List<CardData> row)
    {
        int score = 0;
        foreach (CardData card in row) score += card.basePower;
        return score;
    }

    // Clears all cards from the board and resets scores to 0
    public void ClearBoard()
    {
        p1Melee.Clear(); p1Ranged.Clear(); p1Siege.Clear();
        p2Melee.Clear(); p2Ranged.Clear(); p2Siege.Clear();
        CalculateScores();
        Debug.Log("Board cleared for the next round.");
    }
}