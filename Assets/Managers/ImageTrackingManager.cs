using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch; 
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ARImageTrackingManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARTrackedImageManager unityTrackedImageManager;
    [SerializeField] private XRReferenceImageLibrary myReferenceLibrary;
    [SerializeField] private GameObject packPrefab; 

    // Debug State
    private string debugStatus = "Initializing...";
    private Dictionary<string, GameObject> spawnedPacks = new Dictionary<string, GameObject>();
    private bool isProcessingScan = false;
    
    // NEW: Flag to stop Update() from overwriting our success message
    private bool isShowingSuccessMessage = false; 

    // --- UI STYLES ---
    private GUIStyle containerStyle;
    private GUIStyle textStyle;
    private Texture2D backgroundTexture;

    private void InitStyles()
    {
        if (containerStyle != null) return;

        // 1. Create a dark semi-transparent background texture
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f)); // Black with 70% opacity
        backgroundTexture.Apply();

        // 2. Setup Container Style
        containerStyle = new GUIStyle(GUI.skin.box);
        containerStyle.normal.background = backgroundTexture;

        // 3. Setup Text Style
        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 40; // Nice and big
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.wordWrap = true;
    }

    private void OnGUI()
    {
        InitStyles();

        // Layout Dimensions (Responsive to screen size)
        float width = Screen.width * 0.9f;  // 90% of screen width
        float height = 300f;                // Fixed height for text area
        float x = (Screen.width - width) / 2;
        float y = Screen.height - height - 100; // 100px from bottom

        // Draw Background Box
        GUI.Box(new Rect(x, y, width, height), GUIContent.none, containerStyle);

        // Draw Text inside
        GUI.Label(new Rect(x + 20, y + 20, width - 40, height - 40), debugStatus, textStyle);
    }

    private void OnEnable()
    {
        debugStatus = "Ready to Scan.\nPoint camera at the Old Chang Kee Logo.";
        EnhancedTouchSupport.Enable();

        if (unityTrackedImageManager != null)
        {
            if (myReferenceLibrary != null && unityTrackedImageManager.referenceLibrary == null)
            {
                unityTrackedImageManager.referenceLibrary = myReferenceLibrary;
            }
#pragma warning disable 618
            unityTrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
#pragma warning restore 618
        }
    }

    private void OnDisable()
    {
        if (unityTrackedImageManager != null)
        {
#pragma warning disable 618
            unityTrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
#pragma warning restore 618
        }
        EnhancedTouchSupport.Disable();
        ResetScanning();
    }

    // --- NEW INPUT SYSTEM ---
    private void Update()
    {
        // 1. UPDATE TEXT STATUS based on state
        // We only update the text if we are NOT currently showing the "Added to Collection" message
        if (!isShowingSuccessMessage && spawnedPacks.Count > 0)
        {
            foreach (var kvp in spawnedPacks)
            {
                GameObject packObj = kvp.Value;
                if (packObj != null && packObj.activeSelf)
                {
                    PackController pc = packObj.GetComponent<PackController>();
                    if (pc != null)
                    {
                        if (pc.IsOpened) debugStatus = "Tap the Food to Collect it!";
                        else debugStatus = "Pack Found!\nTap the Pack to Open!";
                    }
                }
            }
        }

        // 2. HANDLE INPUT
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (spawnedPacks.Count > 0)
                {
                    foreach (var kvp in spawnedPacks)
                    {
                        GameObject packObj = kvp.Value;
                        if (packObj != null && packObj.activeSelf)
                        {
                            PackController packController = packObj.GetComponent<PackController>();
                            if (packController == null) packController = packObj.GetComponentInParent<PackController>();

                            if (packController != null)
                            {
                                // CHECK: Was the pack already open before this tap?
                                bool wasAlreadyOpen = packController.IsOpened;

                                // Perform the standard tap action
                                packController.TapPack();

                                // If it was ALREADY open, this tap means we just collected the food
                                if (wasAlreadyOpen)
                                {
                                    StartCoroutine(ShowCollectionSuccess());
                                }

                                return; 
                            }
                        }
                    }
                }
            }
        }
    }

    // --- NEW COROUTINE FOR SUCCESS MESSAGE ---
    private IEnumerator ShowCollectionSuccess()
    {
        // lock the update loop so it doesn't overwrite our text
        isShowingSuccessMessage = true; 
        
        debugStatus = "Added to Collection!";
        
        // Wait for 2 seconds (or however long your fly-away animation is)
        yield return new WaitForSeconds(2.0f);
        
        // Unlock logic so it goes back to "Pack Found" or "Ready to Scan"
        isShowingSuccessMessage = false;
        
        // Optional: If you destroy the pack after collection, you might want to reset text here
        debugStatus = "Enjoying the app? Rate us!";
    }

    public void ResetScanning()
    {
        isProcessingScan = false;
        isShowingSuccessMessage = false; // Reset this flag too
        
        foreach (var pack in spawnedPacks.Values)
        {
            if (pack != null) Destroy(pack);
        }
        spawnedPacks.Clear();
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added) HandleTrackedImage(trackedImage);
        foreach (ARTrackedImage trackedImage in eventArgs.updated) HandleTrackedImage(trackedImage);
    }

    private void HandleTrackedImage(ARTrackedImage trackedImage)
    {
        if (trackedImage.referenceImage == null) return;
        string name = trackedImage.referenceImage.name;

        // Logic 1: Handle existing pack
        if (spawnedPacks.ContainsKey(name))
        {
             GameObject pack = spawnedPacks[name];
             if (pack != null)
             {
                 pack.transform.position = trackedImage.transform.position;
                 pack.transform.rotation = trackedImage.transform.rotation;
                 pack.SetActive(true); 
             }
             return; 
        }

        if (isProcessingScan) return;

        // Logic 2: Spawn new pack
        if (name == "OCK_Logo")
        {
             if (trackedImage.trackingState == TrackingState.Tracking)
             {
                 StartCoroutine(SpawnSequence(trackedImage));
             }
             else
             {
                 // Only update status if we aren't showing the success message
                 if (!isShowingSuccessMessage)
                 {
                     if (trackedImage.trackingState == TrackingState.Limited)
                        debugStatus = "Aligning Camera...\nHold Steady.";
                     else
                        debugStatus = "Scan the Logo to Begin";
                 }
             }
        }
    }

    private IEnumerator SpawnSequence(ARTrackedImage trackedImage)
    {
        isProcessingScan = true;
        string name = trackedImage.referenceImage.name;

        if (packPrefab == null)
        {
            debugStatus = "ERROR: Pack Prefab is NULL!";
            isProcessingScan = false;
            yield break;
        }

        debugStatus = "Scanning...";
        GameObject pack = Instantiate(packPrefab, trackedImage.transform.position, trackedImage.transform.rotation);
        
        pack.transform.parent = trackedImage.transform;
        pack.transform.localPosition = new Vector3(0, 0.05f, 0); 
        pack.transform.localScale = Vector3.one * 0.05f; 
        
        spawnedPacks[name] = pack;

        if (FirebaseManager.Instance == null || string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserId))
        {
            debugStatus = "Error: Login Required.";
            isProcessingScan = false;
            yield break;
        }

        bool firebaseDone = false;
        FirebaseManager.Instance.RecordScan(result =>
        {
            // Text is handled by Update loop now
            PackController pc = pack.GetComponent<PackController>();
            if (pc != null) pc.Initialize(result);
            firebaseDone = true;
        });
        
        float timeout = 2f;
        while (!firebaseDone && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        
        isProcessingScan = false;
    }
}