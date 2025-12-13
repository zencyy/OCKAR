using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    
    private DatabaseReference databaseReference;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    
    public bool IsInitialized { get; private set; }
    public string CurrentUserId => currentUser?.UserId;

    private readonly List<Action> _actions = new List<Action>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else { Destroy(gameObject); }
    }
    
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                IsInitialized = true;
                Debug.Log("Firebase initialized");
                SyncGameData();
            }
            else { Debug.LogError($"Firebase Error: {task.Result}"); }
        });
    }

    private void Update()
    {
        if (_actions.Count > 0)
        {
            List<Action> localActions = new List<Action>();
            lock (_actions)
            {
                localActions.AddRange(_actions);
                _actions.Clear();
            }
            foreach (var action in localActions) action();
        }
    }

    // Update SyncGameData to use this queue
    private void SyncGameData()
    {
        databaseReference.Child("game_data").Child("items").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                lock (_actions)
                {
                    _actions.Add(() => 
                    {
                        foreach (var itemSnapshot in task.Result.Children)
                        {
                            try {
                                string itemId = itemSnapshot.Key;
                                int dropRate = int.Parse(itemSnapshot.Child("dropRate").Value.ToString());
                                int threshold = int.Parse(itemSnapshot.Child("rewardThreshold").Value.ToString());
                                ItemDatabase.Instance.UpdateItemStats(itemId, dropRate, threshold);
                            } catch {}
                        }
                        Debug.Log("✅ Items Synced from Cloud");
                    });
                }
            }
        });
    }

    
    
    // --- AUTH ---
    public void SignUpWithEmail(string email, string password, string username, Action<bool, string> callback)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled) { callback?.Invoke(false, "Error"); return; }
            currentUser = task.Result.User;
            CreateUserProfile(currentUser.UserId, username, email);
            callback?.Invoke(true, "Success");
        });
    }

    public void SignInWithEmail(string email, string password, Action<bool, string> callback)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled) { callback?.Invoke(false, "Error"); return; }
            currentUser = task.Result.User;
            callback?.Invoke(true, "Success");
        });
    }

    public void SignOut() { auth.SignOut(); currentUser = null; }

    // --- SETUP ---
    private void CreateUserProfile(string userId, string username, string email)
    {
        // We store username and email for display purposes.
        // NEVER store the password here.
        var userData = new Dictionary<string, object> 
        { 
            { "username", username }, 
            { "email", email }, 
            { "totalScans", 0 },
            { "lastScanTime", "" }
        };
        
        databaseReference.Child("users").Child(userId).SetValueAsync(userData);
        InitializeUserCollection(userId);
    }

    private void InitializeUserCollection(string userId)
    {
        var initialCollection = new Dictionary<string, object> { 
            { "curry_puff", 0 }, { "fish_ball", 0 }, { "sotong_ball", 0 }, 
            { "chicken_wing", 0 }, { "spring_roll", 0 }, { "ngor_hiang", 0 } 
        };
        databaseReference.Child("users").Child(userId).Child("collectedItems").SetValueAsync(initialCollection);
        
        var initialRewards = new Dictionary<string, object> {
            { "curry_puff_reward", false }, { "fish_ball_reward", false },
            { "spring_roll_reward", false }, { "sotong_ball_reward", false },
            { "chicken_wing_reward", false }, { "ngor_hiang_reward", false }
        };
        databaseReference.Child("users").Child(userId).Child("claimedRewards").SetValueAsync(initialRewards);
    }
    /*/public void SaveItemDatabaseToFirebase()
    {
        if (databaseReference == null) return;

        Debug.Log("Starting Item Database Upload...");
        List<ItemModel> allItems = ItemDatabase.Instance.GetAllItems();

        foreach (var item in allItems)
        {
            // We create a clean dictionary. We DO NOT upload Prefabs/Sprites.
            Dictionary<string, object> itemData = new Dictionary<string, object>();
            itemData["name"] = item.itemName;
            itemData["description"] = item.description;
            itemData["rarity"] = item.rarity.ToString(); // Store enum as string
            itemData["dropRate"] = item.dropRate;
            itemData["rewardThreshold"] = item.rewardThreshold;
            itemData["rewardDescription"] = item.rewardDescription;

            // Save to "game_data/items/[itemId]"
            databaseReference.Child("game_data").Child("items").Child(item.itemId).SetValueAsync(itemData);
        }
        
        Debug.Log("Upload complete! Check your Firebase Console.");
    }*/

    // --- SCANNING ---
   // --- SCANNING ---
    public void RecordScan(Action<ScanResult> onItemReceived)
    {
        if (currentUser == null) return;
        string userId = currentUser.UserId;

        // 1. Update Scans
        databaseReference.Child("users").Child(userId).Child("totalScans").RunTransaction(data => {
            int val = data.Value != null ? int.Parse(data.Value.ToString()) : 0;
            data.Value = val + 1;
            return TransactionResult.Success(data);
        });

        // 2. Logic
        string itemId = ItemDatabase.Instance.RollRandomItem();
        ScanResult result = new ScanResult(itemId);

        // 3. Update Collection & GET THE NEW COUNT
        databaseReference.Child("users").Child(userId).Child("collectedItems").Child(itemId).RunTransaction(data => {
            int count = data.Value != null ? int.Parse(data.Value.ToString()) : 0;
            data.Value = count + 1;
            return TransactionResult.Success(data);
        }).ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Transaction failed.");
                return;
            }

            // --- FIX START: Get the updated count from the transaction result ---
            DataSnapshot snapshot = task.Result; // This contains the value AFTER the update
            int updatedCount = int.Parse(snapshot.Value.ToString());

            // Update the Result Object
            result.newCount = updatedCount;
            result.isFirstTime = (updatedCount == 1);
            // --- FIX END ---

            // Check reward using the CORRECT new count
            CheckRewardUnlocked(userId, itemId, result.newCount, (unlockedReward) =>
            {
                if (unlockedReward != null)
                {
                    result.completedNewCombo = true;
                    result.rewardEarned = unlockedReward;
                }
                onItemReceived?.Invoke(result);
            });
        });
    }

    // FIX IS HERE: We DO NOT set the database to true here. We only check.
    private void CheckRewardUnlocked(string userId, string itemId, int newCount, Action<string> callback)
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if (item == null) { callback?.Invoke(null); return; }

        // Only notify exactly when they hit the target
        if (newCount == item.rewardThreshold)
        {
            string rewardKey = itemId + "_reward";
            databaseReference.Child("users").Child(userId).Child("claimedRewards").Child(rewardKey)
                .GetValueAsync().ContinueWith(task => 
                {
                    bool isClaimed = task.Result.Exists && bool.Parse(task.Result.Value.ToString());
                    
                    // Only notify if NOT already claimed
                    if (!isClaimed)
                    {
                        // WE DO NOT SAVE TO DATABASE HERE. User must click "Claim" button.
                        callback?.Invoke(item.rewardDescription);
                    }
                    else
                    {
                        callback?.Invoke(null);
                    }
                });
        }
        else
        {
            callback?.Invoke(null);
        }
    }

    // --- REWARDS SYSTEM ---

    // 1. Claim Reward (Generates Voucher + Saves to DB)
    public void ClaimReward(string itemId, string rewardDesc, Action<bool> callback)
    {
        if (currentUser == null) return;
        string userId = currentUser.UserId;
        string rewardKey = itemId + "_reward";

        // Generate Code
        string uniqueCode = "OCK-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

        // Prepare Data
        Dictionary<string, object> voucherData = new Dictionary<string, object>();
        voucherData["code"] = uniqueCode;
        voucherData["description"] = rewardDesc;
        voucherData["itemId"] = itemId;
        voucherData["date"] = DateTime.Now.ToString("dd/MM/yyyy");

        // 1. Mark as Claimed (So button disables)
        databaseReference.Child("users").Child(userId).Child("claimedRewards").Child(rewardKey).SetValueAsync(true);
        
        // 2. Save Voucher (So it appears in My Rewards)
        string pushKey = databaseReference.Child("users").Child(userId).Child("myVouchers").Push().Key;
        databaseReference.Child("users").Child(userId).Child("myVouchers").Child(pushKey).SetValueAsync(voucherData)
            .ContinueWith(task => {
                if (task.IsCompleted) callback?.Invoke(true);
                else callback?.Invoke(false);
            });
    }

    // 2. Get Vouchers List
    public void GetMyVouchers(Action<List<VoucherData>> callback)
    {
        if (currentUser == null) return;
        databaseReference.Child("users").Child(currentUser.UserId).Child("myVouchers").GetValueAsync().ContinueWith(task => {
            List<VoucherData> vouchers = new List<VoucherData>();
            if (task.IsCompleted && task.Result.Exists) {
                foreach (var c in task.Result.Children) {
                    try {
                        VoucherData v = new VoucherData();
                        v.code = c.Child("code").Value?.ToString() ?? "Error";
                        v.description = c.Child("description").Value?.ToString() ?? "Reward";
                        v.date = c.Child("date").Value?.ToString() ?? "";
                        v.itemId = c.Child("itemId").Value?.ToString() ?? "curry_puff";
                        vouchers.Add(v);
                    } catch { }
                }
            }
            callback?.Invoke(vouchers);
        });
    }

    // 3. Get Claim Status
    public void GetClaimedRewardsStatus(Action<Dictionary<string, bool>> callback)
    {
        if (currentUser == null) return;
        databaseReference.Child("users").Child(currentUser.UserId).Child("claimedRewards").GetValueAsync().ContinueWith(task =>
        {
            Dictionary<string, bool> claimedRewards = new Dictionary<string, bool>();
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var child in task.Result.Children)
                    claimedRewards[child.Key] = bool.Parse(child.Value.ToString());
            }
            callback?.Invoke(claimedRewards);
        });
    }
    
    // Redirect for compatibility
    public void GetClaimedRewards(Action<Dictionary<string, bool>> callback) => GetClaimedRewardsStatus(callback);

    // --- HELPERS ---
    public void GetUserCollection(Action<Dictionary<string, int>> callback)
    {
        if (currentUser == null) return;
        databaseReference.Child("users").Child(currentUser.UserId).Child("collectedItems").GetValueAsync().ContinueWith(task =>
        {
            Dictionary<string, int> collection = new Dictionary<string, int>();
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var child in task.Result.Children)
                    collection[child.Key] = int.Parse(child.Value.ToString());
            }
            callback?.Invoke(collection);
        });
    }

    public void GetUserStats(Action<int, int> callback)
    {
        if (currentUser == null) return;
        databaseReference.Child("users").Child(currentUser.UserId).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int totalScans = snapshot.Child("totalScans").Exists ? int.Parse(snapshot.Child("totalScans").Value.ToString()) : 0;
                int uniqueItems = 0;
                foreach (var item in snapshot.Child("collectedItems").Children)
                    if (int.Parse(item.Value.ToString()) > 0) uniqueItems++;
                callback?.Invoke(totalScans, uniqueItems);
            }
        });
    }
    
    private void GetItemCount(string userId, string itemId, Action<int> callback)
    {
        databaseReference.Child("users").Child(userId).Child("collectedItems").Child(itemId).GetValueAsync().ContinueWith(task =>
        {
            int count = (task.IsCompleted && task.Result.Value != null) ? int.Parse(task.Result.Value.ToString()) : 0;
            callback?.Invoke(count);
        });
    }
}

public class VoucherData {
    public string code;
    public string description;
    public string date;
    public string itemId;
}