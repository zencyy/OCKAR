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
    [SerializeField] private GameObject customScreen; 

    [Header("AR Components")]
    [SerializeField] private Camera arCamera; 
    [SerializeField] private ARImageTrackingManager arTrackingManager; 

    [Header("Login UI")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button openSignupButton; 
    [SerializeField] private TextMeshProUGUI loginStatusText;

    [Header("Signup UI")]
    [SerializeField] private GameObject signupPanel;
    [SerializeField] private TMP_InputField signupEmailInput; 
    [SerializeField] private TMP_InputField signupPasswordInput;
    [SerializeField] private TMP_InputField signupUsernameInput;
    [SerializeField] private Button submitSignupButton; 
    [SerializeField] private Button backToLoginButton;
    [SerializeField] private TextMeshProUGUI signupStatusText;

    [Header("Main Menu UI")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI totalScansText;
    [SerializeField] private TextMeshProUGUI uniqueItemsText;
    [SerializeField] private TextMeshProUGUI totalRewardsText;
    [SerializeField] private Button startScanButton;
    [SerializeField] private Button viewCollectionButton;
    [SerializeField] private Button viewRewardsButton; 
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button mainMenuCustomButton; 

    [Header("Custom Screen UI")]
    [SerializeField] private Button backFromCustomButton;

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip claimSuccessSound;

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

    // --- CHANGED: NEW NOTIFICATION SYSTEM ---
    [Header("Notification System")]
    [SerializeField] private GameObject notificationPrefab;    // DRAG YOUR NEW PREFAB HERE
    [SerializeField] private Transform notificationContainer;  // DRAG YOUR CONTAINER (LAYOUT GROUP) HERE

    // Text Colors (for Login/Signup status)
    private Color colorSuccess = new Color(0f, 0.8f, 0.4f); 
    private Color colorError = new Color(1f, 0.3f, 0.3f);   
    private Color colorLoading = new Color(1f, 0.8f, 0f);   

    // Background Colors (for Notification Cards)
    private Color colorSuccessBg = new Color(0f, 0.6f, 0.3f, 0.95f); // Darker Green for card bg
    private Color colorErrorBg = new Color(0.8f, 0.2f, 0.2f, 0.95f);   // Darker Red for card bg

    private readonly List<Action> _mainThreadActions = new List<Action>();

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    private void Start()
    {
        // notificationPanel logic removed here
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

        mainMenuCustomButton?.onClick.AddListener(ShowCustomScreen);

        // Back Buttons
        backFromScanButton?.onClick.AddListener(OnBackToMainMenu);
        backFromCollectionButton?.onClick.AddListener(OnBackToMainMenu);
        backFromRewardsButton?.onClick.AddListener(OnBackToMainMenu);

        backFromCustomButton?.onClick.AddListener(OnBackToMainMenu);
        
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
        ResetInputFields();
    }
    public void ShowCustomScreen() 
    { 
        HideAllScreens(); 
        customScreen?.SetActive(true); 
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
        customScreen?.SetActive(false);
    }

    private void ResetInputFields()
    {
        if(emailInput) emailInput.text = "";
        if(passwordInput) passwordInput.text = "";
        if(signupEmailInput) signupEmailInput.text = "";
        if(signupPasswordInput) signupPasswordInput.text = "";
        if(signupUsernameInput) signupUsernameInput.text = "";
        if(loginStatusText) loginStatusText.text = "";
        if(signupStatusText) signupStatusText.text = "";
    }

    // --- LOGIN / SIGNUP LOGIC ---

    private void OnOpenSignupClicked()
    {
        signupPanel.SetActive(true);
        ResetInputFields();
        StartCoroutine(ShakeAnimation(signupPanel.transform, 0.1f, 5f));
    }

    private void OnCancelSignupClicked()
    {
        signupPanel.SetActive(false);
        ResetInputFields();
    }

    private void OnSubmitSignupClicked()
    {
        string email = signupEmailInput.text?.Trim();
        string password = signupPasswordInput.text;
        string username = signupUsernameInput.text?.Trim();

        // VALIDATION (Text replaced with safe characters)
        if (string.IsNullOrEmpty(username)) { ShowStatus(signupStatusText, "Username is required", false); return; }
        if (string.IsNullOrEmpty(email)) { ShowStatus(signupStatusText, "Email is required", false); return; }
        if (!IsValidEmail(email)) { ShowStatus(signupStatusText, "Invalid Email Format", false); return; }
        if (string.IsNullOrEmpty(password)) { ShowStatus(signupStatusText, "Password is required", false); return; }
        if (password.Length < 6) { ShowStatus(signupStatusText, "Password too short (Min 6)", false); return; }

        ToggleInteraction(false); 
        ShowStatus(signupStatusText, "Creating Account...", true, true);

        FirebaseManager.Instance.SignUpWithEmail(email, password, username, (success, message) =>
        {
            RunOnMainThread(() => 
            {
                ToggleInteraction(true); 
                if (success)
                {
                    signupPanel.SetActive(false);
                    ShowStatus(loginStatusText, "Account Created! Please Log In.", true);
                    
                    emailInput.text = "";
                    passwordInput.text = "";
                }
                else
                {
                    string cleanError = CleanFirebaseError(message);
                    ShowStatus(signupStatusText, cleanError, false);
                }
            });
        });
    }

    private void OnLoginButtonClicked()
    {
        string email = emailInput.text?.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email)) { ShowStatus(loginStatusText, "Enter Email", false); return; }
        if (string.IsNullOrEmpty(password)) { ShowStatus(loginStatusText, "Enter Password", false); return; }

        ToggleInteraction(false);
        ShowStatus(loginStatusText, "Signing In...", true, true);

        FirebaseManager.Instance.SignInWithEmail(email, password, (success, message) =>
        {
            RunOnMainThread(() => {
                ToggleInteraction(true);
                if (success) {
                    ShowStatus(loginStatusText, "Login Successful!", true);
                    ShowMainMenu();
                } else {
                    string cleanError = CleanFirebaseError(message);
                    ShowStatus(loginStatusText, cleanError, false);
                }
            });
        });
    }

    // --- FEEDBACK SYSTEM (For Login/Signup Text) ---

    private void ShowStatus(TextMeshProUGUI targetText, string message, bool isSuccess, bool isLoading = false)
    {
        if (targetText == null) return;

        targetText.text = message;
        targetText.gameObject.SetActive(true);
        targetText.alpha = 1f;

        if (isLoading)
        {
            targetText.color = colorLoading;
        }
        else if (isSuccess)
        {
            targetText.color = colorSuccess;
        }
        else
        {
            // Error: Make it Red and Shake
            targetText.color = colorError;
            StartCoroutine(ShakeAnimation(targetText.transform, 0.3f, 10f));
        }

        StopCoroutine("AutoFadeOut");
        if (!isLoading) StartCoroutine(AutoFadeOut(targetText));
    }

    private IEnumerator ShakeAnimation(Transform target, float duration, float magnitude)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            target.localPosition = new Vector3(originalPos.x + x, originalPos.y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
    }

    private IEnumerator AutoFadeOut(TextMeshProUGUI targetText)
    {
        yield return new WaitForSeconds(2.5f);
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if(targetText != null) targetText.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        
        if(targetText != null) targetText.text = "";
    }

    private void ToggleInteraction(bool interactable)
    {
        if(loginButton) loginButton.interactable = interactable;
        if(submitSignupButton) submitSignupButton.interactable = interactable;
        if(openSignupButton) openSignupButton.interactable = interactable;
        if(backToLoginButton) backToLoginButton.interactable = interactable;
    }

    private string CleanFirebaseError(string originalMessage)
    {
        if (string.IsNullOrEmpty(originalMessage)) return "Unknown Error";
        string msg = originalMessage.ToLower();

        if (msg.Contains("invalid_email") || msg.Contains("badly formatted")) return "Invalid Email Format";
        if (msg.Contains("wrong_password") || msg.Contains("invalid_login")) return "Wrong Credentials";
        if (msg.Contains("user_not_found")) return "Account Not Found";
        if (msg.Contains("email_exists") || msg.Contains("already_in_use")) return "Email Taken";
        if (msg.Contains("weak_password")) return "Password Too Weak";
        if (msg.Contains("network")) return "Network Error";

        return "Connection Error";
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // --- OTHER UI LOGIC (Collection, Rewards, Etc) ---

    private void OnLogoutButtonClicked() { FirebaseManager.Instance.SignOut(); ShowLoginScreen(); }

    private void UpdateMainMenuStats()
    {
        // 1. Get & Display Username
        FirebaseManager.Instance.GetUsername(username => 
        {
            RunOnMainThread(() => {
                if (welcomeText != null) 
                    welcomeText.text = username;
            });
        });

        // 2. Get Stats (Total Scans / Unique Items)
        FirebaseManager.Instance.GetUserStats((totalScans, uniqueItems) =>
        {
            RunOnMainThread(() => {
                if (totalScansText != null) totalScansText.text = $"Total Scans: {totalScans}";
                if (uniqueItemsText != null) uniqueItemsText.text = $"Unique Items: {uniqueItems}/6";
            });
        });

        // 3. Get Rewards Count
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
                    
                    // --- NEW: Play Sound Effect ---
                    if (uiAudioSource != null && claimSuccessSound != null)
                        uiAudioSource.PlayOneShot(claimSuccessSound);
                    // ------------------------------

                    ShowRewardNotification(item.itemName, item.rewardDescription);
                    LoadRewardProgress(); 
                }
            });
        });
    }

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
                    vObj.transform.localScale = Vector3.one; 
                    Vector3 pos = vObj.transform.localPosition;
                    vObj.transform.localPosition = new Vector3(pos.x, pos.y, 0);
                    VoucherUI vUI = vObj.GetComponent<VoucherUI>();
                    ItemModel originalItem = ItemDatabase.Instance.GetItem(v.itemId);
                    Sprite icon = originalItem != null ? originalItem.itemSprite : null;
                    if (vUI != null) vUI.Setup(v.description, v.code, v.date, icon);
                }
            });
        });
    }

    // --- CHANGED: NEW NOTIFICATION SYSTEM METHODS ---

    public void ShowCollectionNotification(ScanResult result) {
        if (result == null) return;
        ItemModel item = ItemDatabase.Instance.GetItem(result.itemId);
        
        string msg = result.GetCongratsMessage();
        Sprite icon = item != null ? item.itemSprite : null;
        
        // Spawn with Success Color
        SpawnNotification(msg, icon, colorSuccessBg);
    }

    public void ShowRewardNotification(string itemName, string rewardDescription) {
        string msg = $"Reward Unlocked!\n{rewardDescription}";
        SpawnNotification(msg, null, colorSuccessBg);
    }

    public void ShowErrorNotification(string message) {
        SpawnNotification(message, null, colorErrorBg);
    }

    // Helper Method to Instantiate the Card
    private void SpawnNotification(string message, Sprite icon, Color color)
    {
        if (notificationPrefab == null || notificationContainer == null)
        {
            Debug.LogWarning("Notification Prefab or Container is not assigned in UIManager!");
            return;
        }

        GameObject newNote = Instantiate(notificationPrefab, notificationContainer);
        newNote.transform.localScale = Vector3.one;

        NotificationCard cardScript = newNote.GetComponent<NotificationCard>();
        if (cardScript != null)
        {
            cardScript.Setup(message, icon, color);
        }
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