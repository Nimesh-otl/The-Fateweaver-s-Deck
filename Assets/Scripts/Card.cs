using UnityEngine;


[CreateAssetMenu(fileName = "New Card", menuName = "Cards/New Card")]
public class Card : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public CardType cardType;
    public Sprite cardArt;          //  sprite in here

    [Header("Combat Stats")]
    public int health;
    public int damageMin;
    public int damageMax;
    public int stamina;             // higher stamina = attacks first

    [Header("Special")]
    public bool canBeReward;        // Mimic only: 50% chance to drop loot instead
    public DamageType damageType;

    [Header("Scene Transition")]
    public string nextSceneName;    // Exit only: scene to load

    // Helper: roll damage this turn
    public int RollDamage()
    {
        return Random.Range(damageMin, damageMax + 1);
    }
}

public enum CardType
{
    BasicEnemy,
    RangedEnemy,
    Mimic,
    Loot,
    Exit,
    Puzzle
}

public enum DamageType
{
    Physical,
    Arrow,
    Bite,
    Stab
}
