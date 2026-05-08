using UnityEngine;
using GwentLogic;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState;

    [Header("References")]
    public PlayerManager player1;
    public PlayerManager player2;
    public BoardManager boardManager;

    [Header("Match Stats")]
    public int p1Lives = 2;
    public int p2Lives = 2;

    void Start()
    {
        // Start the game with the setup phase
        ChangeState(GameState.Setup);
    }

    // Main method to handle phase transitions
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"=== Game State Changed: {currentState} ===");

        switch (currentState)
        {
            case GameState.Setup:
                // Move to Player 1's turn after setup
                ChangeState(GameState.Player1Turn);
                break;

            case GameState.Player1Turn:
                if (player1.hasPassed)
                {
                    Debug.Log("Player 1 has passed. Automatically ending turn.");
                    EndTurn();
                }
                break;

            case GameState.Player2Turn:
                if (player2.hasPassed)
                {
                    Debug.Log("Player 2 has passed. Automatically ending turn.");
                    EndTurn();
                }
                break;

            case GameState.RoundEnd:
                DetermineRoundWinner();
                break;
        }
    }

    // Method to pass the turn to the other player
    public void EndTurn()
    {
        // If both players passed, end the round
        if (player1.hasPassed && player2.hasPassed)
        {
            ChangeState(GameState.RoundEnd);
            return;
        }

        // Pass turn from P1 to P2
        if (currentState == GameState.Player1Turn && !player2.hasPassed)
        {
            ChangeState(GameState.Player2Turn);
        }
        // Pass turn from P2 to P1
        else if (currentState == GameState.Player2Turn && !player1.hasPassed)
        {
            ChangeState(GameState.Player1Turn);
        }
    }

    private void DetermineRoundWinner()
    {
        Debug.Log($"=== ROUND ENDED ===");
        Debug.Log($"Final Scores -> Player 1: {boardManager.p1TotalScore} | Player 2: {boardManager.p2TotalScore}");

        if (boardManager.p1TotalScore > boardManager.p2TotalScore)
        {
            Debug.Log("Player 1 wins the round!");
            p2Lives--; // Гравець 2 втрачає життя
        }
        else if (boardManager.p2TotalScore > boardManager.p1TotalScore)
        {
            Debug.Log("Player 2 wins the round!");
            p1Lives--; // Гравець 1 втрачає життя
        }
        else
        {
            Debug.Log("Round is a DRAW!");
            // При нічиїй в оригінальному Гвінті життя втрачають обидва гравці (крім фракції Нільфгаард, але ми це поки ігноруємо)
            p1Lives--;
            p2Lives--;
        }

        CheckMatchEnd();
    }

    private void CheckMatchEnd()
    {
        if (p1Lives <= 0 || p2Lives <= 0)
        {
            ChangeState(GameState.MatchEnd);

            if (p1Lives <= 0 && p2Lives <= 0) Debug.Log("MATCH OVER: It's a Draw!");
            else if (p1Lives <= 0) Debug.Log("MATCH OVER: Player 2 Wins the Match!");
            else Debug.Log("MATCH OVER: Player 1 Wins the Match!");
        }
        else
        {
            StartNewRound();
        }
    }

    private void StartNewRound()
    {
        Debug.Log("=== STARTING NEW ROUND ===");

        // Очищаємо стіл
        boardManager.ClearBoard();

        // Знімаємо статус "Пас" з обох гравців
        player1.hasPassed = false;
        player2.hasPassed = false;

        // Той, хто виграв попередній раунд, ходить першим.
        // Але зараз для простоти просто передаємо хід Гравцю 1
        ChangeState(GameState.Player1Turn);
    }
}