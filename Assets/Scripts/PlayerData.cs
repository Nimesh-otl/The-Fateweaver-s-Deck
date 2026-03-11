using UnityEngine;

// Right-click > Create > Cards > Player Data
[CreateAssetMenu(fileName = "PlayerData", menuName = "Cards/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Base Stats")]
    public string playerName = "The Fool";
    public int maxHealth = 20;
    public int damageMin = 2;
    public int damageMax = 4;
    public int stamina = 5;

    // Runtime values (reset each dungeon run)
    [HideInInspector] public int currentHealth;

    public void InitRun()
    {
        currentHealth = maxHealth;
    }

    public bool IsAlive => currentHealth > 0;

    public int RollDamage()
    {
        return Random.Range(damageMin, damageMax + 1);
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
