using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Ensures the AR Tracked Image Manager always has its Reference Library assigned.
/// Attach this to the same GameObject as AR Tracked Image Manager.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARLibraryKeeper : MonoBehaviour
{
    [Header("Assign Your Reference Library Here")]
    [Tooltip("Drag your XR Reference Image Library asset here")]
    [SerializeField] private XRReferenceImageLibrary referenceLibrary;
    
    private ARTrackedImageManager trackedImageManager;
    
    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        
        if (trackedImageManager == null)
        {
            Debug.LogError("ARTrackedImageManager component not found!");
            return;
        }
        
        // Force assign the library
        EnsureLibraryIsAssigned();
    }
    
    private void OnEnable()
    {
        // Double-check on enable
        EnsureLibraryIsAssigned();
    }
    
    private void EnsureLibraryIsAssigned()
    {
        if (trackedImageManager == null) return;
        
        if (referenceLibrary == null)
        {
            Debug.LogError("Reference Library is not assigned in ARLibraryKeeper! Please assign it in the Inspector.");
            return;
        }
        
        if (trackedImageManager.referenceLibrary == null)
        {
            Debug.LogWarning("AR Tracked Image Manager lost its Reference Library. Re-assigning...");
            trackedImageManager.referenceLibrary = referenceLibrary;
        }
        
        if (trackedImageManager.referenceLibrary != null)
        {
        }
    }
    
#if UNITY_EDITOR
    // Validate in editor
    private void OnValidate()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }
        
        if (referenceLibrary != null && trackedImageManager != null)
        {
            trackedImageManager.referenceLibrary = referenceLibrary;
        }
    }
#endif
}