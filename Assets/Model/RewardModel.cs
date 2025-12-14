using System;
using UnityEngine;

[Serializable]
public class RewardModel
{
    public string rewardId;
    public string rewardName;
    public string description;
    public RewardType rewardType;
    public int value; // Dollar amount or quantity
    public Sprite rewardIcon;
    public string voucherCode; // For generating unique codes
    
    public RewardModel() { }
    
    public RewardModel(string rewardId, string rewardName, string description, RewardType rewardType, int value)
    {
        this.rewardId = rewardId;
        this.rewardName = rewardName;
        this.description = description;
        this.rewardType = rewardType;
        this.value = value;
    }
    
    // Generate unique voucher code
    public string GenerateVoucherCode()
    {
        string prefix = GetVoucherPrefix();
        string randomCode = GenerateRandomString(8);
        voucherCode = $"{prefix}-{randomCode}";
        return voucherCode;
    }
    
    private string GetVoucherPrefix()
    {
        switch (rewardType)
        {
            case RewardType.Discount:
                return "OCK";
            case RewardType.FreeItem:
                return "FREE";
            case RewardType.Points:
                return "PTS";
            default:
                return "RWD";
        }
    }
    
    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] result = new char[length];
        System.Random random = new System.Random();
        
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(result);
    }
    
    // Get display text
    public string GetDisplayText()
    {
        switch (rewardType)
        {
            case RewardType.Discount:
                return $"${value} Off Voucher";
            case RewardType.FreeItem:
                return $"Free {rewardName}";
            case RewardType.Points:
                return $"{value} Points";
            default:
                return rewardName;
        }
    }
    
    // Get color based on reward value
    public Color GetRewardColor()
    {
        if (value >= 10)
            return new Color(1f, 0.65f, 0f); // Gold
        else if (value >= 5)
            return new Color(0.2f, 0.5f, 1f); // Blue
        else
            return new Color(0.2f, 0.8f, 0.2f); // Green
    }
}

// Reward type enumeration
public enum RewardType
{
    Discount,    // Voucher with $ off
    FreeItem,    // Free food item
    Points       // Loyalty points
}