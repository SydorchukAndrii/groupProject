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

    public enum GameState
    {
        Setup,          // Роздача карт на початку
        Player1Turn,    // Хід першого гравця
        Player2Turn,    // Хід другого гравця
        RoundEnd,       // Підрахунок очок за раунд
        MatchEnd        // Кінець всієї гри (хтось виграв 2 раунди)
    }

    public enum CardAbility
    {
        None,           // Немає здібності
        Spy,            // Шпигун (йде до суперника, дає +2 карти)
        Medic,          // Медик (воскрешає карту з відбою)
        TightBond,
        CommandersHorn,
        Scorch
    }
}