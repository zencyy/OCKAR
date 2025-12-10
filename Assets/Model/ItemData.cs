using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemModel
{
    public string itemId;
    public string itemName;
    public string description;
    public RarityType rarity;
    public int dropRate;
    public Sprite itemSprite;
    public GameObject itemPrefab;
    public string imageUrl;
    
    // NEW: Reward system
    public int rewardThreshold; // How many to collect for reward
    public string rewardDescription; // E.g., "$5 Off Voucher"
    
    public ItemModel() { }
    
    public ItemModel(string itemId, string itemName, string description, RarityType rarity, int dropRate, int rewardThreshold, string rewardDescription)
    {
        this.itemId = itemId;
        this.itemName = itemName;
        this.description = description;
        this.rarity = rarity;
        this.dropRate = dropRate;
        this.rewardThreshold = rewardThreshold;
        this.rewardDescription = rewardDescription;
    }
    
    // Get rarity as string
    public string GetRarityString()
    {
        return rarity.ToString();
    }
    
    // Get rarity color
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case RarityType.Common:
                return new Color(0.7f, 0.7f, 0.7f); // Gray
            case RarityType.Uncommon:
                return new Color(0.2f, 0.8f, 0.2f); // Green
            case RarityType.Rare:
                return new Color(0.2f, 0.5f, 1f); // Blue
            case RarityType.Legendary:
                return new Color(1f, 0.65f, 0f); // Gold
            default:
                return Color.white;
        }
    }
    
    // Get rarity display name
    public string GetRarityDisplayName()
    {
        switch (rarity)
        {
            case RarityType.Common:
                return "COMMON";
            case RarityType.Uncommon:
                return "UNCOMMON";
            case RarityType.Rare:
                return "RARE";
            case RarityType.Legendary:
                return "LEGENDARY";
            default:
                return "UNKNOWN";
        }
    }
    
    // Convert to dictionary for Firebase
    public static Dictionary<string, object> ToDictionary(ItemModel item)
    {
        return new Dictionary<string, object>
        {
            { "name", item.itemName },
            { "description", item.description },
            { "rarity", item.GetRarityString().ToLower() },
            { "dropRate", item.dropRate },
            { "rewardThreshold", item.rewardThreshold },
            { "rewardDescription", item.rewardDescription },
            { "imageUrl", item.imageUrl ?? "" }
        };
    }
}

// Rarity enumeration
public enum RarityType
{
    Common,
    Uncommon,
    Rare,
    Legendary
}