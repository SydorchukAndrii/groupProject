using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GwentLogic;

/// <summary>
/// Компонент для відображення однієї карти в UI (рука або ряд на дошці).
/// Прив'яж до Card prefab у Canvas.
/// </summary>
public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum Owner  { Player1, Player2 }
    public enum DisplayMode { Hand, Board }

    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI abilityText;
    public TextMeshProUGUI factionText;
    public Image cardBackground;
    public Image cardBorder;
    public Image typeIcon;          // опціонально — іконка типу карти
    public GameObject heroStarIcon; // зірочка для Героїв

    [Header("Colors")]
    public Color unitColor    = new Color(0.15f, 0.25f, 0.15f);
    public Color heroColor    = new Color(0.25f, 0.18f, 0f);
    public Color weatherColor = new Color(0.05f, 0.15f, 0.25f);
    public Color specialColor = new Color(0.18f, 0.05f, 0.25f);

    public Color unitBorderColor    = new Color(0.3f, 0.5f, 0.3f);
    public Color heroBorderColor    = new Color(0.8f, 0.6f, 0f);
    public Color weatherBorderColor = new Color(0.2f, 0.5f, 0.7f);
    public Color specialBorderColor = new Color(0.5f, 0.25f, 0.75f);

    public Color selectedBorderColor = new Color(1f, 0.88f, 0.25f);
    public Color hoverBorderColor    = new Color(0.7f, 0.9f, 0.7f);

    // ─────────────────────────────────────────────────────────────

    private CardData data;
    private Owner    owner;
    private DisplayMode mode;
    private bool isSelected;
    private Color normalBorderColor;

    // Посилання на PlayerManager, щоб гравець міг клікнути карту
    private PlayerManager playerRef;

    // ─────────────────────────────────────────────────────────────

    public void Setup(CardData card, Owner cardOwner, DisplayMode displayMode)
    {
        data  = card;
        owner = cardOwner;
        mode  = displayMode;

        ApplyVisuals();

        // Для режиму Board — не клікабельно (карта вже зіграна)
        Button btn = GetComponent<Button>();
        if (btn) btn.interactable = (mode == DisplayMode.Hand);

        // Знаходимо PlayerManager автоматично за власником
        playerRef = card.owner;
    }

    private void ApplyVisuals()
    {
        if (data == null) return;

        // Текст
        SetText(cardNameText, data.cardName);
        SetText(powerText,    data.type == CardType.Weather || data.type == CardType.Special
                              ? "" : data.basePower.ToString());
        SetText(factionText,  data.faction.ToString());
        SetText(abilityText,  AbilityLabel(data.ability));

        // Колір по типу
        Color bg, border;
        switch (data.type)
        {
            case CardType.Hero:    bg = heroColor;    border = heroBorderColor;    break;
            case CardType.Weather: bg = weatherColor; border = weatherBorderColor; break;
            case CardType.Special: bg = specialColor; border = specialBorderColor; break;
            default:               bg = unitColor;    border = unitBorderColor;    break;
        }

        if (cardBackground) cardBackground.color = bg;
        if (cardBorder)     cardBorder.color     = border;
        normalBorderColor = border;

        // Герої мають зірочку
        if (heroStarIcon) heroStarIcon.SetActive(data.type == CardType.Hero);
    }

    // ─────────────────────────────── INTERACTION ────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (mode != DisplayMode.Hand) return;

        isSelected = !isSelected;
        UpdateBorderForState();

        if (isSelected && playerRef != null)
        {
            // Повідомляємо HandUI, щоб він зняв виділення з інших карт
            GetComponentInParent<HandUI>()?.OnCardSelected(this, data);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mode != DisplayMode.Hand || isSelected) return;
        if (cardBorder) cardBorder.color = hoverBorderColor;

        // Показуємо tooltip
        CardTooltip.Instance?.Show(data, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected) UpdateBorderForState();
        CardTooltip.Instance?.Hide();
    }

    public void Deselect()
    {
        isSelected = false;
        UpdateBorderForState();
    }

    private void UpdateBorderForState()
    {
        if (!cardBorder) return;
        cardBorder.color = isSelected ? selectedBorderColor : normalBorderColor;

        // Піднімаємо вибрану карту трохи вгору через Scale
        transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
    }

    // ─────────────────────────────── HELPERS ────────────────────

    private void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp) tmp.text = value;
    }

    private string AbilityLabel(CardAbility ability)
    {
        return ability switch
        {
            CardAbility.Spy            => "SPY",
            CardAbility.Medic          => "MEDIC",
            CardAbility.TightBond      => "BOND",
            CardAbility.CommandersHorn => "HORN",
            CardAbility.Scorch         => "SCORCH",
            _                          => ""
        };
    }

    public CardData GetCardData() => data;
}
