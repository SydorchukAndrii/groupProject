using UnityEngine;
using GwentLogic; // Підключаємо наші енуми

// Цей рядок дозволить створювати карти через меню Unity (Right Click -> Create -> Gwent -> Card)
[CreateAssetMenu(fileName = "NewCard", menuName = "Gwent/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Faction faction;
    public CardType type;
    public CardRow allowedRow;
    public int basePower;

    [Header("Special Ability")]
    public CardAbility ability = CardAbility.None;

    [HideInInspector]
    public PlayerManager owner;

    [TextArea]
    public string description; // Опис для UI
}