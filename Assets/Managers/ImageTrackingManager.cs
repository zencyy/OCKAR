using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
// New Input System Namespaces
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
    private float lastScanTime = 0f; 
    private bool isProcessingScan = false;

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 40;
        style.normal.textColor = Color.yellow; 
        style.wordWrap = true;
        
        // Draw debug box
        GUI.Box(new Rect(10, 10, Screen.width - 20, 450), "");
        GUI.Label(new Rect(20, 20, Screen.width - 40, 430), debugStatus, style);
    }

    private void OnEnable()
    {
        debugStatus = "Screen Loaded. Warming up...";
        
        // 1. Enable Enhanced Touch
        EnhancedTouchSupport.Enable();

        // 2. Setup Library
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
        
        // 3. Prevent "Ghost Scan" - Set timer to NOW so we have to wait 2 seconds
        lastScanTime = Time.time; 
    }

    private void OnDisable()
    {
        if (unityTrackedImageManager != null)
        {
#pragma warning disable 618
            unityTrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
#pragma warning restore 618
        }
        
        // Disable Enhanced Touch
        EnhancedTouchSupport.Disable();

        ResetScanning();
    }

    // --- NEW INPUT SYSTEM (Global Tap) ---
    private void Update()
    {
        // Only run if we touched the screen
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];
            
            // Trigger on the start of the touch (Tap)
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Check if we have any spawned packs
                if (spawnedPacks.Count > 0)
                {
                    // Find the pack for "OCK_Logo" (or just the first available one)
                    foreach (var kvp in spawnedPacks)
                    {
                        GameObject packObj = kvp.Value;
                        if (packObj != null && packObj.activeSelf)
                        {
                            // Get the controller
                            PackController packController = packObj.GetComponent<PackController>();
                            if (packController == null) 
                                packController = packObj.GetComponentInParent<PackController>();

                            if (packController != null)
                            {
                                debugStatus = "GLOBAL TAP DETECTED! OPENING PACK...";
                                packController.TapPack();
                                return; // Stop after opening one
                            }
                        }
                    }
                }
            }
        }
    }
    // ----------------------------------------

    public void ResetScanning()
    {
        isProcessingScan = false;
        // Important: Set this to Time.time so we don't scan instantly upon returning
        lastScanTime = Time.time; 

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

        // Logic 1: Handle existing pack (Move it)
        if (spawnedPacks.ContainsKey(name))
        {
             GameObject pack = spawnedPacks[name];
             if (pack != null)
             {
                 pack.transform.position = trackedImage.transform.position;
                 pack.transform.rotation = trackedImage.transform.rotation;
                 
                 // Hide if tracking is lost completely
                 if (trackedImage.trackingState == TrackingState.None)
                    pack.SetActive(false);
                 else
                    pack.SetActive(true);
             }
             return; 
        }

        // Logic 2: Spawn new pack (With Strict Rules)
        if (isProcessingScan) return;

        // COOLDOWN CHECK (Must wait 2 seconds after entering screen)
        if (Time.time - lastScanTime < 2.0f)
        {
            return;
        }

        if (name == "OCK_Logo")
        {
             // STRICT TRACKING: Only spawn if tracking is PERFECT
             if (trackedImage.trackingState == TrackingState.Tracking)
             {
                 StartCoroutine(SpawnSequence(trackedImage));
             }
             else
             {
                 debugStatus = $"Align Camera... ({trackedImage.trackingState})";
             }
        }
    }

    private IEnumerator SpawnSequence(ARTrackedImage trackedImage)
    {
        isProcessingScan = true;
        lastScanTime = Time.time;
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
            // Update prompt to reflect new interaction
            debugStatus = $"PACK FOUND!\nTap ANYWHERE to Open."; 
            PackController pc = pack.GetComponent<PackController>();
            if (pc != null) pc.Initialize(result);
            firebaseDone = true;
        });
        
        yield return new WaitForSeconds(2f);
        if(!firebaseDone) debugStatus += "\n(Network Lag...)";
        
        isProcessingScan = false;
    }
}