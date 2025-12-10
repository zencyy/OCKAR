using System;
using System.Collections.Generic;

[Serializable]
public class UserModel
{
    public string userId;
    public string username;
    public string email;
    public int totalScans;
    public string lastScanTime;
    public Dictionary<string, int> collectedItems;
    public Dictionary<string, bool> claimedCombos;
    public Dictionary<string, int> rewards;
    
    public UserModel()
    {
        collectedItems = new Dictionary<string, int>();
        claimedCombos = new Dictionary<string, bool>();
        rewards = new Dictionary<string, int>();
    }
    
    public UserModel(string userId, string username, string email)
    {
        this.userId = userId;
        this.username = username;
        this.email = email;
        this.totalScans = 0;
        this.lastScanTime = DateTime.UtcNow.ToString("o");
        
        // Initialize empty collections
        collectedItems = new Dictionary<string, int>
        {
            { "curry_puff", 0 },
            { "fish_ball", 0 },
            { "sotong_ball", 0 },
            { "chicken_wing", 0 },
            { "spring_roll", 0 },
            { "ngor_hiang", 0 }
        };
        
        claimedCombos = new Dictionary<string, bool>
        {
            { "classic_trio", false },
            { "seafood_special", false },
            { "mega_feast", false }
        };
        
        rewards = new Dictionary<string, int>
        {
            { "voucher_5off", 0 },
            { "voucher_10off", 0 },
            { "free_meal_voucher", 0 }
        };
    }
    
    // Get total unique items collected (items with count > 0)
    public int GetUniqueItemsCount()
    {
        int count = 0;
        foreach (var item in collectedItems.Values)
        {
            if (item > 0)
                count++;
        }
        return count;
    }
    
    // Get total items collected (sum of all counts)
    public int GetTotalItemsCount()
    {
        int count = 0;
        foreach (var item in collectedItems.Values)
        {
            count += item;
        }
        return count;
    }
    
    // Check if user has collected all items at least once
    public bool HasCompleteCollection()
    {
        foreach (var item in collectedItems.Values)
        {
            if (item == 0)
                return false;
        }
        return true;
    }
    
    // Get completion percentage (0-100)
    public float GetCompletionPercentage()
    {
        return (GetUniqueItemsCount() / 6f) * 100f;
    }
    
    // Get total claimed combos
    public int GetClaimedCombosCount()
    {
        int count = 0;
        foreach (var combo in claimedCombos.Values)
        {
            if (combo)
                count++;
        }
        return count;
    }
    
    // Get total rewards earned
    public int GetTotalRewardsCount()
    {
        int count = 0;
        foreach (var reward in rewards.Values)
        {
            count += reward;
        }
        return count;
    }
    
    // Add item to collection
    public void AddItem(string itemId)
    {
        if (collectedItems.ContainsKey(itemId))
        {
            collectedItems[itemId]++;
        }
    }
    
    // Increment scan count
    public void IncrementScans()
    {
        totalScans++;
        lastScanTime = DateTime.UtcNow.ToString("o");
    }
    
    // Mark combo as claimed
    public void ClaimCombo(string comboId)
    {
        if (claimedCombos.ContainsKey(comboId))
        {
            claimedCombos[comboId] = true;
        }
    }
    
    // Add reward
    public void AddReward(string rewardId, int amount = 1)
    {
        if (rewards.ContainsKey(rewardId))
        {
            rewards[rewardId] += amount;
        }
        else
        {
            rewards[rewardId] = amount;
        }
    }
    
    // Convert to dictionary for Firebase
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "totalScans", totalScans },
            { "lastScanTime", lastScanTime }
        };
    }
}