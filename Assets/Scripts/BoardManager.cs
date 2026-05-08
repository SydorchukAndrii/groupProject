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
        // Тепер ми передаємо стан погоди у кожен ряд
        p1TotalScore = CalculateRowScore(p1Melee, isMeleeWeatherActive) +
                       CalculateRowScore(p1Ranged, isRangedWeatherActive) +
                       CalculateRowScore(p1Siege, isSiegeWeatherActive);

        p2TotalScore = CalculateRowScore(p2Melee, isMeleeWeatherActive) +
                       CalculateRowScore(p2Ranged, isRangedWeatherActive) +
                       CalculateRowScore(p2Siege, isSiegeWeatherActive);

        Debug.Log($"Scores -> P1: {p1TotalScore} | P2: {p2TotalScore}");
    }

    private int CalculateRowScore(List<CardData> row, bool isWeatherActive)
    {
        int score = 0;
        Dictionary<string, int> tightBondCounts = new Dictionary<string, int>();

        // 1. Рахуємо карти з Міцним зв'язком (Tight Bond)
        foreach (CardData card in row)
        {
            if (card.ability == CardAbility.TightBond)
            {
                if (tightBondCounts.ContainsKey(card.cardName)) tightBondCounts[card.cardName]++;
                else tightBondCounts.Add(card.cardName, 1);
            }
        }

        // 2. Рахуємо фінальні очки кожної карти
        foreach (CardData card in row)
        {
            int currentPower = card.basePower;

            // Якщо карта не є Героєм, на неї діє погода
            if (card.type != CardType.Hero)
            {
                // Якщо погода активна, базова сила стає 1
                if (isWeatherActive)
                {
                    currentPower = 1;
                }

                // Здібності (наприклад, Tight Bond) застосовуються ПІСЛЯ погоди
                if (card.ability == CardAbility.TightBond && tightBondCounts.ContainsKey(card.cardName))
                {
                    int multiplier = tightBondCounts[card.cardName];
                    currentPower *= multiplier; // Навіть якщо погода зробила силу 1, три карти дадуть 1*3 = 3 кожна
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
}