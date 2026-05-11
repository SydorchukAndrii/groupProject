using UnityEngine;
using UnityEngine.UI;
using GwentLogic;

/// <summary>
/// Керує рукою гравця: відображення карт і обробка вибору/гри.
/// Прив'яж до контейнера руки (Horizontal Layout Group рекомендовано).
/// </summary>
public class HandUI : MonoBehaviour
{
    [Header("References")]
    public PlayerManager playerManager;
    public BoardManager boardManager;
    public GameManager gameManager;
    public GwentUIManager uiManager;

    [Header("Play Button")]
    public Button playSelectedButton;
    public GameObject selectedCardInfoPanel;
    public TMPro.TextMeshProUGUI selectedCardInfoText;

    private CardUI currentlySelected;
    private CardData selectedCard;

    void Start()
    {
        if (playSelectedButton)
            playSelectedButton.onClick.AddListener(PlaySelected);

        if (selectedCardInfoPanel)
            selectedCardInfoPanel.SetActive(false);

        if (playSelectedButton)
            playSelectedButton.interactable = false;
    }

    void Update()
    {
        // Дублюємо клавіатурний контроль з PlayerManager для кнопки Play
        bool isMyTurn =
            (playerManager.isPlayer1 && gameManager.currentState == GameState.Player1Turn) ||
            (!playerManager.isPlayer1 && gameManager.currentState == GameState.Player2Turn);

        if (playSelectedButton)
            playSelectedButton.interactable = isMyTurn && selectedCard != null && !playerManager.hasPassed;
    }

    /// <summary>Викликається CardUI при кліку на карту.</summary>
    public void OnCardSelected(CardUI cardUI, CardData card)
    {
        // Знімаємо виділення з попередньої карти
        if (currentlySelected != null && currentlySelected != cardUI)
            currentlySelected.Deselect();

        currentlySelected = cardUI;
        selectedCard      = card;

        // Показуємо інфо про карту
        ShowSelectedInfo(card);
    }

    private void ShowSelectedInfo(CardData card)
    {
        if (selectedCardInfoPanel) selectedCardInfoPanel.SetActive(true);

        if (selectedCardInfoText)
        {
            string abilityDesc = card.ability switch
            {
                CardAbility.Spy            => "Placed on opponent's side. You draw 2 cards.",
                CardAbility.Medic          => "Revives a Unit from your discard pile.",
                CardAbility.TightBond      => "Doubles power for each copy in the same row.",
                CardAbility.CommandersHorn => "Doubles the score of its row.",
                CardAbility.Scorch         => "Destroys all units with the highest power.",
                _                          => "No special ability."
            };

            selectedCardInfoText.text =
                $"<b>{card.cardName}</b>  [{card.faction}]\n" +
                $"Type: {card.type}  •  Row: {card.allowedRow}  •  Power: {card.basePower}\n" +
                $"{abilityDesc}";
        }
    }

    private void PlaySelected()
    {
        if (selectedCard == null || playerManager == null) return;

        bool isMyTurn =
            (playerManager.isPlayer1 && gameManager.currentState == GameState.Player1Turn) ||
            (!playerManager.isPlayer1 && gameManager.currentState == GameState.Player2Turn);

        if (!isMyTurn || playerManager.hasPassed) return;

        boardManager.PlayCard(playerManager, selectedCard);
        gameManager.EndTurn();

        // Скидаємо вибір
        currentlySelected = null;
        selectedCard      = null;
        if (selectedCardInfoPanel) selectedCardInfoPanel.SetActive(false);

        // Перебудовуємо UI
        uiManager?.OnCardPlayed();
    }
}
