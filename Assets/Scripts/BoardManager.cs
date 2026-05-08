using System;
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

    [Header("Weather States")]
    public bool isMeleeWeatherActive = false;  // Frost
    public bool isRangedWeatherActive = false; // Fog
    public bool isSiegeWeatherActive = false;  // Rain

    [Header("Horn States")]
    public bool p1MeleeHorn = false;
    public bool p1RangedHorn = false;
    public bool p1SiegeHorn = false;

    public bool p2MeleeHorn = false;
    public bool p2RangedHorn = false;
    public bool p2SiegeHorn = false;

    // --- Events for UI ---
    // Ці події "вистрілюватимуть" щоразу, коли щось змінюється на столі
    public event Action OnScoresUpdated;
    public event Action OnBoardCleared;

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

        // --- WEATHER LOGIC ---
        if (card.type == CardType.Weather)
        {
            Debug.Log($"*** WEATHER CARD PLAYED: {card.cardName} ***");
            ApplyWeather(card);

            // Погодні карти не лежать у рядах із загонами, ми їх просто скидаємо у відбій гравця після активації
            // В оригіналі вони лежать у спеціальній зоні, але для бекенду простіше так
            player.discardPile.Add(card);
            CalculateScores();

            // Закінчуємо розіграш, щоб вона не пішла в звичайні ряди
            if (!card.ability.Equals(CardAbility.Spy) && !card.ability.Equals(CardAbility.Medic))
            {
                // Передаємо хід, бо гравець розіграв погоду
                player.gameManager.EndTurn();
            }
            return;
        }

        // --- SPECIAL CARDS LOGIC (Horn & Scorch) ---
        if (card.type == CardType.Special)
        {
            Debug.Log($"*** SPECIAL CARD PLAYED: {card.cardName} ***");

            if (card.ability == CardAbility.CommandersHorn)
            {
                ApplyHorn(player, card);
            }
            else if (card.ability == CardAbility.Scorch)
            {
                ExecuteScorch(player);
            }

            // Special cards go to the discard pile immediately after use
            player.discardPile.Add(card);
            CalculateScores();
            player.gameManager.EndTurn();
            return;
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
        // Передаємо погоду І наявність рогу
        p1TotalScore = CalculateRowScore(p1Melee, isMeleeWeatherActive, p1MeleeHorn) +
                       CalculateRowScore(p1Ranged, isRangedWeatherActive, p1RangedHorn) +
                       CalculateRowScore(p1Siege, isSiegeWeatherActive, p1SiegeHorn);

        p2TotalScore = CalculateRowScore(p2Melee, isMeleeWeatherActive, p2MeleeHorn) +
                       CalculateRowScore(p2Ranged, isRangedWeatherActive, p2RangedHorn) +
                       CalculateRowScore(p2Siege, isSiegeWeatherActive, p2SiegeHorn);

        Debug.Log($"Scores -> P1: {p1TotalScore} | P2: {p2TotalScore}");

        OnScoresUpdated?.Invoke();
    }

    private int CalculateRowScore(List<CardData> row, bool isWeatherActive, bool isHornActive)
    {
        int score = 0;
        Dictionary<string, int> tightBondCounts = new Dictionary<string, int>();

        foreach (CardData card in row)
        {
            if (card.ability == CardAbility.TightBond)
            {
                if (tightBondCounts.ContainsKey(card.cardName)) tightBondCounts[card.cardName]++;
                else tightBondCounts.Add(card.cardName, 1);
            }
        }

        foreach (CardData card in row)
        {
            int currentPower = card.basePower;

            if (card.type != CardType.Hero)
            {
                if (isWeatherActive) currentPower = 1;

                if (card.ability == CardAbility.TightBond && tightBondCounts.ContainsKey(card.cardName))
                {
                    currentPower *= tightBondCounts[card.cardName];
                }

                // Apply Commander's Horn (doubles the final calculated power)
                if (isHornActive)
                {
                    currentPower *= 2;
                }
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

        isMeleeWeatherActive = false;
        isRangedWeatherActive = false;
        isSiegeWeatherActive = false;

        // Reset Horns
        p1MeleeHorn = false; p1RangedHorn = false; p1SiegeHorn = false;
        p2MeleeHorn = false; p2RangedHorn = false; p2SiegeHorn = false;

        CalculateScores();
        Debug.Log("Board cleared. Cards moved to correct discard piles.");

        OnBoardCleared?.Invoke();
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

    private void ApplyWeather(CardData card)
    {
        // Визначаємо, який ряд морозить ця карта, на основі її allowedRow
        if (card.allowedRow == CardRow.Melee) isMeleeWeatherActive = true;
        else if (card.allowedRow == CardRow.Ranged) isRangedWeatherActive = true;
        else if (card.allowedRow == CardRow.Siege) isSiegeWeatherActive = true;
        else if (card.allowedRow == CardRow.None)
        {
            // Якщо allowedRow == None, вважаємо, що це карта "Ясне небо" (Clear Skies)
            isMeleeWeatherActive = false;
            isRangedWeatherActive = false;
            isSiegeWeatherActive = false;
            Debug.Log("Clear Skies played! All weather effects removed.");
        }
    }

    private void ApplyHorn(PlayerManager player, CardData card)
    {
        // Apply horn to the specific row of the player who played it
        if (player.isPlayer1)
        {
            if (card.allowedRow == CardRow.Melee) p1MeleeHorn = true;
            else if (card.allowedRow == CardRow.Ranged) p1RangedHorn = true;
            else if (card.allowedRow == CardRow.Siege) p1SiegeHorn = true;
        }
        else
        {
            if (card.allowedRow == CardRow.Melee) p2MeleeHorn = true;
            else if (card.allowedRow == CardRow.Ranged) p2RangedHorn = true;
            else if (card.allowedRow == CardRow.Siege) p2SiegeHorn = true;
        }
    }

    private void ExecuteScorch(PlayerManager playerWhoPlayed)
    {
        Debug.Log("Executing SCORCH! Searching for the highest power unit...");
        int maxPower = 0;
        List<CardData> cardsToDestroy = new List<CardData>();

        // We need to check all 6 rows to find the highest power (Heroes are immune)
        // Helper action to check a row
        System.Action<List<CardData>> checkRow = (row) =>
        {
            foreach (CardData c in row)
            {
                if (c.type == CardType.Hero) continue; // Heroes immune to Scorch

                // We need its ACTUAL power (with weather/bond), not base power
                // For MVP, let's do a simple check. To be perfectly accurate, 
                // we should calculate its current dynamic power. 
                // To keep the backend fast, we'll check its basePower for now.
                // (Note: In a full release, Scorch calculates after Weather/Horn).
                int currentPower = c.basePower;

                if (currentPower > maxPower)
                {
                    maxPower = currentPower;
                    cardsToDestroy.Clear(); // New highest found, clear previous targets
                    cardsToDestroy.Add(c);
                }
                else if (currentPower == maxPower && maxPower > 0)
                {
                    cardsToDestroy.Add(c); // Tie for highest, add to destruction list
                }
            }
        };

        checkRow(p1Melee); checkRow(p1Ranged); checkRow(p1Siege);
        checkRow(p2Melee); checkRow(p2Ranged); checkRow(p2Siege);

        // Now destroy them (remove from board, add to discard)
        foreach (CardData target in cardsToDestroy)
        {
            Debug.Log($"SCORCH destroys: {target.cardName} (Power: {maxPower})");

            // Remove from board rows
            if (p1Melee.Contains(target)) p1Melee.Remove(target);
            else if (p1Ranged.Contains(target)) p1Ranged.Remove(target);
            else if (p1Siege.Contains(target)) p1Siege.Remove(target);
            else if (p2Melee.Contains(target)) p2Melee.Remove(target);
            else if (p2Ranged.Contains(target)) p2Ranged.Remove(target);
            else if (p2Siege.Contains(target)) p2Siege.Remove(target);

            // Send to owner's discard pile
            if (target.owner != null) target.owner.discardPile.Add(target);
        }
    }
}