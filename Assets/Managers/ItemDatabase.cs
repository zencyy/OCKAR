using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    
    [Header("Item Prefabs")]
    [SerializeField] private GameObject curryPuffPrefab;
    [SerializeField] private GameObject fishBallPrefab;
    [SerializeField] private GameObject sotongBallPrefab;
    [SerializeField] private GameObject chickenWingPrefab;
    [SerializeField] private GameObject springRollPrefab;
    [SerializeField] private GameObject ngorHiangPrefab;
    
    [Header("Item Sprites")]
    [SerializeField] private Sprite curryPuffSprite;
    [SerializeField] private Sprite fishBallSprite;
    [SerializeField] private Sprite sotongBallSprite;
    [SerializeField] private Sprite chickenWingSprite;
    [SerializeField] private Sprite springRollSprite;
    [SerializeField] private Sprite ngorHiangSprite;
    
    private Dictionary<string, ItemModel> itemDatabase;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeDatabase()
    {
        itemDatabase = new Dictionary<string, ItemModel>
        {
            {
                "curry_puff", new ItemModel(
                    "curry_puff",
                    "Curry Puff",
                    "The iconic Old Chang Kee snack with a crispy golden crust",
                    RarityType.Common,
                    40, // Drop rate
                    10, // Need 10 for reward
                    "$5 Off Voucher"
                )
                {
                    itemPrefab = curryPuffPrefab,
                    itemSprite = curryPuffSprite
                }
            },
            {
                "fish_ball", new ItemModel(
                    "fish_ball",
                    "Fish Ball",
                    "Crispy on the outside, tender fish paste inside",
                    RarityType.Common,
                    30,
                    10,
                    "$5 Off Voucher"
                )
                {
                    itemPrefab = fishBallPrefab,
                    itemSprite = fishBallSprite
                }
            },
            {
                "spring_roll", new ItemModel(
                    "spring_roll",
                    "Spring Roll",
                    "Crispy vegetable spring roll",
                    RarityType.Uncommon,
                    20,
                    8,
                    "$10 Off Voucher"
                )
                {
                    itemPrefab = springRollPrefab,
                    itemSprite = springRollSprite
                }
            },
            {
                "sotong_ball", new ItemModel(
                    "sotong_ball",
                    "Sotong Ball",
                    "Delicious squid ball with a crispy coating",
                    RarityType.Uncommon,
                    15,
                    8,
                    "$10 Off Voucher"
                )
                {
                    itemPrefab = sotongBallPrefab,
                    itemSprite = sotongBallSprite
                }
            },
            {
                "chicken_wing", new ItemModel(
                    "chicken_wing",
                    "Chicken Wing",
                    "Perfectly seasoned and fried chicken wings",
                    RarityType.Rare,
                    10,
                    5,
                    "$15 Off Voucher"
                )
                {
                    itemPrefab = chickenWingPrefab,
                    itemSprite = chickenWingSprite
                }
            },
            {
                "ngor_hiang", new ItemModel(
                    "ngor_hiang",
                    "Ngor Hiang",
                    "The ultimate Old Chang Kee treat! Five-spice meat roll",
                    RarityType.Legendary,
                    5,
                    3,
                    "$20 Off Voucher"
                )
                {
                    itemPrefab = ngorHiangPrefab    ,
                    itemSprite = ngorHiangSprite
                }
            }
        };
    }
    
    // Get item by ID
    public ItemModel GetItem(string itemId)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            return itemDatabase[itemId];
        }
        
        Debug.LogWarning($"Item {itemId} not found in database!");
        return null;
    }
    
    // Get item name
    public string GetItemName(string itemId)
    {
        ItemModel item = GetItem(itemId);
        return item != null ? item.itemName : "Unknown";
    }
    
    // Get item prefab
    public GameObject GetItemPrefab(string itemId)
    {
        ItemModel item = GetItem(itemId);
        return item?.itemPrefab;
    }
    
    // Get item sprite
    public Sprite GetItemSprite(string itemId)
    {
        ItemModel item = GetItem(itemId);
        return item?.itemSprite;
    }
    
    // Get item rarity
    public RarityType GetItemRarity(string itemId)
    {
        ItemModel item = GetItem(itemId);
        return item != null ? item.rarity : RarityType.Common;
    }
    
    // Get item description
    public string GetItemDescription(string itemId)
    {
        ItemModel item = GetItem(itemId);
        return item != null ? item.description : "";
    }
    
    // Get all items
    public List<ItemModel> GetAllItems()
    {
        return new List<ItemModel>(itemDatabase.Values);
    }
    
    // Get items by rarity
    public List<ItemModel> GetItemsByRarity(RarityType rarity)
    {
        List<ItemModel> items = new List<ItemModel>();
        
        foreach (var item in itemDatabase.Values)
        {
            if (item.rarity == rarity)
            {
                items.Add(item);
            }
        }
        
        return items;
    }
    
    // Roll for random item based on drop rates
    public string RollRandomItem()
    {
        int totalWeight = 0;
        foreach (var item in itemDatabase.Values)
        {
            totalWeight += item.dropRate;
        }
        
        int roll = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var item in itemDatabase.Values)
        {
            currentWeight += item.dropRate;
            if (roll < currentWeight)
            {
                return item.itemId;
            }
        }
        
        return "curry_puff"; // Fallback
    }
    public void UpdateItemStats(string itemId, int newDropRate, int newThreshold)
    {
        if (itemDatabase.ContainsKey(itemId))
        {
            itemDatabase[itemId].dropRate = newDropRate;
            itemDatabase[itemId].rewardThreshold = newThreshold;
            Debug.Log($"Updated {itemId}: Rate {newDropRate}, Threshold {newThreshold}");
        }
    }

    /*/[ContextMenu("Upload Items to Firebase")]
    public void UploadToFirebase()
    {
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialized)
        {
            FirebaseManager.Instance.SaveItemDatabaseToFirebase();
        }
        else
        {
            Debug.LogError("Firebase is not initialized yet. Play the game first!");
        }
    }*/
}