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

        // Check if the card is a Spy
        bool isSpy = card.ability == CardAbility.Spy;

        // If P1 plays a normal card OR P2 plays a spy -> it goes to P1's side
        if ((player.isPlayer1 && !isSpy) || (!player.isPlayer1 && isSpy))
        {
            PlaceCardInRow(card, p1Melee, p1Ranged, p1Siege);
        }
        // If P2 plays a normal card OR P1 plays a spy -> it goes to P2's side
        else
        {
            PlaceCardInRow(card, p2Melee, p2Ranged, p2Siege);
        }

        // --- SPY LOGIC ---
        if (isSpy)
        {
            Debug.Log($"*** SPY PLAYED! {(player.isPlayer1 ? "Player 1" : "Player 2")} draws 2 cards! ***");
            player.DrawCard();
            player.DrawCard();
        }

        // --- MEDIC LOGIC ---
        if (card.ability == CardAbility.Medic)
        {
            Debug.Log($"*** MEDIC PLAYED by {(player.isPlayer1 ? "Player 1" : "Player 2")}! Searching graveyard... ***");
            ReviveCardFromDiscard(player);
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

        // Dictionary to count how many cards with TightBond share the same name in this row
        Dictionary<string, int> tightBondCounts = new Dictionary<string, int>();

        // First pass: Count the Tight Bond cards
        foreach (CardData card in row)
        {
            if (card.ability == CardAbility.TightBond)
            {
                if (tightBondCounts.ContainsKey(card.cardName))
                    tightBondCounts[card.cardName]++;
                else
                    tightBondCounts.Add(card.cardName, 1);
            }
        }

        // Second pass: Calculate actual score
        foreach (CardData card in row)
        {
            int currentPower = card.basePower;

            // Apply Tight Bond multiplier if applicable
            if (card.ability == CardAbility.TightBond && tightBondCounts.ContainsKey(card.cardName))
            {
                int multiplier = tightBondCounts[card.cardName];
                currentPower *= multiplier;
            }

            score += currentPower;
        }

        return score;
    }

    // Clears all cards from the board and resets scores to 0
    public void ClearBoard()
    {
        MoveToDiscard(p1Melee); MoveToDiscard(p1Ranged); MoveToDiscard(p1Siege);
        MoveToDiscard(p2Melee); MoveToDiscard(p2Ranged); MoveToDiscard(p2Siege);

        CalculateScores();
        Debug.Log("Board cleared. Cards moved to correct discard piles.");
    }

    private void MoveToDiscard(List<CardData> row)
    {
        foreach (CardData card in row)
        {
            // Карта йде у відбій того гравця, в чиїй колоді вона починала гру
            if (card.owner != null)
            {
                card.owner.discardPile.Add(card);
            }
        }
        row.Clear();
    }

    // Helper method to find and revive a card
    private void ReviveCardFromDiscard(PlayerManager player)
    {
        if (player.discardPile.Count == 0)
        {
            Debug.Log("Discard pile is empty. Medic has nobody to revive.");
            return;
        }

        CardData cardToRevive = null;

        // Find the first valid Unit in the graveyard
        foreach (CardData c in player.discardPile)
        {
            // Перевіряємо, що це звичайний загін (Unit), і що це не Шпигун
            if (c.type == CardType.Unit && c.ability != CardAbility.Spy)
            {
                cardToRevive = c;
                break;
            }
        }

        if (cardToRevive != null)
        {
            Debug.Log($"Medic successfully revived: {cardToRevive.cardName}!");

            // Remove from graveyard
            player.discardPile.Remove(cardToRevive);

            // Temporarily add to hand so PlayCard logic can process it properly
            player.hand.Add(cardToRevive);

            // Play it recursively (this will place it in the correct row and trigger abilities)
            PlayCard(player, cardToRevive);
        }
        else
        {
            Debug.Log("No valid units to revive in the discard pile.");
        }
    }
}