using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Spawns N cards in a circle, rotates the ring left/right,
/// and exposes the "centre" card (the one the player is about to interact with).
/// Attach to a Canvas GameObject called "RingManager".
public class RingManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject cardPrefab;           // Prefab with CardView on it
    public RectTransform ringRoot;          // Empty RectTransform in the Canvas that the ring orbits around
    public float radius = 280f;             // Pixel radius of the ring
    public List<Card> deckForThisRoom;      // Drag your Card ScriptableObjects in here

    [Header("References")]
    public CombatManager combatManager;
    public Button rotateLeftButton;
    public Button rotateRightButton;

    // Private state
    private List<CardView> spawnedCards = new List<CardView>();
    private int centerIndex = 0;            // Which card is currently "facing" the player
    private bool isAnimating = false;

    void Start()
    {
        BuildRing();
        HighlightCenter();

        rotateLeftButton.onClick.AddListener(()  => RotateRing(-1));
        rotateRightButton.onClick.AddListener(() => RotateRing( 1));
    }

    // ── Build ──────────────────────────────────────────────────────────────
    void BuildRing()
    {
        int count = deckForThisRoom.Count;
        if (count == 0) { Debug.LogWarning("RingManager: no cards in deck!"); return; }

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i - 90f;    // start at top
            Vector2 pos  = AngleToPosition(angle);

            GameObject go   = Instantiate(cardPrefab, ringRoot);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            // Rotate the card so it faces outward (optional, looks nice)
            go.transform.localRotation = Quaternion.Euler(0, 0, angle + 90f);

            CardView view = go.GetComponent<CardView>();
            view.Initialise(deckForThisRoom[i], this);
            spawnedCards.Add(view);
        }
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

        centerIndex = (centerIndex - direction + spawnedCards.Count) % spawnedCards.Count;

        int count = spawnedCards.Count;
        for (int i = 0; i < count; i++)
        {
            // Recalculate visual position for every card
            int visualIndex = (i - centerIndex + count) % count;
            float angle     = (360f / count) * visualIndex - 90f;
            RectTransform rt = spawnedCards[i].GetComponent<RectTransform>();
            rt.anchoredPosition = AngleToPosition(angle);
            spawnedCards[i].transform.localRotation = Quaternion.Euler(0, 0, angle + 90f);
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
        CardView cv = spawnedCards[centerIndex];
        combatManager.BeginEncounter(cv);
    }

    /// Called by CombatManager when a card is defeated / collected
    public void RemoveCenterCard()
    {
        CardView cv = spawnedCards[centerIndex];
        spawnedCards.RemoveAt(centerIndex);
        Destroy(cv.gameObject);

        if (spawnedCards.Count == 0)
        {
            combatManager.OnRoomCleared();
            return;
        }

        centerIndex = centerIndex % spawnedCards.Count;
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
            spawnedCards[i].transform.localRotation = Quaternion.Euler(0, 0, angle + 90f);
        }
    }
}
