using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject scanScreen;
    [SerializeField] private GameObject collectionScreen;
    [SerializeField] private GameObject rewardsScreen;
    [SerializeField] private GameObject myVouchersScreen; 

    [Header("AR Components")]
    [Tooltip("Drag the Main Camera from: XR Origin > Camera Offset > Main Camera")]
    [SerializeField] private Camera arCamera; 
    [Tooltip("Drag the GameObject that has ARImageTrackingManager.cs script")]
    [SerializeField] private ARImageTrackingManager arTrackingManager; 

    [Header("Login UI")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button signupButton;
    [SerializeField] private TextMeshProUGUI loginStatusText;
    [SerializeField] private GameObject signupPanel;

    [Header("Main Menu UI")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI totalScansText;
    [SerializeField] private TextMeshProUGUI uniqueItemsText;
    [SerializeField] private TextMeshProUGUI totalRewardsText;
    [SerializeField] private Button startScanButton;
    [SerializeField] private Button viewCollectionButton;
    [SerializeField] private Button viewRewardsButton; 
    [SerializeField] private Button logoutButton;

    [Header("Scan Screen UI")]
    [SerializeField] private TextMeshProUGUI scanInstructionText;
    [SerializeField] private Button backFromScanButton;
    [SerializeField] private GameObject scanningIndicator;

    [Header("Collection UI")]
    [SerializeField] private Transform collectionGrid;
    [SerializeField] private GameObject collectionItemPrefab;
    [SerializeField] private Button backFromCollectionButton;

    [Header("Rewards UI")]
    [SerializeField] private Transform rewardsGrid;
    [SerializeField] private GameObject rewardCardPrefab; 
    [SerializeField] private Button goToMyVouchersButton; 
    [SerializeField] private Button backFromRewardsButton;

    [Header("My Vouchers UI")]
    [SerializeField] private Transform vouchersGrid;
    [SerializeField] private GameObject voucherCardPrefab; 
    [SerializeField] private Button backFromVouchersButton;

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image notificationIcon;

    // --- INTERNAL ACTION QUEUE ---
    private readonly List<Action> _mainThreadActions = new List<Action>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        notificationPanel?.SetActive(false);
        SetupButtonListeners();

        // Keep XR Origin enabled, but disable just the camera
        if (arCamera != null)
            arCamera.enabled = false;

        if (arTrackingManager != null)
            arTrackingManager.enabled = false;

        ShowLoginScreen();
    }

    // --- CRITICAL UPDATE LOOP ---
    private void Update()
    {
        if (_mainThreadActions.Count > 0)
        {
            List<Action> actionsToRun = new List<Action>();

            lock (_mainThreadActions)
            {
                actionsToRun.AddRange(_mainThreadActions);
                _mainThreadActions.Clear();
            }

            foreach (var action in actionsToRun)
            {
                action?.Invoke();
            }
        }
    }
    
    private void RunOnMainThread(Action action)
    {
        lock (_mainThreadActions)
        {
            _mainThreadActions.Add(action);
        }
    }

    private void SetupButtonListeners()
    {
        loginButton?.onClick.AddListener(OnLoginButtonClicked);
        signupButton?.onClick.AddListener(OnSignupButtonClicked);
        logoutButton?.onClick.AddListener(OnLogoutButtonClicked);

        startScanButton?.onClick.AddListener(OnStartScanClicked);
        viewCollectionButton?.onClick.AddListener(OnViewCollectionClicked);
        viewRewardsButton?.onClick.AddListener(OnViewRewardsClicked); 

        backFromScanButton?.onClick.AddListener(OnBackToMainMenu);
        backFromCollectionButton?.onClick.AddListener(OnBackToMainMenu);
        
        // Rewards Page Buttons
        backFromRewardsButton?.onClick.AddListener(OnBackToMainMenu);
        goToMyVouchersButton?.onClick.AddListener(ShowMyVouchersScreen);
        
        // Vouchers Page Buttons
        backFromVouchersButton?.onClick.AddListener(ShowRewardsScreen); // Back goes to rewards, not main menu
    }

    // SCREEN NAVIGATION
    public void ShowLoginScreen()
    {
        HideAllScreens();
        loginScreen?.SetActive(true);
    }

    public void ShowMainMenu()
    {
        HideAllScreens();
        
        if(mainMenuScreen != null)
        {
            mainMenuScreen.SetActive(true);
            UpdateMainMenuStats();
        }
        else
        {
            Debug.LogError("Main Menu Screen reference is missing in Inspector!");
        }
    }

    public void ShowScanScreen()
    {
        HideAllScreens();
        scanScreen?.SetActive(true);
        EnableARCamera();
        
        if (scanInstructionText != null)
            scanInstructionText.text = "Point your camera at the Old Chang Kee logo";
    }

    public void ShowCollectionScreen()
    {
        HideAllScreens();
        collectionScreen?.SetActive(true);
        LoadCollection();
    }

    public void ShowRewardsScreen()
    {
        HideAllScreens();
        rewardsScreen?.SetActive(true);
        LoadRewardProgress();
    }

    public void ShowMyVouchersScreen()
    {
        HideAllScreens();
        myVouchersScreen?.SetActive(true);
        LoadMyVouchers();
    }

    private void HideAllScreens()
    {
        loginScreen?.SetActive(false);
        mainMenuScreen?.SetActive(false);
        scanScreen?.SetActive(false);
        collectionScreen?.SetActive(false);
        rewardsScreen?.SetActive(false);
        
        // FIX: Ensure vouchers screen is also hidden
        myVouchersScreen?.SetActive(false); 
    }

    // LOGIN/SIGNUP
    private void OnLoginButtonClicked()
    {
        string email = emailInput.text?.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginStatus("Please fill in all fields", false);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowLoginStatus("Please enter a valid email address", false);
            return;
        }

        Debug.Log($"Attempting sign-in for: {email}");

        FirebaseManager.Instance.SignInWithEmail(email, password, (success, message) =>
        {
            RunOnMainThread(() => 
            {
                ShowLoginStatus(message, success);
                if (success)
                {
                    Debug.Log("Login Successful. Switching Screens.");
                    ShowMainMenu();
                }
            });
        });
    }

    private void OnSignupButtonClicked()
    {
        signupPanel?.SetActive(true);

        string email = emailInput.text?.Trim();
        string password = passwordInput.text;
        string username = usernameInput.text?.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowLoginStatus("Please fill in all fields", false);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowLoginStatus("Please enter a valid email address", false);
            return;
        }

        FirebaseManager.Instance.SignUpWithEmail(email, password, username, (success, message) =>
        {
            RunOnMainThread(() => 
            {
                ShowLoginStatus(message, success);
                if (success)
                {
                    Debug.Log("Signup Successful. Switching Screens.");
                    ShowMainMenu();
                }
            });
        });
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private void OnLogoutButtonClicked()
    {
        FirebaseManager.Instance.SignOut();
        ShowLoginScreen();
    }

    private void ShowLoginStatus(string message, bool isSuccess)
    {
        if (loginStatusText != null)
        {
            loginStatusText.text = message;
            loginStatusText.color = isSuccess ? Color.green : Color.red;
        }
    }

    // MAIN MENU
    private void UpdateMainMenuStats()
    {
        FirebaseManager.Instance.GetUserStats((totalScans, uniqueItems) =>
        {
            RunOnMainThread(() => {
                if (totalScansText != null) totalScansText.text = $"Total Scans: {totalScans}";
                if (uniqueItemsText != null) uniqueItemsText.text = $"Unique Items: {uniqueItems}/6";
            });
        });

        // Get total rewards claimed
        FirebaseManager.Instance.GetClaimedRewards(claimedRewards =>
        {
            RunOnMainThread(() => {
                if (totalRewardsText != null)
                {
                    int totalRewards = 0;
                    foreach (var reward in claimedRewards.Values)
                    {
                        if (reward) totalRewards++;
                    }
                    totalRewardsText.text = $"Rewards Earned: {totalRewards}";
                }
            });
        });
    }

    // COLLECTION SCREEN
    private void LoadCollection()
    {
        foreach (Transform child in collectionGrid)
        {
            Destroy(child.gameObject);
        }

        FirebaseManager.Instance.GetUserCollection(collection =>
        {
            RunOnMainThread(() => {
                // Safety check
                if (collectionScreen == null || !collectionScreen.activeInHierarchy) return;

                foreach (var item in collection)
                {
                    GameObject itemObj = Instantiate(collectionItemPrefab, collectionGrid);
                    itemObj.transform.localScale = Vector3.one;
                    Vector3 pos = itemObj.transform.localPosition;
                    itemObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);

                    CollectionItemUI itemUI = itemObj.GetComponent<CollectionItemUI>();

                    if (itemUI != null)
                    {
                        ItemInfo info = GetItemInfo(item.Key);
                        itemUI.SetupItem(info.name, info.rarity, item.Value, info.sprite);
                    }
                }
            });
        });
    }

    // REWARDS SCREEN
    private void LoadRewardProgress()
    {
        foreach (Transform child in rewardsGrid) Destroy(child.gameObject);

        // 1. Get Collection Counts
        FirebaseManager.Instance.GetUserCollection(collection => 
        {
            // 2. Get Claim Status
            FirebaseManager.Instance.GetClaimedRewardsStatus(claimedStatus => 
            {
                RunOnMainThread(() => 
                {
                    if (rewardsScreen == null || !rewardsScreen.activeInHierarchy) return;

                    List<ItemModel> allItems = ItemDatabase.Instance.GetAllItems();

                    foreach (ItemModel item in allItems)
                    {
                        GameObject cardObj = Instantiate(rewardCardPrefab, rewardsGrid);
                        // Fix scale
                        cardObj.transform.localScale = Vector3.one;
                        Vector3 pos = cardObj.transform.localPosition;
                        cardObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);

                        RewardCardUI cardUI = cardObj.GetComponent<RewardCardUI>();

                        int count = collection.ContainsKey(item.itemId) ? collection[item.itemId] : 0;
                        string rewardKey = item.itemId + "_reward";
                        bool isClaimed = claimedStatus.ContainsKey(rewardKey) && claimedStatus[rewardKey];

                        if (cardUI != null)
                        {
                            cardUI.Setup(item, count, isClaimed, OnClaimButtonClicked);
                        }
                    }
                });
            });
        });
    }

    // Handle Claim Click
    private void OnClaimButtonClicked(string itemId)
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if(item == null) return;

        Debug.Log("Claiming: " + item.itemName);

        FirebaseManager.Instance.ClaimReward(itemId, item.rewardDescription, (success) => 
        {
            RunOnMainThread(() => {
                if (success)
                {
                    ShowRewardNotification(item.itemName, item.rewardDescription);
                    LoadRewardProgress(); 
                }
            });
        });
    }

    // MY VOUCHERS SCREEN
    private void LoadMyVouchers()
    {
        foreach (Transform child in vouchersGrid) Destroy(child.gameObject);

        FirebaseManager.Instance.GetMyVouchers(vouchers => 
        {
            RunOnMainThread(() => 
            {
                // Safety Check: Don't spawn if user left the screen
                if (myVouchersScreen == null || !myVouchersScreen.activeInHierarchy) return;

                foreach (var v in vouchers)
                {
                    GameObject vObj = Instantiate(voucherCardPrefab, vouchersGrid);
                    
                    // --- THE FIX: FORCE VISIBILITY ---
                    vObj.transform.localScale = Vector3.one; 
                    Vector3 pos = vObj.transform.localPosition;
                    vObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);
                    // ---------------------------------

                    VoucherUI vUI = vObj.GetComponent<VoucherUI>();
                    
                    // Find icon from database
                    ItemModel originalItem = ItemDatabase.Instance.GetItem(v.itemId);
                    Sprite icon = originalItem != null ? originalItem.itemSprite : null;

                    if (vUI != null)
                    {
                        vUI.Setup(v.description, v.code, v.date, icon);
                    }
                }
            });
        });
    }

    // NOTIFICATIONS
    public void ShowCollectionNotification(ScanResult result)
    {
        if (result == null) return;
        ItemModel item = ItemDatabase.Instance.GetItem(result.itemId);
        if (item == null) return;

        notificationText.text = result.GetCongratsMessage();

        if (notificationIcon != null && item.itemSprite != null)
            notificationIcon.sprite = item.itemSprite;

        StartCoroutine(ShowNotificationRoutine());
    }

    public void ShowRewardNotification(string itemName, string rewardDescription)
    {
        notificationText.text = $"🎉 Reward Unlocked!\n🎁 {rewardDescription}\nfor collecting {itemName}!";
        StartCoroutine(ShowNotificationRoutine());
    }

    public void ShowErrorNotification(string message)
    {
        notificationText.text = $"❌ {message}";
        StartCoroutine(ShowNotificationRoutine());
    }

    private IEnumerator ShowNotificationRoutine()
    {
        notificationPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        notificationPanel.SetActive(false);
    }

    private void OnBackToMainMenu()
    {
        DisableARCamera();
        ShowMainMenu();
    }

    // AR CAMERA CONTROL
    private void EnableARCamera()
    {
        if (arCamera != null) arCamera.enabled = true;
        if (arTrackingManager != null) arTrackingManager.enabled = true;
        Debug.Log("AR Camera enabled");
    }

    private void DisableARCamera()
    {
        if (arCamera != null) arCamera.enabled = false;
        if (arTrackingManager != null) arTrackingManager.enabled = false;
        Debug.Log("AR Camera disabled");
    }

    // HELPERS
    private void OnStartScanClicked() { ShowScanScreen(); }
    private void OnViewCollectionClicked() { ShowCollectionScreen(); }
    private void OnViewRewardsClicked() { ShowRewardsScreen(); }

    private ItemInfo GetItemInfo(string itemId)
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if (item != null)
        {
            return new ItemInfo(item.itemName, item.GetRarityDisplayName(), item.itemSprite);
        }
        return new ItemInfo("Unknown", "Common", null);
    }

    private struct ItemInfo
    {
        public string name;
        public string rarity;
        public Sprite sprite;

        public ItemInfo(string name, string rarity, Sprite sprite)
        {
            this.name = name;
            this.rarity = rarity;
            this.sprite = sprite;
        }
    }
}