using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// Handles a single fight between the player and one enemy card.
/// Attach to a persistent manager GameObject in your scene.
/// UPDATED: supports player hand cards (shield, stamina boost, magic blast, heal)
public class CombatManager : MonoBehaviour
{
    [Header("Player")]
    public PlayerData player;

    [Header("UI - Combat Panel")]
    public GameObject combatPanel;
    public TextMeshProUGUI combatLogText;
    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI enemyNameText;
    public Button attackButton;

    [Header("UI - HUD (always visible)")]
    public TextMeshProUGUI hudPlayerHP;
    public TextMeshProUGUI hudShieldText;   // shows shield count when active

    [Header("References")]
    public RingManager ringManager;
    public PlayerHandView playerHand;       // drag PlayerHand GameObject here

    // Runtime state
    private CardView currentEnemy;
    private int enemyCurrentHP;
    private bool combatActive = false;

    // Card effect state (reset each fight)
    private int shieldBlocksRemaining = 0;
    private int bonusStamina = 0;

    void Start()
    {
        player.InitRun();
        combatPanel.SetActive(false);
        attackButton.onClick.AddListener(OnAttackButtonPressed);
        UpdateHUD();

        if (playerHand) playerHand.combatManager = this;
    }

    // ── Entry point called by RingManager ──────────────────────────────────
    public void BeginEncounter(CardView cardView)
    {
        if (combatActive) return;

        Card card = cardView.cardData;

        if (card.cardType == CardType.Loot)
        {
            GiveLoot(card);
            ringManager.RemoveCenterCard();
            return;
        }

        if (card.cardType == CardType.Exit)
        {
            Log("You found the exit! Room complete.");
            ringManager.RemoveCenterCard();
            return;
        }

        if (card.cardType == CardType.Mimic)
        {
            bool wasLoot = ResolveMimic(card);
            if (wasLoot) { ringManager.RemoveCenterCard(); return; }
        }

        // Start combat
        currentEnemy   = cardView;
        enemyCurrentHP = card.health;
        combatActive   = true;
        bonusStamina   = 0;

        combatPanel.SetActive(true);
        enemyNameText.text = card.cardName;
        Log($"Encounter: {card.cardName}  HP {enemyCurrentHP}  STM {card.stamina}");
        RefreshCombatUI();
    }

    // ── Attack button ──────────────────────────────────────────────────────
    void OnAttackButtonPressed()
    {
        if (!combatActive) return;
        StartCoroutine(ResolveCombatRound());
    }

    IEnumerator ResolveCombatRound()
    {
        attackButton.interactable = false;

        Card enemy = currentEnemy.cardData;
        int effectiveStamina = player.stamina + bonusStamina;
        bool playerGoesFirst = effectiveStamina >= enemy.stamina;

        if (playerGoesFirst)
        {
            yield return StartCoroutine(PlayerAttack(enemy));
            if (enemyCurrentHP > 0 && player.IsAlive)
                yield return StartCoroutine(EnemyAttack(enemy));
        }
        else
        {
            yield return StartCoroutine(EnemyAttack(enemy));
            if (player.IsAlive)
                yield return StartCoroutine(PlayerAttack(enemy));
        }

        RefreshCombatUI();

        if (!player.IsAlive)
        {
            Log("You have fallen... Game Over.");
            yield break;
        }

        if (enemyCurrentHP <= 0)
        {
            Log($"{enemy.cardName} is defeated!");
            yield return new WaitForSeconds(0.6f);
            combatPanel.SetActive(false);
            combatActive = false;
            ringManager.RemoveCenterCard();
        }
        else
        {
            attackButton.interactable = true;
        }
    }

    IEnumerator PlayerAttack(Card enemy)
    {
        int dmg        = player.RollDamage();
        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);
        Log($"You deal {dmg} damage → {enemy.cardName} HP: {enemyCurrentHP}");
        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator EnemyAttack(Card enemy)
    {
        int dmg = enemy.RollDamage();

        if (shieldBlocksRemaining > 0)
        {
            shieldBlocksRemaining--;
            Log($"Shield blocks the attack! ({shieldBlocksRemaining} blocks left)");
            UpdateHUD();
            yield return new WaitForSeconds(0.4f);
            yield break;
        }

        player.TakeDamage(dmg);
        Log($"{enemy.cardName} hits for {dmg} ({enemy.damageType}) → Your HP: {player.currentHealth}");
        UpdateHUD();
        yield return new WaitForSeconds(0.4f);
    }

    // ── Player hand card effects ───────────────────────────────────────────
    /// Returns true if the card was consumed, false if it cannot be used now.
    public bool UsePlayerCard(PlayerCard card)
    {
        switch (card.cardType)
        {
            case PlayerCardType.HealthPotion:
                player.Heal(card.value);
                Log($"{card.cardName}: healed {card.value} HP → {player.currentHealth}/{player.maxHealth}");
                UpdateHUD();
                return true;

            case PlayerCardType.StaminaPotion:
                bonusStamina += card.value;
                Log($"{card.cardName}: +{card.value} stamina this fight!");
                return true;

            case PlayerCardType.Shield:
                shieldBlocksRemaining += card.value;
                Log($"{card.cardName}: next {card.value} hit(s) will be blocked!");
                UpdateHUD();
                return true;

            case PlayerCardType.MagicBlast:
                if (!combatActive)
                {
                    Log("No enemy to blast — enter combat first!");
                    return false;       // card is NOT consumed
                }
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - card.value);
                Log($"{card.cardName}: {card.value} magic damage → enemy HP: {enemyCurrentHP}");
                RefreshCombatUI();

                if (enemyCurrentHP <= 0)
                {
                    Log($"{currentEnemy.cardData.cardName} destroyed by magic!");
                    combatPanel.SetActive(false);
                    combatActive = false;
                    ringManager.RemoveCenterCard();
                }
                return true;

            default:
                return false;
        }
    }

    // ── Mimic & Loot ───────────────────────────────────────────────────────
    bool ResolveMimic(Card card)
    {
        if (card.canBeReward && Random.value < 0.5f)
        {
            Log("The Mimic reveals itself as treasure!");
            GiveLoot(card);
            return true;
        }
        Log("It's a Mimic! Prepare to fight!");
        return false;
    }

    void GiveLoot(Card card)
    {
        player.Heal(4);
        Log($"Loot found! +4 HP → {player.currentHealth}/{player.maxHealth}");
        UpdateHUD();
    }

    // ── UI helpers ─────────────────────────────────────────────────────────
    void RefreshCombatUI()
    {
        playerHPText.text = $"Your HP: {player.currentHealth} / {player.maxHealth}";
        enemyHPText.text  = $"Enemy HP: {enemyCurrentHP}";
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (hudPlayerHP)
            hudPlayerHP.text = $"HP: {player.currentHealth} / {player.maxHealth}";

        if (hudShieldText)
            hudShieldText.text = shieldBlocksRemaining > 0
                ? $"Shield: {shieldBlocksRemaining}"
                : "";
    }

    void Log(string msg)
    {
        if (combatLogText) combatLogText.text += "\n" + msg;
        Debug.Log("[Combat] " + msg);
    }

    public void OnRoomCleared()
    {
        Log("Room cleared! Dungeon complete.");
    }
}
