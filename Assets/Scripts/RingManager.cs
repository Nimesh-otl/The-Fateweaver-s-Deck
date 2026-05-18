using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// Spawns N cards in a circle, rotates the ring left/right,
/// and exposes the "centre" card (the one the player is about to interact with).
/// Attach to a Canvas GameObject called "RingManager".
public class RingManager : MonoBehaviour
{
    public enum RoomState
    {
        InProgress,
        Cleared
    }

    [Header("Setup")]
    public GameObject cardPrefab;           // Prefab with CardView on it
    public RectTransform ringRoot;          // Empty RectTransform in the Canvas that the ring orbits around
    public float radius = 280f;             // Pixel radius of the ring
    public List<Card> deckForThisRoom;      // Drag your Card ScriptableObjects in here
    public string nextSceneName;            // Scene to load when exit is used
    public Card secondDoorCardData;         // Optional second exit for branching paths

    [Header("References")]
    public CombatManager combatManager;
    public ScrollManager scrollManager;
    public PlayerData player;
    public Button rotateLeftButton;
    public Button rotateRightButton;

    // Private state
    private List<CardView> spawnedCards = new List<CardView>();
    private int centerIndex = 0;            // Which card is currently "facing" the player
    private bool isAnimating = false;
    private Card doorCardData;
    private int activeEnemyCount;
    private RoomState roomState = RoomState.InProgress;

    void Start()
    {
        if (!combatManager) combatManager = FindObjectOfType<CombatManager>();
        if (!combatManager)
            Debug.LogError("RingManager: CombatManager reference is missing.");

        BuildRing();
        HighlightCenter();

        rotateLeftButton.onClick.AddListener(()  => RotateRing(-1));
        rotateRightButton.onClick.AddListener(() => RotateRing( 1));
    }

    // ── Build ──────────────────────────────────────────────────────────────
    void BuildRing()
    {
        spawnedCards.Clear();
        centerIndex = 0;
        activeEnemyCount = 0;
        roomState = RoomState.InProgress;
        doorCardData = null;

        List<Card> cardsToSpawn = new List<Card>();

        for (int i = 0; i < deckForThisRoom.Count; i++)
        {
            Card card = deckForThisRoom[i];
            if (card == null) continue;

            if (card.cardType == CardType.Exit)
            {
                if (doorCardData == null)
                    doorCardData = card;
                continue;
            }

            cardsToSpawn.Add(card);
            if (IsEnemyCard(card))
                activeEnemyCount++;
        }

        int count = cardsToSpawn.Count;
        if (count == 0)
        {
            Debug.LogWarning("RingManager: no non-exit cards to spawn!");
            OnAllEnemiesCleared();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i - 90f;    // start at top
            Vector2 pos  = AngleToPosition(angle);

            GameObject go   = Instantiate(cardPrefab, ringRoot);
            if (go == null)
            {
                Debug.LogError("RingManager: Failed to instantiate cardPrefab!");
                return;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;


            CardView view = go.GetComponent<CardView>();
            if (view == null)
            {
                Debug.LogError($"RingManager: cardPrefab is missing CardView component! Card #{i}");
                return;
            }

            view.Initialise(cardsToSpawn[i], this);
            spawnedCards.Add(view);
        }

        if (activeEnemyCount == 0)
            OnAllEnemiesCleared();
    }

    Vector2 AngleToPosition(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
    }

    // ── Rotation ───────────────────────────────────────────────────────────
    /// direction: +1 = clockwise, -1 = counter-clockwise
    public void RotateRing(int direction)
    {
        if (isAnimating) return;
        if (spawnedCards.Count == 0) return;

        centerIndex = (centerIndex - direction + spawnedCards.Count) % spawnedCards.Count;

        int count = spawnedCards.Count;
        for (int i = 0; i < count; i++)
        {
            // Recalculate visual position for every card
            int visualIndex = (i - centerIndex + count) % count;
            float angle     = (360f / count) * visualIndex - 90f;
            RectTransform rt = spawnedCards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = AngleToPosition(angle);
        }

        HighlightCenter();
    }

    void HighlightCenter()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
            spawnedCards[i].SetCenter(i == centerIndex);
    }

