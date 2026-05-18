using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Manages the fan of player cards at the bottom of the screen.
/// Attach to a GameObject called "PlayerHand" in the Canvas.
public class PlayerHandView : MonoBehaviour
{
    [Header("Setup")]
    public GameObject playerCardPrefab;     // Prefab: PlayerCardItemView on root
    public RectTransform handRoot;          // Horizontal layout root for the fan
    public List<PlayerCard> startingHand;   // Drag your PlayerCard SOs here

    [Header("Selection UI")]
    // [Optional]
    public GameObject confirmPanel;         // Panel shown when a card is selected
    // [Optional]
    public TextMeshProUGUI selectedNameText;
    // [Optional]
    public TextMeshProUGUI selectedDescText;
    public Button confirmButton;
    public Button cancelButton;

    // Runtime
    private List<PlayerCardItemView> spawnedItems = new List<PlayerCardItemView>();
    private PlayerCardItemView selectedItem = null;

    [HideInInspector] public CombatManager combatManager;   // set by CombatManager on Start

    void Start()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        BuildHand();
    }

    // ── Build ──────────────────────────────────────────────────────────────
    void BuildHand()
    {
        foreach (PlayerCard pc in startingHand)
            SpawnCard(pc);

        LayoutFan();
    }

    void SpawnCard(PlayerCard data)
    {
        GameObject go       = Instantiate(playerCardPrefab, handRoot);
        PlayerCardItemView v = go.GetComponent<PlayerCardItemView>();
        v.Initialise(data, this);
        spawnedItems.Add(v);
    }

    // ── Fan layout ─────────────────────────────────────────────────────────
    // Spreads cards in a slight arc like the reference image
    void LayoutFan()
    {
        int count = spawnedItems.Count;
        if (count == 0) return;

        float totalSpread  = Mathf.Min(count * 60f, 200f);  // max 200px spread
        float startX       = -totalSpread / 2f;
        float step         = count > 1 ? totalSpread / (count - 1) : 0f;

        float maxTilt      = 8f;    // degrees
        float arcHeight    = 18f;   // px — cards in the middle are slightly higher

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = spawnedItems[i].GetComponent<RectTransform>();

            float t        = count > 1 ? (float)i / (count - 1) : 0.5f;
            float x        = startX + step * i;
            float y        = -arcHeight * (2f * t - 1f) * (2f * t - 1f) + arcHeight; // parabola
            float rotation = Mathf.Lerp(-maxTilt, maxTilt, t);

            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation    = Quaternion.Euler(0, 0, -rotation);
        }
    }

    // ── Selection ──────────────────────────────────────────────────────────
    public void OnCardSelected(PlayerCardItemView item)
    {
        // Deselect previous
        if (selectedItem != null) selectedItem.SetSelected(false);

        if (selectedItem == item)
        {
            // Clicking same card again deselects
            selectedItem = null;
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
            return;
        }

        selectedItem = item;
        selectedItem.SetSelected(true);

        if (selectedNameText != null)
        {
            selectedNameText.text = item.cardData.cardName;
        }

        if (selectedDescText != null)
        {
            selectedDescText.text = item.cardData.description;
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
    }

    void OnCancel()
    {
        if (selectedItem != null) selectedItem.SetSelected(false);
        selectedItem = null;
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    void OnConfirm()
    {
        if (selectedItem == null) return;

        bool used = combatManager.UsePlayerCard(selectedItem.cardData);

        if (used)
        {
            // Remove card from hand after use
            spawnedItems.Remove(selectedItem);
            Destroy(selectedItem.gameObject);
            selectedItem = null;
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
            LayoutFan();    // re-fan the remaining cards
        }
        else
        {
            // Card couldn't be used (e.g. magic blast outside combat)
            if (selectedDescText != null)
            {
                selectedDescText.text = "Can't use that right now!";
            }
        }
    }

    // ── Public helpers ─────────────────────────────────────────────────────
    /// Add a card to the hand (e.g. picked up as loot)
    public void AddCard(PlayerCard data)
    {
        SpawnCard(data);
        LayoutFan();
    }
}
