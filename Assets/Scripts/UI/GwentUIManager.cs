using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GwentLogic;

/// <summary>
/// Головний менеджер UI. Підключається до BoardManager та GameManager через події.
/// Прив'яжи цей компонент до порожнього GameObject на Canvas.
/// </summary>
public class GwentUIManager : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public GameManager gameManager;
    public BoardManager boardManager;
    public PlayerManager player1;
    public PlayerManager player2;

    [Header("=== PLAYER 1 INFO ===")]
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p1HandCountText;
    public TextMeshProUGUI p1DeckCountText;
    public TextMeshProUGUI p1DiscardCountText;
    public Image p1Life1Image;
    public Image p1Life2Image;

    [Header("=== PLAYER 2 INFO ===")]
    public TextMeshProUGUI p2ScoreText;
    public TextMeshProUGUI p2HandCountText;
    public TextMeshProUGUI p2DeckCountText;
    public TextMeshProUGUI p2DiscardCountText;
    public Image p2Life1Image;
    public Image p2Life2Image;

    [Header("=== ROW SCORES ===")]
    public TextMeshProUGUI p1MeleeScoreText;
    public TextMeshProUGUI p1RangedScoreText;
    public TextMeshProUGUI p1SiegeScoreText;
    public TextMeshProUGUI p2MeleeScoreText;
    public TextMeshProUGUI p2RangedScoreText;
    public TextMeshProUGUI p2SiegeScoreText;

    [Header("=== WEATHER INDICATORS ===")]
    public GameObject frostIndicator;   // активний = підсвічений
    public GameObject fogIndicator;
    public GameObject rainIndicator;

    [Header("=== BOARD ROW CONTAINERS ===")]
    // Transform, в які будуть спавнитись CardUI prefab-и
    public Transform p1MeleeContainer;
    public Transform p1RangedContainer;
    public Transform p1SiegeContainer;
    public Transform p2MeleeContainer;
    public Transform p2RangedContainer;
    public Transform p2SiegeContainer;

    [Header("=== PLAYER HANDS ===")]
    public Transform p1HandContainer;   // HandUI.cs сидить тут
    public Transform p2HandContainer;

    [Header("=== GAME STATE UI ===")]
    public TextMeshProUGUI gameStateText;
    public TextMeshProUGUI turnIndicatorText;
    public GameObject player1TurnHighlight;
    public GameObject player2TurnHighlight;

    [Header("=== BUTTONS ===")]
    public Button p1PassButton;
    public Button p2PassButton;

    [Header("=== ROUND / MATCH END PANEL ===")]
    public GameObject roundEndPanel;
    public TextMeshProUGUI roundEndTitleText;
    public TextMeshProUGUI roundEndDetailsText;
    public Button roundEndContinueButton;

    [Header("=== PREFABS ===")]
    public GameObject cardUIPrefab;     // CardUI.cs prefab

    [Header("=== LIFE GEM COLORS ===")]
    public Color lifeActiveColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Color lifeDeadColor   = new Color(0.2f, 0.2f, 0.2f, 1f);

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Підписуємось на події BoardManager
        boardManager.OnScoresUpdated += OnScoresUpdated;
        boardManager.OnBoardCleared  += OnBoardCleared;

        // Кнопки
        if (p1PassButton) p1PassButton.onClick.AddListener(OnP1PassClicked);
        if (p2PassButton) p2PassButton.onClick.AddListener(OnP2PassClicked);
        if (roundEndContinueButton) roundEndContinueButton.onClick.AddListener(OnRoundEndContinue);

        // Ховаємо панель завершення раунду
        if (roundEndPanel) roundEndPanel.SetActive(false);

        // Перший рендер
        RefreshAll();
    }

    void OnDestroy()
    {
        // Відписуємось, щоб уникнути витоків пам'яті
        if (boardManager)
        {
            boardManager.OnScoresUpdated -= OnScoresUpdated;
            boardManager.OnBoardCleared  -= OnBoardCleared;
        }
    }

    // ─────────────────────────────── CALLBACKS ──────────────────

    private void OnScoresUpdated()
    {
        RefreshScores();
        RefreshBoardRows();
        RefreshWeather();
        RefreshGameState();
        RefreshPlayerInfo();
    }

    private void OnBoardCleared()
    {
        RefreshBoardRows();
        RefreshWeather();
        RefreshScores();
    }

    // ─────────────────────────────── REFRESH ────────────────────

    public void RefreshAll()
    {
        RefreshScores();
        RefreshBoardRows();
        RefreshWeather();
        RefreshGameState();
        RefreshPlayerInfo();
        RefreshHands();
    }

    private void RefreshScores()
    {
        SetText(p1ScoreText,         boardManager.p1TotalScore.ToString());
        SetText(p2ScoreText,         boardManager.p2TotalScore.ToString());
        SetText(p1MeleeScoreText,    ScoreForRow(boardManager.p1Melee,  boardManager.isMeleeWeatherActive,  boardManager.p1MeleeHorn));
        SetText(p1RangedScoreText,   ScoreForRow(boardManager.p1Ranged, boardManager.isRangedWeatherActive, boardManager.p1RangedHorn));
        SetText(p1SiegeScoreText,    ScoreForRow(boardManager.p1Siege,  boardManager.isSiegeWeatherActive,  boardManager.p1SiegeHorn));
        SetText(p2MeleeScoreText,    ScoreForRow(boardManager.p2Melee,  boardManager.isMeleeWeatherActive,  boardManager.p2MeleeHorn));
        SetText(p2RangedScoreText,   ScoreForRow(boardManager.p2Ranged, boardManager.isRangedWeatherActive, boardManager.p2RangedHorn));
        SetText(p2SiegeScoreText,    ScoreForRow(boardManager.p2Siege,  boardManager.isSiegeWeatherActive,  boardManager.p2SiegeHorn));
    }

    private void RefreshBoardRows()
    {
        RebuildRow(p1MeleeContainer,  boardManager.p1Melee,  CardUI.Owner.Player1);
        RebuildRow(p1RangedContainer, boardManager.p1Ranged, CardUI.Owner.Player1);
        RebuildRow(p1SiegeContainer,  boardManager.p1Siege,  CardUI.Owner.Player1);
        RebuildRow(p2MeleeContainer,  boardManager.p2Melee,  CardUI.Owner.Player2);
        RebuildRow(p2RangedContainer, boardManager.p2Ranged, CardUI.Owner.Player2);
        RebuildRow(p2SiegeContainer,  boardManager.p2Siege,  CardUI.Owner.Player2);
    }

    private void RebuildRow(Transform container, System.Collections.Generic.List<CardData> cards, CardUI.Owner owner)
    {
        if (!container) return;

        // Очищаємо старі дочірні об'єкти
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // Спавнимо нові
        foreach (CardData card in cards)
        {
            GameObject go = Instantiate(cardUIPrefab, container);
            CardUI ui = go.GetComponent<CardUI>();
            if (ui) ui.Setup(card, owner, CardUI.DisplayMode.Board);
        }
    }

    private void RefreshHands()
    {
        RebuildHand(p1HandContainer, player1, CardUI.Owner.Player1);
        RebuildHand(p2HandContainer, player2, CardUI.Owner.Player2);
    }

    private void RebuildHand(Transform container, PlayerManager player, CardUI.Owner owner)
    {
        if (!container || !player) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (CardData card in player.hand)
        {
            GameObject go = Instantiate(cardUIPrefab, container);
            CardUI ui = go.GetComponent<CardUI>();
            if (ui) ui.Setup(card, owner, CardUI.DisplayMode.Hand);
        }
    }

    private void RefreshWeather()
    {
        SetActive(frostIndicator, boardManager.isMeleeWeatherActive);
        SetActive(fogIndicator,   boardManager.isRangedWeatherActive);
        SetActive(rainIndicator,  boardManager.isSiegeWeatherActive);
    }

    private void RefreshGameState()
    {
        GameState state = gameManager.currentState;

        string stateLabel = state switch
        {
            GameState.Player1Turn => "⚔  PLAYER 1 TURN",
            GameState.Player2Turn => "⚔  PLAYER 2 TURN",
            GameState.RoundEnd    => "🏁  ROUND END",
            GameState.MatchEnd    => "🏆  MATCH OVER",
            _                    => state.ToString()
        };
        SetText(gameStateText, stateLabel);

        bool p1Active = state == GameState.Player1Turn;
        bool p2Active = state == GameState.Player2Turn;

        SetActive(player1TurnHighlight, p1Active);
        SetActive(player2TurnHighlight, p2Active);

        // Кнопки Pass
        if (p1PassButton) p1PassButton.interactable = p1Active && !player1.hasPassed;
        if (p2PassButton) p2PassButton.interactable = p2Active && !player2.hasPassed;

        SetText(turnIndicatorText,
            p1Active ? "Player 1 — Space to play / P to pass" :
            p2Active ? "Player 2 — Enter to play / L to pass" : "");

        // Round / Match end panel
        if (state == GameState.RoundEnd || state == GameState.MatchEnd)
            ShowEndPanel(state);
        else if (roundEndPanel && roundEndPanel.activeSelf && state == GameState.Player1Turn)
            roundEndPanel.SetActive(false);
    }

    private void RefreshPlayerInfo()
    {
        // Counts
        SetText(p1HandCountText,    $"Hand: {player1.hand.Count}");
        SetText(p1DeckCountText,    $"Deck: {player1.deck.Count}");
        SetText(p1DiscardCountText, $"Graveyard: {player1.discardPile.Count}");

        SetText(p2HandCountText,    $"Hand: {player2.hand.Count}");
        SetText(p2DeckCountText,    $"Deck: {player2.deck.Count}");
        SetText(p2DiscardCountText, $"Graveyard: {player2.discardPile.Count}");

        // Life gems
        SetLifeGem(p1Life1Image, gameManager.p1Lives >= 1);
        SetLifeGem(p1Life2Image, gameManager.p1Lives >= 2);
        SetLifeGem(p2Life1Image, gameManager.p2Lives >= 1);
        SetLifeGem(p2Life2Image, gameManager.p2Lives >= 2);
    }

    // ─────────────────────────────── END PANEL ──────────────────

    private void ShowEndPanel(GameState state)
    {
        if (!roundEndPanel) return;
        roundEndPanel.SetActive(true);

        if (state == GameState.MatchEnd)
        {
            SetText(roundEndTitleText, "🏆 MATCH OVER");
            string result =
                gameManager.p1Lives <= 0 && gameManager.p2Lives <= 0 ? "It's a Draw!" :
                gameManager.p1Lives <= 0 ? "Player 2 Wins!" : "Player 1 Wins!";
            SetText(roundEndDetailsText, result);
            if (roundEndContinueButton)
                roundEndContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "New Game";
        }
        else
        {
            int p1 = boardManager.p1TotalScore, p2 = boardManager.p2TotalScore;
            string winner = p1 > p2 ? "Player 1 wins the round!" :
                            p2 > p1 ? "Player 2 wins the round!" : "Round Draw!";
            SetText(roundEndTitleText, "🏁 ROUND END");
            SetText(roundEndDetailsText, $"P1: {p1}  vs  P2: {p2}\n{winner}");
            if (roundEndContinueButton)
                roundEndContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Next Round →";
        }
    }

    // ─────────────────────────────── BUTTON HANDLERS ────────────

    private void OnP1PassClicked()
    {
        if (gameManager.currentState != GameState.Player1Turn) return;
        player1.hasPassed = true;
        Debug.Log("[UI] Player 1 passed via button.");
        gameManager.EndTurn();
        RefreshAll();
    }

    private void OnP2PassClicked()
    {
        if (gameManager.currentState != GameState.Player2Turn) return;
        player2.hasPassed = true;
        Debug.Log("[UI] Player 2 passed via button.");
        gameManager.EndTurn();
        RefreshAll();
    }

    private void OnRoundEndContinue()
    {
        if (roundEndPanel) roundEndPanel.SetActive(false);
    }

    // ─────────────────────────────── HELPERS ────────────────────

    private void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp) tmp.text = value;
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go) go.SetActive(active);
    }

    private void SetLifeGem(Image img, bool alive)
    {
        if (img) img.color = alive ? lifeActiveColor : lifeDeadColor;
    }

    /// <summary>Рахує бали для одного ряду (дублює логіку BoardManager для відображення).</summary>
    private string ScoreForRow(
        System.Collections.Generic.List<CardData> row,
        bool weatherActive, bool hornActive)
    {
        var bonds = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var c in row)
            if (c.ability == CardAbility.TightBond)
                bonds[c.cardName] = bonds.ContainsKey(c.cardName) ? bonds[c.cardName] + 1 : 1;

        int total = 0;
        foreach (var c in row)
        {
            int p = c.basePower;
            if (c.type != CardType.Hero)
            {
                if (weatherActive) p = 1;
                if (c.ability == CardAbility.TightBond && bonds.ContainsKey(c.cardName))
                    p *= bonds[c.cardName];
                if (hornActive) p *= 2;
            }
            total += p;
        }
        return total.ToString();
    }

    // Called from PlayerManager or any external script after a card is played
    public void OnCardPlayed()
    {
        RefreshHands();
        // Scores are refreshed via OnScoresUpdated event
    }
}