    // ── Interaction ────────────────────────────────────────────────────────
    /// Called by CardView when the centre card is clicked
    public void InteractWithCenter()
    {
        if (combatManager != null && combatManager.combatActive) return;
        if (spawnedCards.Count == 0) return;

        if (!combatManager)
        {
            Debug.LogError("RingManager: Cannot start encounter because CombatManager is missing.");
            return;
        }

        CardView cv = spawnedCards[centerIndex];

        if (roomState == RoomState.Cleared && cv.cardData.cardType != CardType.Exit)
            return;

        if (cv.cardData.cardType == CardType.Exit && roomState != RoomState.Cleared)
        {
            Debug.LogWarning("RingManager: Exit is locked until all enemies are defeated.");
            return;
        }

        if (cv.cardData.cardType == CardType.Exit)
        {
            if (!string.IsNullOrEmpty(cv.cardData.nextSceneName) &&
                (cv.cardData.nextSceneName == "The_Hermit" ||
                 cv.cardData.nextSceneName == "The_Devil"))
            {
                RunData.ChoseHermit = cv.cardData.nextSceneName == "The_Hermit";
            }
        }

        combatManager.BeginEncounter(cv);
    }

    /// Called by CombatManager when a card is defeated / collected
    public void RemoveCenterCard()
    {
        if (spawnedCards.Count == 0) return;

        CardView cv = spawnedCards[centerIndex];
        bool removedEnemy = IsEnemyCard(cv.cardData);

        spawnedCards.RemoveAt(centerIndex);
        Destroy(cv.gameObject);

        if (removedEnemy && roomState == RoomState.InProgress)
        {
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
            if (activeEnemyCount == 0)
                OnAllEnemiesCleared();
        }

        if (spawnedCards.Count == 0) return;

        centerIndex = centerIndex % spawnedCards.Count;
        RebuildPositions();
        HighlightCenter();
    }

    bool IsEnemyCard(Card card)
    {
        if (card == null) return false;

        return card.cardType == CardType.BasicEnemy
            || card.cardType == CardType.RangedEnemy
            || card.cardType == CardType.Mimic;
    }

    public List<CardView> GetOffScreenRangedEnemies(CardView activeEnemy)
    {
        List<CardView> rangedEnemies = new List<CardView>();

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            CardView cv = spawnedCards[i];
            if (!cv || cv == activeEnemy) continue;
            if (cv.cardData == null) continue;

            if (cv.cardData.cardType == CardType.RangedEnemy)
                rangedEnemies.Add(cv);
        }

        return rangedEnemies;
    }

    void OnAllEnemiesCleared()
    {
        if (roomState == RoomState.Cleared) return;

        roomState = RoomState.Cleared;

        if (scrollManager != null)
        {
            scrollManager.ShowScroll();
            return;
        }

        if (combatManager)
            combatManager.OnRoomCleared();

        SpawnDoorCard();
    }

    public void SpawnDoorCard()
    {
        if (doorCardData == null)
        {
            Debug.LogWarning("RingManager: Room cleared but no Exit card configured.");
            return;
        }

        int firstDoorIndex = spawnedCards.Count;

        GameObject go = Instantiate(cardPrefab, ringRoot);
        if (go == null)
        {
            Debug.LogError("RingManager: Failed to instantiate door card!");
            return;
        }

        CardView view = go.GetComponent<CardView>();
        if (view == null)
        {
            Debug.LogError("RingManager: cardPrefab is missing CardView component for door.");
            Destroy(go);
            return;
        }

        view.Initialise(doorCardData, this);
        spawnedCards.Add(view);

        if (secondDoorCardData != null)
        {
            GameObject secondGo = Instantiate(cardPrefab, ringRoot);
            if (secondGo == null)
            {
                Debug.LogError("RingManager: Failed to instantiate second door card!");
                return;
            }

            CardView secondView = secondGo.GetComponent<CardView>();
            if (secondView == null)
            {
                Debug.LogError("RingManager: cardPrefab is missing CardView component for second door.");
                Destroy(secondGo);
                return;
            }

            secondView.Initialise(secondDoorCardData, this);
            spawnedCards.Add(secondView);
        }

        centerIndex = firstDoorIndex;
        RebuildPositions();
        HighlightCenter();
    }

    void RebuildPositions()
    {
        int count = spawnedCards.Count;
        for (int i = 0; i < count; i++)
        {
            int visualIndex = (i - centerIndex + count) % count;
            float angle     = (360f / count) * visualIndex - 90f;
            RectTransform rt = spawnedCards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = AngleToPosition(angle);
        }
    }
}
