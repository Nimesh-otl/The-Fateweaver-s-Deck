using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Sits on each card prefab in the player's hand.
/// Handles visuals and click → tells PlayerHandView a card was selected.
public class PlayerCardItemView : MonoBehaviour
{
    [Header("UI References")]
    public Image cardBackground;
    public Image cardArtImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardValueText;   // e.g. "+4 HP" or "3 DMG"
    public GameObject selectedGlow;         // Image outline, set inactive by default

    // Runtime
    [HideInInspector] public PlayerCard cardData;
    private PlayerHandView handView;
    private Vector3 basePosition;
    private bool isSelected = false;

    // Colour tints per card type
    static readonly Color ColourHealth   = new Color(0.85f, 0.25f, 0.25f, 1f); // red
    static readonly Color ColourStamina  = new Color(0.25f, 0.65f, 0.85f, 1f); // blue
    static readonly Color ColourShield   = new Color(0.55f, 0.55f, 0.85f, 1f); // purple
    static readonly Color ColourMagic    = new Color(0.85f, 0.55f, 0.10f, 1f); // orange

    public void Initialise(PlayerCard data, PlayerHandView parent)
    {
        cardData = data;
        handView = parent;

        if (selectedGlow) selectedGlow.SetActive(false);

        Refresh();
    }

    void Refresh()
    {
        cardNameText.text = cardData.cardName;

        // Value label
        cardValueText.text = cardData.cardType switch
        {
            PlayerCardType.HealthPotion  => $"+{cardData.value} HP",
            PlayerCardType.StaminaPotion => $"+{cardData.value} STM",
            PlayerCardType.Shield        => $"Block {cardData.value}",
            PlayerCardType.MagicBlast    => $"{cardData.value} DMG",
            _                            => cardData.value.ToString()
        };

        // Background tint
        if (cardBackground)
        {
            cardBackground.color = cardData.cardType switch
            {
                PlayerCardType.HealthPotion  => ColourHealth,
                PlayerCardType.StaminaPotion => ColourStamina,
                PlayerCardType.Shield        => ColourShield,
                PlayerCardType.MagicBlast    => ColourMagic,
                _                            => Color.white
            };
        }

        if (cardArtImage && cardData.cardArt)
            cardArtImage.sprite = cardData.cardArt;
    }

    // Called by Unity Button on this prefab
    public void OnClicked()
    {
        handView.OnCardSelected(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedGlow) selectedGlow.SetActive(selected);

        // Lift the card up when selected
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 pos      = rt.anchoredPosition;
        rt.anchoredPosition = selected
            ? new Vector2(pos.x, pos.y + 30f)
            : new Vector2(pos.x, pos.y - 30f);
    }
}
