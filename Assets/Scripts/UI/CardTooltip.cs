using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GwentLogic;

/// <summary>
/// Singleton-tooltip: з'являється при наведенні на карту в руці.
/// Прив'яж до порожнього Panel у верхньому шарі Canvas (Sort Order вище за карти).
/// </summary>
public class CardTooltip : MonoBehaviour
{
    public static CardTooltip Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI abilityText;
    public TextMeshProUGUI descriptionText;

    [Header("Offset від курсора")]
    public Vector2 offset = new Vector2(120f, -60f);

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    void Awake()
    {
        Instance      = this;
        rectTransform = GetComponent<RectTransform>();
        rootCanvas    = GetComponentInParent<Canvas>();
        Hide();
    }

    public void Show(CardData card, Vector3 worldPos)
    {
        if (!panel || card == null) return;

        // Текст
        if (nameText)        nameText.text = card.cardName;
        if (typeText)        typeText.text = $"{card.type} • {card.faction} • {card.allowedRow}";
        if (powerText)       powerText.text = card.basePower > 0 ? $"Power: {card.basePower}" : "";
        if (abilityText)     abilityText.text = card.ability != CardAbility.None ? card.ability.ToString() : "";
        if (descriptionText) descriptionText.text = card.description;

        // Позиція
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = new Vector3(screenPoint.x + offset.x, screenPoint.y + offset.y, 0f);
        }

        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }
}
