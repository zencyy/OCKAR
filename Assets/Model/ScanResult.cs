using System;

[Serializable]
public class ScanResult
{
    public string itemId;
    public string itemName;
    public RarityType rarity;
    public int newCount; // New total count of this item
    public bool isFirstTime; // First time getting this item?
    public bool completedNewCombo; // Did this scan complete a combo?
    public string completedComboId;
    public string rewardEarned;
    public DateTime scanTime;
    
    public ScanResult() { }
    
    public ScanResult(string itemId)
    {
        this.itemId = itemId;
        this.scanTime = DateTime.UtcNow;
        
        // Get item details from database
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if (item != null)
        {
            this.itemName = item.itemName;
            this.rarity = item.rarity;
        }
    }
    
    // Get congratulations message
    // Get congratulations message
    public string GetCongratsMessage()
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        string message = $"You got {itemName}!";
        
        if (isFirstTime)
        {
            message += "\n✨ NEW! First time getting this item!";
        }
        
        message += $"\n{GetRarityAnnouncement()}";
        
        if (item != null)
        {
            // --- FIX: Handle 0 count (fallback to 1 if data hasn't loaded yet) ---
            int displayCount = (newCount > 0) ? newCount : 1; 
            // --------------------------------------------------------------------

            message += $"\n\nProgress: {displayCount}/{item.rewardThreshold}";
            
            if (completedNewCombo) // Reusing this for "reward unlocked"
            {
                message += $"\n\n🎉 REWARD UNLOCKED!";
                message += $"\n🎁 {rewardEarned}";
                message += $"\n💡 Check your Rewards screen!";
            }
            else if (displayCount < item.rewardThreshold)
            {
                int remaining = item.rewardThreshold - displayCount;
                message += $"\n({remaining} more for {item.rewardDescription})";
            }
        }
        
        return message;
    }
    
    // Get rarity announcement
    public string GetRarityAnnouncement()
    {
        switch (rarity)
        {
            case RarityType.Common:
                return "Common Item";
            case RarityType.Uncommon:
                return "⭐ Uncommon Item!";
            case RarityType.Rare:
                return "⭐⭐ RARE ITEM!";
            case RarityType.Legendary:
                return "⭐⭐⭐ LEGENDARY ITEM!!!";
            default:
                return "Item Obtained";
        }
    }
    
    // Should play special animation?
    public bool ShouldPlaySpecialAnimation()
    {
        return rarity == RarityType.Rare || rarity == RarityType.Legendary || isFirstTime || completedNewCombo;
    }
}