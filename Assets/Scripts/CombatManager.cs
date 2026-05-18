using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// Handles a single fight between the player and one enemy card.
/// Attach to a persistent manager GameObject in your scene.
/// UPDATED: supports player hand cards (shield, stamina boost, magic blast, heal)
public class CombatManager : MonoBehaviour
{
    [Header("Player")]
    public PlayerData player;

    [Header("UI - Combat Panel")]
    public GameObject combatPanel;
    public Button attackButton;
    public Button cancelButton;

    [Header("UI - Defeat")]
    public GameObject defeatPanel;
    public Button restartButton;

    [Header("UI - HUD (always visible)")]
    public Image hudHPBarFill;
    public GameObject hudShieldIcon;
    public Image damageVignette;

    [Header("Off-Screen Ranged Attacks")]
    public int offScreenRangedMinDamage = 1;
    public int offScreenRangedMaxDamage = 2;
    public int offScreenRangedCooldownTurns = 2;
    public bool offScreenRangedCannotKill = true;

    [Header("References")]
    public RingManager ringManager;
    public PlayerHandView playerHand;       // drag PlayerHand GameObject here

    // Runtime state
    private CardView currentEnemy;
    private int enemyCurrentHP;
    public bool combatActive = false;
    private bool isPlayerTurn = true;
    private bool turnInProgress = false;
    private bool defeatTriggered = false;
    private bool awaitingPlayerResponseAfterOpeningEnemyTurn = false;

    // Card effect state (reset each fight)
    private int shieldBlocksRemaining = 0;
    private int bonusStamina = 0;
    private Dictionary<CardView, int> rangedAttackCooldowns = new Dictionary<CardView, int>();
    private Dictionary<CardView, int> enemyHealthByCard = new Dictionary<CardView, int>();

    void Start()
    {
        if (!ringManager) ringManager = FindObjectOfType<RingManager>();
        if (!playerHand) playerHand = FindObjectOfType<PlayerHandView>();

        if (ringManager && ringManager.combatManager != this)
            ringManager.combatManager = this;

        player.InitRun();
        if (combatPanel) combatPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(false);

        if (attackButton)
            attackButton.onClick.AddListener(OnAttackButtonPressed);
        else
            Debug.LogError("CombatManager: Attack button reference is missing.");

        if (cancelButton)
            cancelButton.onClick.AddListener(OnCancelButtonPressed);
        else
            Debug.LogError("CombatManager: Cancel button reference is missing.");

        if (restartButton)
            restartButton.onClick.AddListener(RestartCurrentScene);

        UpdateHUD();

        if (playerHand)
            playerHand.combatManager = this;

        if (SceneManager.GetActiveScene().name != "The_Fool")
            SaveSystem.SaveGame();
    }

