using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// Attached to each card prefab in the ring.
/// Displays the card data and handles the hover/click visual feedback.
public class CardView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image cardArtImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI cardTypeText;
    public GameObject highlightBorder;      // a coloured outline Image, default hidden

    // The data this card represents
    [HideInInspector] public Card cardData;
    [HideInInspector] public bool isCenter;  // true when this is the card facing the player

    private RingManager ringManager;
    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
        if (highlightBorder) highlightBorder.SetActive(false);
    }

    public void Initialise(Card data, RingManager manager)
    {
        cardData = data;
        ringManager = manager;
        Refresh();
    }

    public void Refresh()
    {
        {
            if (cardData == null) return;

            cardNameText.text = cardData.cardName;

            // Hide HP and DMG for non-enemy cards
            bool isEnemy = cardData.cardType == CardType.BasicEnemy ||
                           cardData.cardType == CardType.RangedEnemy ||
                           cardData.cardType == CardType.Mimic;

            if (healthText) healthText.gameObject.SetActive(isEnemy);
            if (damageText) damageText.gameObject.SetActive(isEnemy);

            if (cardArtImage && cardData.cardArt)
                cardArtImage.sprite = cardData.cardArt;
        }
        if (cardData == null) return;

        cardNameText.text   = cardData.cardName;
        healthText.text     = $"{cardData.health}";
        damageText.text     = $"DMG: {cardData.damageMin}-{cardData.damageMax}";
        cardTypeText.text   = cardData.cardType.ToString();

        if (cardArtImage && cardData.cardArt)
            cardArtImage.sprite = cardData.cardArt;
    }

    public void UpdateHP(int currentHP)
    {
        if (healthText) healthText.text = currentHP.ToString();
    }

    public void SetCenter(bool center)
    {
        isCenter = center;
        if (highlightBorder) highlightBorder.SetActive(center);
        // Scale up the centre card slightly so it stands out
        transform.localScale = center ? baseScale * 1.15f : baseScale;
    }

    // Called by Unity UI Button component on this prefab
    public void OnCardClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCardClick);
        if (isCenter)
            ringManager.InteractWithCenter();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked();
    }
}
