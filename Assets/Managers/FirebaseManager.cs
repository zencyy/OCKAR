using System;
using System.Collections;
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
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
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
                Debug.Log("Firebase initialized successfully");
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {task.Result}");
            }
        });
    }
    
    // AUTHENTICATION
    public void SignUpWithEmail(string email, string password, string username, Action<bool, string> callback)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                callback?.Invoke(false, "Sign up failed: " + task.Exception?.Message);
                return;
            }
            
            currentUser = task.Result.User;
            CreateUserProfile(currentUser.UserId, username, email);
            callback?.Invoke(true, "Sign up successful!");
        });
    }
    
    public void SignInWithEmail(string email, string password, Action<bool, string> callback)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                callback?.Invoke(false, "Sign in failed: " + task.Exception?.Message);
                return;
            }
            
            currentUser = task.Result.User;
            callback?.Invoke(true, "Sign in successful!");
        });
    }
    
    public void SignOut()
    {
        auth.SignOut();
        currentUser = null;
    }
    
    // USER PROFILE
    private void CreateUserProfile(string userId, string username, string email)
    {
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "totalScans", 0 },
            { "lastScanTime", "" }
        };
        
        databaseReference.Child("users").Child(userId).SetValueAsync(userData);
        
        // Initialize empty collection
        InitializeUserCollection(userId);
    }
    
    private void InitializeUserCollection(string userId)
    {
        Dictionary<string, object> initialCollection = new Dictionary<string, object>
        {
            { "curry_puff", 0 },
            { "fish_ball", 0 },
            { "sotong_ball", 0 },
            { "chicken_wing", 0 },
            { "spring_roll", 0 },
            { "ngor_hiang", 0 }
        };
        
        databaseReference.Child("users").Child(userId).Child("collectedItems").SetValueAsync(initialCollection);
        
        // Initialize claimed rewards (NEW)
        Dictionary<string, object> initialRewards = new Dictionary<string, object>
        {
            { "curry_puff_reward", false },
            { "fish_ball_reward", false },
            { "spring_roll_reward", false },
            { "sotong_ball_reward", false },
            { "chicken_wing_reward", false },
            { "ngor_hiang_reward", false }
        };
        
        databaseReference.Child("users").Child(userId).Child("claimedRewards").SetValueAsync(initialRewards);
    }
    
    // SCANNING & ITEM COLLECTION
    public void RecordScan(Action<ScanResult> onItemReceived)
    {
        if (currentUser == null) return;
        
        string userId = currentUser.UserId;
        
        // Increment total scans
        databaseReference.Child("users").Child(userId).Child("totalScans").RunTransaction(mutableData =>
        {
            int currentScans = mutableData.Value != null ? int.Parse(mutableData.Value.ToString()) : 0;
            mutableData.Value = currentScans + 1;
            return TransactionResult.Success(mutableData);
        });
        
        // Update last scan time
        databaseReference.Child("users").Child(userId).Child("lastScanTime")
            .SetValueAsync(DateTime.UtcNow.ToString("o"));
        
        // Roll for item using ItemDatabase
        string itemId = ItemDatabase.Instance.RollRandomItem();
        
        // Create scan result
        ScanResult result = new ScanResult(itemId);
        
        // Check if first time
        GetItemCount(userId, itemId, (currentCount) =>
        {
            result.isFirstTime = (currentCount == 0);
            result.newCount = currentCount + 1;
            
            // Add item to collection
            AddItemToCollection(userId, itemId);
            
            // Check if reward unlocked (NEW)
            CheckRewardUnlocked(userId, itemId, result.newCount, (unlockedReward) =>
            {
                if (unlockedReward != null)
                {
                    result.completedNewCombo = true; // Reusing this field for "reward unlocked"
                    result.completedComboId = itemId + "_reward";
                    result.rewardEarned = unlockedReward;
                }
                
                onItemReceived?.Invoke(result);
            });
        });
    }
    
    private void CheckRewardUnlocked(string userId, string itemId, int newCount, Action<string> callback)
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if (item == null)
        {
            callback?.Invoke(null);
            return;
        }
        
        // Check if just reached threshold
        if (newCount == item.rewardThreshold)
        {
            // Check if not already claimed
            string rewardKey = itemId + "_reward";
            databaseReference.Child("users").Child(userId).Child("claimedRewards").Child(rewardKey)
                .GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsCompleted)
                    {
                        bool alreadyClaimed = task.Result.Value != null && bool.Parse(task.Result.Value.ToString());
                        
                        if (!alreadyClaimed)
                        {
                            // Mark as claimed
                            databaseReference.Child("users").Child(userId).Child("claimedRewards").Child(rewardKey)
                                .SetValueAsync(true);
                            
                            callback?.Invoke(item.rewardDescription);
                        }
                        else
                        {
                            callback?.Invoke(null);
                        }
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
    
    private void GetItemCount(string userId, string itemId, Action<int> callback)
    {
        databaseReference.Child("users").Child(userId).Child("collectedItems").Child(itemId)
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    int count = task.Result.Value != null ? int.Parse(task.Result.Value.ToString()) : 0;
                    callback?.Invoke(count);
                }
                else
                {
                    callback?.Invoke(0);
                }
            });
    }
    
    private void AddItemToCollection(string userId, string itemId)
    {
        databaseReference.Child("users").Child(userId).Child("collectedItems").Child(itemId)
            .RunTransaction(mutableData =>
            {
                int currentCount = mutableData.Value != null ? int.Parse(mutableData.Value.ToString()) : 0;
                mutableData.Value = currentCount + 1;
                return TransactionResult.Success(mutableData);
            });
    }
    
    // GET USER DATA
    public void GetUserCollection(Action<Dictionary<string, int>> callback)
    {
        if (currentUser == null) return;
        
        databaseReference.Child("users").Child(currentUser.UserId).Child("collectedItems")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Dictionary<string, int> collection = new Dictionary<string, int>();
                    
                    foreach (var child in snapshot.Children)
                    {
                        collection[child.Key] = int.Parse(child.Value.ToString());
                    }
                    
                    callback?.Invoke(collection);
                }
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
                int totalScans = snapshot.Child("totalScans").Exists ? 
                    int.Parse(snapshot.Child("totalScans").Value.ToString()) : 0;
                
                int uniqueItems = 0;
                var collectedItems = snapshot.Child("collectedItems");
                foreach (var item in collectedItems.Children)
                {
                    if (int.Parse(item.Value.ToString()) > 0)
                        uniqueItems++;
                }
                
                callback?.Invoke(totalScans, uniqueItems);
            }
        });
    }
    
    // GET CLAIMED REWARDS (Replaces GetClaimedCombos)
    public void GetClaimedRewards(Action<Dictionary<string, bool>> callback)
    {
        if (currentUser == null) return;
        
        databaseReference.Child("users").Child(currentUser.UserId).Child("claimedRewards")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Dictionary<string, bool> claimedRewards = new Dictionary<string, bool>();
                    
                    if (snapshot.Exists)
                    {
                        foreach (var child in snapshot.Children)
                        {
                            claimedRewards[child.Key] = bool.Parse(child.Value.ToString());
                        }
                    }
                    
                    callback?.Invoke(claimedRewards);
                }
                else
                {
                    callback?.Invoke(new Dictionary<string, bool>());
                }
            });
    }
}