    IEnumerator FlashVignette(Image vignette, float targetAlpha, float flashDuration)
    {
        if (vignette == null || flashDuration <= 0f)
            yield break;

        float halfDuration = flashDuration * 0.5f;
        float elapsed = 0f;
        Color color = vignette.color;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            color.a = Mathf.Lerp(0f, targetAlpha, t);
            vignette.color = color;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            color.a = Mathf.Lerp(targetAlpha, 0f, t);
            vignette.color = color;
            yield return null;
        }
    }

    IEnumerator ShakeCard(RectTransform rt, float strength, float duration, float speed)
    {
        if (rt == null || duration <= 0f)
            yield break;

        Vector2 originalPosition = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rt == null) yield break;
            elapsed += Time.deltaTime;
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * strength;
            rt.anchoredPosition = originalPosition + offset;
            yield return new WaitForSeconds(1f / Mathf.Max(1f, speed));
        }

        rt.anchoredPosition = originalPosition;
    }

    IEnumerator LungeCard(RectTransform rt, Vector2 lungeDirection, float lungeDistance, float duration)
    {
        if (rt == null || duration <= 0f)
            yield break;

        Vector2 originalPosition = rt.anchoredPosition;
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;
        Vector2 targetOffset = lungeDirection.normalized * lungeDistance;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rt.anchoredPosition = originalPosition + Vector2.Lerp(Vector2.zero, targetOffset, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            rt.anchoredPosition = originalPosition + Vector2.Lerp(targetOffset, Vector2.zero, t);
            yield return null;
        }

        rt.anchoredPosition = originalPosition;
    }

    // ── Entry point called by RingManager ──────────────────────────────────
    public void BeginEncounter(CardView cardView)
    {
        Debug.Log("[Combat] BeginEncounter called.");

        if (cardView == null)
        {
            Debug.LogError("CombatManager: BeginEncounter called with null CardView.");
            return;
        }

        if (turnInProgress)
            return;

        Card card = cardView.cardData;
        if (card == null)
        {
            Debug.LogError("CombatManager: CardView has no cardData.");
            return;
        }

        if (card.cardType == CardType.Loot)
        {
            GiveLoot(card);
            ringManager.RemoveCenterCard();
            return;
        }

        if (card.cardType == CardType.Exit)
        {
            RunData.SavePlayerHP(player.currentHealth, player.maxHealth);

            string nextSceneName = !string.IsNullOrEmpty(card.nextSceneName)
                ? card.nextSceneName
                : (ringManager != null ? ringManager.nextSceneName : string.Empty);
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxDoorTransition);
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("CombatManager: RingManager nextSceneName is empty.");
            }
            return;
        }

        if (card.cardType == CardType.Mimic)
        {
            bool wasLoot = ResolveMimic(card);
            if (wasLoot) { ringManager.RemoveCenterCard(); return; }
        }

        // Start combat
        currentEnemy   = cardView;
        if (!enemyHealthByCard.ContainsKey(cardView))
            enemyHealthByCard[cardView] = card.health;
        enemyCurrentHP = enemyHealthByCard[cardView];
        combatActive   = true;
        bonusStamina   = 0;
        turnInProgress = false;
        rangedAttackCooldowns.Clear();

        int effectiveStamina = player.stamina + bonusStamina;
        isPlayerTurn = effectiveStamina >= card.stamina;

        if (combatPanel) combatPanel.SetActive(isPlayerTurn);
        Log($"Encounter: {card.cardName}  HP {enemyCurrentHP}  STM {card.stamina}");
        Log(isPlayerTurn ? "You are faster. Your turn first." : $"{card.cardName} is faster. Enemy turn first.");
        RefreshCombatUI();

        if (attackButton) attackButton.interactable = isPlayerTurn;

        if (!isPlayerTurn)
        {
            awaitingPlayerResponseAfterOpeningEnemyTurn = true;
            StartCoroutine(ResolveEnemyAutoTurn());
        }
    }

    // ── Attack button ──────────────────────────────────────────────────────
    void OnAttackButtonPressed()
    {
        if (!combatActive || turnInProgress || !isPlayerTurn) return;
        StartCoroutine(ResolveSingleTurn());
    }

    void OnCancelButtonPressed()
    {
        if (!combatActive || turnInProgress || !isPlayerTurn) return;

        if (combatPanel) combatPanel.SetActive(false);

        isPlayerTurn = true;
        combatActive = true;
        if (attackButton) attackButton.interactable = false;
        Log("Action canceled.");
    }

    IEnumerator ResolveSingleTurn()
    {
        turnInProgress = true;
        if (attackButton) attackButton.interactable = false;

        Card enemy = currentEnemy.cardData;

        yield return StartCoroutine(PlayerAttack(enemy));

        if (combatPanel) combatPanel.SetActive(false);

        RefreshCombatUI();

        if (!player.IsAlive)
        {
            TriggerDefeat();
            turnInProgress = false;
            yield break;
        }

        if (enemyCurrentHP <= 0)
        {
            Log($"{enemy.cardName} is defeated!");
            yield return new WaitForSeconds(0.6f);
            if (combatPanel) combatPanel.SetActive(false);
            combatActive = false;
            enemyHealthByCard.Remove(currentEnemy);
            turnInProgress = false;
            ringManager.RemoveCenterCard();
            yield break;
        }

        isPlayerTurn = false;
        Log($"{enemy.cardName}'s turn.");
        yield return new WaitForSeconds(0.6f);
        turnInProgress = false;

        StartCoroutine(ResolveEnemyAutoTurn());
    }

    IEnumerator ResolveEnemyAutoTurn()
    {
        if (!combatActive || turnInProgress) yield break;

        turnInProgress = true;
        if (attackButton) attackButton.interactable = false;

        Card enemy = currentEnemy.cardData;
        yield return StartCoroutine(EnemyAttack(enemy));

        if (!player.IsAlive)
        {
            TriggerDefeat();
            turnInProgress = false;
            yield break;
        }

        if (enemyCurrentHP <= 0)
        {
            Log($"{enemy.cardName} is defeated!");
            yield return new WaitForSeconds(0.6f);
            if (combatPanel) combatPanel.SetActive(false);
            combatActive = false;
            enemyHealthByCard.Remove(currentEnemy);
            turnInProgress = false;
            ringManager.RemoveCenterCard();
            yield break;
        }

        HandleOffScreenRangedAttacks();

        if (!player.IsAlive)
        {
            TriggerDefeat();
            turnInProgress = false;
            yield break;
        }

        RefreshCombatUI();

        isPlayerTurn = true;
        if (awaitingPlayerResponseAfterOpeningEnemyTurn)
        {
            awaitingPlayerResponseAfterOpeningEnemyTurn = false;
            combatActive = true;
            if (combatPanel) combatPanel.SetActive(true);
            Log("Your turn.");

            if (attackButton) attackButton.interactable = true;
            turnInProgress = false;
            yield break;
        }

        combatActive = false;
        if (combatPanel) combatPanel.SetActive(false);
        Log("Your turn.");

        if (attackButton) attackButton.interactable = false;
        turnInProgress = false;
    }

    void HandleOffScreenRangedAttacks()
    {
        if (!combatActive || !ringManager || currentEnemy == null) return;

        List<CardView> rangedAttackers = ringManager.GetOffScreenRangedEnemies(currentEnemy);
        HashSet<CardView> activeRangedSet = new HashSet<CardView>(rangedAttackers);

        List<CardView> cooldownKeys = new List<CardView>(rangedAttackCooldowns.Keys);
        for (int i = 0; i < cooldownKeys.Count; i++)
        {
            CardView key = cooldownKeys[i];
            if (!key || !activeRangedSet.Contains(key))
                rangedAttackCooldowns.Remove(key);
        }

        for (int i = 0; i < rangedAttackers.Count; i++)
        {
            CardView attacker = rangedAttackers[i];
            if (!attacker || attacker.cardData == null) continue;

            if (!rangedAttackCooldowns.ContainsKey(attacker))
                rangedAttackCooldowns[attacker] = offScreenRangedCooldownTurns;

            rangedAttackCooldowns[attacker]--;

            if (rangedAttackCooldowns[attacker] > 0)
                continue;

            int dmg = Random.Range(offScreenRangedMinDamage, offScreenRangedMaxDamage + 1);
            ApplyOffScreenRangedDamage(attacker.cardData.cardName, dmg);
            rangedAttackCooldowns[attacker] = offScreenRangedCooldownTurns;

            if (!player.IsAlive)
                break;
        }
    }

    void ApplyOffScreenRangedDamage(string attackerName, int dmg)
    {
        if (shieldBlocksRemaining > 0)
        {
            shieldBlocksRemaining--;
            Log($"{attackerName} fires from afar, but your shield blocks it! ({shieldBlocksRemaining} blocks left)");
            UpdateHUD();
            return;
        }

        int finalDamage = Mathf.Max(0, dmg);

        if (offScreenRangedCannotKill)
        {
            int maxAllowedDamage = Mathf.Max(0, player.currentHealth - 1);
            finalDamage = Mathf.Min(finalDamage, maxAllowedDamage);
        }

        if (finalDamage <= 0)
        {
            Log($"{attackerName} fires from afar, but you hold on at 1 HP!");
            return;
        }

        player.TakeDamage(finalDamage);
        Log($"{attackerName} fires from afar for {finalDamage} damage!");
        UpdateHUD();
    }

    IEnumerator PlayerAttack(Card enemy)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxAttackHit);
        int dmg        = player.RollDamage();
        enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - dmg);
        if (currentEnemy)
            enemyHealthByCard[currentEnemy] = enemyCurrentHP;
        currentEnemy.UpdateHP(enemyCurrentHP);
        RectTransform enemyTransform = currentEnemy.GetComponent<RectTransform>();
        StartCoroutine(ShakeCard(enemyTransform, 12f, 0.2f, 40f));
        Image enemyImage = currentEnemy.GetComponent<Image>();
        StartCoroutine(FlashVignette(enemyImage, 0.3f, 0.3f));
        Log($"You deal {dmg} damage → {enemy.cardName} HP: {enemyCurrentHP}");
        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator EnemyAttack(Card enemy)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxAttackHit);
        RectTransform enemyTransform = currentEnemy.GetComponent<RectTransform>();
        StartCoroutine(LungeCard(enemyTransform, Vector2.down, 30f, 0.3f));
        StartCoroutine(FlashVignette(damageVignette, 0.6f, 0.4f));
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
        if (combatActive)
        {
            if (turnInProgress)
                return false;

            if (!isPlayerTurn)
            {
                Log("It's not your turn.");
                return false;
            }
        }

        switch (card.cardType)
        {
            case PlayerCardType.HealthPotion:
                player.Heal(card.value);
                Log($"{card.cardName}: healed {card.value} HP → {player.currentHealth}/{player.maxHealth}");
                UpdateHUD();
                if (combatActive) StartCoroutine(ConsumePlayerTurnAfterCardUseDelayed());
                return true;

            case PlayerCardType.StaminaPotion:
                bonusStamina += card.value;
                Log($"{card.cardName}: +{card.value} stamina this fight!");
                if (combatActive) StartCoroutine(ConsumePlayerTurnAfterCardUseDelayed());
                return true;

            case PlayerCardType.Shield:
                shieldBlocksRemaining += card.value;
                Log($"{card.cardName}: next {card.value} hit(s) will be blocked!");
                UpdateHUD();
                if (combatActive) StartCoroutine(ConsumePlayerTurnAfterCardUseDelayed());
                return true;

            case PlayerCardType.MagicBlast:
                if (!combatActive)
                {
                    Log("No enemy to blast — enter combat first!");
                    return false;       // card is NOT consumed
                }
                enemyCurrentHP = Mathf.Max(0, enemyCurrentHP - card.value);
                if (currentEnemy)
                    enemyHealthByCard[currentEnemy] = enemyCurrentHP;
                currentEnemy.UpdateHP(enemyCurrentHP);
                StartCoroutine(ShakeCard(currentEnemy.GetComponent<RectTransform>(), 12f, 0.2f, 40f));
                Log($"{card.cardName}: {card.value} magic damage → enemy HP: {enemyCurrentHP}");
                RefreshCombatUI();

                if (enemyCurrentHP <= 0)
                {
                    Log($"{currentEnemy.cardData.cardName} destroyed by magic!");
                    combatPanel.SetActive(false);
                    combatActive = false;
                    enemyHealthByCard.Remove(currentEnemy);
                    ringManager.RemoveCenterCard();
                }
                else
                {
                    StartCoroutine(ConsumePlayerTurnAfterCardUseDelayed());
                }
                return true;

            default:
                return false;
        }
    }

    IEnumerator ConsumePlayerTurnAfterCardUseDelayed()
    {
        yield return new WaitForSeconds(0.6f);
        if (!combatActive) yield break;

        isPlayerTurn = false;
        if (attackButton) attackButton.interactable = false;
        Log($"{currentEnemy.cardData.cardName}'s turn.");
        StartCoroutine(ResolveEnemyAutoTurn());
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
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (hudHPBarFill)
            hudHPBarFill.fillAmount =
                (float)player.currentHealth / player.maxHealth;

        if (hudShieldIcon)
            hudShieldIcon.SetActive(shieldBlocksRemaining > 0);
    }

    void Log(string msg)
    {
        Debug.Log("[Combat] " + msg);
    }

    public void TriggerDefeat()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxDefeat);
        if (defeatTriggered) return;
        defeatTriggered = true;

        combatActive = false;
        isPlayerTurn = false;
        turnInProgress = false;

        if (combatPanel) combatPanel.SetActive(false);
        if (defeatPanel) defeatPanel.SetActive(true);

        Log("You have fallen... Game Over.");
        Time.timeScale = 0f;
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnRoomCleared()
    {
        Log("Room cleared! The door has appeared.");
    }
}
