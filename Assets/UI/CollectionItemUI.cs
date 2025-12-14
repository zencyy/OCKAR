using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionItemUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject lockedOverlay;
    
    public void SetupItem(string itemName, string rarity, int count, Sprite itemSprite)
    {
        itemNameText.text = itemName;
        rarityText.text = rarity;
        countText.text = $"x{count}";
        
        // Set rarity color
        Color rarityColor = GetRarityColor(rarity);
        rarityText.color = rarityColor;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
        }
        
        // Show/hide locked state
        if (count > 0)
        {
            if (lockedOverlay != null)
                lockedOverlay.SetActive(false);
                
            if (itemImage != null && itemSprite != null)
            {
                itemImage.sprite = itemSprite;
                itemImage.color = Color.white;
            }
        }
        else
        {
            if (lockedOverlay != null)
                lockedOverlay.SetActive(true);
                
            if (itemImage != null)
                itemImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }
    
    private Color GetRarityColor(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common":
                return new Color(0.7f, 0.7f, 0.7f); // Gray
            case "uncommon":
                return new Color(0.2f, 0.8f, 0.2f); // Green
            case "rare":
                return new Color(0.2f, 0.5f, 1f); // Blue
            case "legendary":
                return new Color(1f, 0.65f, 0f); // Gold
            default:
                return Color.white;
        }
    }
}