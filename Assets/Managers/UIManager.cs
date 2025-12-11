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
    [SerializeField] private Camera arCamera; 
    [SerializeField] private ARImageTrackingManager arTrackingManager; 

    [Header("Login UI")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button openSignupButton; // Button on Login Page to go to Signup
    [SerializeField] private TextMeshProUGUI loginStatusText;

    [Header("Signup UI")]
    [SerializeField] private GameObject signupPanel;
    [SerializeField] private TMP_InputField signupEmailInput; // Separate input to avoid confusion
    [SerializeField] private TMP_InputField signupPasswordInput;
    [SerializeField] private TMP_InputField signupUsernameInput;
    [SerializeField] private Button submitSignupButton; // "Create Account"
    [SerializeField] private Button backToLoginButton; // "Cancel / Back"

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

    private readonly List<Action> _mainThreadActions = new List<Action>();

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    private void Start()
    {
        notificationPanel?.SetActive(false);
        SetupButtonListeners();

        if (arCamera != null) arCamera.enabled = false;
        if (arTrackingManager != null) arTrackingManager.enabled = false;

        ShowLoginScreen();
    }

    private void Update()
    {
        if (_mainThreadActions.Count > 0)
        {
            List<Action> actionsToRun = new List<Action>();
            lock (_mainThreadActions) { actionsToRun.AddRange(_mainThreadActions); _mainThreadActions.Clear(); }
            foreach (var action in actionsToRun) action?.Invoke();
        }
    }
    
    private void RunOnMainThread(Action action) { lock (_mainThreadActions) { _mainThreadActions.Add(action); } }

    private void SetupButtonListeners()
    {
        // Login Page
        loginButton?.onClick.AddListener(OnLoginButtonClicked);
        openSignupButton?.onClick.AddListener(OnOpenSignupClicked);

        // Signup Panel
        submitSignupButton?.onClick.AddListener(OnSubmitSignupClicked);
        backToLoginButton?.onClick.AddListener(OnCancelSignupClicked);

        // Main Menu
        logoutButton?.onClick.AddListener(OnLogoutButtonClicked);
        startScanButton?.onClick.AddListener(OnStartScanClicked);
        viewCollectionButton?.onClick.AddListener(OnViewCollectionClicked);
        viewRewardsButton?.onClick.AddListener(OnViewRewardsClicked); 

        // Back Buttons
        backFromScanButton?.onClick.AddListener(OnBackToMainMenu);
        backFromCollectionButton?.onClick.AddListener(OnBackToMainMenu);
        backFromRewardsButton?.onClick.AddListener(OnBackToMainMenu);
        
        // Navigation
        goToMyVouchersButton?.onClick.AddListener(ShowMyVouchersScreen);
        backFromVouchersButton?.onClick.AddListener(ShowRewardsScreen);
    }

    // --- SCREEN NAVIGATION ---
    public void ShowLoginScreen()
    {
        HideAllScreens();
        loginScreen?.SetActive(true);
        signupPanel?.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllScreens();
        if(mainMenuScreen != null) {
            mainMenuScreen.SetActive(true);
            UpdateMainMenuStats();
        }
    }

    public void ShowScanScreen() { HideAllScreens(); scanScreen?.SetActive(true); EnableARCamera(); }
    public void ShowCollectionScreen() { HideAllScreens(); collectionScreen?.SetActive(true); LoadCollection(); }
    public void ShowRewardsScreen() { HideAllScreens(); rewardsScreen?.SetActive(true); LoadRewardProgress(); }
    public void ShowMyVouchersScreen() { HideAllScreens(); myVouchersScreen?.SetActive(true); LoadMyVouchers(); }

    private void HideAllScreens()
    {
        loginScreen?.SetActive(false);
        mainMenuScreen?.SetActive(false);
        scanScreen?.SetActive(false);
        collectionScreen?.SetActive(false);
        rewardsScreen?.SetActive(false);
        myVouchersScreen?.SetActive(false);
        signupPanel?.SetActive(false);
    }

    // --- LOGIN / SIGNUP LOGIC ---

    // 1. User clicks "Sign Up" on Login Page -> Opens Panel
    private void OnOpenSignupClicked()
    {
        signupPanel.SetActive(true);
        // Clear previous inputs
        if(signupEmailInput) signupEmailInput.text = "";
        if(signupPasswordInput) signupPasswordInput.text = "";
        if(signupUsernameInput) signupUsernameInput.text = "";
    }

    // 2. User clicks "Back" on Signup Panel -> Closes Panel
    private void OnCancelSignupClicked()
    {
        signupPanel.SetActive(false);
    }

    // 3. User clicks "Create Account" -> Creates & returns to Login
    private void OnSubmitSignupClicked()
    {
        string email = signupEmailInput.text?.Trim();
        string password = signupPasswordInput.text;
        string username = signupUsernameInput.text?.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowLoginStatus("Please fill all fields", false);
            return;
        }

        FirebaseManager.Instance.SignUpWithEmail(email, password, username, (success, message) =>
        {
            RunOnMainThread(() => 
            {
                if (success)
                {
                    // SUCCESS: Hide panel, force user to log in
                    signupPanel.SetActive(false);
                    ShowLoginStatus(message, true); // "Account Created! Please Log In"
                    
                    // Clear inputs so they have to type again
                    emailInput.text = "";
                    passwordInput.text = "";
                }
                else
                {
                    ShowLoginStatus(message, false);
                }
            });
        });
    }

    // 4. Standard Login
    private void OnLoginButtonClicked()
    {
        string email = emailInput.text?.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
            ShowLoginStatus("Fill all fields", false);
            return;
        }

        FirebaseManager.Instance.SignInWithEmail(email, password, (success, message) =>
        {
            RunOnMainThread(() => {
                ShowLoginStatus(message, success);
                if (success) ShowMainMenu();
            });
        });
    }

    private void OnLogoutButtonClicked()
    {
        FirebaseManager.Instance.SignOut();
        ShowLoginScreen();
    }

    private void ShowLoginStatus(string message, bool isSuccess)
    {
        if (loginStatusText != null) {
            loginStatusText.text = message;
            loginStatusText.color = isSuccess ? Color.green : Color.red;
        }
    }

    // --- MAIN MENU ---
    private void UpdateMainMenuStats()
    {
        FirebaseManager.Instance.GetUserStats((totalScans, uniqueItems) =>
        {
            RunOnMainThread(() => {
                if (totalScansText != null) totalScansText.text = $"Total Scans: {totalScans}";
                if (uniqueItemsText != null) uniqueItemsText.text = $"Unique Items: {uniqueItems}/6";
            });
        });

        FirebaseManager.Instance.GetClaimedRewards(claimedRewards =>
        {
            RunOnMainThread(() => {
                if (totalRewardsText != null) {
                    int totalRewards = 0;
                    foreach (var reward in claimedRewards.Values) if (reward) totalRewards++;
                    totalRewardsText.text = $"Rewards Earned: {totalRewards}";
                }
            });
        });
    }

    // --- COLLECTION ---
    private void LoadCollection()
    {
        foreach (Transform child in collectionGrid) Destroy(child.gameObject);

        FirebaseManager.Instance.GetUserCollection(collection =>
        {
            RunOnMainThread(() => {
                if (collectionScreen == null || !collectionScreen.activeInHierarchy) return;

                foreach (var item in collection)
                {
                    GameObject itemObj = Instantiate(collectionItemPrefab, collectionGrid);
                    itemObj.transform.localScale = Vector3.one;
                    Vector3 pos = itemObj.transform.localPosition;
                    itemObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);

                    CollectionItemUI itemUI = itemObj.GetComponent<CollectionItemUI>();
                    if (itemUI != null) {
                        ItemInfo info = GetItemInfo(item.Key);
                        itemUI.SetupItem(info.name, info.rarity, item.Value, info.sprite);
                    }
                }
            });
        });
    }

    // --- REWARDS ---
    private void LoadRewardProgress()
    {
        foreach (Transform child in rewardsGrid) Destroy(child.gameObject);

        FirebaseManager.Instance.GetUserCollection(collection => 
        {
            FirebaseManager.Instance.GetClaimedRewardsStatus(claimedStatus => 
            {
                RunOnMainThread(() => 
                {
                    if (rewardsScreen == null || !rewardsScreen.activeInHierarchy) return;

                    List<ItemModel> allItems = ItemDatabase.Instance.GetAllItems();

                    foreach (ItemModel item in allItems)
                    {
                        GameObject cardObj = Instantiate(rewardCardPrefab, rewardsGrid);
                        cardObj.transform.localScale = Vector3.one;
                        Vector3 pos = cardObj.transform.localPosition;
                        cardObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);

                        RewardCardUI cardUI = cardObj.GetComponent<RewardCardUI>();
                        int count = collection.ContainsKey(item.itemId) ? collection[item.itemId] : 0;
                        string rewardKey = item.itemId + "_reward";
                        bool isClaimed = claimedStatus.ContainsKey(rewardKey) && claimedStatus[rewardKey];

                        if (cardUI != null) cardUI.Setup(item, count, isClaimed, OnClaimButtonClicked);
                    }
                });
            });
        });
    }

    private void OnClaimButtonClicked(string itemId)
    {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        if(item == null) return;

        FirebaseManager.Instance.ClaimReward(itemId, item.rewardDescription, (success) => 
        {
            RunOnMainThread(() => {
                if (success) {
                    ShowRewardNotification(item.itemName, item.rewardDescription);
                    LoadRewardProgress(); 
                }
            });
        });
    }

    // --- VOUCHERS (FIXED INVISIBILITY) ---
    private void LoadMyVouchers()
    {
        foreach (Transform child in vouchersGrid) Destroy(child.gameObject);

        FirebaseManager.Instance.GetMyVouchers(vouchers => 
        {
            RunOnMainThread(() => 
            {
                if (myVouchersScreen == null || !myVouchersScreen.activeInHierarchy) return;

                foreach (var v in vouchers)
                {
                    GameObject vObj = Instantiate(voucherCardPrefab, vouchersGrid);
                    
                    // --- FORCE VISIBILITY ---
                    vObj.transform.localScale = Vector3.one; 
                    Vector3 pos = vObj.transform.localPosition;
                    vObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);
                    // ------------------------

                    VoucherUI vUI = vObj.GetComponent<VoucherUI>();
                    ItemModel originalItem = ItemDatabase.Instance.GetItem(v.itemId);
                    Sprite icon = originalItem != null ? originalItem.itemSprite : null;

                    if (vUI != null) vUI.Setup(v.description, v.code, v.date, icon);
                }
            });
        });
    }

    // --- NOTIFICATIONS ---
    public void ShowCollectionNotification(ScanResult result) {
        if (result == null) return;
        ItemModel item = ItemDatabase.Instance.GetItem(result.itemId);
        if (item == null) return;
        notificationText.text = result.GetCongratsMessage();
        if (notificationIcon != null) notificationIcon.sprite = item.itemSprite;
        StartCoroutine(ShowNotificationRoutine());
    }

    public void ShowRewardNotification(string itemName, string rewardDescription) {
        notificationText.text = $"🎉 Reward Unlocked!\n🎁 {rewardDescription}\nfor collecting {itemName}!";
        StartCoroutine(ShowNotificationRoutine());
    }

    private IEnumerator ShowNotificationRoutine() {
        notificationPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        notificationPanel.SetActive(false);
    }

    private void OnBackToMainMenu() { DisableARCamera(); ShowMainMenu(); }
    private void EnableARCamera() { if (arCamera != null) arCamera.enabled = true; if (arTrackingManager != null) arTrackingManager.enabled = true; }
    private void DisableARCamera() { if (arCamera != null) arCamera.enabled = false; if (arTrackingManager != null) arTrackingManager.enabled = false; }
    private void OnStartScanClicked() { ShowScanScreen(); }
    private void OnViewCollectionClicked() { ShowCollectionScreen(); }
    private void OnViewRewardsClicked() { ShowRewardsScreen(); }

    private ItemInfo GetItemInfo(string itemId) {
        ItemModel item = ItemDatabase.Instance.GetItem(itemId);
        return item != null ? new ItemInfo(item.itemName, item.GetRarityDisplayName(), item.itemSprite) : new ItemInfo("Unknown", "Common", null);
    }

    private struct ItemInfo {
        public string name; public string rarity; public Sprite sprite;
        public ItemInfo(string name, string rarity, Sprite sprite) { this.name = name; this.rarity = rarity; this.sprite = sprite; }
    }
}