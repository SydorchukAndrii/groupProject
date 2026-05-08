namespace GwentLogic
{
    public enum CardRow
    {
        Melee,      // Ближній бій
        Ranged,     // Дальній бій
        Siege,      // Облоговий
        Any,        // Для шпигунів чи героїв, які стають куди завгодно
        None        // Для погоди чи спеціальних карт
    }

    public enum CardType
    {
        Unit,       // Звичайний загін
        Hero,       // Герой (імунітет до погоди)
        Weather,    // Погода
        Special     // Опудало, ріг тощо
    }

    public enum Faction
    {
        Humans,
        Elfs,
        Orks,
        Undead
    }
}