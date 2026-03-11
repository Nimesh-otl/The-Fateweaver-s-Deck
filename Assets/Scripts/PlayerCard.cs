using UnityEngine;

// Right-click in Project > Create > Cards > New Player Card
[CreateAssetMenu(fileName = "New Player Card", menuName = "Cards/New Player Card")]
public class PlayerCard : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public PlayerCardType cardType;
    public Sprite cardArt;
    [TextArea] public string description;   // shown in UI when selected

    [Header("Values")]
    public int value;   // HP healed / stamina gained / damage dealt / hits blocked
}

public enum PlayerCardType
{
    HealthPotion,       // Heal value HP
    StaminaPotion,      // +value stamina this combat (resets after fight)
    Shield,             // Block next value hits
    MagicBlast          // Deal value magic damage to current enemy, ignores stamina order
}
